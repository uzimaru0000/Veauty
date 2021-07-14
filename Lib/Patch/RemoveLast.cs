namespace Veauty.Patch
{
    public class RemoveLast<T> : IPatch<T>
    {
        private int index;
        private T target;

        public readonly int length;
        public readonly int diff;

        public RemoveLast(int index, int length, int diff)
        {
            this.index = index;
            this.length = length;
            this.diff = diff;
            this.target = default(T);
        }

        public T GetTarget() => this.target;
        public void SetTarget(in T target) => this.target = target;
        public int GetIndex() => this.index;

        public override bool Equals(object obj) => this.Equals(obj as RemoveLast<T>);

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

        public override int GetHashCode()
        {
            return new { index, length, diff }.GetHashCode();
        }
    }
}