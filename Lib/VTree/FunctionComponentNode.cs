using System;

namespace Veauty.VTree
{
    /// <summary>A parameterless function component: renders to a virtual tree.</summary>
    /// <returns>The rendered tree; must not be <c>null</c>.</returns>
    public delegate IVTree FunctionComponent();

    /// <summary>A function component that receives props.</summary>
    /// <typeparam name="TProps">The props type.</typeparam>
    /// <param name="props">The props passed at creation time.</param>
    /// <returns>The rendered tree; must not be <c>null</c>.</returns>
    public delegate IVTree FunctionComponent<in TProps>(TProps props);

    /// <summary>
    /// A virtual tree placeholder for a function component. It is not renderable by itself:
    /// <see cref="HookRuntime.Resolve{T}(IVTree)"/> must expand it into concrete nodes before
    /// <see cref="Diff{T}.Calc(IVTree, IVTree)"/> is called — diffing an unresolved
    /// component throws <see cref="InvalidOperationException"/>.
    /// </summary>
    /// <remarks>
    /// Usually produced by a <c>[Component]</c>-annotated method or by
    /// <see cref="FunctionComponents"/> rather than constructed directly. Equality compares
    /// <see cref="key"/>, <see cref="component"/> and <see cref="props"/>, so re-creating a node
    /// with the same delegate and props compares equal across renders.
    /// </remarks>
    public class FunctionComponentNode : IVTree
    {
        /// <summary>Optional key that pins the component's identity when its position among siblings changes; <c>null</c> means positional identity.</summary>
        public readonly string key;
        /// <summary>The component delegate; its method and target type contribute to the component's identity token.</summary>
        public readonly Delegate component;
        /// <summary>The props captured at creation time, or <c>null</c> for parameterless components.</summary>
        public readonly object props;
        private readonly Func<IVTree> render;

        /// <summary>Creates a component node without a key.</summary>
        /// <param name="component">The component delegate.</param>
        /// <exception cref="ArgumentNullException"><paramref name="component"/> is <c>null</c>.</exception>
        public FunctionComponentNode(FunctionComponent component)
            : this(null, component)
        {
        }

        /// <summary>Creates a keyed component node.</summary>
        /// <param name="key">The identity key, or <c>null</c>.</param>
        /// <param name="component">The component delegate.</param>
        /// <exception cref="ArgumentNullException"><paramref name="component"/> is <c>null</c>.</exception>
        public FunctionComponentNode(string key, FunctionComponent component)
            : this(key, component, null, () => component())
        {
        }

        /// <summary>Creates a keyed component node (argument-order convenience overload).</summary>
        /// <param name="component">The component delegate.</param>
        /// <param name="key">The identity key, or <c>null</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="component"/> is <c>null</c>.</exception>
        public FunctionComponentNode(FunctionComponent component, string key)
            : this(key, component)
        {
        }

        /// <summary>Creates a component node with untyped props.</summary>
        /// <param name="component">The component delegate receiving the props.</param>
        /// <param name="props">The props to pass on every render.</param>
        /// <exception cref="ArgumentNullException"><paramref name="component"/> is <c>null</c>.</exception>
        public FunctionComponentNode(FunctionComponent<object> component, object props)
            : this(null, component, props, () => component(props))
        {
        }

        /// <summary>Creates a keyed component node with untyped props.</summary>
        /// <param name="key">The identity key, or <c>null</c>.</param>
        /// <param name="component">The component delegate receiving the props.</param>
        /// <param name="props">The props to pass on every render.</param>
        /// <exception cref="ArgumentNullException"><paramref name="component"/> is <c>null</c>.</exception>
        public FunctionComponentNode(string key, FunctionComponent<object> component, object props)
            : this(key, component, props, () => component(props))
        {
        }

        internal FunctionComponentNode(string key, Delegate component, object props, Func<IVTree> render)
        {
            if (component == null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            if (render == null)
            {
                throw new ArgumentNullException(nameof(render));
            }

            this.key = key;
            this.component = component;
            this.props = props;
            this.render = render;
        }

        /// <summary>Gets the kind of this node: <see cref="VTreeType.FunctionComponent"/>.</summary>
        /// <returns><see cref="VTreeType.FunctionComponent"/>.</returns>
        public VTreeType GetNodeType() => VTreeType.FunctionComponent;

        /// <summary>
        /// Always returns 0: a component placeholder contributes no descendants to the
        /// pre-order index until it is resolved into its rendered output.
        /// </summary>
        /// <returns>0.</returns>
        public int GetDescendantsCount() => 0;

        internal IVTree Render() => this.render();

        /// <summary>Determines equality by key, component delegate and props.</summary>
        /// <param name="obj">The object to compare with.</param>
        /// <returns><c>true</c> when key, delegate and props are all equal.</returns>
        public override bool Equals(object obj) => this.Equals(obj as FunctionComponentNode);

        private bool Equals(FunctionComponentNode obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (object.ReferenceEquals(this, obj))
            {
                return true;
            }

            return this.key == obj.key &&
                   object.Equals(this.component, obj.component) &&
                   object.Equals(this.props, obj.props);
        }

        /// <summary>Returns a hash code combining key, component delegate and props.</summary>
        /// <returns>The combined hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                hash = (hash * 397) ^ (this.key != null ? this.key.GetHashCode() : 0);
                hash = (hash * 397) ^ (this.component != null ? this.component.GetHashCode() : 0);
                hash = (hash * 397) ^ (this.props != null ? this.props.GetHashCode() : 0);
                return hash;
            }
        }
    }

    /// <summary>
    /// Factory methods for creating <see cref="FunctionComponentNode"/>s. Methods annotated
    /// with <see cref="ComponentAttribute"/> are rewritten at compile time to call
    /// <see cref="Create(FunctionComponent)"/>, so most user code never calls these directly.
    /// </summary>
    public static class FunctionComponents
    {
        /// <summary>Creates a component node without a key.</summary>
        /// <param name="component">The component delegate.</param>
        /// <returns>A new <see cref="FunctionComponentNode"/>.</returns>
        public static FunctionComponentNode Create(FunctionComponent component)
            => new FunctionComponentNode(component);

        /// <summary>Creates a keyed component node.</summary>
        /// <param name="key">The identity key that keeps hook state attached when siblings reorder.</param>
        /// <param name="component">The component delegate.</param>
        /// <returns>A new <see cref="FunctionComponentNode"/>.</returns>
        public static FunctionComponentNode Create(string key, FunctionComponent component)
            => new FunctionComponentNode(key, component);

        /// <summary>Creates a keyed component node (alias of <see cref="Create(string, FunctionComponent)"/>).</summary>
        /// <param name="key">The identity key.</param>
        /// <param name="component">The component delegate.</param>
        /// <returns>A new <see cref="FunctionComponentNode"/>.</returns>
        public static FunctionComponentNode Keyed(string key, FunctionComponent component)
            => new FunctionComponentNode(key, component);

        /// <summary>Creates a component node with typed props.</summary>
        /// <typeparam name="TProps">The props type.</typeparam>
        /// <param name="component">The component delegate receiving the props.</param>
        /// <param name="props">The props to pass on every render.</param>
        /// <returns>A new <see cref="FunctionComponentNode"/>.</returns>
        public static FunctionComponentNode Create<TProps>(FunctionComponent<TProps> component, TProps props)
            => new FunctionComponentNode(null, component, props, () => component(props));

        /// <summary>Creates a keyed component node with typed props.</summary>
        /// <typeparam name="TProps">The props type.</typeparam>
        /// <param name="key">The identity key.</param>
        /// <param name="component">The component delegate receiving the props.</param>
        /// <param name="props">The props to pass on every render.</param>
        /// <returns>A new <see cref="FunctionComponentNode"/>.</returns>
        public static FunctionComponentNode Create<TProps>(string key, FunctionComponent<TProps> component, TProps props)
            => new FunctionComponentNode(key, component, props, () => component(props));

        /// <summary>Creates a keyed component node with typed props (alias of <see cref="Create{TProps}(string, FunctionComponent{TProps}, TProps)"/>).</summary>
        /// <typeparam name="TProps">The props type.</typeparam>
        /// <param name="key">The identity key.</param>
        /// <param name="component">The component delegate receiving the props.</param>
        /// <param name="props">The props to pass on every render.</param>
        /// <returns>A new <see cref="FunctionComponentNode"/>.</returns>
        public static FunctionComponentNode Keyed<TProps>(string key, FunctionComponent<TProps> component, TProps props)
            => new FunctionComponentNode(key, component, props, () => component(props));
    }
}
