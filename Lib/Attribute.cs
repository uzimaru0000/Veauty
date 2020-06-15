using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Veauty
{
    public class Attributes
    {
        public Dictionary<string, IAttribute> attrs = new Dictionary<string, IAttribute>();

        public Attributes(IEnumerable<IAttribute> attrs)
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

    public interface IAttribute
    {
        string GetKey();
        void Apply(GameObject obj);
    }
    
    public abstract class Attribute<T> : IAttribute
    {
        private readonly string key;
        private readonly T value;

        protected Attribute(string key, T value)
        {
            this.key = key;
            this.value = value;
        }

        public string GetKey() => this.key;

        protected T GetValue() => this.value;

        public abstract void Apply(GameObject obj);

        public override bool Equals(object obj)
        {
            if (obj is Attribute<T> other)
            {
                return Equals(other);
            }

            return false;
        }

        private bool Equals(Attribute<T> other)
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
