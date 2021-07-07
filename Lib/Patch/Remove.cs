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
    }
}