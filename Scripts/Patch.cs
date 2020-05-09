using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VDOM
{
    public interface IPatch
    {
        string GetType();
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

        public string GetType() => "REDRAW";
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

        public string GetType() => "TEXT";
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

        public string GetType() => "ATTRS";
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

        public string GetType() => "REMOVE_LAST";
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

        public string GetType() => "APPEND";
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

        public string GetType() => "REORDER";
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

        public string GetType() => "REMOVE";
        public GameObject GetGameObject() => this.gameObject;
        public int GetIndex() => this.index;
    }
}
