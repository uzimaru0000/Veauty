using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace Veauty.CodeGen
{
    public class ComponentILPostProcessor : ILPostProcessor
    {
        const string ComponentAttributeName = "Veauty.ComponentAttribute";

        public override ILPostProcessor GetInstance() => this;

        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            return compiledAssembly.References
                .Any(r => Path.GetFileNameWithoutExtension(r) == "Veauty");
        }

        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            if (!WillProcess(compiledAssembly))
                return null;

            var resolver = new PostProcessorAssemblyResolver(compiledAssembly);
            var readerParams = new ReaderParameters
            {
                AssemblyResolver = resolver,
                InMemory = true,
                ReadingMode = ReadingMode.Immediate,
                ReadSymbols = true,
                SymbolReaderProvider = new PortablePdbReaderProvider()
            };

            AssemblyDefinition assembly;
            using (var peStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PeData))
            using (var pdbStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData))
            {
                readerParams.SymbolStream = pdbStream;
                try
                {
                    assembly = AssemblyDefinition.ReadAssembly(peStream, readerParams);
                }
                catch
                {
                    readerParams.ReadSymbols = false;
                    readerParams.SymbolReaderProvider = null;
                    readerParams.SymbolStream = null;
                    peStream.Position = 0;
                    assembly = AssemblyDefinition.ReadAssembly(peStream, readerParams);
                }
            }

            var module = assembly.MainModule;
            var diagnostics = new List<DiagnosticMessage>();
            bool modified = false;

            var veautyRef = module.AssemblyReferences
                .FirstOrDefault(r => r.Name == "Veauty");
            if (veautyRef == null)
                return null;

            foreach (var type in GetAllTypes(module).ToList())
            {
                foreach (var method in type.Methods.ToList())
                {
                    var attr = method.CustomAttributes.FirstOrDefault(
                        a => a.AttributeType.FullName == ComponentAttributeName);
                    if (attr == null)
                        continue;

                    if (!method.IsStatic)
                    {
                        diagnostics.Add(new DiagnosticMessage
                        {
                            DiagnosticType = DiagnosticType.Error,
                            MessageData = $"[Component] method '{method.FullName}' must be static."
                        });
                        continue;
                    }

                    TransformMethod(type, method, module, veautyRef);
                    method.CustomAttributes.Remove(attr);
                    modified = true;
                }
            }

            if (!modified)
                return null;

            var pe = new MemoryStream();
            var pdb = new MemoryStream();
            var writerParams = new WriterParameters
            {
                WriteSymbols = true,
                SymbolWriterProvider = new PortablePdbWriterProvider()
            };
            assembly.Write(pe, writerParams);

            return new ILPostProcessResult(
                new InMemoryAssembly(pe.ToArray(), pdb.ToArray()),
                diagnostics);
        }

        static void TransformMethod(
            TypeDefinition declaringType,
            MethodDefinition method,
            ModuleDefinition module,
            AssemblyNameReference veautyRef)
        {
            var implMethod = new MethodDefinition(
                $"<{method.Name}>__ComponentImpl",
                MethodAttributes.Private | MethodAttributes.Static | MethodAttributes.HideBySig,
                method.ReturnType);

            foreach (var param in method.Parameters)
                implMethod.Parameters.Add(new ParameterDefinition(
                    param.Name, param.Attributes, param.ParameterType));

            declaringType.Methods.Add(implMethod);

            // Swap bodies
            var originalBody = method.Body;
            method.Body = new MethodBody(method);
            implMethod.Body = originalBody;

            RemapParameters(implMethod, method.Parameters, implMethod.Parameters);

            var il = method.Body.GetILProcessor();

            var fcType = new TypeReference(
                "Veauty.VTree", "FunctionComponent", module, veautyRef);
            var fcCtor = new MethodReference(".ctor", module.TypeSystem.Void, fcType)
            { HasThis = true };
            fcCtor.Parameters.Add(new ParameterDefinition(module.TypeSystem.Object));
            fcCtor.Parameters.Add(new ParameterDefinition(module.TypeSystem.IntPtr));

            var fcnType = new TypeReference(
                "Veauty.VTree", "FunctionComponentNode", module, veautyRef);
            var fcsType = new TypeReference(
                "Veauty.VTree", "FunctionComponents", module, veautyRef);
            var createMethod = new MethodReference("Create", fcnType, fcsType)
            { HasThis = false };
            createMethod.Parameters.Add(new ParameterDefinition(fcType));

            if (method.Parameters.Count == 0)
            {
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ldftn, implMethod);
                il.Emit(OpCodes.Newobj, fcCtor);
                il.Emit(OpCodes.Call, createMethod);
                il.Emit(OpCodes.Ret);
            }
            else
            {
                var closureType = CreateClosureType(
                    declaringType, method, implMethod, module, veautyRef);
                declaringType.NestedTypes.Add(closureType);

                var closureCtor = closureType.Methods.First(m => m.IsConstructor);
                var invokeMethod = closureType.Methods.First(m => m.Name == "__invoke");

                il.Emit(OpCodes.Newobj, closureCtor);

                for (int i = 0; i < method.Parameters.Count; i++)
                {
                    il.Emit(OpCodes.Dup);
                    il.Emit(OpCodes.Ldarg, method.Parameters[i]);
                    il.Emit(OpCodes.Stfld, closureType.Fields[i]);
                }

                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldftn, invokeMethod);
                il.Emit(OpCodes.Newobj, fcCtor);
                il.Emit(OpCodes.Call, createMethod);
                il.Emit(OpCodes.Ret);
            }
        }

        static TypeDefinition CreateClosureType(
            TypeDefinition declaringType,
            MethodDefinition originalMethod,
            MethodDefinition implMethod,
            ModuleDefinition module,
            AssemblyNameReference veautyRef)
        {
            var closureType = new TypeDefinition(
                "",
                $"<{originalMethod.Name}>__ComponentClosure",
                TypeAttributes.NestedPrivate | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                module.TypeSystem.Object);

            foreach (var param in originalMethod.Parameters)
                closureType.Fields.Add(new FieldDefinition(
                    param.Name, FieldAttributes.Public, param.ParameterType));

            var objectCtor = new MethodReference(
                ".ctor", module.TypeSystem.Void, module.TypeSystem.Object)
            { HasThis = true };

            var ctor = new MethodDefinition(
                ".ctor",
                MethodAttributes.Public | MethodAttributes.HideBySig |
                MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                module.TypeSystem.Void);
            var ctorIl = ctor.Body.GetILProcessor();
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Call, objectCtor);
            ctorIl.Emit(OpCodes.Ret);
            closureType.Methods.Add(ctor);

            var ivtreeType = new TypeReference(
                "Veauty", "IVTree", module, veautyRef);

            var invoke = new MethodDefinition(
                "__invoke",
                MethodAttributes.Public | MethodAttributes.HideBySig,
                ivtreeType);
            var invokeIl = invoke.Body.GetILProcessor();
            for (int i = 0; i < closureType.Fields.Count; i++)
            {
                invokeIl.Emit(OpCodes.Ldarg_0);
                invokeIl.Emit(OpCodes.Ldfld, closureType.Fields[i]);
            }
            invokeIl.Emit(OpCodes.Call, implMethod);
            invokeIl.Emit(OpCodes.Ret);
            closureType.Methods.Add(invoke);

            return closureType;
        }

        static void RemapParameters(
            MethodDefinition targetMethod,
            Mono.Collections.Generic.Collection<ParameterDefinition> oldParams,
            Mono.Collections.Generic.Collection<ParameterDefinition> newParams)
        {
            if (targetMethod.Body == null) return;

            foreach (var instr in targetMethod.Body.Instructions)
            {
                if (instr.Operand is ParameterDefinition param)
                {
                    int idx = oldParams.IndexOf(param);
                    if (idx >= 0 && idx < newParams.Count)
                        instr.Operand = newParams[idx];
                }
            }
        }

        static IEnumerable<TypeDefinition> GetAllTypes(ModuleDefinition module)
        {
            foreach (var type in module.Types)
            {
                yield return type;
                foreach (var nested in GetNestedTypes(type))
                    yield return nested;
            }
        }

        static IEnumerable<TypeDefinition> GetNestedTypes(TypeDefinition type)
        {
            foreach (var nested in type.NestedTypes)
            {
                yield return nested;
                foreach (var sub in GetNestedTypes(nested))
                    yield return sub;
            }
        }

        class PostProcessorAssemblyResolver : IAssemblyResolver
        {
            readonly Dictionary<string, string> refPaths;

            public PostProcessorAssemblyResolver(ICompiledAssembly compiledAssembly)
            {
                refPaths = new Dictionary<string, string>();
                foreach (var r in compiledAssembly.References)
                {
                    var name = Path.GetFileNameWithoutExtension(r);
                    refPaths[name] = r;
                }
            }

            public AssemblyDefinition Resolve(AssemblyNameReference name)
                => Resolve(name, new ReaderParameters());

            public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
            {
                if (!refPaths.TryGetValue(name.Name, out var path))
                    return null;

                parameters.AssemblyResolver = this;
                parameters.InMemory = true;
                parameters.ReadingMode = ReadingMode.Deferred;
                parameters.ReadSymbols = false;
                parameters.SymbolReaderProvider = null;
                return AssemblyDefinition.ReadAssembly(path, parameters);
            }

            public void Dispose() { }
        }
    }
}
