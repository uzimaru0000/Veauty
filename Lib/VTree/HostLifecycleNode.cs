using System;
using System.Collections.Generic;
using System.Linq;

namespace Veauty.VTree
{
    public interface IHostLifecycle<T>
    {
        T Init(T obj);
        void AfterRenderKids(T obj);
        void Destroy(T obj);
    }

    public interface IHostLifecycleTree<T>
    {
        IHostLifecycle<T>[] GetHostLifecycles();
    }

    public class HostLifecycleNode<T> : Node<T>, IHostLifecycleTree<T>
    {
        private readonly IHostLifecycle<T>[] lifecycles;

        public HostLifecycleNode(
            string tag,
            IEnumerable<IAttribute<T>> attrs,
            IEnumerable<IHostLifecycle<T>> lifecycles,
            params IVTree[] kids
        ) : base(tag, attrs, kids)
        {
            this.lifecycles = ToArray(lifecycles);
        }

        public HostLifecycleNode(
            string tag,
            IEnumerable<IAttribute<T>> attrs,
            IEnumerable<IHostLifecycle<T>> lifecycles,
            IEnumerable<IVTree> kids
        ) : this(tag, attrs, lifecycles, kids.ToArray())
        {
        }

        public IHostLifecycle<T>[] GetHostLifecycles() => this.lifecycles;

        private static IHostLifecycle<T>[] ToArray(IEnumerable<IHostLifecycle<T>> lifecycles)
        {
            return lifecycles == null ? new IHostLifecycle<T>[] {} : lifecycles.ToArray();
        }
    }

    public class HostLifecycleNode<T, U> : Node<T, U>, IHostLifecycleTree<T>
    {
        private readonly IHostLifecycle<T>[] lifecycles;

        public HostLifecycleNode(
            string tag,
            IEnumerable<IAttribute<T>> attrs,
            IEnumerable<IHostLifecycle<T>> lifecycles,
            params IVTree[] kids
        ) : base(tag, attrs, kids)
        {
            this.lifecycles = ToArray(lifecycles);
        }

        public HostLifecycleNode(
            string tag,
            IEnumerable<IAttribute<T>> attrs,
            IEnumerable<IHostLifecycle<T>> lifecycles,
            IEnumerable<IVTree> kids
        ) : this(tag, attrs, lifecycles, kids.ToArray())
        {
        }

        public IHostLifecycle<T>[] GetHostLifecycles() => this.lifecycles;

        private static IHostLifecycle<T>[] ToArray(IEnumerable<IHostLifecycle<T>> lifecycles)
        {
            return lifecycles == null ? new IHostLifecycle<T>[] {} : lifecycles.ToArray();
        }
    }

    public class HostLifecycleKeyedNode<T> : KeyedNode<T>, IHostLifecycleTree<T>
    {
        private readonly IHostLifecycle<T>[] lifecycles;

        public HostLifecycleKeyedNode(
            string tag,
            IEnumerable<IAttribute<T>> attrs,
            IEnumerable<IHostLifecycle<T>> lifecycles,
            params (string, IVTree)[] kids
        ) : base(tag, attrs.ToArray(), kids)
        {
            this.lifecycles = ToArray(lifecycles);
        }

        public HostLifecycleKeyedNode(
            string tag,
            IEnumerable<IAttribute<T>> attrs,
            IEnumerable<IHostLifecycle<T>> lifecycles,
            IEnumerable<(string, IVTree)> kids
        ) : this(tag, attrs, lifecycles, kids.ToArray())
        {
        }

        public IHostLifecycle<T>[] GetHostLifecycles() => this.lifecycles;

        private static IHostLifecycle<T>[] ToArray(IEnumerable<IHostLifecycle<T>> lifecycles)
        {
            return lifecycles == null ? new IHostLifecycle<T>[] {} : lifecycles.ToArray();
        }
    }

    public class HostLifecycleKeyedNode<T, U> : KeyedNode<T, U>, IHostLifecycleTree<T>
    {
        private readonly IHostLifecycle<T>[] lifecycles;

        public HostLifecycleKeyedNode(
            string tag,
            IEnumerable<IAttribute<T>> attrs,
            IEnumerable<IHostLifecycle<T>> lifecycles,
            params (string, IVTree)[] kids
        ) : base(tag, attrs, kids)
        {
            this.lifecycles = ToArray(lifecycles);
        }

        public HostLifecycleKeyedNode(
            string tag,
            IEnumerable<IAttribute<T>> attrs,
            IEnumerable<IHostLifecycle<T>> lifecycles,
            IEnumerable<(string, IVTree)> kids
        ) : this(tag, attrs, lifecycles, kids.ToArray())
        {
        }

        public IHostLifecycle<T>[] GetHostLifecycles() => this.lifecycles;

        private static IHostLifecycle<T>[] ToArray(IEnumerable<IHostLifecycle<T>> lifecycles)
        {
            return lifecycles == null ? new IHostLifecycle<T>[] {} : lifecycles.ToArray();
        }
    }
}
