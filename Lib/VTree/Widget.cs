using System.Linq;

namespace Veauty.VTree
{
    public abstract class Widget<T> : IVTree, IParent
    {
        public VTreeType GetNodeType() => VTreeType.Widget;

        public abstract IVTree[] GetKids();

        public abstract T Init(T obj);

        public abstract IVTree Render();

        public abstract void Destroy(T obj);

        public int GetDescendantsCount() => this.GetKids().Sum(x => x.GetDescendantsCount()) + this.GetKids().Length;
    }
}