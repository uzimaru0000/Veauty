using System.Collections.Generic;
using UnityEngine;
using Veauty.VTree;

namespace Veauty.Patch
{
    public enum PatchType
    {   
        Redraw,
        Text,
        Attrs,
        RemoveLast,
        Append,
        Remove,
        Reorder,
        Attach
    }
    
    public interface IPatch
    {
        PatchType GetType();
        GameObject GetGameObject();
        void SetGameObject(in GameObject go);
        int GetIndex();
    }

    public class Entry
    {
        public enum Type
        {
            Move,
            Remove,
            Insert
        }
        
        public Type tag;
        public IVTree vTree;
        public int index;
        public object data;

        public Entry(Type tag, IVTree vTree, int index)
        {
            this.tag = tag;
            this.vTree = vTree;
            this.index = index;
        }
    }
}