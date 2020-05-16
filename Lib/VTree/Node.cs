using System;
using UnityEngine;

namespace Veauty.VTree
{
    public interface ITypedNode
    {
        System.Type GetComponentType();
    }

    public abstract class NodeBase : IVTree, IParent
    {
        public readonly string tag;
        public readonly Attributes attrs;

        protected NodeBase(string tag, IAttribute[] attrs)
        {
            this.tag = tag;
            this.attrs = new Attributes(attrs);
        }

        public VTreeType GetNodeType() => VTreeType.Node;
        public abstract int GetDescendantsCount();
        public abstract IVTree[] GetKids();
    }

    // Node

    public class BaseNode : NodeBase 
    {
        public readonly IVTree[] kids;
        private readonly int descendantsCount;

        protected BaseNode(string tag, IAttribute[] attrs, IVTree[] kids): base(tag, attrs)
        {
            this.kids = kids;
            this.descendantsCount = 0;

            foreach (var kid in kids)
            {
                this.descendantsCount += kid.GetDescendantsCount();
            }
            this.descendantsCount += kids.Length;
        }

        public override int GetDescendantsCount() => this.descendantsCount;
        public override IVTree[] GetKids() => this.kids;
    }
    
    public class Node<T> : BaseNode, ITypedNode where T : Component 
    {
        public Node(string tag, IAttribute[] attrs, IVTree[] kids) : base(tag, attrs, kids) { }

        public Type GetComponentType() => typeof(T);
    }
    
    public class Node : BaseNode
    {
        public Node(string tag, IAttribute[] attrs, IVTree[] kids) : base(tag, attrs, kids) { }
    }
    
    // KeyedNode

    public class BaseKeyedNode : NodeBase 
    {
        public readonly (string, IVTree)[] kids;
        private readonly int descendantsCount;
        private readonly IVTree[] dekeyedKids;

        protected BaseKeyedNode(string tag, IAttribute[] attrs, (string, IVTree)[] kids) : base(tag, attrs)
        {
            this.kids = kids;
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

        public override int GetDescendantsCount() => this.descendantsCount;

        public override IVTree[] GetKids() => this.dekeyedKids;
    }
    
    public class KeyedNode<T> : BaseKeyedNode, ITypedNode where T : Component
    {
        public KeyedNode(string tag, IAttribute[] attrs, (string, IVTree)[] kids) : base(tag, attrs, kids) { }

        public Type GetComponentType() => typeof(T);
    }

    public class KeyedNode : BaseKeyedNode
    {
        public KeyedNode(string tag, IAttribute[] attrs, (string, IVTree)[] kids) : base(tag, attrs, kids) { }
    }
}