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
    }
}