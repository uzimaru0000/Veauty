using System.Collections.Generic;
using UnityEngine;

namespace Veauty.Patch 
{
    public class Attrs : IPatch
    {

        public int index;
        public GameObject gameObject;

        public Dictionary<string, IAttribute> attrs;

        public Attrs(int index, Dictionary<string, IAttribute> attrs)
        {
            this.index = index;
            this.attrs = attrs;
            this.gameObject = null;
        }

        public PatchType GetType() => PatchType.Attrs;
        public GameObject GetGameObject() => this.gameObject;
        public void SetGameObject(in GameObject go) => this.gameObject = go;
        public int GetIndex() => this.index;
    }
}