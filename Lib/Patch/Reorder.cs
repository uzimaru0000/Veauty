using System.Linq;

namespace Veauty.Patch
{
    public class Reorder<T> : IPatch<T>
    {
        private int index;
        private T target;
        public readonly IPatch<T>[] patches;
        public readonly Insert[] inserts;
        public readonly Insert[] endInserts;

        public Reorder(int index, IPatch<T>[] patches, Insert[] inserts, Insert[] endInserts)
        {
            this.index = index;
            this.patches = patches;
            this.inserts = inserts;
            this.endInserts = endInserts;
            this.target = default(T);
        }

        public T GetTarget() => this.target;
        public void SetTarget(in T target) => this.target = target; 
        public int GetIndex() => this.index;

        public override bool Equals(object obj) => this.Equals(obj as Reorder<T>);

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

        public class Insert
        {
            public int index;
            public Entry entry;

            public override bool Equals(object obj) => this.Equals(obj as Insert);

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
