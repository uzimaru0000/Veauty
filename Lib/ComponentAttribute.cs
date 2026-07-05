using System;

namespace Veauty
{
    /// <summary>
    /// Marks a static method as a Veauty function component. At compile time an
    /// ILPostProcessor (shipped as <c>CodeGen/Veauty.CodeGen.dll</c>) moves the method body
    /// into a hidden implementation method and rewrites the original method to return
    /// <c>FunctionComponents.Create(...)</c>, so call sites read like plain method calls
    /// while hooks get their own component instance and lifecycle.
    /// </summary>
    /// <remarks>
    /// The annotated method must be <c>static</c> (the post-processor emits a compile error
    /// otherwise) and should return <see cref="IVTree"/>. Generic methods are not supported.
    /// The attribute itself is removed from the method during IL post-processing, so it never
    /// exists at runtime.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ComponentAttribute : Attribute
    {
    }
}
