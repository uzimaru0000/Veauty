namespace Veauty.Patch
{
    /// <summary>
    /// Patch: the new node has fewer children than the old one — remove the trailing
    /// <see cref="diff"/> children from the target node.
    /// </summary>
    /// <typeparam name="T">The host object type (e.g. <c>GameObject</c>).</typeparam>
    public class RemoveLast<T> : IPatch<T>
    {
        private int index;
        private T target;

        /// <summary>The number of children that remain (the new child count).</summary>
        public readonly int length;
        /// <summary>The number of trailing children to remove.</summary>
        public readonly int diff;

        /// <summary>Initializes a remove-last patch.</summary>
        /// <param name="index">The pre-order index of the parent node.</param>
        /// <param name="length">The new (smaller) child count.</param>
        /// <param name="diff">How many trailing children to remove.</param>
        public RemoveLast(int index, int length, int diff)
        {
            this.index = index;
            this.length = length;
            this.diff = diff;
            this.target = default(T);
        }

        /// <summary>Gets the host object this patch was bound to.</summary>
        /// <returns>The bound target, or <c>default</c> before binding.</returns>
        public T GetTarget() => this.target;
        /// <summary>Binds the patch to its host object.</summary>
        /// <param name="target">The parent host object to trim children from.</param>
        public void SetTarget(in T target) => this.target = target;
        /// <summary>Gets the pre-order index of the parent node.</summary>
        /// <returns>The zero-based pre-order index.</returns>
        public int GetIndex() => this.index;

        /// <summary>Determines structural equality with another object.</summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns><c>true</c> when index, length and diff are all equal.</returns>
        public override bool Equals(object obj) => this.Equals(obj as RemoveLast<T>);

        /// <summary>Determines structural equality with another remove-last patch.</summary>
        /// <param name="obj">The patch to compare with.</param>
        /// <returns><c>true</c> when index, length and diff are all equal.</returns>
        public bool Equals(RemoveLast<T> obj)
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


            return this.index == obj.index && this.length == obj.length && this.diff == obj.diff;
        }

        /// <summary>Returns a hash code combining index, length and diff.</summary>
        /// <returns>The combined hash code.</returns>
        public override int GetHashCode()
        {
            return new { index, length, diff }.GetHashCode();
        }
    }
}
