# Function components

[日本語](ja/function-components.md)

The `[Component]` attribute, the `FunctionComponents` factory, `FunctionComponentNode`, resolution via `HookRuntime.Resolve`, and how the compile-time code generation actually works.

## Two ways to create a component

### 1. `[Component]` (recommended)

```csharp
using Veauty;
using Veauty.VTree;

[Component]
static IVTree Timer(float interval)
{
    var elapsed = Hooks.UseState(0f);
    Hooks.UseEffect(() => { /* ... */ }, new object[] { interval });
    return new Node<GameObject>("Timer", new IAttribute<GameObject>[] { });
}

// call site — an ordinary call, returns a FunctionComponentNode:
var tree = Timer(1.0f);
```

Constraints (enforced by the IL post-processor):

- The method **must be `static`** — otherwise compilation fails with `[Component] method '...' must be static.`
- It should be declared to return **`IVTree`** (the rewritten body returns a `FunctionComponentNode`; the generated closure invoker is typed `IVTree`).
- **Generic methods are not supported** (the rewriter does not copy generic parameters).
- It works in any assembly that references the `Veauty` assembly — including your game assemblies; the processor skips assemblies that don't reference Veauty.

### 2. `FunctionComponents` factory (manual)

```csharp
// parameterless
FunctionComponents.Create(() => Render());
FunctionComponents.Create("myKey", () => Render());          // keyed
FunctionComponents.Keyed("myKey", () => Render());           // alias

// with typed props
FunctionComponents.Create((Props p) => Render(p), props);
FunctionComponents.Create("myKey", (Props p) => Render(p), props);
FunctionComponents.Keyed("myKey", (Props p) => Render(p), props);
```

All of these construct a `FunctionComponentNode`. Direct construction is also public (`new FunctionComponentNode(...)`) but rarely needed.

## FunctionComponentNode

A `FunctionComponentNode` is a *placeholder*: `GetNodeType()` is `VTreeType.FunctionComponent` and `GetDescendantsCount()` is always `0`. It carries:

- `key` — optional identity key (see below),
- `component` — the delegate (used for the identity token),
- `props` — the captured props (used for equality),
- an internal `render` closure that actually invokes the function.

It **must be resolved before diffing**. Passing a tree containing one to `Diff<T>.Calc` throws:

```
InvalidOperationException: Function components must be resolved before diffing.
```

## HookRuntime.Resolve

```csharp
var runtime = new HookRuntime(requestRender);
IVTree resolved = runtime.Resolve<GameObject>(tree);
```

`Resolve<T>` walks the tree and replaces every `FunctionComponentNode` with the output of its render function (recursively — a component may return another component). Concrete nodes are rebuilt with their resolved children, preserving typed components and host lifecycles. Hooks work only inside this call. Both the old and the new tree handed to `Diff.Calc` must come from `Resolve`.

### Identity

A component instance (and therefore its hook state) is keyed by:

```
identity = renderPath + "/c" + componentToken
componentToken = delegateType | module MVID | method metadata token | target type
```

- `renderPath` encodes the position: child index (`i0`), key of a keyed child (`k...`), nested component boundary (`c...`), rendered-root (`r`).
- A `key` on the node **replaces the positional segment**, so `FunctionComponents.Keyed("item-42", ...)` keeps its state when siblings reorder.
- The token uses the method's metadata token and declaring module, so it is stable across renders even though `[Component]` methods with parameters allocate a fresh closure object each call (the *target type* of the delegate is stable, the instance is not part of the token).

Consequence: two different component functions at the same tree position get different instances; the same function moved to a different unkeyed position gets a *new* instance (old one unmounts).

## How the code generation works

### What ships and what runs

The repository contains **two** codegen implementations under `SourceGenerator~/` (a folder Unity ignores because of the `~` suffix):

| File | Kind | Status |
|------|------|--------|
| `ComponentILPostProcessor.cs` | Unity `ILPostProcessor` (Mono.Cecil) | **Active** — compiled into `CodeGen/Veauty.CodeGen.dll` |
| `ComponentGenerator.cs` | Roslyn `IIncrementalGenerator` | **Inactive** — excluded from the build (`<Compile Remove="ComponentGenerator.cs" />` in the csproj) |

The shipped `CodeGen/Veauty.CodeGen.dll` is an Editor-enabled plugin. Unity's scripting pipeline discovers the `ILPostProcessor` subclass in it and runs it after compiling each assembly. (The abandoned Roslyn generator took a different approach — `_`-prefixed methods in `partial` classes with a generated wrapper — which is *not* how the current system works.)

### What the IL rewrite does

For each `static` method carrying `[Component]` in an assembly that references `Veauty`:

1. The original method body is moved to a hidden method `<Name>__ComponentImpl` (same parameters).
2. The original method gets a new body:
   - **No parameters:** `return FunctionComponents.Create(new FunctionComponent(<Name>__ComponentImpl));`
   - **With parameters:** a private nested closure class `<Name>__ComponentClosure` is generated with one public field per parameter and an `__invoke() : IVTree` method that forwards the fields to the impl. The wrapper instantiates the closure, copies the arguments in, and returns `FunctionComponents.Create(new FunctionComponent(closure.__invoke))`.
3. The `[Component]` attribute is removed from the method — it does not exist in the compiled assembly.

So *calling* the method never runs your body; it just captures the arguments. The body runs later, inside `HookRuntime.Resolve`, once per render.

### Note: `[Component]` methods have no `key` parameter

The rewrite always calls the key-less `FunctionComponents.Create`. If you render a *list* of the same component and need keyed identity, wrap manually:

```csharp
FunctionComponents.Keyed(item.Id, () => TodoItem(item));
```

### Rebuilding the post-processor

```bash
cd SourceGenerator~
dotnet build -c Release
cp bin/Release/netstandard2.1/Veauty.CodeGen.dll ../CodeGen/
```

The csproj references `Unity.CompilationPipeline.Common` and Mono.Cecil from a local Unity installation; adjust the `HintPath`s to your Unity version if they differ.

See also: [Hooks](hooks.md), [Architecture](architecture.md), [API reference](api-reference.md).
