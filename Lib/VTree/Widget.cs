using System.Collections.Generic;
using System.Linq;

namespace Veauty.VTree
{
    public abstract class Widget<T> : IVTree, IParent
    {
        protected IEnumerable<IAttribute<T>> attrs;
        protected IVTree[] kids;
        
        public Widget(IEnumerable<IAttribute<T>> attrs, params IVTree[] kids)
        {
            this.attrs = attrs;
            this.kids = kids;
        }

        public Widget(IEnumerable<IAttribute<T>> attrs, IEnumerable<IVTree> kids) : this(attrs, kids.ToArray()) {}

        public VTreeType GetNodeType() => VTreeType.Widget;

        public IVTree[] GetKids() => this.kids;

        public abstract T Init(T obj);

        public abstract IVTree Render();

        public abstract void Destroy(T obj);

        public int GetDescendantsCount() => this.GetKids().Sum(x => x.GetDescendantsCount()) + this.GetKids().Length;
    }
}
