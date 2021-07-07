namespace Veauty.Patch
{
    public class Redraw<T> : IPatch<T>
    {
        public int index;
        public IVTree vTree;
        public T target;

        public Redraw(int index, IVTree vTree)
        {
            this.index = index;
            this.vTree = vTree;
            this.target = default(T);
        }

        public T GetTarget() => this.target;
        public void SetTarget(in T target) => this.target = target;

        public int GetIndex() => this.index;

    }
}