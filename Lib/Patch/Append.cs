using System.Linq;

namespace Veauty.Patch
{
    public class Append<T> : IPatch<T>
    {
        private int index;
        private T target;

        public readonly int length;
        public readonly IVTree[] kids;

        public Append(int index, int length, IVTree[] kids)
        {
            this.index = index;
            this.length = length;
            this.kids = kids;
            this.target = default(T);
        }

        public T GetTarget() => this.target;
        public void SetTarget(in T target) => this.target = target;
        public int GetIndex() => this.index;

        public override bool Equals(object obj) => this.Equals(obj as Append<T>);

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

        public override int GetHashCode()
        {
            return new { index, length, kids }.GetHashCode();
        }
    }
}