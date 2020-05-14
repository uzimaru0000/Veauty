using UnityEngine;
using Veauty.VTree;

namespace Veauty.Patch
{
    public class Redraw : IPatch
    {
        public int index;
        public IVTree vTree;
        public GameObject gameObject;

        public Redraw(int index, IVTree vTree)
        {
            this.index = index;
            this.vTree = vTree;
            this.gameObject = null;
        }

        public PatchType GetType() => PatchType.Redraw;
        public GameObject GetGameObject() => this.gameObject;
        public void SetGameObject(in GameObject go) => this.gameObject = go;

        public int GetIndex() => this.index;

    }
}