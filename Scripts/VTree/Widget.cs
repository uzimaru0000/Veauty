using System.Linq;
using UnityEngine;

namespace Veauty.VTree
{
    public abstract class Widget : IVTree, IParent
    {
        public Attributes attrs;
        
        public VTreeType GetType() => VTreeType.Widget;

        public abstract IVTree[] GetKids();

        public abstract GameObject Init(GameObject go);

        public abstract IVTree Render();

        public abstract void Destroy(GameObject go);

        public int GetDescendantsCount() => this.GetKids().Sum(x => x.GetDescendantsCount()) + this.GetKids().Length;
    }
}