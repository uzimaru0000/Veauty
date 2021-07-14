using System;
using System.Collections.Generic;
using System.Linq;

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

        protected NodeBase(string tag, IEnumerable<IAttribute<T>> attrs)
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

        protected BaseNode(string tag, IEnumerable<IAttribute<T>> attrs, params IVTree[] kids): base(tag, attrs)
        {
            this.kids = kids;
            this.descendantsCount = 0;

            foreach (var kid in kids)
            {
                this.descendantsCount += kid.GetDescendantsCount();
            }
            this.descendantsCount += kids.Length;
        }

        protected BaseNode(string tag, IEnumerable<IAttribute<T>> attrs, IEnumerable<IVTree> kids): this(tag, attrs, kids.ToArray()) {}

        public override int GetDescendantsCount() => this.descendantsCount;
        public override IVTree[] GetKids() => this.kids;

        public override bool Equals(object obj) => this.Equals(obj as BaseNode<T>);

        bool Equals(BaseNode<T> obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (System.Object.ReferenceEquals(this, obj))
            {
                return true;
            }

            if (this.GetType() != obj.GetType())
            {
                return false;
            }


            return this.tag == obj.tag &&
                   this.attrs.attrs.SequenceEqual(this.attrs.attrs) &&
                   this.descendantsCount == obj.descendantsCount;
        }
        
        public override int GetHashCode()
        {
            return new { tag, kids, attrs }.GetHashCode();
        }
    }
    
    public class Node<T, U> : BaseNode<T>, ITypedNode
    {
        private System.Type componentType;

        public Node(string tag, IEnumerable<IAttribute<T>> attrs, params IVTree[] kids) : base(tag, attrs, kids)
        {
            componentType = typeof(U);
        }

        public Node(string tag, IEnumerable<IAttribute<T>> attrs, IEnumerable<IVTree> kids) : this(tag, attrs, kids.ToArray()) {}

        public Type GetComponentType() => componentType;

        public override bool Equals(object obj) => this.Equals(obj as Node<T, U>);

        bool Equals(Node<T, U> obj)
        {
            return base.Equals(obj as BaseNode<T>) && this.componentType == obj.componentType;
        }
        
        public override int GetHashCode()
        {
            return new { tag, componentType, kids, attrs }.GetHashCode();
        }
    }
    
    public class Node<T> : BaseNode<T>
    {
        public Node(string tag, IEnumerable<IAttribute<T>> attrs, params IVTree[] kids) : base(tag, attrs, kids) { }
        public Node(string tag, IEnumerable<IAttribute<T>> attrs, IEnumerable<IVTree> kids) : base(tag, attrs, kids) { }
    }
    
    // KeyedNode

    public class BaseKeyedNode<T> : NodeBase<T>
    {
        public readonly (string, IVTree)[] kids;
        private readonly int descendantsCount;
        private readonly IVTree[] dekeyedKids;

        protected BaseKeyedNode(string tag, IEnumerable<IAttribute<T>> attrs, params (string, IVTree)[] kids) : base(tag, attrs)
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

        protected BaseKeyedNode(string tag, IEnumerable<IAttribute<T>> attrs, IEnumerable<(string, IVTree)> kids) : this(tag, attrs, kids.ToArray()) {}

        public override int GetDescendantsCount() => this.descendantsCount;

        public override IVTree[] GetKids() => this.dekeyedKids;

        public override bool Equals(object obj) => this.Equals(obj as BaseKeyedNode<T>);

        bool Equals(BaseKeyedNode<T> obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (System.Object.ReferenceEquals(this, obj))
            {
                return true;
            }

            if (this.GetType() != obj.GetType())
            {
                return false;
            }


            return this.tag == obj.tag &&
                   this.attrs.attrs.SequenceEqual(this.attrs.attrs) &&
                   this.descendantsCount == obj.descendantsCount;
        }
        
        public override int GetHashCode()
        {
            return new { tag, kids, attrs }.GetHashCode();
        }
    }
    
    public class KeyedNode<T, U> : BaseKeyedNode<T>, ITypedNode
    {
        private System.Type componentType;

        public KeyedNode(string tag, IEnumerable<IAttribute<T>> attrs, params (string, IVTree)[] kids) : base(tag, attrs, kids)
        {
            componentType = typeof(U);
        }
        public KeyedNode(string tag, IEnumerable<IAttribute<T>> attrs, IEnumerable<(string, IVTree)> kids) : this(tag, attrs, kids.ToArray()) {}

        public Type GetComponentType() => componentType;

        public override bool Equals(object obj) => this.Equals(obj as KeyedNode<T, U>);

        bool Equals(KeyedNode<T, U> obj)
        {
            return base.Equals(obj as BaseNode<T>) && this.componentType == obj.componentType;
        }
        
        public override int GetHashCode()
        {
            return new { tag, componentType, kids, attrs }.GetHashCode();
        }
    }

    public class KeyedNode<T> : BaseKeyedNode<T>
    {
        public KeyedNode(string tag, IAttribute<T>[] attrs, params (string, IVTree)[] kids) : base(tag, attrs, kids) { }
    }
}