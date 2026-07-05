using System.Linq;

namespace Veauty.Patch
{
    /// <summary>
    /// Patch: remove a keyed child from its parent. Emitted inside a
    /// <see cref="Reorder{T}"/> patch by the keyed diff. When the same key is re-inserted
    /// elsewhere, <see cref="patches"/> and <see cref="entry"/> are populated and the removal
    /// becomes the "detach" half of a move instead of a destruction.
    /// </summary>
    /// <typeparam name="T">The host object type (e.g. <c>GameObject</c>).</typeparam>
    public class Remove<T> : IPatch<T>
    {
        private readonly int index;
        private T target;

        /// <summary>Sub-patches to apply to the moved node, or <c>null</c> for a plain removal.</summary>
        public IPatch<T>[] patches;

        /// <summary>The move bookkeeping entry, or <c>null</c> for a plain removal.</summary>
        public Entry entry;

        /// <summary>Initializes a remove (or move-detach) patch.</summary>
        /// <param name="index">The pre-order index of the node to remove.</param>
        /// <param name="patches">Sub-patches for the moved node; <c>null</c> for a plain removal.</param>
        /// <param name="entry">The move entry; <c>null</c> for a plain removal.</param>
        public Remove(int index, IPatch<T>[] patches = null, Entry entry = null)
        {
            this.index = index;
            this.patches = patches;
            this.entry = entry;
            this.target = default(T);
        }

        /// <summary>Gets the host object this patch was bound to.</summary>
        /// <returns>The bound target, or <c>default</c> before binding.</returns>
        public T GetTarget() => this.target;
        /// <summary>Binds the patch to its host object.</summary>
        /// <param name="target">The host object to remove or detach.</param>
        public void SetTarget(in T target) => this.target = target;
        /// <summary>Gets the pre-order index of the node to remove.</summary>
        /// <returns>The zero-based pre-order index.</returns>
        public int GetIndex() => this.index;

        /// <summary>Determines structural equality with another object.</summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns><c>true</c> when index, entry and sub-patches are equal.</returns>
        public override bool Equals(object obj) => this.Equals(obj as Remove<T>);

        /// <summary>Determines structural equality with another remove patch.</summary>
        /// <param name="obj">The patch to compare with.</param>
        /// <returns><c>true</c> when index, entry and sub-patches are equal.</returns>
        public bool Equals(Remove<T> obj)
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
                   object.Equals(this.entry, obj.entry) &&
                   PatchesEqual(this.patches, obj.patches);
        }

        /// <summary>Returns a hash code combining index, entry and sub-patches.</summary>
        /// <returns>The combined hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 397) ^ index;
                hash = (hash * 397) ^ (entry != null ? entry.GetHashCode() : 0);
                if (patches != null)
                {
                    foreach (var patch in patches)
                    {
                        hash = (hash * 397) ^ (patch != null ? patch.GetHashCode() : 0);
                    }
                }

                return hash;
            }
        }

        private static bool PatchesEqual(IPatch<T>[] x, IPatch<T>[] y)
        {
            if (x == null || y == null)
            {
                return x == y;
            }

            return x.SequenceEqual(y);
        }
    }
}
