using System;

namespace Veauty.VTree
{
    public delegate IVTree FunctionComponent();
    public delegate IVTree FunctionComponent<in TProps>(TProps props);

    public class FunctionComponentNode : IVTree
    {
        public readonly string key;
        public readonly Delegate component;
        public readonly object props;
        private readonly Func<IVTree> render;

        public FunctionComponentNode(FunctionComponent component)
            : this(null, component)
        {
        }

        public FunctionComponentNode(string key, FunctionComponent component)
            : this(key, component, null, () => component())
        {
        }

        public FunctionComponentNode(FunctionComponent component, string key)
            : this(key, component)
        {
        }

        public FunctionComponentNode(FunctionComponent<object> component, object props)
            : this(null, component, props, () => component(props))
        {
        }

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

        public VTreeType GetNodeType() => VTreeType.FunctionComponent;

        public int GetDescendantsCount() => 0;

        internal IVTree Render() => this.render();

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

    public static class FunctionComponents
    {
        public static FunctionComponentNode Create(FunctionComponent component)
            => new FunctionComponentNode(component);

        public static FunctionComponentNode Create(string key, FunctionComponent component)
            => new FunctionComponentNode(key, component);

        public static FunctionComponentNode Keyed(string key, FunctionComponent component)
            => new FunctionComponentNode(key, component);

        public static FunctionComponentNode Create<TProps>(FunctionComponent<TProps> component, TProps props)
            => new FunctionComponentNode(null, component, props, () => component(props));

        public static FunctionComponentNode Create<TProps>(string key, FunctionComponent<TProps> component, TProps props)
            => new FunctionComponentNode(key, component, props, () => component(props));

        public static FunctionComponentNode Keyed<TProps>(string key, FunctionComponent<TProps> component, TProps props)
            => new FunctionComponentNode(key, component, props, () => component(props));
    }
}
