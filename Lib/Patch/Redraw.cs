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

        public override bool Equals(object obj) => this.Equals(obj as Redraw<T>);

        public bool Equals(Redraw<T> obj)
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


            return this.index == obj.index && this.vTree.Equals(this.vTree);
        }

        public override int GetHashCode()
        {
            return new { index, vTree }.GetHashCode();
        }

    }
}