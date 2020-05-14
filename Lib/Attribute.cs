using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Veauty
{
    public class Attributes
    {
        public Dictionary<string, IAttribute> attrs = new Dictionary<string, IAttribute>();

        public Attributes(IAttribute[] attrs)
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
        bool Equals(IAttribute attr);
    }

}
