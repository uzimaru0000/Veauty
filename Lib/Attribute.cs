using System.Collections.Generic;

namespace Veauty
{
    public class Attributes<T>
    {
        public Dictionary<string, IAttribute<T>> attrs = new Dictionary<string, IAttribute<T>>();

        public Attributes(IEnumerable<IAttribute<T>> attrs)
        {
            foreach (var attr in attrs)
            {
                if (!this.attrs.ContainsKey(attr.GetKey()))
                {
                    this.attrs.Add(attr.GetKey(), attr);
                }
                else
                {
                    this.attrs[attr.GetKey()] = attr;
                }
            }
        }
    }

    public interface IAttribute<T>
    {
        string GetKey();
        void Apply(T obj);
    }
    
    public abstract class Attribute<T, U> : IAttribute<T>
    {
        private readonly string key;
        private readonly U value;

        protected Attribute(string key, U value)
        {
            this.key = key;
            this.value = value;
        }

        public string GetKey() => this.key;

        protected U GetValue() => this.value;

        public abstract void Apply(T obj);

        public override bool Equals(object obj)
        {
            if (obj is Attribute<T, U> other)
            {
                return Equals(other);
            }

            return false;
        }

        private bool Equals(Attribute<T, U> other)
        {
            return key == other.key && value.Equals(other.value);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((key != null ? key.GetHashCode() : 0) * 397) ^ value.GetHashCode();
            }
        }
    }
}
