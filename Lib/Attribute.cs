using System.Collections.Generic;

namespace Veauty
{
    /// <summary>
    /// A key-indexed collection of attributes attached to a virtual tree node.
    /// </summary>
    /// <typeparam name="T">The host object type the attributes apply to (e.g. <c>GameObject</c>).</typeparam>
    public class Attributes<T>
    {
        /// <summary>The attributes keyed by <see cref="IAttribute{T}.GetKey"/>.</summary>
        public Dictionary<string, IAttribute<T>> attrs = new Dictionary<string, IAttribute<T>>();

        /// <summary>
        /// Initializes the collection from a sequence of attributes.
        /// </summary>
        /// <param name="attrs">The attributes to index by key.</param>
        /// <remarks>
        /// When two attributes share the same key, the later one silently overwrites the
        /// earlier one; no exception is thrown.
        /// </remarks>
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

    /// <summary>
    /// An attribute that can be applied to a host object of type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The host object type the attribute applies to (e.g. <c>GameObject</c>).</typeparam>
    public interface IAttribute<T>
    {
        /// <summary>Gets the unique key identifying this attribute within a node.</summary>
        /// <returns>The attribute key used for diffing and deduplication.</returns>
        string GetKey();

        /// <summary>Applies this attribute to the host object.</summary>
        /// <param name="obj">The host object to mutate.</param>
        void Apply(T obj);
    }

    /// <summary>
    /// Base class for value-carrying attributes. Two attributes are considered equal when
    /// their keys match and their values are equal via <see cref="object.Equals(object, object)"/>;
    /// the diff algorithm relies on this to skip unchanged attributes.
    /// </summary>
    /// <typeparam name="T">The host object type the attribute applies to.</typeparam>
    /// <typeparam name="U">The type of the carried value.</typeparam>
    public abstract class Attribute<T, U> : IAttribute<T>
    {
        private readonly string key;
        private readonly U value;

        /// <summary>Initializes the attribute with its key and value.</summary>
        /// <param name="key">The unique attribute key.</param>
        /// <param name="value">The value to carry; exposed to subclasses via <see cref="GetValue"/>.</param>
        protected Attribute(string key, U value)
        {
            this.key = key;
            this.value = value;
        }

        /// <summary>Gets the unique key identifying this attribute within a node.</summary>
        /// <returns>The attribute key.</returns>
        public string GetKey() => this.key;

        /// <summary>Gets the value carried by this attribute.</summary>
        /// <returns>The value passed to the constructor.</returns>
        protected U GetValue() => this.value;

        /// <summary>Applies the carried value to the host object.</summary>
        /// <param name="obj">The host object to mutate.</param>
        public abstract void Apply(T obj);

        /// <summary>
        /// Determines equality by key and value. Used by <see cref="Diff{T}"/> to decide
        /// whether an <c>Attrs</c> patch is needed.
        /// </summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns><c>true</c> when <paramref name="obj"/> is an <see cref="Attribute{T,U}"/> with the same key and an equal value.</returns>
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
            return key == other.key && object.Equals(value, other.value);
        }

        /// <summary>Returns a hash code combining the key and value.</summary>
        /// <returns>The combined hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((key != null ? key.GetHashCode() : 0) * 397) ^ (value != null ? value.GetHashCode() : 0);
            }
        }
    }
}
