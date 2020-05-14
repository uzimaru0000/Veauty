using UnityEngine;

namespace Veauty.Patch
{
    public class Remove : IPatch
    {
        private readonly int index;
        private GameObject gameObject;

        public readonly IPatch[] patches;

        public readonly Entry entry;

        public Remove(int index, IPatch[] patches = null, Entry entry = null)
        {
            this.index = index;
            this.patches = patches;
            this.entry = entry;
            this.gameObject = null;
        }

        public PatchType GetType() => PatchType.Remove;
        public GameObject GetGameObject() => this.gameObject;
        public void SetGameObject(in GameObject go) => this.gameObject = go;
        public int GetIndex() => this.index;
    }
}