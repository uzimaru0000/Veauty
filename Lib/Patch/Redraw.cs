namespace Veauty.Patch
{
    /// <summary>
    /// Patch: the node cannot be updated in place (different node type or tag) — destroy the
    /// target's subtree and render <see cref="vTree"/> from scratch in its position.
    /// </summary>
    /// <typeparam name="T">The host object type (e.g. <c>GameObject</c>).</typeparam>
    public class Redraw<T> : IPatch<T>
    {
        /// <summary>The pre-order index of the node to replace.</summary>
        public int index;
        /// <summary>The new virtual tree to render in place of the old node.</summary>
        public IVTree vTree;
        /// <summary>The host object this patch was bound to.</summary>
        public T target;

        /// <summary>Initializes a redraw patch.</summary>
        /// <param name="index">The pre-order index of the node to replace.</param>
        /// <param name="vTree">The replacement tree.</param>
        public Redraw(int index, IVTree vTree)
        {
            this.index = index;
            this.vTree = vTree;
            this.target = default(T);
        }

        /// <summary>Gets the host object this patch was bound to.</summary>
        /// <returns>The bound target, or <c>default</c> before binding.</returns>
        public T GetTarget() => this.target;
        /// <summary>Binds the patch to its host object.</summary>
        /// <param name="target">The host object to replace.</param>
        public void SetTarget(in T target) => this.target = target;

        /// <summary>Gets the pre-order index of the node to replace.</summary>
        /// <returns>The zero-based pre-order index.</returns>
        public int GetIndex() => this.index;

        /// <summary>Determines structural equality with another object.</summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns><c>true</c> when index and replacement tree are equal.</returns>
        public override bool Equals(object obj) => this.Equals(obj as Redraw<T>);

        /// <summary>Determines structural equality with another redraw patch.</summary>
        /// <param name="obj">The patch to compare with.</param>
        /// <returns><c>true</c> when index and replacement tree are equal.</returns>
        public bool Equals(Redraw<T> obj)
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


            return this.index == obj.index && object.Equals(this.vTree, obj.vTree);
        }

        /// <summary>Returns a hash code combining index and replacement tree.</summary>
        /// <returns>The combined hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((index * 397) ^ (vTree != null ? vTree.GetHashCode() : 0));
            }
        }

    }
}
