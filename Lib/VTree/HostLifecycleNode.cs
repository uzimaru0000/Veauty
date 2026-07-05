using System;
using System.Collections.Generic;
using System.Linq;

namespace Veauty.VTree
{
    /// <summary>
    /// Imperative lifecycle callbacks invoked by the renderer against the host object of a
    /// node. This is the low-level replacement for the former <c>Widget&lt;T&gt;</c> type.
    /// </summary>
    /// <typeparam name="T">The host object type (e.g. <c>GameObject</c>).</typeparam>
    public interface IHostLifecycle<T>
    {
        /// <summary>Called after the host object is created, before attributes and children are applied.</summary>
        /// <param name="obj">The freshly created host object.</param>
        /// <returns>The host object to continue rendering with (may be <paramref name="obj"/> itself or a replacement).</returns>
        T Init(T obj);

        /// <summary>Called after all virtual children have been rendered under the host object.</summary>
        /// <param name="obj">The host object with its children in place.</param>
        void AfterRenderKids(T obj);

        /// <summary>Called before the host object is destroyed.</summary>
        /// <param name="obj">The host object being torn down.</param>
        void Destroy(T obj);
    }

    /// <summary>
    /// A virtual tree node that carries host lifecycle callbacks.
    /// </summary>
    /// <typeparam name="T">The host object type (e.g. <c>GameObject</c>).</typeparam>
    public interface IHostLifecycleTree<T>
    {
        /// <summary>Gets the lifecycle callbacks attached to this node.</summary>
        /// <returns>The lifecycles in registration order; never <c>null</c>.</returns>
        IHostLifecycle<T>[] GetHostLifecycles();
    }

    /// <summary>
    /// An untyped node with host lifecycle callbacks.
    /// </summary>
    /// <typeparam name="T">The host object type (e.g. <c>GameObject</c>).</typeparam>
    public class HostLifecycleNode<T> : Node<T>, IHostLifecycleTree<T>
    {
        private readonly IHostLifecycle<T>[] lifecycles;

        /// <summary>Initializes the node with lifecycle callbacks.</summary>
        /// <param name="tag">The node tag.</param>
        /// <param name="attrs">The attributes.</param>
        /// <param name="lifecycles">The lifecycle callbacks; <c>null</c> is treated as empty.</param>
        /// <param name="kids">The children in render order.</param>
        public HostLifecycleNode(
            string tag,
            IEnumerable<IAttribute<T>> attrs,
            IEnumerable<IHostLifecycle<T>> lifecycles,
            params IVTree[] kids
        ) : base(tag, attrs, kids)
        {
            this.lifecycles = ToArray(lifecycles);
        }

        /// <summary>Initializes the node with lifecycle callbacks from an enumerable of children.</summary>
        /// <param name="tag">The node tag.</param>
        /// <param name="attrs">The attributes.</param>
        /// <param name="lifecycles">The lifecycle callbacks; <c>null</c> is treated as empty.</param>
        /// <param name="kids">The children in render order.</param>
        public HostLifecycleNode(
            string tag,
            IEnumerable<IAttribute<T>> attrs,
            IEnumerable<IHostLifecycle<T>> lifecycles,
            IEnumerable<IVTree> kids
        ) : this(tag, attrs, lifecycles, kids.ToArray())
        {
        }

        /// <summary>Gets the lifecycle callbacks attached to this node.</summary>
        /// <returns>The lifecycles in registration order; never <c>null</c>.</returns>
        public IHostLifecycle<T>[] GetHostLifecycles() => this.lifecycles;

        private static IHostLifecycle<T>[] ToArray(IEnumerable<IHostLifecycle<T>> lifecycles)
        {
            return lifecycles == null ? new IHostLifecycle<T>[] {} : lifecycles.ToArray();
        }
    }

    /// <summary>
    /// A typed node (attaching component <typeparamref name="U"/>) with host lifecycle callbacks.
    /// </summary>
    /// <typeparam name="T">The host object type (e.g. <c>GameObject</c>).</typeparam>
    /// <typeparam name="U">The host component type to attach.</typeparam>
    public class HostLifecycleNode<T, U> : Node<T, U>, IHostLifecycleTree<T>
    {
        private readonly IHostLifecycle<T>[] lifecycles;

        /// <summary>Initializes the node with lifecycle callbacks.</summary>
        /// <param name="tag">The node tag.</param>
        /// <param name="attrs">The attributes.</param>
        /// <param name="lifecycles">The lifecycle callbacks; <c>null</c> is treated as empty.</param>
        /// <param name="kids">The children in render order.</param>
        public HostLifecycleNode(
            string tag,
            IEnumerable<IAttribute<T>> attrs,
            IEnumerable<IHostLifecycle<T>> lifecycles,
            params IVTree[] kids
        ) : base(tag, attrs, kids)
        {
            this.lifecycles = ToArray(lifecycles);
        }

        /// <summary>Initializes the node with lifecycle callbacks from an enumerable of children.</summary>
        /// <param name="tag">The node tag.</param>
        /// <param name="attrs">The attributes.</param>
        /// <param name="lifecycles">The lifecycle callbacks; <c>null</c> is treated as empty.</param>
        /// <param name="kids">The children in render order.</param>
        public HostLifecycleNode(
            string tag,
            IEnumerable<IAttribute<T>> attrs,
            IEnumerable<IHostLifecycle<T>> lifecycles,
            IEnumerable<IVTree> kids
        ) : this(tag, attrs, lifecycles, kids.ToArray())
        {
        }

        /// <summary>Gets the lifecycle callbacks attached to this node.</summary>
        /// <returns>The lifecycles in registration order; never <c>null</c>.</returns>
        public IHostLifecycle<T>[] GetHostLifecycles() => this.lifecycles;

        private static IHostLifecycle<T>[] ToArray(IEnumerable<IHostLifecycle<T>> lifecycles)
        {
            return lifecycles == null ? new IHostLifecycle<T>[] {} : lifecycles.ToArray();
        }
    }

    /// <summary>
    /// An untyped keyed node with host lifecycle callbacks.
    /// </summary>
    /// <typeparam name="T">The host object type (e.g. <c>GameObject</c>).</typeparam>
    public class HostLifecycleKeyedNode<T> : KeyedNode<T>, IHostLifecycleTree<T>
    {
        private readonly IHostLifecycle<T>[] lifecycles;

        /// <summary>Initializes the keyed node with lifecycle callbacks.</summary>
        /// <param name="tag">The node tag.</param>
        /// <param name="attrs">The attributes.</param>
        /// <param name="lifecycles">The lifecycle callbacks; <c>null</c> is treated as empty.</param>
        /// <param name="kids">The (key, child) pairs in render order.</param>
        public HostLifecycleKeyedNode(
            string tag,
            IEnumerable<IAttribute<T>> attrs,
            IEnumerable<IHostLifecycle<T>> lifecycles,
            params (string, IVTree)[] kids
        ) : base(tag, attrs.ToArray(), kids)
        {
            this.lifecycles = ToArray(lifecycles);
        }

        /// <summary>Initializes the keyed node with lifecycle callbacks from an enumerable of (key, child) pairs.</summary>
        /// <param name="tag">The node tag.</param>
        /// <param name="attrs">The attributes.</param>
        /// <param name="lifecycles">The lifecycle callbacks; <c>null</c> is treated as empty.</param>
        /// <param name="kids">The (key, child) pairs in render order.</param>
        public HostLifecycleKeyedNode(
            string tag,
            IEnumerable<IAttribute<T>> attrs,
            IEnumerable<IHostLifecycle<T>> lifecycles,
            IEnumerable<(string, IVTree)> kids
        ) : this(tag, attrs, lifecycles, kids.ToArray())
        {
        }

        /// <summary>Gets the lifecycle callbacks attached to this node.</summary>
        /// <returns>The lifecycles in registration order; never <c>null</c>.</returns>
        public IHostLifecycle<T>[] GetHostLifecycles() => this.lifecycles;

        private static IHostLifecycle<T>[] ToArray(IEnumerable<IHostLifecycle<T>> lifecycles)
        {
            return lifecycles == null ? new IHostLifecycle<T>[] {} : lifecycles.ToArray();
        }
    }

    /// <summary>
    /// A typed keyed node (attaching component <typeparamref name="U"/>) with host lifecycle callbacks.
    /// </summary>
    /// <typeparam name="T">The host object type (e.g. <c>GameObject</c>).</typeparam>
    /// <typeparam name="U">The host component type to attach.</typeparam>
    public class HostLifecycleKeyedNode<T, U> : KeyedNode<T, U>, IHostLifecycleTree<T>
    {
        private readonly IHostLifecycle<T>[] lifecycles;

        /// <summary>Initializes the keyed node with lifecycle callbacks.</summary>
        /// <param name="tag">The node tag.</param>
        /// <param name="attrs">The attributes.</param>
        /// <param name="lifecycles">The lifecycle callbacks; <c>null</c> is treated as empty.</param>
        /// <param name="kids">The (key, child) pairs in render order.</param>
        public HostLifecycleKeyedNode(
            string tag,
            IEnumerable<IAttribute<T>> attrs,
            IEnumerable<IHostLifecycle<T>> lifecycles,
            params (string, IVTree)[] kids
        ) : base(tag, attrs, kids)
        {
            this.lifecycles = ToArray(lifecycles);
        }

        /// <summary>Initializes the keyed node with lifecycle callbacks from an enumerable of (key, child) pairs.</summary>
        /// <param name="tag">The node tag.</param>
        /// <param name="attrs">The attributes.</param>
        /// <param name="lifecycles">The lifecycle callbacks; <c>null</c> is treated as empty.</param>
        /// <param name="kids">The (key, child) pairs in render order.</param>
        public HostLifecycleKeyedNode(
            string tag,
            IEnumerable<IAttribute<T>> attrs,
            IEnumerable<IHostLifecycle<T>> lifecycles,
            IEnumerable<(string, IVTree)> kids
        ) : this(tag, attrs, lifecycles, kids.ToArray())
        {
        }

        /// <summary>Gets the lifecycle callbacks attached to this node.</summary>
        /// <returns>The lifecycles in registration order; never <c>null</c>.</returns>
        public IHostLifecycle<T>[] GetHostLifecycles() => this.lifecycles;

        private static IHostLifecycle<T>[] ToArray(IEnumerable<IHostLifecycle<T>> lifecycles)
        {
            return lifecycles == null ? new IHostLifecycle<T>[] {} : lifecycles.ToArray();
        }
    }
}
