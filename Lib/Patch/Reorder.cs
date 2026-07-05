using System.Linq;

namespace Veauty.Patch
{
    /// <summary>
    /// Patch: the children of a keyed node changed — carries the per-child sub-patches
    /// (updates, removals, move-detaches) plus the insertions/moves to perform, split into
    /// positional <see cref="inserts"/> and trailing <see cref="endInserts"/>.
    /// </summary>
    /// <typeparam name="T">The host object type (e.g. <c>GameObject</c>).</typeparam>
    public class Reorder<T> : IPatch<T>
    {
        private int index;
        private T target;
        /// <summary>Sub-patches for children that exist in both trees (including <see cref="Remove{T}"/> patches).</summary>
        public readonly IPatch<T>[] patches;
        /// <summary>Insertions (or arrivals of moved nodes) at specific positions in the new child list.</summary>
        public readonly Insert[] inserts;
        /// <summary>Insertions appended after all surviving children (from new trailing keys).</summary>
        public readonly Insert[] endInserts;

        /// <summary>Initializes a reorder patch.</summary>
        /// <param name="index">The pre-order index of the keyed parent node.</param>
        /// <param name="patches">Sub-patches for matched children.</param>
        /// <param name="inserts">Positional insertions/moves.</param>
        /// <param name="endInserts">Trailing insertions (their entry index is -1).</param>
        public Reorder(int index, IPatch<T>[] patches, Insert[] inserts, Insert[] endInserts)
        {
            this.index = index;
            this.patches = patches;
            this.inserts = inserts;
            this.endInserts = endInserts;
            this.target = default(T);
        }

        /// <summary>Gets the host object this patch was bound to.</summary>
        /// <returns>The bound target, or <c>default</c> before binding.</returns>
        public T GetTarget() => this.target;
        /// <summary>Binds the patch to its host object.</summary>
        /// <param name="target">The keyed parent host object.</param>
        public void SetTarget(in T target) => this.target = target;
        /// <summary>Gets the pre-order index of the keyed parent node.</summary>
        /// <returns>The zero-based pre-order index.</returns>
        public int GetIndex() => this.index;

        /// <summary>Determines structural equality with another object.</summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns><c>true</c> when index, sub-patches and all inserts are equal.</returns>
        public override bool Equals(object obj) => this.Equals(obj as Reorder<T>);

        /// <summary>Determines structural equality with another reorder patch.</summary>
        /// <param name="obj">The patch to compare with.</param>
        /// <returns><c>true</c> when index, sub-patches and all inserts are equal.</returns>
        public bool Equals(Reorder<T> obj)
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
                   this.patches.SequenceEqual(obj.patches) &&
                   this.inserts.SequenceEqual(obj.inserts) &&
                   this.endInserts.SequenceEqual(obj.endInserts);
        }

        /// <summary>Returns a hash code combining index, sub-patches and inserts.</summary>
        /// <returns>The combined hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 397) ^ index;
                hash = CombineArrayHash(hash, patches);
                hash = CombineArrayHash(hash, inserts);
                hash = CombineArrayHash(hash, endInserts);
                return hash;
            }
        }

        private static int CombineArrayHash<TValue>(int hash, TValue[] values)
        {
            unchecked
            {
                foreach (var value in values)
                {
                    hash = (hash * 397) ^ (value != null ? value.GetHashCode() : 0);
                }

                return hash;
            }
        }

        /// <summary>
        /// One insertion within a reorder: place the node described by <see cref="entry"/>
        /// at child position <see cref="index"/>. An entry tagged <see cref="Entry.Type.Move"/>
        /// re-parents the existing host object instead of rendering a new one.
        /// </summary>
        public class Insert
        {
            /// <summary>The position in the new child list, or -1 for end inserts.</summary>
            public int index;
            /// <summary>The bookkeeping entry describing what to insert or move.</summary>
            public Entry entry;

            /// <summary>Determines structural equality with another object.</summary>
            /// <param name="obj">The object to compare with.</param>
            /// <returns><c>true</c> when index and entry are equal.</returns>
            public override bool Equals(object obj) => this.Equals(obj as Insert);

            /// <summary>Determines structural equality with another insert.</summary>
            /// <param name="obj">The insert to compare with.</param>
            /// <returns><c>true</c> when index and entry are equal.</returns>
            public bool Equals(Insert obj)
            {
                if (obj is null)
                {
                    return false;
                }

                if (System.Object.ReferenceEquals(this, obj))
                {
                    return true;
                }

                return index == obj.index && entry.Equals(obj.entry);
            }

            /// <summary>Returns a hash code combining index and entry.</summary>
            /// <returns>The combined hash code.</returns>
            public override int GetHashCode()
            {
                unchecked
                {
                    return (index * 397) ^ (entry != null ? entry.GetHashCode() : 0);
                }
            }
        }
    }
}
