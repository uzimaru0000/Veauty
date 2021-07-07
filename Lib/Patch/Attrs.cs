using System.Collections.Generic;

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
    }
}