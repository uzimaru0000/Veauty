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


            return this.index == obj.index && this.attrs.SequenceEqual(obj.attrs);
        }

        public override int GetHashCode()
        {
            return new { index, attrs }.GetHashCode();
        }
    }
}