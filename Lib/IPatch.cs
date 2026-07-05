namespace Veauty
{
    /// <summary>
    /// A single mutation produced by <see cref="Diff{T}"/> that a renderer applies to the
    /// existing host tree. Patches address nodes by pre-order index
    /// (see <see cref="IVTree.GetDescendantsCount"/>).
    /// </summary>
    /// <typeparam name="T">The host object type (e.g. <c>GameObject</c>).</typeparam>
    public interface IPatch<T>
    {
        /// <summary>Gets the host object this patch was bound to.</summary>
        /// <returns>The target set by <see cref="SetTarget"/>, or <c>default</c> before binding.</returns>
        T GetTarget();

        /// <summary>Binds the patch to the host object found at <see cref="GetIndex"/>.</summary>
        /// <param name="target">The host object the patch will mutate.</param>
        void SetTarget(in T target);

        /// <summary>Gets the pre-order index of the virtual node this patch targets.</summary>
        /// <returns>The zero-based pre-order index within the old tree.</returns>
        int GetIndex();
    }

    /// <summary>
    /// Bookkeeping record used by the keyed diff to track how a keyed child changed.
    /// A key that is both removed and inserted is promoted to a <see cref="Type.Move"/>.
    /// </summary>
    public class Entry
    {
        /// <summary>The kind of change recorded for a keyed child.</summary>
        public enum Type
        {
            /// <summary>The child exists in both trees but changes position.</summary>
            Move,
            /// <summary>The child exists only in the old tree.</summary>
            Remove,
            /// <summary>The child exists only in the new tree.</summary>
            Insert
        }

        /// <summary>The current classification of this entry.</summary>
        public Type tag;
        /// <summary>The virtual node associated with the change.</summary>
        public IVTree vTree;
        /// <summary>For inserts: the position in the new child list (or -1 for end inserts). For removes: the pre-order index in the old tree.</summary>
        public int index;
        /// <summary>Scratch slot linking a pending <c>Remove</c> patch so it can be upgraded to a move.</summary>
        public object data;

        /// <summary>Initializes an entry.</summary>
        /// <param name="tag">The change classification.</param>
        /// <param name="vTree">The virtual node associated with the change.</param>
        /// <param name="index">The list position (insert) or pre-order index (remove).</param>
        public Entry(Type tag, IVTree vTree, int index)
        {
            this.tag = tag;
            this.vTree = vTree;
            this.index = index;
        }

        /// <summary>Determines structural equality with another object.</summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns><c>true</c> when <paramref name="obj"/> is an <see cref="Entry"/> with equal tag, index, tree and data.</returns>
        public override bool Equals(object obj) => this.Equals(obj as Entry);

        /// <summary>Determines structural equality with another entry.</summary>
        /// <param name="obj">The entry to compare with.</param>
        /// <returns><c>true</c> when tag, index, tree and data are all equal.</returns>
        public bool Equals(Entry obj)
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

            return this.index == obj.index &&
                   this.tag == obj.tag &&
                   (
                        this.vTree == null
                            ? this.vTree == obj.vTree
                            : this.vTree.Equals(obj.vTree)
                   ) &&
                   (
                        this.data == null
                            ? this.data == obj.data
                            : this.data.Equals(obj.data)
                   );
        }

        /// <summary>Returns a hash code combining tag, index, tree and data.</summary>
        /// <returns>The combined hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 397) ^ index;
                hash = (hash * 397) ^ tag.GetHashCode();
                hash = (hash * 397) ^ (vTree != null ? vTree.GetHashCode() : 0);
                hash = (hash * 397) ^ (data != null ? data.GetHashCode() : 0);
                return hash;
            }
        }
    }
}
