using UnityEngine;

namespace Veauty.Patch
{
    public class RemoveLast : IPatch
    {
        private int index;
        private GameObject gameObject;

        public readonly int length;
        public readonly int diff;

        public RemoveLast(int index, int length, int diff)
        {
            this.index = index;
            this.length = length;
            this.diff = diff;
            this.gameObject = null;
        }

        public GameObject GetGameObject() => this.gameObject;
        public void SetGameObject(in GameObject go) => this.gameObject = go;
        public int GetIndex() => this.index;
    }
}