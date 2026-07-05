using System.Linq;

namespace Veauty.Patch
{
    /// <summary>
    /// Patch: the new node has more children than the old one — render and append the
    /// extra children at the end of the target node.
    /// </summary>
    /// <typeparam name="T">The host object type (e.g. <c>GameObject</c>).</typeparam>
    public class Append<T> : IPatch<T>
    {
        private int index;
        private T target;

        /// <summary>The number of children the old node already has; new children start at <see cref="kids"/>[<c>length</c>].</summary>
        public readonly int length;
        /// <summary>The FULL new child list; the applicator renders entries from index <see cref="length"/> onward.</summary>
        public readonly IVTree[] kids;

        /// <summary>Initializes an append patch.</summary>
        /// <param name="index">The pre-order index of the parent node.</param>
        /// <param name="length">The old child count (index of the first child to append).</param>
        /// <param name="kids">The full new child list.</param>
        public Append(int index, int length, IVTree[] kids)
        {
            this.index = index;
            this.length = length;
            this.kids = kids;
            this.target = default(T);
        }

        /// <summary>Gets the host object this patch was bound to.</summary>
        /// <returns>The bound target, or <c>default</c> before binding.</returns>
        public T GetTarget() => this.target;
        /// <summary>Binds the patch to its host object.</summary>
        /// <param name="target">The parent host object to append children under.</param>
        public void SetTarget(in T target) => this.target = target;
        /// <summary>Gets the pre-order index of the parent node.</summary>
        /// <returns>The zero-based pre-order index.</returns>
        public int GetIndex() => this.index;

        /// <summary>Determines structural equality with another object.</summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns><c>true</c> when index, length and kids are all equal.</returns>
        public override bool Equals(object obj) => this.Equals(obj as Append<T>);

        /// <summary>Determines structural equality with another append patch.</summary>
        /// <param name="obj">The patch to compare with.</param>
        /// <returns><c>true</c> when index, length and kids are all equal.</returns>
        public bool Equals(Append<T> obj)
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


            return this.index == obj.index && this.length == obj.length && this.kids.SequenceEqual(obj.kids);
        }

        /// <summary>Returns a hash code combining index, length and kids.</summary>
        /// <returns>The combined hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 397) ^ index;
                hash = (hash * 397) ^ length;
                foreach (var kid in kids)
                {
                    hash = (hash * 397) ^ (kid != null ? kid.GetHashCode() : 0);
                }

                return hash;
            }
        }
    }
}
