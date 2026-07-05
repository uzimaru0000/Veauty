# API reference

[日本語](ja/api-reference.md)

Every public type in the `Veauty` assembly (`com.uzimaru.veauty` 0.5.0), grouped by namespace. `T` is always the host object type (e.g. `GameObject`); `U` is a host component type.

## Namespace `Veauty`

### enum `VTreeType`

```csharp
public enum VTreeType { Node, KeyedNode, FunctionComponent }
```

Discriminates node kinds for the diff. `FunctionComponent` nodes are not diffable — they must be resolved first.

### interface `IVTree`

```csharp
public interface IVTree
{
    VTreeType GetNodeType();
    int GetDescendantsCount();
}
```

| Member | Description |
|--------|-------------|
| `GetNodeType()` | The node kind discriminator. |
| `GetDescendantsCount()` | Total transitive child count; defines the pre-order index contract shared by diff and patch (see [Diffing](diffing.md)). `FunctionComponentNode` returns 0. |

### interface `IParent`

```csharp
public interface IParent
{
    IVTree[] GetKids();
}
```

Children in render order. Keyed nodes return de-keyed children.

### interface `IVTreeWrapper`

```csharp
public interface IVTreeWrapper : IVTree
{
    IVTree Unwrap();
}
```

A node that delegates to another node. `Diff.Calc` and `HookRuntime.Resolve` unwrap these transparently.

### class `Attributes<T>`

```csharp
public class Attributes<T>
{
    public Dictionary<string, IAttribute<T>> attrs;
    public Attributes(IEnumerable<IAttribute<T>> attrs);
}
```

Key-indexed attribute set. **Gotcha:** duplicate keys are collapsed silently — the later attribute wins.

### interface `IAttribute<T>`

```csharp
public interface IAttribute<T>
{
    string GetKey();
    void Apply(T obj);
}
```

### abstract class `Attribute<T, U>` : `IAttribute<T>`

```csharp
public abstract class Attribute<T, U> : IAttribute<T>
{
    protected Attribute(string key, U value);
    public string GetKey();
    protected U GetValue();
    public abstract void Apply(T obj);
    public override bool Equals(object obj);   // key == key && object.Equals(value, value)
    public override int GetHashCode();
}
```

Value-carrying attribute base. Equality (key + `object.Equals` on value) is what lets the diff skip unchanged attributes — carry values with meaningful `Equals`, or the attribute will be re-applied every render.

### sealed class `ComponentAttribute` : `System.Attribute`

```csharp
[AttributeUsage(AttributeTargets.Method)]
public sealed class ComponentAttribute : Attribute { }
```

Marks a **static** method returning `IVTree` as a function component. Rewritten (and stripped) at compile time by the bundled ILPostProcessor — see [Function components](function-components.md).

### static class `Hooks`

```csharp
public static class Hooks
{
    public static State<T> UseState<T>(T initialValue);
    public static Ref<T>   UseRef<T>(T initialValue = default);
    public static void UseEffect(Action effect);                                   // every render
    public static void UseEffect(Action effect, params object[] dependencies);     // on deps change
    public static void UseEffect(Func<Action> effect);                             // every render, with cleanup
    public static void UseEffect(Func<Action> effect, params object[] dependencies);
}
```

| Member | Description |
|--------|-------------|
| `UseState` | State slot; initial value stored on first render only. |
| `UseRef` | Stable mutable `Ref<T>` box; mutation never re-renders. |
| `UseEffect` | Stages an effect executed in `HookRuntime.CommitEffects()`. Deps compared element-wise with `object.Equals`; empty array = mount only; overloads without deps = every render. `Func<Action>` overloads return a cleanup. |

Throws `InvalidOperationException` when called outside a function-component render. Full semantics: [Hooks](hooks.md).

### readonly struct `State<T>`

```csharp
public readonly struct State<T>
{
    public T Value { get; }
    public void Set(T value);
    public void Set(Func<T, T> update);   // throws ArgumentNullException if update is null
}
```

Handle to a state slot. `Set` writes synchronously and always invokes the runtime's `requestRender` callback (no equality check, no batching).

### sealed class `Ref<T>`

```csharp
public sealed class Ref<T>
{
    public Ref(T current);
    public T Current { get; set; }
}
```

### sealed class `HookRuntime`

```csharp
public sealed class HookRuntime
{
    public HookRuntime(Action requestRender = null);
    public IVTree Resolve<T>(IVTree tree);
    public void CommitEffects();
    public void Dispose();
}
```

| Member | Description |
|--------|-------------|
| ctor | `requestRender` fires whenever `State<T>.Set` is called; defaults to a no-op. |
| `Resolve<T>` | Expands every `FunctionComponentNode` into concrete nodes, running hooks. Both trees passed to `Diff.Calc` must be resolved. Throws `ArgumentNullException` (null tree), `InvalidOperationException` (null component render / hook rule violations), `ArgumentException` (unknown node type). |
| `CommitEffects` | Runs staged effects (previous cleanup first), then cleans up and removes instances unmounted by the last `Resolve`. Call after applying patches. |
| `Dispose` | Runs every stored cleanup and clears all instances. |

Remarks: the current runtime used by `Hooks` is `[ThreadStatic]` and only set during component render. One runtime per mounted tree.

### static class `Diff<T>`

```csharp
public static class Diff<T>
{
    public static IPatch<T>[] Calc(IVTree x, IVTree y);
}
```

Diffs old tree `x` against new tree `y`; returns patches ordered by pre-order index. Throws `InvalidOperationException` if either tree contains a `FunctionComponentNode`. Algorithm details: [Diffing](diffing.md).

### interface `IPatch<T>`

```csharp
public interface IPatch<T>
{
    T GetTarget();
    void SetTarget(in T target);
    int GetIndex();
}
```

A host-tree mutation addressed by pre-order index into the old tree. The applicator binds the real object with `SetTarget` before applying.

### class `Entry`

```csharp
public class Entry
{
    public enum Type { Move, Remove, Insert }
    public Type tag;
    public IVTree vTree;
    public int index;
    public object data;
    public Entry(Type tag, IVTree vTree, int index);
}
```

Keyed-diff bookkeeping. A key seen as both removed and inserted is promoted to `Move`, which makes the applicator re-parent the existing host object.

## Namespace `Veauty.VTree`

### interface `ITypedNode`

```csharp
public interface ITypedNode
{
    System.Type GetComponentType();
}
```

Implemented by `Node<T,U>` / `KeyedNode<T,U>`. A change of component type between renders emits an `Attach<T>` patch.

### abstract class `NodeBase<T>` : `IVTree`, `IParent`

```csharp
public abstract class NodeBase<T> : IVTree, IParent
{
    public readonly string tag;
    public readonly Attributes<T> attrs;
    protected NodeBase(string tag, IEnumerable<IAttribute<T>> attrs);
    public virtual VTreeType GetNodeType();          // VTreeType.Node
    public abstract int GetDescendantsCount();
    public abstract IVTree[] GetKids();
    // protected helpers: AttributesEqual, CombineHash, GetAttributesHashCode, GetKidsHashCode
}
```

### class `BaseNode<T>` : `NodeBase<T>`

```csharp
public class BaseNode<T> : NodeBase<T>
{
    public readonly IVTree[] kids;
    protected BaseNode(string tag, IEnumerable<IAttribute<T>> attrs, params IVTree[] kids);
    protected BaseNode(string tag, IEnumerable<IAttribute<T>> attrs, IEnumerable<IVTree> kids);
    public override int GetDescendantsCount();       // precomputed
    public override IVTree[] GetKids();
    public override bool Equals(object obj);         // structural
    public override int GetHashCode();
}
```

Plain node; children diffed by position. Descendant count is precomputed in the constructor.

### class `Node<T>` : `BaseNode<T>` / class `Node<T, U>` : `BaseNode<T>`, `ITypedNode`

```csharp
public class Node<T> : BaseNode<T>
{
    public Node(string tag, IEnumerable<IAttribute<T>> attrs, params IVTree[] kids);
    public Node(string tag, IEnumerable<IAttribute<T>> attrs, IEnumerable<IVTree> kids);
}

public class Node<T, U> : BaseNode<T>, ITypedNode
{
    public Node(string tag, IEnumerable<IAttribute<T>> attrs, params IVTree[] kids);
    public Node(string tag, IEnumerable<IAttribute<T>> attrs, IEnumerable<IVTree> kids);
    public Type GetComponentType();                  // typeof(U)
}
```

### class `BaseKeyedNode<T>` : `NodeBase<T>`

```csharp
public class BaseKeyedNode<T> : NodeBase<T>
{
    public readonly (string, IVTree)[] kids;
    protected BaseKeyedNode(string tag, IEnumerable<IAttribute<T>> attrs, params (string, IVTree)[] kids);
    protected BaseKeyedNode(string tag, IEnumerable<IAttribute<T>> attrs, IEnumerable<(string, IVTree)> kids);
    public override VTreeType GetNodeType();         // VTreeType.KeyedNode
    public override int GetDescendantsCount();
    public override IVTree[] GetKids();              // de-keyed children
    public override bool Equals(object obj);
    public override int GetHashCode();
}
```

Keyed node; children diffed by key. Keys must be stable and unique within a parent (see [Diffing](diffing.md)).

### class `KeyedNode<T>` / class `KeyedNode<T, U>`

```csharp
public class KeyedNode<T> : BaseKeyedNode<T>
{
    public KeyedNode(string tag, IAttribute<T>[] attrs, params (string, IVTree)[] kids);
}

public class KeyedNode<T, U> : BaseKeyedNode<T>, ITypedNode
{
    public KeyedNode(string tag, IEnumerable<IAttribute<T>> attrs, params (string, IVTree)[] kids);
    public KeyedNode(string tag, IEnumerable<IAttribute<T>> attrs, IEnumerable<(string, IVTree)> kids);
    public Type GetComponentType();
}
```

Note: `KeyedNode<T>`'s only constructor takes `IAttribute<T>[]` (an array), unlike the other node types which accept `IEnumerable`.

### interface `IHostLifecycle<T>`

```csharp
public interface IHostLifecycle<T>
{
    T Init(T obj);                 // after host creation, before attrs/kids
    void AfterRenderKids(T obj);   // after all children rendered
    void Destroy(T obj);           // before host destruction
}
```

Imperative per-node lifecycle callbacks (the replacement for the removed `Widget<T>`). Invoked by the renderer package.

### interface `IHostLifecycleTree<T>`

```csharp
public interface IHostLifecycleTree<T>
{
    IHostLifecycle<T>[] GetHostLifecycles();
}
```

### classes `HostLifecycleNode<T>` / `HostLifecycleNode<T, U>` / `HostLifecycleKeyedNode<T>` / `HostLifecycleKeyedNode<T, U>`

```csharp
public class HostLifecycleNode<T> : Node<T>, IHostLifecycleTree<T>
{
    public HostLifecycleNode(string tag, IEnumerable<IAttribute<T>> attrs,
        IEnumerable<IHostLifecycle<T>> lifecycles, params IVTree[] kids);
    public HostLifecycleNode(string tag, IEnumerable<IAttribute<T>> attrs,
        IEnumerable<IHostLifecycle<T>> lifecycles, IEnumerable<IVTree> kids);
    public IHostLifecycle<T>[] GetHostLifecycles();
}
// HostLifecycleNode<T,U> : Node<T,U>; HostLifecycleKeyedNode<T> : KeyedNode<T>;
// HostLifecycleKeyedNode<T,U> : KeyedNode<T,U> — same shape, keyed variants take (string, IVTree) kids.
```

A `null` lifecycles argument is treated as empty. `HookRuntime.Resolve` preserves these nodes (rebuilding them around resolved children).

### delegates `FunctionComponent` / `FunctionComponent<TProps>`

```csharp
public delegate IVTree FunctionComponent();
public delegate IVTree FunctionComponent<in TProps>(TProps props);
```

### class `FunctionComponentNode` : `IVTree`

```csharp
public class FunctionComponentNode : IVTree
{
    public readonly string key;
    public readonly Delegate component;
    public readonly object props;

    public FunctionComponentNode(FunctionComponent component);
    public FunctionComponentNode(string key, FunctionComponent component);
    public FunctionComponentNode(FunctionComponent component, string key);
    public FunctionComponentNode(FunctionComponent<object> component, object props);
    public FunctionComponentNode(string key, FunctionComponent<object> component, object props);

    public VTreeType GetNodeType();       // VTreeType.FunctionComponent
    public int GetDescendantsCount();     // always 0
    public override bool Equals(object obj);   // key + component + props
    public override int GetHashCode();
}
```

Placeholder node; must be expanded by `HookRuntime.Resolve` before diffing. Constructors throw `ArgumentNullException` for a null component.

### static class `FunctionComponents`

```csharp
public static class FunctionComponents
{
    public static FunctionComponentNode Create(FunctionComponent component);
    public static FunctionComponentNode Create(string key, FunctionComponent component);
    public static FunctionComponentNode Keyed(string key, FunctionComponent component);
    public static FunctionComponentNode Create<TProps>(FunctionComponent<TProps> component, TProps props);
    public static FunctionComponentNode Create<TProps>(string key, FunctionComponent<TProps> component, TProps props);
    public static FunctionComponentNode Keyed<TProps>(string key, FunctionComponent<TProps> component, TProps props);
}
```

`Keyed` is an alias of the keyed `Create` overloads. `[Component]`-rewritten methods call the key-less `Create`.

## Namespace `Veauty.Patch`

All patch classes implement `IPatch<T>` (`GetTarget` / `SetTarget` / `GetIndex`) and structural `Equals` / `GetHashCode`; those members are omitted below.

### class `Redraw<T>`

```csharp
public class Redraw<T> : IPatch<T>
{
    public int index;
    public IVTree vTree;    // replacement subtree
    public T target;
    public Redraw(int index, IVTree vTree);
}
```

Replace the node entirely (emitted on node-type or tag mismatch).

### class `Attach<T>`

```csharp
public class Attach<T> : IPatch<T>
{
    public readonly System.Type oldComponent;
    public readonly System.Type newComponent;
    public Attach(int index, System.Type oldComponent, System.Type newComponent);
}
```

Swap the typed component (`Node<T,U>`'s `U` changed).

### class `Attrs<T>`

```csharp
public class Attrs<T> : IPatch<T>
{
    public int index;
    public T target;
    public Dictionary<string, IAttribute<T>> attrs;  // value null = attribute removed
    public Attrs(int index, Dictionary<string, IAttribute<T>> attrs);
}
```

### class `Append<T>`

```csharp
public class Append<T> : IPatch<T>
{
    public readonly int length;     // old child count; new kids start here
    public readonly IVTree[] kids;  // FULL new child list
    public Append(int index, int length, IVTree[] kids);
}
```

### class `RemoveLast<T>`

```csharp
public class RemoveLast<T> : IPatch<T>
{
    public readonly int length;  // new (remaining) child count
    public readonly int diff;    // number of trailing children to remove
    public RemoveLast(int index, int length, int diff);
}
```

### class `Remove<T>`

```csharp
public class Remove<T> : IPatch<T>
{
    public IPatch<T>[] patches;  // non-null when part of a keyed move
    public Entry entry;          // non-null when part of a keyed move
    public Remove(int index, IPatch<T>[] patches = null, Entry entry = null);
}
```

Plain removal of a keyed child, or the "detach" half of a move when `entry`/`patches` are set.

### class `Reorder<T>` (+ nested `Reorder<T>.Insert`)

```csharp
public class Reorder<T> : IPatch<T>
{
    public readonly IPatch<T>[] patches;    // sub-patches incl. Remove
    public readonly Insert[] inserts;       // positional inserts/moves
    public readonly Insert[] endInserts;    // trailing inserts (entry.index == -1)
    public Reorder(int index, IPatch<T>[] patches, Insert[] inserts, Insert[] endInserts);

    public class Insert
    {
        public int index;    // position in the new child list
        public Entry entry;  // what to insert; Entry.Type.Move re-parents an existing object
    }
}
```

One per keyed parent whose children changed. See [Diffing](diffing.md) for the matching heuristics.
