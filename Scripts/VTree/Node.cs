using System;
using UnityEngine;

namespace Veauty.VTree
{
    public interface ITypedNode
    {
        System.Type GetComponentType();
    }
    
    // Node
    
    public class BaseNode : IVTree, IParent
    {
        public readonly string tag;
        public readonly IVTree[] kids;

        public readonly Attributes attrs;

        private readonly int descendantsCount;
        public BaseNode(string tag, IAttribute[] attrs, IVTree[] kids)
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
    
    public class Node<T> : BaseNode, ITypedNode where T : MonoBehaviour
    {
        public Node(string tag, IAttribute[] attrs, IVTree[] kids) : base(tag, attrs, kids) { }

        public Type GetComponentType() => typeof(T);
    }
    
    public class Node : BaseNode
    {
        public Node(string tag, IAttribute[] attrs, IVTree[] kids) : base(tag, attrs, kids) { }
    }
    
    // KeyedNode

    public class BaseKeyedNode : IVTree, IParent
    {
        public readonly string tag;
        public readonly (string, IVTree)[] kids;
        public readonly Attributes attrs;

        private readonly int descendantsCount;
        private readonly IVTree[] dekeyedKids;

        protected BaseKeyedNode(string tag, IAttribute[] attrs, (string, IVTree)[] kids)
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
    
    public class KeyedNode<T> : BaseKeyedNode, ITypedNode where T : MonoBehaviour 
    {
        public KeyedNode(string tag, IAttribute[] attrs, (string, IVTree)[] kids) : base(tag, attrs, kids) { }

        public Type GetComponentType() => typeof(T);
    }

    public class KeyedNode : BaseKeyedNode
    {
        public KeyedNode(string tag, IAttribute[] attrs, (string, IVTree)[] kids) : base(tag, attrs, kids) { }
    }
}