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
    }
}