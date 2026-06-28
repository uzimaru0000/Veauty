using System.Linq;

namespace Veauty.Patch
{
    public class Remove<T> : IPatch<T>
    {
        private readonly int index;
        private T target;

        public IPatch<T>[] patches;

        public Entry entry;

        public Remove(int index, IPatch<T>[] patches = null, Entry entry = null)
        {
            this.index = index;
            this.patches = patches;
            this.entry = entry;
            this.target = default(T);
        }

        public T GetTarget() => this.target;
        public void SetTarget(in T target) => this.target = target;
        public int GetIndex() => this.index;

        public override bool Equals(object obj) => this.Equals(obj as Remove<T>);

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
