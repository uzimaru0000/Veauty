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

        public struct Insert
        {
            public int index;
            public Entry entry;
        }
    }
}