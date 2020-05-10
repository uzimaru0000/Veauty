﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VDOM
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

    public struct VNode : IVTree
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

    }

    public struct KeyedVNode : IVTree
    {
        public readonly string tag;
        public readonly (string, IVTree)[] kids;
        public readonly Attributes attrs;

        private readonly int descendantsCount;
        public KeyedVNode(string tag, IAttribute[] attrs, (string, IVTree)[] kids)
        {
            this.tag = tag;
            this.kids = kids;
            this.attrs = new Attributes(attrs);
            this.descendantsCount = 0;

            foreach (var (_, kid) in kids)
            {
                this.descendantsCount += kid.GetDescendantsCount();
            }
            this.descendantsCount += kids.Length;
        }

        public VTreeType GetType() => VTreeType.KeyedNode;
        public int GetDescendantsCount() => this.descendantsCount;
    }

    public abstract class Widget : IVTree
    {
        public Attributes attrs;
        
        public abstract int GetDescendantsCount();

        public abstract GameObject Init();

        public abstract IVTree Render();

        public abstract void Destroy(GameObject go);

        public VTreeType GetType() => VTreeType.Widget;
    }
    
}
