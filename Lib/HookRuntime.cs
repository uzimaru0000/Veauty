using System;
using System.Collections.Generic;
using System.Globalization;
using Veauty.VTree;

namespace Veauty
{
    public sealed class HookRuntime
    {
        [ThreadStatic] private static HookRuntime current;

        private readonly Action requestRender;
        private readonly Dictionary<string, ComponentInstance> instances = new Dictionary<string, ComponentInstance>();
        private int renderVersion;
        private ComponentInstance currentInstance;
        private int currentHookIndex;

        public HookRuntime(Action requestRender = null)
        {
            this.requestRender = requestRender ?? (() => {});
        }

        internal static HookRuntime Current
        {
            get
            {
                if (current == null)
                {
                    throw new InvalidOperationException("Hooks can only be used while rendering a function component.");
                }

                return current;
            }
        }

        public IVTree Resolve<T>(IVTree tree)
        {
            if (tree == null)
            {
                throw new ArgumentNullException(nameof(tree));
            }

            this.renderVersion++;
            return this.ResolveTree<T>(tree, RenderPath.Root);
        }

        public void CommitEffects()
        {
            var unmounted = new List<string>();

            foreach (var kv in this.instances)
            {
                var instance = kv.Value;
                if (instance.LastSeenVersion == this.renderVersion)
                {
                    CommitEffects(instance);
                }
                else
                {
                    CleanupEffects(instance);
                    unmounted.Add(kv.Key);
                }
            }

            foreach (var key in unmounted)
            {
                this.instances.Remove(key);
            }
        }

        public void Dispose()
        {
            foreach (var instance in this.instances.Values)
            {
                CleanupEffects(instance);
            }

            this.instances.Clear();
        }

        internal State<T> UseState<T>(T initialValue)
        {
            var slot = this.UseSlot(HookKind.State);
            if (!slot.Initialized)
            {
                slot.Value = initialValue;
                slot.ValueType = typeof(T);
                slot.Initialized = true;
            }
            else if (slot.ValueType != typeof(T))
            {
                throw new InvalidOperationException("Hook slot type changed between renders.");
            }

            return new State<T>(this, slot);
        }

        internal Ref<T> UseRef<T>(T initialValue)
        {
            var slot = this.UseSlot(HookKind.Ref);
            if (!slot.Initialized)
            {
                slot.Value = new Ref<T>(initialValue);
                slot.ValueType = typeof(T);
                slot.Initialized = true;
            }
            else if (slot.ValueType != typeof(T))
            {
                throw new InvalidOperationException("Hook slot type changed between renders.");
            }

            return (Ref<T>)slot.Value;
        }

        internal void UseEffect(Func<Action> effect, object[] dependencies)
        {
            if (effect == null)
            {
                throw new ArgumentNullException(nameof(effect));
            }

            var slot = this.UseSlot(HookKind.Effect);
            if (!slot.Initialized)
            {
                slot.Effect = new EffectState();
                slot.Initialized = true;
            }

            if (slot.Effect.ShouldRun(dependencies))
            {
                slot.Effect.NextEffect = effect;
                slot.Effect.NextDependencies = CopyDependencies(dependencies);
                slot.Effect.Pending = true;
            }
        }

        internal void SetState<T>(HookSlot slot, T value)
        {
            if (slot == null || slot.Kind != HookKind.State)
            {
                throw new InvalidOperationException("Invalid state hook slot.");
            }

            if (slot.ValueType != typeof(T))
            {
                throw new InvalidOperationException("Hook slot type changed between renders.");
            }

            slot.Value = value;
            this.requestRender();
        }

        private IVTree ResolveTree<T>(IVTree tree, RenderPath path)
        {
            if (tree is IVTreeWrapper wrapper)
            {
                return this.ResolveTree<T>(wrapper.Unwrap(), path);
            }

            switch (tree)
            {
                case FunctionComponentNode component:
                    return this.ResolveComponent<T>(component, path);
                case BaseNode<T> node:
                    return this.ResolveNode(node, path);
                case BaseKeyedNode<T> keyedNode:
                    return this.ResolveKeyedNode(keyedNode, path);
                default:
                    throw new ArgumentException($"Unsupported IVTree type for hook resolution: {tree.GetType()}");
            }
        }

        private IVTree ResolveComponent<T>(FunctionComponentNode component, RenderPath path)
        {
            var componentToken = GetComponentToken(component);
            var componentPath = component.key != null ? path.ReplaceLastWithKey(component.key) : path;
            var identityPath = componentPath.ChildComponent(componentToken);
            var identity = identityPath.ToString();

            if (!this.instances.TryGetValue(identity, out var instance))
            {
                instance = new ComponentInstance(identity);
                this.instances.Add(identity, instance);
            }

            instance.LastSeenVersion = this.renderVersion;

            var previousRuntime = current;
            var previousInstance = this.currentInstance;
            var previousHookIndex = this.currentHookIndex;

            current = this;
            this.currentInstance = instance;
            this.currentHookIndex = 0;

            try
            {
                var rendered = component.Render();
                if (rendered == null)
                {
                    throw new InvalidOperationException("Function components must return a non-null IVTree.");
                }

                return this.ResolveTree<T>(rendered, identityPath.ChildRenderedRoot());
            }
            finally
            {
                current = previousRuntime;
                this.currentInstance = previousInstance;
                this.currentHookIndex = previousHookIndex;
            }
        }

        private IVTree ResolveNode<T>(BaseNode<T> node, RenderPath path)
        {
            var sourceKids = node.GetKids();
            var kids = new IVTree[sourceKids.Length];
            for (var i = 0; i < sourceKids.Length; i++)
            {
                kids[i] = this.ResolveTree<T>(sourceKids[i], path.ChildIndex(i));
            }

            var attrs = GetAttributes(node.attrs);
            if (node is ITypedNode typedNode)
            {
                var lifecycles = GetLifecycles<T>(node);
                if (lifecycles.Length > 0)
                {
                    var lifecycleNodeType = typeof(HostLifecycleNode<,>).MakeGenericType(typeof(T), typedNode.GetComponentType());
                    return (IVTree)Activator.CreateInstance(lifecycleNodeType, new object[] { node.tag, attrs, lifecycles, kids });
                }

                var nodeType = typeof(Node<,>).MakeGenericType(typeof(T), typedNode.GetComponentType());
                return (IVTree)Activator.CreateInstance(nodeType, new object[] { node.tag, attrs, kids });
            }

            var nodeLifecycles = GetLifecycles<T>(node);
            if (nodeLifecycles.Length > 0)
            {
                return new HostLifecycleNode<T>(
                    node.tag,
                    attrs,
                    (IEnumerable<IHostLifecycle<T>>)nodeLifecycles,
                    (IEnumerable<IVTree>)kids
                );
            }

            return new Node<T>(node.tag, attrs, kids);
        }

        private IVTree ResolveKeyedNode<T>(BaseKeyedNode<T> node, RenderPath path)
        {
            var kids = new (string, IVTree)[node.kids.Length];
            for (var i = 0; i < node.kids.Length; i++)
            {
                var key = node.kids[i].Item1;
                kids[i] = (key, this.ResolveTree<T>(node.kids[i].Item2, path.ChildKey(key)));
            }

            var attrs = GetAttributes(node.attrs);
            if (node is ITypedNode typedNode)
            {
                var lifecycles = GetLifecycles<T>(node);
                if (lifecycles.Length > 0)
                {
                    var lifecycleNodeType = typeof(HostLifecycleKeyedNode<,>).MakeGenericType(typeof(T), typedNode.GetComponentType());
                    return (IVTree)Activator.CreateInstance(lifecycleNodeType, new object[] { node.tag, attrs, lifecycles, kids });
                }

                var nodeType = typeof(KeyedNode<,>).MakeGenericType(typeof(T), typedNode.GetComponentType());
                return (IVTree)Activator.CreateInstance(nodeType, new object[] { node.tag, attrs, kids });
            }

            var keyedLifecycles = GetLifecycles<T>(node);
            if (keyedLifecycles.Length > 0)
            {
                return new HostLifecycleKeyedNode<T>(
                    node.tag,
                    attrs,
                    (IEnumerable<IHostLifecycle<T>>)keyedLifecycles,
                    (IEnumerable<(string, IVTree)>)kids
                );
            }

            return new KeyedNode<T>(node.tag, attrs, kids);
        }

        private static IHostLifecycle<T>[] GetLifecycles<T>(IVTree tree)
        {
            return tree is IHostLifecycleTree<T> lifecycleTree
                ? lifecycleTree.GetHostLifecycles()
                : new IHostLifecycle<T>[] {};
        }

        private HookSlot UseSlot(HookKind kind)
        {
            if (this.currentInstance == null)
            {
                throw new InvalidOperationException("Hooks can only be used while rendering a function component.");
            }

            var hooks = this.currentInstance.Hooks;
            var index = this.currentHookIndex++;
            if (index == hooks.Count)
            {
                var slot = new HookSlot(kind);
                hooks.Add(slot);
                return slot;
            }

            var existing = hooks[index];
            if (existing.Kind != kind)
            {
                throw new InvalidOperationException("Hook order changed between renders.");
            }

            return existing;
        }

        private static IAttribute<T>[] GetAttributes<T>(Attributes<T> attrs)
        {
            var result = new IAttribute<T>[attrs.attrs.Count];
            attrs.attrs.Values.CopyTo(result, 0);
            return result;
        }

        private static string GetComponentToken(FunctionComponentNode component)
        {
            var method = component.component.Method;
            var targetType = component.component.Target != null
                ? component.component.Target.GetType().FullName
                : "<static>";
            return component.component.GetType().FullName + "|" +
                   method.Module.ModuleVersionId.ToString("N") + "|" +
                   method.MetadataToken.ToString(CultureInfo.InvariantCulture) + "|" +
                   targetType;
        }

        private static object[] CopyDependencies(object[] dependencies)
        {
            if (dependencies == null)
            {
                return null;
            }

            var result = new object[dependencies.Length];
            Array.Copy(dependencies, result, dependencies.Length);
            return result;
        }

        private static void CommitEffects(ComponentInstance instance)
        {
            foreach (var slot in instance.Hooks)
            {
                if (slot.Kind != HookKind.Effect || slot.Effect == null || !slot.Effect.Pending)
                {
                    continue;
                }

                slot.Effect.Cleanup?.Invoke();
                slot.Effect.Cleanup = slot.Effect.NextEffect();
                slot.Effect.Dependencies = slot.Effect.NextDependencies;
                slot.Effect.HasRun = true;
                slot.Effect.Pending = false;
                slot.Effect.NextEffect = null;
                slot.Effect.NextDependencies = null;
            }
        }

        private static void CleanupEffects(ComponentInstance instance)
        {
            foreach (var slot in instance.Hooks)
            {
                if (slot.Kind == HookKind.Effect && slot.Effect != null && slot.Effect.Cleanup != null)
                {
                    slot.Effect.Cleanup();
                    slot.Effect.Cleanup = null;
                }
            }
        }

        internal enum HookKind
        {
            State,
            Ref,
            Effect
        }

        internal sealed class HookSlot
        {
            public readonly HookKind Kind;
            public bool Initialized;
            public object Value;
            public Type ValueType;
            public EffectState Effect;

            public HookSlot(HookKind kind)
            {
                this.Kind = kind;
            }
        }

        private sealed class ComponentInstance
        {
            public readonly string Identity;
            public readonly List<HookSlot> Hooks = new List<HookSlot>();
            public int LastSeenVersion;

            public ComponentInstance(string identity)
            {
                this.Identity = identity;
            }
        }

        internal sealed class EffectState
        {
            public object[] Dependencies;
            public object[] NextDependencies;
            public Func<Action> NextEffect;
            public Action Cleanup;
            public bool HasRun;
            public bool Pending;

            public bool ShouldRun(object[] dependencies)
            {
                if (!this.HasRun)
                {
                    return true;
                }

                if (dependencies == null || this.Dependencies == null)
                {
                    return true;
                }

                if (dependencies.Length != this.Dependencies.Length)
                {
                    return true;
                }

                for (var i = 0; i < dependencies.Length; i++)
                {
                    if (!object.Equals(dependencies[i], this.Dependencies[i]))
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private sealed class RenderPath
        {
            public static readonly RenderPath Root = new RenderPath(new string[] {});
            private readonly string[] segments;

            private RenderPath(string[] segments)
            {
                this.segments = segments;
            }

            public RenderPath ChildIndex(int index)
                => this.Append(Segment("i", index.ToString(CultureInfo.InvariantCulture)));

            public RenderPath ChildKey(string key)
                => this.Append(Segment("k", key ?? ""));

            public RenderPath ChildComponent(string token)
                => this.Append(Segment("c", token ?? ""));

            public RenderPath ChildRenderedRoot()
                => this.Append(Segment("r", ""));

            public RenderPath ReplaceLastWithKey(string key)
            {
                if (this.segments.Length == 0)
                {
                    return this.ChildKey(key);
                }

                var next = new string[this.segments.Length];
                Array.Copy(this.segments, next, this.segments.Length);
                next[next.Length - 1] = Segment("k", key ?? "");
                return new RenderPath(next);
            }

            public override string ToString()
                => string.Join("/", this.segments);

            private RenderPath Append(string segment)
            {
                var next = new string[this.segments.Length + 1];
                Array.Copy(this.segments, next, this.segments.Length);
                next[next.Length - 1] = segment;
                return new RenderPath(next);
            }

            private static string Segment(string kind, string value)
                => kind + value.Length.ToString(CultureInfo.InvariantCulture) + ":" + value;
        }
    }
}
