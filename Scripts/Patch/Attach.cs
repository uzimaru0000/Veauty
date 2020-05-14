using UnityEngine;

namespace Veauty.Patch
{
    public class Attach : IPatch
    {
        private GameObject gameObject;
        private readonly int index;
        public readonly System.Type oldComponent;
        public readonly System.Type newComponent;

        public Attach(int index, System.Type oldComponent, System.Type newComponent)
        {
            this.index = index;
            this.oldComponent = oldComponent;
            this.newComponent = newComponent;
            this.gameObject = null;
        }
        
        public PatchType GetType() => PatchType.Attach;

        public GameObject GetGameObject() => this.gameObject;

        public void SetGameObject(in GameObject go) => this.gameObject = go;

        public int GetIndex() => this.index;
    }
}