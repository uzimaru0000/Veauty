using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Veauty 
{
    public enum VTreeType
    {
        Text,
        Node,
        KeyedNode,
        Widget
    }

    public interface IVTree
    {
        VTreeType GetType();
        int GetDescendantsCount();
    }

    public interface IParent
    {
        IVTree[] GetKids();
    }

    public struct VText : IVTree
    {
        public readonly string text;
        public VText(string text)
        {
            this.text = text;
        }

        public VTreeType GetType() => VTreeType.Text;
        public int GetDescendantsCount() => 0;

    }

    public struct VNode : IVTree, IParent
    {
        public readonly string tag;
        public readonly IVTree[] kids;

        public readonly Attributes attrs;

        private readonly int descendantsCount;
        public VNode(string tag, IAttribute[] attrs, IVTree[] kids)
        {
            this.tag = tag;
            this.kids = kids;
            this.attrs = new Attributes(attrs);
            this.descendantsCount = 0;

            foreach (var kid in kids)
            {
                this.descendantsCount += kid.GetDescendantsCount();
            }
            this.descendantsCount += kids.Length;
        }

        public VTreeType GetType() => VTreeType.Node;
        public int GetDescendantsCount() => this.descendantsCount;
        public IVTree[] GetKids() => this.kids;
    }

    public struct KeyedVNode : IVTree, IParent
    {
        public readonly string tag;
        public readonly (string, IVTree)[] kids;
        public readonly Attributes attrs;

        private readonly int descendantsCount;
        private readonly IVTree[] dekeyedKids;
        public KeyedVNode(string tag, IAttribute[] attrs, (string, IVTree)[] kids)
        {
            this.tag = tag;
            this.kids = kids;
            this.attrs = new Attributes(attrs);
            this.descendantsCount = 0;
            this.dekeyedKids = new IVTree[kids.Length];

            var i = 0;
            foreach (var (_, kid) in kids)
            {
                this.descendantsCount += kid.GetDescendantsCount();
                this.dekeyedKids[i++] = kid;
            }
            this.descendantsCount += kids.Length;
        }

        public VTreeType GetType() => VTreeType.KeyedNode;
        public int GetDescendantsCount() => this.descendantsCount;

        public IVTree[] GetKids() => this.dekeyedKids;
    }

    public abstract class Widget : IVTree
    {
        public Attributes attrs;
        
        public VTreeType GetType() => VTreeType.Widget;

        public abstract GameObject Init(GameObject go);

        public abstract IVTree Render();

        public abstract void Destroy(GameObject go);
        
        public abstract int GetDescendantsCount();
    }
    
}
