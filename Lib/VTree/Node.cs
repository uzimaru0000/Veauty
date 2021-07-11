using System;

namespace Veauty.VTree
{
    public interface ITypedNode
    {
        System.Type GetComponentType();
    }

    public abstract class NodeBase<T> : IVTree, IParent
    {
        public readonly string tag;
        public readonly Attributes<T> attrs;

        protected NodeBase(string tag, IAttribute<T>[] attrs)
        {
            this.tag = tag;
            this.attrs = new Attributes<T>(attrs);
        }

        public VTreeType GetNodeType() => VTreeType.Node;
        public abstract int GetDescendantsCount();
        public abstract IVTree[] GetKids();
    }

    // Node

    public class BaseNode<T> : NodeBase<T>
    {
        public readonly IVTree[] kids;
        private readonly int descendantsCount;

        protected BaseNode(string tag, IAttribute<T>[] attrs, params IVTree[] kids): base(tag, attrs)
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
    
    public class Node<T, U> : BaseNode<U>, ITypedNode
    {
        private System.Type componentType;

        public Node(string tag, IAttribute<U>[] attrs, params IVTree[] kids) : base(tag, attrs, kids)
        {
            componentType = typeof(T);
        }

        public Type GetComponentType() => componentType;
    }
    
    public class Node<T> : BaseNode<T>
    {
        public Node(string tag, IAttribute<T>[] attrs, params IVTree[] kids) : base(tag, attrs, kids) { }
    }
    
    // KeyedNode

    public class BaseKeyedNode<T> : NodeBase<T>
    {
        public readonly (string, IVTree)[] kids;
        private readonly int descendantsCount;
        private readonly IVTree[] dekeyedKids;

        protected BaseKeyedNode(string tag, IAttribute<T>[] attrs, params (string, IVTree)[] kids) : base(tag, attrs)
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
    
    public class KeyedNode<T, U> : BaseKeyedNode<U>, ITypedNode
    {
        private System.Type componentType;

        public KeyedNode(string tag, IAttribute<U>[] attrs, params (string, IVTree)[] kids) : base(tag, attrs, kids)
        {
            componentType = typeof(T);
        }

        public Type GetComponentType() => componentType;
    }

    public class KeyedNode<T> : BaseKeyedNode<T>
    {
        public KeyedNode(string tag, IAttribute<T>[] attrs, params (string, IVTree)[] kids) : base(tag, attrs, kids) { }
    }
}