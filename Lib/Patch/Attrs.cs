using System.Collections.Generic;
using System.Linq;

namespace Veauty.Patch 
{
    public class Attrs<T> : IPatch<T>
    {

        public int index;
        public T target;

        public Dictionary<string, IAttribute<T>> attrs;

        public Attrs(int index, Dictionary<string, IAttribute<T>> attrs)
        {
            this.index = index;
            this.attrs = attrs;
            this.target = default(T);
        }

        public T GetTarget() => this.target;
        public void SetTarget(in T target) => this.target = target;
        public int GetIndex() => this.index;

        public override bool Equals(object obj) => this.Equals(obj as Attrs<T>);

        public bool Equals(Attrs<T> obj)
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


            return this.index == obj.index && AttrsEqual(this.attrs, obj.attrs);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 397) ^ index;
                foreach (var attr in attrs.OrderBy(x => x.Key))
                {
                    hash = (hash * 397) ^ (attr.Key != null ? attr.Key.GetHashCode() : 0);
                    hash = (hash * 397) ^ (attr.Value != null ? attr.Value.GetHashCode() : 0);
                }

                return hash;
            }
        }

        private static bool AttrsEqual(Dictionary<string, IAttribute<T>> x, Dictionary<string, IAttribute<T>> y)
        {
            if (x.Count != y.Count)
            {
                return false;
            }

            foreach (var attr in x)
            {
                if (!y.TryGetValue(attr.Key, out var otherAttr) || !object.Equals(attr.Value, otherAttr))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
