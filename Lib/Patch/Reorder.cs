using UnityEngine;

namespace Veauty.Patch
{
    public class Reorder : IPatch
    {
        private int index;
        private GameObject gameObject;
        public readonly IPatch[] patches;
        public readonly Insert[] inserts;
        public readonly Insert[] endInserts;

        public Reorder(int index, IPatch[] patches, Insert[] inserts, Insert[] endInserts)
        {
            this.index = index;
            this.patches = patches;
            this.inserts = inserts;
            this.endInserts = endInserts;
            this.gameObject = null;
        }

        public GameObject GetGameObject() => this.gameObject;
        public void SetGameObject(in GameObject go) => this.gameObject = go;   
        public int GetIndex() => this.index;

        public struct Insert
        {
            public int index;
            public Entry entry;
        }
    }
}