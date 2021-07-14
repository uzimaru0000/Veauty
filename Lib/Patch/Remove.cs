using System.Linq;

namespace Veauty.Patch
{
    public class Remove<T> : IPatch<T>
    {
        private readonly int index;
        private T target;

        public readonly IPatch<T>[] patches;

        public readonly Entry entry;

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
                   this.entry == obj.entry &&
                   this.patches == null
                        ? this.patches == obj.patches
                        : this.patches.SequenceEqual(obj.patches);
        }

        public override int GetHashCode()
        {
            return new { index, patches, entry }.GetHashCode();
        }
    }
}