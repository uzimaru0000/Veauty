namespace Veauty
{
    public interface IPatch<T>
    {
        T GetTarget();
        void SetTarget(in T target);
        int GetIndex();
    }

    public class Entry
    {
        public enum Type
        {
            Move,
            Remove,
            Insert
        }
        
        public Type tag;
        public IVTree vTree;
        public int index;
        public object data;

        public Entry(Type tag, IVTree vTree, int index)
        {
            this.tag = tag;
            this.vTree = vTree;
            this.index = index;
        }

        public override bool Equals(object obj) => this.Equals(obj as Entry);

        public bool Equals(Entry obj)
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
                   this.tag == obj.tag &&
                   (
                        this.vTree == null
                            ? this.vTree == obj.vTree
                            : this.vTree.Equals(obj.vTree)
                   ) &&
                   (
                        this.data == null
                            ? this.data == obj.data
                            : this.data.Equals(obj.data)
                   );
        }

        public override int GetHashCode()
        {
            return new { index, tag, vTree, data }.GetHashCode();
        }
    }
}