using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VDOM
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
        Widget
    }
    
    public interface IPatch
    {
        PatchType GetType();
        GameObject GetGameObject();
        int GetIndex();
    }

    public class Entry
    {
        public string tag;
        public IVTree vTree;
        public int index;
        public object data;

        public Entry(string tag, IVTree vTree, int index)
        {
            this.tag = tag;
            this.vTree = vTree;
            this.index = index;
        }
    }

    public struct Redraw : IPatch
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
        public int GetIndex() => this.index;

    }

    public struct Text : IPatch
    {
        public int index;
        public string text;
        public GameObject gameObject;

        public Text(int index, string text)
        {
            this.index = index;
            this.text = text;
            this.gameObject = null;
        }

        public PatchType GetType() => PatchType.Text;
        public GameObject GetGameObject() => this.gameObject;
        public int GetIndex() => this.index;
    }

    public struct Attrs : IPatch
    {

        public int index;
        public GameObject gameObject;

        public Attributes attrs;

        public Attrs(int index, Attributes attrs)
        {
            this.index = index;
            this.attrs = attrs;
            this.gameObject = null;
        }

        public PatchType GetType() => PatchType.Attrs;
        public GameObject GetGameObject() => this.gameObject;
        public int GetIndex() => this.index;
    }

    public struct RemoveLast : IPatch
    {

        public int index;
        public GameObject gameObject;

        public int length;
        public int diff;

        public RemoveLast(int index, int length, int diff)
        {
            this.index = index;
            this.length = length;
            this.diff = diff;
            this.gameObject = null;
        }

        public PatchType GetType() => PatchType.RemoveLast;
        public GameObject GetGameObject() => this.gameObject;
        public int GetIndex() => this.index;
    }

    public struct Append : IPatch
    {

        public int index;
        public GameObject gameObject;

        public int length;
        public IVTree[] kids;

        public Append(int index, int length, IVTree[] kids)
        {
            this.index = index;
            this.length = length;
            this.kids = kids;
            this.gameObject = null;
        }

        public PatchType GetType() => PatchType.Append;
        public GameObject GetGameObject() => this.gameObject;
        public int GetIndex() => this.index;
    }

    public struct Reorder : IPatch
    {

        public int index;
        public IPatch[] patches;
        public Insert[] inserts;
        public Insert[] endInserts;
        public GameObject gameObject;

        public Reorder(int index, IPatch[] patches, Insert[] inserts, Insert[] endInserts)
        {
            this.index = index;
            this.patches = patches;
            this.inserts = inserts;
            this.endInserts = endInserts;
            this.gameObject = null;
        }

        public PatchType GetType() => PatchType.Reorder;
        public GameObject GetGameObject() => this.gameObject;
        public int GetIndex() => this.index;

        public struct Insert
        {
            public int index;
            public Entry entry;
        }
    }

    public struct Remove : IPatch
    {
        public int index;
        public GameObject gameObject;

        public IPatch[] patches;

        public Entry entry;

        public Remove(int index, IPatch[] patches, Entry entry)
        {
            this.index = index;
            this.patches = patches;
            this.entry = entry;
            this.gameObject = null;
        }

        public Remove(int index) : this(index, new IPatch[] { }, null) { }

        public PatchType GetType() => PatchType.Remove;
        public GameObject GetGameObject() => this.gameObject;
        public int GetIndex() => this.index;
    }

    public struct WidgetPatch : IPatch
    {
        public int index;
        public GameObject gameObject;
        private Widget oldWidget;
        private Widget newWidget;

        public WidgetPatch(int index, Widget oldWidget, Widget newWidget)
        {
            this.index = index;
            this.oldWidget = oldWidget;
            this.newWidget = newWidget;
            this.gameObject = null;
        }
        
        public PatchType GetType() => PatchType.Widget;
        public GameObject GetGameObject() => this.gameObject;
        public int GetIndex() => this.index;
    }
}
