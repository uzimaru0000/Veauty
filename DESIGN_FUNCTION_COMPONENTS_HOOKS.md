# Function Components and Hooks Design

This note describes a possible redesign of Veauty's component model around
function components, hooks, and a lower-level host bridge for Unity
GameObject/uGUI integration.

## Goals

- Make `FunctionComponent` the public component abstraction users interact with.
- Allow user code to use hooks such as `useState`, `useRef`, and `useEffect`.
- Keep low-level Unity `GameObject` lifecycle control available to library code.
- Bridge low-level host primitives into the same tree model as function
  components.
- Remove the current `Widget.Render()`-during-diff model, because it conflicts
  with hook execution semantics.
- Do not preserve backwards compatibility with the current `Widget<T>` API.

## Current Problem

The current `Widget<T>` type mixes two different responsibilities:

- User-defined reusable component abstraction.
- Host lifecycle abstraction for Unity/uGUI primitives:
  `Init`, `AfterRenderKids`, `Destroy`, and sub-component filtering.

This is workable for stateless widgets, but it is a poor fit for hooks. Hooks
need a stable render context: the current component instance, its hook slot
index, and the persisted hook state for that component identity.

The current diff implementation renders widgets while calculating patches:

```text
DiffWidget(oldWidget, newWidget)
  oldTree = oldWidget.Render()
  newTree = newWidget.Render()
  Helper(oldTree, newTree)
```

If hooks are added to `Widget.Render()`, this design becomes ambiguous:

- Rendering the old widget during diff may read the new hook state.
- Hook slot order and component identity become tied to a diff traversal that
  should remain pure.
- Effects and cleanup become hard to schedule because rendering is no longer a
  single controlled phase.

The core fix is to stop rendering components inside diff. Diff should compare
already resolved host trees.

## Proposed Architecture

Split the current `Widget` responsibilities into two layers:

```text
FunctionComponent
  Public user component model.
  Can use hooks.
  Returns IVTree.

HostBridge
  Internal renderer/native component model.
  Can directly create and mutate Unity GameObjects.
  Owns mount, prop updates, after-children work, and unmount cleanup.
```

The render pipeline becomes:

```text
Root FunctionComponent tree
  -> HookRuntime.Resolve()
  -> resolved host tree
  -> Diff.Calc(oldResolvedTree, newResolvedTree)
  -> Patch.Apply(...)
  -> HookRuntime.CommitEffects()
```

`Diff.Calc` should only see host nodes, keyed host nodes, and possibly inert
data nodes. It should not execute user component functions.

## Public User API

Users write function components and hooks:

```csharp
static IVTree Counter()
{
    var count = Hooks.UseState(0);

    return V.Column()[
        V.Text($"count: {count.Value}"),
        V.Button(onClick: () => count.Set(x => x + 1))[
            V.Text("Increment")
        ]
    ];
}
```

Potential component construction forms:

```csharp
V.Component(Counter)
V.Component("counter", Counter)
V.Component(props => Counter(props), props)
V.KeyedComponent(key, props => TodoItem(props), props)
```

The key requirement is that component identity must be stable across renders.
For unkeyed children, identity can be based on parent identity + child index +
component function. For keyed children, identity should use the explicit key.

## Core VTree Model

Suggested core node categories:

```text
IVTree
  FunctionComponentNode
  HostNode
  KeyedHostNode
```

`FunctionComponentNode` is unresolved and is only consumed by `HookRuntime`.

`HostNode` is the tree node diff/patch operates on. It represents a Unity host
object or primitive and may carry:

- Host type id.
- Props.
- Children.
- Optional host bridge reference.

`KeyedHostNode` is the keyed variant for list diffing.

The current `Node<T>` and `KeyedNode<T>` concepts can evolve into `HostNode`
and `KeyedHostNode`. The current `Widget<T>` concept should be removed or
replaced by the two-layer model above.

## Host Bridge

`HostBridge` is the low-level API that lets Veauty packages implement Unity and
uGUI primitives while still exposing them as function-component-like factories.

Example shape:

```csharp
public interface IHostBridge<TProps>
{
    string TypeId { get; }

    UnityEngine.GameObject Mount(HostContext context, TProps props);
    void ApplyProps(UnityEngine.GameObject go, TProps oldProps, TProps newProps);
    void AfterChildren(UnityEngine.GameObject go, TProps props);
    void Unmount(UnityEngine.GameObject go, TProps props);
}
```

`HostContext` can carry renderer-specific information:

- Whether the tree is uGUI.
- Parent transform information.
- Helpers for creating internal children.
- Scheduling hooks/effects if needed by bridge code.

For simple GameObject nodes, the bridge can:

- Create the GameObject.
- Attach a component type.
- Apply attributes/props.
- Destroy or reset properties on unmount.

For uGUI primitives, the bridge replaces current `GUIBase<T>` and initializer
partials:

- `Mount`: create root GameObject, attach `RectTransform`, component, and
  required internal children.
- `ApplyProps`: update generated attributes/properties/events.
- `AfterChildren`: wire references that depend on child objects, such as
  `ScrollRect.content`.
- `Unmount`: remove event subscriptions or custom resources.

## Bridging uGUI Primitives

Public uGUI factories should look like normal component factories:

```csharp
V.Button(onClick: () => count.Set(x => x + 1))[
    V.Text("Increment")
]

V.Slider(value: value.Value, onValueChanged: value.Set)
```

Internally, they return host nodes:

```csharp
public static IVTree Button(ButtonProps props, params IVTree[] kids)
    => Host.Unity<ButtonBridge, ButtonProps>(props, kids);
```

Complex uGUI controls can still support explicit parts, but parts should be
handled as host configuration rather than VTree children that diff sees.

Example:

```csharp
V.Slider(value: 0.5f)[
    SliderParts.Fill(color: Color.red),
    SliderParts.Handle()
]
```

During host resolution, bridge-owned parts are separated from normal content
children. Diff only sees content children.

## Hook Runtime

The hook runtime owns component render state:

```text
ComponentInstance
  identity
  hookSlots[]
  pendingEffects[]
  cleanupEffects[]
  previousDeps[]
```

During `Resolve()`:

1. Enter component instance.
2. Reset hook index to zero.
3. Execute function component.
4. Resolve the returned tree.
5. Exit component instance.

`UseState<T>` stores a value in the current component's hook slot:

```csharp
public readonly struct State<T>
{
    public T Value { get; }
    public void Set(T value);
    public void Set(Func<T, T> update);
}
```

The setter updates the slot and requests a root render:

```text
state.Set(...)
  -> slot.Value = nextValue
  -> scheduler.RequestRender()
```

`UseRef<T>` stores a stable mutable reference without requesting render.

`UseEffect` stores post-patch effects and dependency values:

```csharp
Hooks.UseEffect(() =>
{
    source.Changed += OnChanged;
    return () => source.Changed -= OnChanged;
}, source);
```

Effects should run after `Patch.Apply`. Cleanup should run before re-running an
effect whose dependencies changed, and when the component instance unmounts.

## Scheduler

The initial implementation can use synchronous rendering to match the current
behavior:

```text
RequestRender()
  -> Resolve root
  -> Diff old/new resolved host trees
  -> Apply patches
  -> Commit effects
```

Later, updates can be batched:

- Multiple hook setters in one frame schedule a single render.
- Rendering can be deferred to Unity's update loop.
- Effects still run after patch commit.

The synchronous version is simpler and is a good first milestone.

## Diff and Patch Changes

Core diff should be simplified so it does not know how to render function
components.

Required changes:

- Remove `DiffWidget`.
- Ensure the diff input is a resolved host tree.
- Keep node/keyed-node diff behavior mostly intact.
- Compare host type ids and props/attributes.
- Emit redraw when host type changes.
- Emit prop patches when props differ.
- Emit reorder patches for keyed children.

Patch application should delegate host lifecycle operations to bridges:

- Redraw: call old bridge `Unmount`, destroy old GameObject, call new bridge
  `Mount`.
- Prop patch: call bridge `ApplyProps`.
- Append: call bridge `Mount` for new host children.
- Remove: call bridge `Unmount`, then destroy.
- Reorder: move existing GameObjects and patch moved children.
- After children: call bridge `AfterChildren` after child operations that may
  affect references.

Unmount must also notify `HookRuntime` so component effects are cleaned up for
removed component identities.

## Renderer Flow

Initial mount:

```text
rootComponent
  -> HookRuntime.Resolve(rootComponent)
  -> Renderer.Render(resolvedHostTree)
  -> CommitEffects()
```

Update:

```text
hook setter or external ForceUpdate
  -> HookRuntime.Resolve(rootComponent)
  -> Diff.Calc(oldResolvedHostTree, newResolvedHostTree)
  -> Patch.Apply(rootGameObject, oldResolvedHostTree, patches)
  -> HookRuntime.CommitEffects()
  -> oldResolvedHostTree = newResolvedHostTree
```

Unmount:

```text
VeautyObject.Dispose()
  -> HostBridge.Unmount for host subtree
  -> HookRuntime.CleanupUnmounted(root identity)
  -> destroy root GameObject
```

## Package Impact

### com.uzimaru.veauty

Owns:

- `IVTree`
- `FunctionComponentNode`
- `HostNode`
- `KeyedHostNode`
- `HookRuntime`
- `Hooks`
- diff and patch data types

This package should stay renderer-independent. It should not reference
`UnityEngine.GameObject` directly except through generic abstractions already
used by the existing patch types.

### com.uzimaru.veauty-gameobject

Owns:

- GameObject renderer.
- GameObject host bridge base types.
- Patch application for Unity GameObjects.
- Root `VeautyObject` integration with the hook scheduler.

The current root `State` generic can be removed or made optional. Root state can
be expressed with `Hooks.UseState`.

### com.uzimaru.veauty-ugui

Owns:

- uGUI host bridges.
- Public `V.*` factories.
- Generated prop/attribute application code.
- Complex control host setup for Slider, Toggle, Scrollbar, InputField,
  ScrollRect, Dropdown, etc.

Generated files should generate prop application or bridge helpers rather than
`Widget` partial classes.

## Migration Plan

### Phase 1: Introduce resolved host tree

- Add host node types alongside current nodes.
- Add a resolver that can pass through host nodes.
- Update `VeautyObject` to store `oldResolvedTree`.
- Keep behavior equivalent to current `Node` rendering.

### Phase 2: Add FunctionComponent and hooks runtime

- Add `FunctionComponentNode`.
- Implement component identity calculation.
- Implement `UseState` and synchronous `RequestRender`.
- Add focused tests for state persistence and rerendering.

### Phase 3: Remove Widget rendering from diff

- Change diff entry points to require resolved host trees.
- Remove `DiffWidget`.
- Add tests that prove component functions are not called during diff.

### Phase 4: Introduce HostBridge

- Implement GameObject host bridge.
- Move `Renderer.Render` lifecycle work into bridge mount/apply/unmount calls.
- Ensure patch operations call bridge lifecycle methods.

### Phase 5: Port uGUI primitives

- Convert `GUIBase<T>` and initializer partials into bridges.
- Keep public `V.*` factories function-component-like.
- Preserve sub-component/part ergonomics through bridge-owned configuration.

### Phase 6: Add effects

- Implement `UseEffect`.
- Run effects after patch commit.
- Run cleanup on dependency changes and unmount.
- Add tests for event subscription cleanup.

### Phase 7: Remove old Widget API

- Delete `Widget<T>`.
- Delete widget-specific renderer/diff/patch branches.
- Update README examples and package docs.

## Testing Strategy

Core tests:

- `UseState` initializes once and persists across renders.
- Multiple hooks preserve slot order.
- State setter triggers one update.
- Keyed function components preserve state during reorder.
- Removed components run effect cleanup.
- Component functions are not invoked by diff.

GameObject tests:

- Initial mount creates expected GameObject tree.
- Hook state update patches existing GameObjects instead of full redraw.
- Removing a subtree calls host unmount and destroys GameObjects.
- Reordering keyed children preserves GameObject instances.

uGUI tests:

- Button click can call hook setter.
- Slider/Toggle/InputField bridges create required internal children.
- Bridge-owned internal children are skipped when mapping VTree children to
  Unity child indices.
- Event attributes replace old listeners without leaking subscriptions.

## Open Questions

- Should the public type be named `FunctionComponent`, `Component`, or just be
  a delegate accepted by `V.Component`?
- Should render scheduling be synchronous in v1, or frame-batched from the
  start?
- How explicit should keys be for component identity in unkeyed lists?
- Should `UseEffect` dependencies use `object.Equals`, reference equality, or a
  custom comparer?
- Should host bridges be class instances, static singletons, or value structs?
- How should bridge-owned parts be represented so they are ergonomic but never
  accidentally diffed as normal children?

## Recommended First Implementation Slice

Start with the smallest vertical slice:

1. Add `FunctionComponentNode`.
2. Add `HookRuntime` with only `UseState`.
3. Resolve function components into the existing `Node<GameObject>` tree.
4. Store `oldResolvedTree` in `VeautyObject`.
5. Keep the existing renderer/patcher for host nodes.
6. Add one runtime test where a button click updates hook state and patches text.

This proves the user-facing model before rewriting every uGUI primitive. After
that works, move `Widget` lifecycle into `HostBridge` and remove widget rendering
from diff.
