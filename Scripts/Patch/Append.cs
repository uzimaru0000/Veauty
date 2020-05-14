using UnityEngine;
using Veauty.VTree;

namespace Veauty.Patch
{
    public class Append : IPatch
    {
        private int index;
        private GameObject gameObject;

        public readonly int length;
        public readonly IVTree[] kids;

        public Append(int index, int length, IVTree[] kids)
        {
            this.index = index;
            this.length = length;
            this.kids = kids;
            this.gameObject = null;
        }

        public PatchType GetType() => PatchType.Append;
        public GameObject GetGameObject() => this.gameObject;
        public void SetGameObject(in GameObject go) => this.gameObject = go;
        public int GetIndex() => this.index;
    }
}