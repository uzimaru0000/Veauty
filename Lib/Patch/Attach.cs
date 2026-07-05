namespace Veauty.Patch
{
    /// <summary>
    /// Patch: the typed component of a node changed (see <see cref="Veauty.VTree.ITypedNode"/>) —
    /// remove <see cref="oldComponent"/> from the target and add <see cref="newComponent"/>.
    /// </summary>
    /// <typeparam name="T">The host object type (e.g. <c>GameObject</c>).</typeparam>
    public class Attach<T> : IPatch<T>
    {
        private T target;
        private readonly int index;
        /// <summary>The component type attached in the old tree.</summary>
        public readonly System.Type oldComponent;
        /// <summary>The component type required by the new tree.</summary>
        public readonly System.Type newComponent;

        /// <summary>Initializes an attach patch.</summary>
        /// <param name="index">The pre-order index of the node.</param>
        /// <param name="oldComponent">The component type to remove.</param>
        /// <param name="newComponent">The component type to add.</param>
        public Attach(int index, System.Type oldComponent, System.Type newComponent)
        {
            this.index = index;
            this.oldComponent = oldComponent;
            this.newComponent = newComponent;
            this.target = default(T);
        }

        /// <summary>Gets the host object this patch was bound to.</summary>
        /// <returns>The bound target, or <c>default</c> before binding.</returns>
        public T GetTarget() => this.target;

        /// <summary>Binds the patch to its host object.</summary>
        /// <param name="target">The host object whose component is swapped.</param>
        public void SetTarget(in T target) => this.target = target;

        /// <summary>Gets the pre-order index of the node.</summary>
        /// <returns>The zero-based pre-order index.</returns>
        public int GetIndex() => this.index;

        /// <summary>Determines structural equality with another object.</summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns><c>true</c> when index and both component types are equal.</returns>
        public override bool Equals(object obj) => this.Equals(obj as Attach<T>);

        /// <summary>Determines structural equality with another attach patch.</summary>
        /// <param name="obj">The patch to compare with.</param>
        /// <returns><c>true</c> when index and both component types are equal.</returns>
        public bool Equals(Attach<T> obj)
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


            return this.index == obj.index && this.oldComponent == obj.oldComponent && this.newComponent == obj.newComponent;
        }

        /// <summary>Returns a hash code combining index and component types.</summary>
        /// <returns>The combined hash code.</returns>
        public override int GetHashCode()
        {
            return new { index, oldComponent, newComponent }.GetHashCode();
        }
    }
}
