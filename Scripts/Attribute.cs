using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Veauty
{
    public enum AttributeType
    {
        Event,
        Props
    }

    public class Attributes
    {
        public Dictionary<AttributeType, Dictionary<string, IAttribute>> attrs = new Dictionary<AttributeType, Dictionary<string, IAttribute>>();

        public Attributes(IAttribute[] attrs)
        {
            foreach (var attr in attrs)
            {
                if (!this.attrs.ContainsKey(attr.GetType()))
                {
                    this.attrs.Add(attr.GetType(), new Dictionary<string, IAttribute>());
                }
                var dict = this.attrs[attr.GetType()];
                dict.Add(attr.GetKey(), attr);
            }
        }
    }

    public interface IAttribute
    {
        AttributeType GetType();
        string GetKey();
    }

    public struct Event<T> : IAttribute
    {
        public bool removed;
        public string key;
        public System.Action<T> handler;

        public Event(string key, System.Action<T> handler)
        {
            this.key = key;
            this.handler = handler;
            this.removed = false;
        }

        public AttributeType GetType() => AttributeType.Event;
        public string GetKey() => this.key;

    }

    public struct Props<T> : IAttribute
    {
        public string key;
        public T value;

        public Props(string key, T value)
        {
            this.key = key;
            this.value = value;
        }

        public AttributeType GetType() => AttributeType.Props;
        public string GetKey() => this.key;

    }
}
