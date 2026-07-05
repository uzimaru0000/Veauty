using System.Collections.Generic;
using System.Linq;

namespace Veauty.Patch
{
    /// <summary>
    /// Patch: one or more attributes changed on a node. The dictionary maps attribute keys to
    /// the new attribute, or to <c>null</c> when the attribute was removed in the new tree.
    /// </summary>
    /// <typeparam name="T">The host object type (e.g. <c>GameObject</c>).</typeparam>
    public class Attrs<T> : IPatch<T>
    {

        /// <summary>The pre-order index of the node.</summary>
        public int index;
        /// <summary>The host object this patch was bound to.</summary>
        public T target;

        /// <summary>The attribute changes: key to new attribute, or key to <c>null</c> for removals.</summary>
        public Dictionary<string, IAttribute<T>> attrs;

        /// <summary>Initializes an attribute patch.</summary>
        /// <param name="index">The pre-order index of the node.</param>
        /// <param name="attrs">The attribute changes (<c>null</c> value = removal).</param>
        public Attrs(int index, Dictionary<string, IAttribute<T>> attrs)
        {
            this.index = index;
            this.attrs = attrs;
            this.target = default(T);
        }

        /// <summary>Gets the host object this patch was bound to.</summary>
        /// <returns>The bound target, or <c>default</c> before binding.</returns>
        public T GetTarget() => this.target;
        /// <summary>Binds the patch to its host object.</summary>
        /// <param name="target">The host object whose attributes are updated.</param>
        public void SetTarget(in T target) => this.target = target;
        /// <summary>Gets the pre-order index of the node.</summary>
        /// <returns>The zero-based pre-order index.</returns>
        public int GetIndex() => this.index;

        /// <summary>Determines structural equality with another object.</summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns><c>true</c> when index and attribute changes are equal.</returns>
        public override bool Equals(object obj) => this.Equals(obj as Attrs<T>);

        /// <summary>Determines structural equality with another attribute patch.</summary>
        /// <param name="obj">The patch to compare with.</param>
        /// <returns><c>true</c> when index and attribute changes are equal.</returns>
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

        /// <summary>Returns an order-independent hash of the attribute changes combined with the index.</summary>
        /// <returns>The combined hash code.</returns>
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
