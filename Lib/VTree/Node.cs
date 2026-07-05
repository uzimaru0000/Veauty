using System;
using System.Collections.Generic;
using System.Linq;

namespace Veauty.VTree
{
    /// <summary>
    /// A node that carries a host component type (the <c>U</c> in <see cref="Node{T,U}"/>).
    /// The diff emits an <see cref="Veauty.Patch.Attach{T}"/> patch when this type changes
    /// between renders.
    /// </summary>
    public interface ITypedNode
    {
        /// <summary>Gets the host component type attached to this node (e.g. a <c>UnityEngine.Component</c> subclass).</summary>
        /// <returns>The component <see cref="System.Type"/>.</returns>
        System.Type GetComponentType();
    }

    /// <summary>
    /// Common base of all concrete tree nodes: holds the tag and the attribute set.
    /// </summary>
    /// <typeparam name="T">The host object type (e.g. <c>GameObject</c>).</typeparam>
    public abstract class NodeBase<T> : IVTree, IParent
    {
        /// <summary>The node tag. Two nodes with different tags are never diffed in place; the new node redraws the old one.</summary>
        public readonly string tag;
        /// <summary>The attributes of this node, indexed by key (duplicate keys: last one wins).</summary>
        public readonly Attributes<T> attrs;

        /// <summary>Initializes the base node with a tag and attributes.</summary>
        /// <param name="tag">The node tag.</param>
        /// <param name="attrs">The attributes; duplicates by key are collapsed, last one wins.</param>
        protected NodeBase(string tag, IEnumerable<IAttribute<T>> attrs)
        {
            this.tag = tag;
            this.attrs = new Attributes<T>(attrs);
        }

        /// <summary>Gets the kind of this node. Defaults to <see cref="VTreeType.Node"/>.</summary>
        /// <returns>The <see cref="VTreeType"/> discriminator.</returns>
        public virtual VTreeType GetNodeType() => VTreeType.Node;

        /// <summary>Gets the total number of descendant nodes (see <see cref="IVTree.GetDescendantsCount"/> for the index contract).</summary>
        /// <returns>The number of all transitive children.</returns>
        public abstract int GetDescendantsCount();

        /// <summary>Gets the child nodes in render order.</summary>
        /// <returns>The children of this node.</returns>
        public abstract IVTree[] GetKids();

        /// <summary>Compares two attribute sets for structural equality (same keys, equal values).</summary>
        /// <param name="x">The first attribute set.</param>
        /// <param name="y">The second attribute set.</param>
        /// <returns><c>true</c> when both sets contain the same keys with equal attributes.</returns>
        protected static bool AttributesEqual(Attributes<T> x, Attributes<T> y)
        {
            if (x.attrs.Count != y.attrs.Count)
            {
                return false;
            }

            foreach (var attr in x.attrs)
            {
                if (!y.attrs.TryGetValue(attr.Key, out var otherAttr) || !object.Equals(attr.Value, otherAttr))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Combines a running hash with a value's hash code.</summary>
        /// <param name="hash">The running hash.</param>
        /// <param name="value">The value to combine; <c>null</c> contributes 0.</param>
        /// <returns>The combined hash.</returns>
        protected static int CombineHash(int hash, object value)
        {
            unchecked
            {
                return (hash * 397) ^ (value != null ? value.GetHashCode() : 0);
            }
        }

        /// <summary>Computes an order-independent hash of an attribute set (keys are sorted before combining).</summary>
        /// <param name="attrs">The attribute set.</param>
        /// <returns>The combined hash.</returns>
        protected static int GetAttributesHashCode(Attributes<T> attrs)
        {
            unchecked
            {
                var hash = 17;
                foreach (var attr in attrs.attrs.OrderBy(x => x.Key))
                {
                    hash = CombineHash(hash, attr.Key);
                    hash = CombineHash(hash, attr.Value);
                }

                return hash;
            }
        }

        /// <summary>Computes an order-dependent hash of a child list.</summary>
        /// <param name="kids">The children to hash.</param>
        /// <returns>The combined hash.</returns>
        protected static int GetKidsHashCode(IEnumerable<IVTree> kids)
        {
            unchecked
            {
                var hash = 17;
                foreach (var kid in kids)
                {
                    hash = CombineHash(hash, kid);
                }

                return hash;
            }
        }
    }

    // Node

    /// <summary>
    /// A plain tree node whose children are diffed by position. Instantiate via
    /// <see cref="Node{T}"/> or <see cref="Node{T,U}"/>.
    /// </summary>
    /// <typeparam name="T">The host object type (e.g. <c>GameObject</c>).</typeparam>
    public class BaseNode<T> : NodeBase<T>
    {
        /// <summary>The children in render order.</summary>
        public readonly IVTree[] kids;
        private readonly int descendantsCount;

        /// <summary>Initializes the node and precomputes the descendant count.</summary>
        /// <param name="tag">The node tag.</param>
        /// <param name="attrs">The attributes.</param>
        /// <param name="kids">The children in render order.</param>
        protected BaseNode(string tag, IEnumerable<IAttribute<T>> attrs, params IVTree[] kids): base(tag, attrs)
        {
            this.kids = kids;
            this.descendantsCount = 0;

            foreach (var kid in kids)
            {
                this.descendantsCount += kid.GetDescendantsCount();
            }
            this.descendantsCount += kids.Length;
        }

        /// <summary>Initializes the node from an enumerable of children.</summary>
        /// <param name="tag">The node tag.</param>
        /// <param name="attrs">The attributes.</param>
        /// <param name="kids">The children in render order.</param>
        protected BaseNode(string tag, IEnumerable<IAttribute<T>> attrs, IEnumerable<IVTree> kids): this(tag, attrs, kids.ToArray()) {}

        /// <summary>Gets the precomputed number of all transitive children.</summary>
        /// <returns>The descendant count (children plus their descendants).</returns>
        public override int GetDescendantsCount() => this.descendantsCount;

        /// <summary>Gets the child nodes in render order.</summary>
        /// <returns>The children of this node.</returns>
        public override IVTree[] GetKids() => this.kids;

        /// <summary>Determines structural equality: same runtime type, tag, attributes and children.</summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns><c>true</c> when the nodes are structurally equal.</returns>
        public override bool Equals(object obj) => this.Equals(obj as BaseNode<T>);

        bool Equals(BaseNode<T> obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (System.Object.ReferenceEquals(this, obj))
            {
                return true;
            }

            if (this.GetType() != obj.GetType())
            {
                return false;
            }


            return this.tag == obj.tag &&
                   AttributesEqual(this.attrs, obj.attrs) &&
                   this.descendantsCount == obj.descendantsCount &&
                   this.kids.SequenceEqual(obj.kids);
        }

        /// <summary>Returns a structural hash combining tag, attributes, descendant count and children.</summary>
        /// <returns>The combined hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = CombineHash(hash, tag);
                hash = CombineHash(hash, GetAttributesHashCode(attrs));
                hash = CombineHash(hash, descendantsCount);
                hash = CombineHash(hash, GetKidsHashCode(kids));
                return hash;
            }
        }
    }

    /// <summary>
    /// A plain node that also attaches a host component of type <typeparamref name="U"/>.
    /// Changing <typeparamref name="U"/> between renders produces an
    /// <see cref="Veauty.Patch.Attach{T}"/> patch.
    /// </summary>
    /// <typeparam name="T">The host object type (e.g. <c>GameObject</c>).</typeparam>
    /// <typeparam name="U">The host component type to attach (e.g. a <c>UnityEngine.Component</c> subclass).</typeparam>
    public class Node<T, U> : BaseNode<T>, ITypedNode
    {
        private System.Type componentType;

        /// <summary>Initializes a typed node.</summary>
        /// <param name="tag">The node tag.</param>
        /// <param name="attrs">The attributes.</param>
        /// <param name="kids">The children in render order.</param>
        public Node(string tag, IEnumerable<IAttribute<T>> attrs, params IVTree[] kids) : base(tag, attrs, kids)
        {
            componentType = typeof(U);
        }

        /// <summary>Initializes a typed node from an enumerable of children.</summary>
        /// <param name="tag">The node tag.</param>
        /// <param name="attrs">The attributes.</param>
        /// <param name="kids">The children in render order.</param>
        public Node(string tag, IEnumerable<IAttribute<T>> attrs, IEnumerable<IVTree> kids) : this(tag, attrs, kids.ToArray()) {}

        /// <summary>Gets the attached component type (<c>typeof(U)</c>).</summary>
        /// <returns>The component <see cref="Type"/>.</returns>
        public Type GetComponentType() => componentType;

        /// <summary>Determines structural equality including the component type.</summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns><c>true</c> when the nodes are structurally equal and share the component type.</returns>
        public override bool Equals(object obj) => this.Equals(obj as Node<T, U>);

        bool Equals(Node<T, U> obj)
        {
            return base.Equals(obj as BaseNode<T>) && this.componentType == obj.componentType;
        }

        /// <summary>Returns a structural hash including the component type.</summary>
        /// <returns>The combined hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return CombineHash(base.GetHashCode(), componentType);
            }
        }
    }

    /// <summary>
    /// A plain, untyped tree node whose children are diffed by position.
    /// </summary>
    /// <typeparam name="T">The host object type (e.g. <c>GameObject</c>).</typeparam>
    public class Node<T> : BaseNode<T>
    {
        /// <summary>Initializes a node.</summary>
        /// <param name="tag">The node tag.</param>
        /// <param name="attrs">The attributes.</param>
        /// <param name="kids">The children in render order.</param>
        public Node(string tag, IEnumerable<IAttribute<T>> attrs, params IVTree[] kids) : base(tag, attrs, kids) { }

        /// <summary>Initializes a node from an enumerable of children.</summary>
        /// <param name="tag">The node tag.</param>
        /// <param name="attrs">The attributes.</param>
        /// <param name="kids">The children in render order.</param>
        public Node(string tag, IEnumerable<IAttribute<T>> attrs, IEnumerable<IVTree> kids) : base(tag, attrs, kids) { }
    }

    // KeyedNode

    /// <summary>
    /// A tree node whose children carry string keys and are diffed by key, producing
    /// <see cref="Veauty.Patch.Reorder{T}"/> patches for moves instead of rewrites.
    /// Instantiate via <see cref="KeyedNode{T}"/> or <see cref="KeyedNode{T,U}"/>.
    /// </summary>
    /// <typeparam name="T">The host object type (e.g. <c>GameObject</c>).</typeparam>
    /// <remarks>
    /// Keys must be stable across renders and unique within one parent; duplicate keys are
    /// disambiguated internally with a sentinel suffix, which defeats move detection.
    /// </remarks>
    public class BaseKeyedNode<T> : NodeBase<T>
    {
        /// <summary>The (key, child) pairs in render order.</summary>
        public readonly (string, IVTree)[] kids;
        private readonly int descendantsCount;
        private readonly IVTree[] dekeyedKids;

        /// <summary>Initializes the keyed node and precomputes the descendant count.</summary>
        /// <param name="tag">The node tag.</param>
        /// <param name="attrs">The attributes.</param>
        /// <param name="kids">The (key, child) pairs in render order.</param>
        protected BaseKeyedNode(string tag, IEnumerable<IAttribute<T>> attrs, params (string, IVTree)[] kids) : base(tag, attrs)
        {
            this.kids = kids;
            this.descendantsCount = 0;
            this.dekeyedKids = new IVTree[kids.Length];

            var i = 0;
            foreach (var (_, kid) in kids)
            {
                this.descendantsCount += kid.GetDescendantsCount();
                this.dekeyedKids[i++] = kid;
            }
            this.descendantsCount += kids.Length;
        }

        /// <summary>Initializes the keyed node from an enumerable of (key, child) pairs.</summary>
        /// <param name="tag">The node tag.</param>
        /// <param name="attrs">The attributes.</param>
        /// <param name="kids">The (key, child) pairs in render order.</param>
        protected BaseKeyedNode(string tag, IEnumerable<IAttribute<T>> attrs, IEnumerable<(string, IVTree)> kids) : this(tag, attrs, kids.ToArray()) {}

        /// <summary>Gets the kind of this node: <see cref="VTreeType.KeyedNode"/>.</summary>
        /// <returns><see cref="VTreeType.KeyedNode"/>.</returns>
        public override VTreeType GetNodeType() => VTreeType.KeyedNode;

        /// <summary>Gets the precomputed number of all transitive children.</summary>
        /// <returns>The descendant count (children plus their descendants).</returns>
        public override int GetDescendantsCount() => this.descendantsCount;

        /// <summary>Gets the child nodes without their keys, in render order.</summary>
        /// <returns>The de-keyed children.</returns>
        public override IVTree[] GetKids() => this.dekeyedKids;

        /// <summary>Determines structural equality: same runtime type, tag, attributes, keys and children.</summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns><c>true</c> when the nodes are structurally equal.</returns>
        public override bool Equals(object obj) => this.Equals(obj as BaseKeyedNode<T>);

        bool Equals(BaseKeyedNode<T> obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (System.Object.ReferenceEquals(this, obj))
            {
                return true;
            }

            if (this.GetType() != obj.GetType())
            {
                return false;
            }


            return this.tag == obj.tag &&
                   AttributesEqual(this.attrs, obj.attrs) &&
                   this.descendantsCount == obj.descendantsCount &&
                   this.kids.SequenceEqual(obj.kids);
        }

        /// <summary>Returns a structural hash combining tag, attributes, descendant count, keys and children.</summary>
        /// <returns>The combined hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = CombineHash(hash, tag);
                hash = CombineHash(hash, GetAttributesHashCode(attrs));
                hash = CombineHash(hash, descendantsCount);
                foreach (var (key, kid) in kids)
                {
                    hash = CombineHash(hash, key);
                    hash = CombineHash(hash, kid);
                }

                return hash;
            }
        }
    }

    /// <summary>
    /// A keyed node that also attaches a host component of type <typeparamref name="U"/>.
    /// </summary>
    /// <typeparam name="T">The host object type (e.g. <c>GameObject</c>).</typeparam>
    /// <typeparam name="U">The host component type to attach.</typeparam>
    public class KeyedNode<T, U> : BaseKeyedNode<T>, ITypedNode
    {
        private System.Type componentType;

        /// <summary>Initializes a typed keyed node.</summary>
        /// <param name="tag">The node tag.</param>
        /// <param name="attrs">The attributes.</param>
        /// <param name="kids">The (key, child) pairs in render order.</param>
        public KeyedNode(string tag, IEnumerable<IAttribute<T>> attrs, params (string, IVTree)[] kids) : base(tag, attrs, kids)
        {
            componentType = typeof(U);
        }

        /// <summary>Initializes a typed keyed node from an enumerable of (key, child) pairs.</summary>
        /// <param name="tag">The node tag.</param>
        /// <param name="attrs">The attributes.</param>
        /// <param name="kids">The (key, child) pairs in render order.</param>
        public KeyedNode(string tag, IEnumerable<IAttribute<T>> attrs, IEnumerable<(string, IVTree)> kids) : this(tag, attrs, kids.ToArray()) {}

        /// <summary>Gets the attached component type (<c>typeof(U)</c>).</summary>
        /// <returns>The component <see cref="Type"/>.</returns>
        public Type GetComponentType() => componentType;

        /// <summary>Determines structural equality including the component type.</summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns><c>true</c> when the nodes are structurally equal and share the component type.</returns>
        public override bool Equals(object obj) => this.Equals(obj as KeyedNode<T, U>);

        bool Equals(KeyedNode<T, U> obj)
        {
            return base.Equals(obj as BaseKeyedNode<T>) && this.componentType == obj.componentType;
        }

        /// <summary>Returns a structural hash including the component type.</summary>
        /// <returns>The combined hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return CombineHash(base.GetHashCode(), componentType);
            }
        }
    }

    /// <summary>
    /// A plain, untyped keyed node whose children are diffed by key.
    /// </summary>
    /// <typeparam name="T">The host object type (e.g. <c>GameObject</c>).</typeparam>
    public class KeyedNode<T> : BaseKeyedNode<T>
    {
        /// <summary>Initializes a keyed node.</summary>
        /// <param name="tag">The node tag.</param>
        /// <param name="attrs">The attributes.</param>
        /// <param name="kids">The (key, child) pairs in render order.</param>
        public KeyedNode(string tag, IAttribute<T>[] attrs, params (string, IVTree)[] kids) : base(tag, attrs, kids) { }
    }
}
