namespace Veauty
{
    /// <summary>
    /// Discriminates the kind of a virtual tree node so the diff algorithm can
    /// dispatch without reflection.
    /// </summary>
    public enum VTreeType
    {
        /// <summary>A plain node whose children are matched by position (<see cref="Veauty.VTree.BaseNode{T}"/>).</summary>
        Node,
        /// <summary>A keyed node whose children are matched by string key (<see cref="Veauty.VTree.BaseKeyedNode{T}"/>).</summary>
        KeyedNode,
        /// <summary>A function component placeholder (<see cref="Veauty.VTree.FunctionComponentNode"/>) that must be resolved before diffing.</summary>
        FunctionComponent
    }

    /// <summary>
    /// A node of the virtual tree. All renderable structures (plain nodes, keyed nodes,
    /// function components) implement this interface.
    /// </summary>
    public interface IVTree
    {
        /// <summary>Gets the kind of this node.</summary>
        /// <returns>The <see cref="VTreeType"/> discriminator of this node.</returns>
        VTreeType GetNodeType();

        /// <summary>
        /// Gets the total number of descendant nodes below this node.
        /// </summary>
        /// <returns>The number of all transitive children (children, grandchildren, ...).</returns>
        /// <remarks>
        /// This count defines the pre-order index contract shared by <see cref="Diff{T}"/> and the
        /// patch applicator: a child at position <c>i</c> occupies index
        /// <c>parentIndex + 1 + sum of preceding siblings' (1 + GetDescendantsCount())</c>.
        /// <see cref="Veauty.VTree.FunctionComponentNode"/> always returns 0 because it is replaced
        /// by its rendered output before diffing.
        /// </remarks>
        int GetDescendantsCount();
    }

    /// <summary>
    /// A virtual tree node that has children.
    /// </summary>
    public interface IParent
    {
        /// <summary>Gets the child nodes of this node.</summary>
        /// <returns>The children in render order. Keyed nodes return their children without keys.</returns>
        IVTree[] GetKids();
    }

    /// <summary>
    /// A node that wraps another node. Both <see cref="Diff{T}"/> and
    /// <see cref="HookRuntime.Resolve{T}(IVTree)"/> transparently unwrap wrappers before processing.
    /// </summary>
    public interface IVTreeWrapper : IVTree
    {
        /// <summary>Returns the wrapped node.</summary>
        /// <returns>The underlying <see cref="IVTree"/> this wrapper delegates to.</returns>
        IVTree Unwrap();
    }
}
