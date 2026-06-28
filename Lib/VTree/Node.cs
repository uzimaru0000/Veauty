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

        public virtual VTreeType GetNodeType() => VTreeType.Node;
        public abstract int GetDescendantsCount();
        public abstract IVTree[] GetKids();

        protected static bool AttributesEqual(Attributes<T> x, Attributes<T> y)
        {
            if (x.attrs.Count != y.attrs.Count)
            {
                return false;
            }

            foreach (var attr in x.attrs)
            {
                if (!y.attrs.TryGetValue(attr.Key, out var otherAttr) || !object.Equals(attr.Value, otherAttr))
                {
                    return false;
                }
            }

            return true;
        }

        protected static int CombineHash(int hash, object value)
        {
            unchecked
            {
                return (hash * 397) ^ (value != null ? value.GetHashCode() : 0);
            }
        }

        protected static int GetAttributesHashCode(Attributes<T> attrs)
        {
            unchecked
            {
                var hash = 17;
                foreach (var attr in attrs.attrs.OrderBy(x => x.Key))
                {
                    hash = CombineHash(hash, attr.Key);
                    hash = CombineHash(hash, attr.Value);
                }

                return hash;
            }
        }

        protected static int GetKidsHashCode(IEnumerable<IVTree> kids)
        {
            unchecked
            {
                var hash = 17;
                foreach (var kid in kids)
                {
                    hash = CombineHash(hash, kid);
                }

                return hash;
            }
        }
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
                   AttributesEqual(this.attrs, obj.attrs) &&
                   this.descendantsCount == obj.descendantsCount &&
                   this.kids.SequenceEqual(obj.kids);
        }
        
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = CombineHash(hash, tag);
                hash = CombineHash(hash, GetAttributesHashCode(attrs));
                hash = CombineHash(hash, descendantsCount);
                hash = CombineHash(hash, GetKidsHashCode(kids));
                return hash;
            }
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
            unchecked
            {
                return CombineHash(base.GetHashCode(), componentType);
            }
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

        public override VTreeType GetNodeType() => VTreeType.KeyedNode;

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
                   AttributesEqual(this.attrs, obj.attrs) &&
                   this.descendantsCount == obj.descendantsCount &&
                   this.kids.SequenceEqual(obj.kids);
        }
        
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = CombineHash(hash, tag);
                hash = CombineHash(hash, GetAttributesHashCode(attrs));
                hash = CombineHash(hash, descendantsCount);
                foreach (var (key, kid) in kids)
                {
                    hash = CombineHash(hash, key);
                    hash = CombineHash(hash, kid);
                }

                return hash;
            }
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
            return base.Equals(obj as BaseKeyedNode<T>) && this.componentType == obj.componentType;
        }
        
        public override int GetHashCode()
        {
            unchecked
            {
                return CombineHash(base.GetHashCode(), componentType);
            }
        }
    }

    public class KeyedNode<T> : BaseKeyedNode<T>
    {
        public KeyedNode(string tag, IAttribute<T>[] attrs, params (string, IVTree)[] kids) : base(tag, attrs, kids) { }
    }
}
