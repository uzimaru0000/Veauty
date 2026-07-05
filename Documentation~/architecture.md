# Architecture

[日本語](ja/architecture.md)

How the core package turns declarative tree descriptions into an ordered list of host-tree mutations.

## The cycle

The core participates in a four-phase cycle. Phases 1, 2 and 4 live in this package; phase 3 (applying patches to real objects) is the renderer's job (`com.uzimaru.veauty-gameobject`).

```
        user code                          this package                        renderer package
 ┌──────────────────────┐        ┌─────────────────────────────┐        ┌──────────────────────────┐
 │ [Component] methods,  │        │ 1. HookRuntime.Resolve<T>   │        │                          │
 │ Node/KeyedNode trees  ├───────►│    expand function comps,   │        │                          │
 └──────────────────────┘        │    run hooks, stage effects │        │                          │
                                 │              │              │        │                          │
                                 │              ▼              │        │                          │
             old resolved tree ─►│ 2. Diff<T>.Calc(old, new)   ├───────►│ 3. Patch.Apply(root,     │
                                 │    → IPatch<T>[]            │        │      oldTree, patches)   │
                                 │                             │        │              │           │
                                 │ 4. HookRuntime.CommitEffects│◄───────┤              ▼           │
                                 │    run effects & cleanups   │        │    updated host objects  │
                                 └─────────────────────────────┘        └──────────────────────────┘
                                        ▲
                                        │  State<T>.Set(...)  →  requestRender callback
                                        └───────────────── triggers the next iteration ─┘
```

### 1. Resolve

`HookRuntime.Resolve<T>(tree)` walks the tree and returns a new tree in which every `FunctionComponentNode` has been replaced by whatever its render function produced:

- `IVTreeWrapper`s are unwrapped.
- For a `FunctionComponentNode`, the runtime computes an **identity** from the node's *render path* (sequence of child indices / keys from the root) plus a *component token* (delegate type + module MVID + method metadata token + target type). The identity maps to a `ComponentInstance` holding the hook slots. A `key` on the node replaces the positional segment, so keyed components keep their state when siblings reorder.
- While the component function runs, a `[ThreadStatic]` "current runtime" pointer is set (and restored in `finally`), which is what makes `Hooks.*` work — and why hooks throw outside of render.
- Plain / keyed nodes are **rebuilt** (children resolved recursively), preserving the typed component (`Node<T,U>`) and any `IHostLifecycle<T>` list via reflection-instantiated `HostLifecycleNode` / `HostLifecycleKeyedNode` variants.
- Each `Resolve` call bumps an internal `renderVersion`; every visited instance records it. Instances *not* visited are unmounted: the next `CommitEffects()` runs their cleanups and drops them.

### 2. Diff

`Diff<T>.Calc(old, new)` compares two **resolved** trees and returns `IPatch<T>[]`. Any remaining `FunctionComponentNode` throws `InvalidOperationException`. Nodes are compared by node type, then tag, then typed component, then attributes, then children (positionally for `Node`, by key for `KeyedNode`). Details in [Diffing](diffing.md).

### 3. Patch (renderer package)

Every patch carries a **pre-order index** into the *old* tree. The applicator walks the real hierarchy with the same numbering — defined by `IVTree.GetDescendantsCount()` — binds each patch to its host object with `SetTarget`, then performs the mutations. Because indices refer to the old tree, patches must be computed and applied against matching snapshots.

### 4. Commit effects

`HookRuntime.CommitEffects()` runs after patching:

- For each still-mounted component instance, every effect staged during resolve first invokes its previous cleanup, then runs and stores the new cleanup.
- Instances whose `LastSeenVersion` is stale run **all** their cleanups and are removed (unmount).
- `Dispose()` does the cleanup pass for everything (full teardown).

## State updates

`State<T>.Set(value)` (or `Set(func)`) writes the slot synchronously and invokes the `requestRender` callback passed to the `HookRuntime` constructor. The core does not schedule anything itself — the host decides when to run the next resolve/diff/patch/commit iteration. Note that `Set` does not compare old and new values; every call requests a render.

## Type map

| Layer | Types | File |
|-------|-------|------|
| Tree contract | `IVTree`, `IParent`, `IVTreeWrapper`, `VTreeType` | `Lib/IVTree.cs` |
| Plain nodes | `NodeBase<T>`, `BaseNode<T>`, `Node<T>`, `Node<T,U>`, `ITypedNode` | `Lib/VTree/Node.cs` |
| Keyed nodes | `BaseKeyedNode<T>`, `KeyedNode<T>`, `KeyedNode<T,U>` | `Lib/VTree/Node.cs` |
| Lifecycle nodes | `IHostLifecycle<T>`, `IHostLifecycleTree<T>`, `HostLifecycleNode<...>`, `HostLifecycleKeyedNode<...>` | `Lib/VTree/HostLifecycleNode.cs` |
| Function components | `FunctionComponentNode`, `FunctionComponents`, `FunctionComponent` delegates | `Lib/VTree/FunctionComponentNode.cs` |
| Component marker | `ComponentAttribute` | `Lib/ComponentAttribute.cs` |
| Hooks | `Hooks`, `State<T>`, `Ref<T>` | `Lib/Hooks.cs` |
| Hook engine | `HookRuntime` | `Lib/HookRuntime.cs` |
| Attributes | `Attributes<T>`, `IAttribute<T>`, `Attribute<T,U>` | `Lib/Attribute.cs` |
| Diff | `Diff<T>` | `Lib/Diff.cs` |
| Patches | `IPatch<T>`, `Entry`, `Append`, `Attach`, `Attrs`, `Redraw`, `Remove`, `RemoveLast`, `Reorder` | `Lib/IPatch.cs`, `Lib/Patch/*.cs` |
| Compile-time codegen | `ComponentILPostProcessor` (prebuilt `CodeGen/Veauty.CodeGen.dll`, source in `SourceGenerator~/`) | see [Function components](function-components.md) |

## Design notes

- **Immutability** — nodes are constructed once; `tag`, `attrs`, `kids` are readonly and the descendant count is precomputed in the constructor. Updates always build a new tree.
- **Structural equality** — nodes, attributes and patches override `Equals`/`GetHashCode` structurally. The diff uses attribute equality to skip unchanged attributes, and tests compare whole patch lists by value.
- **Host-lifecycle nodes replace `Widget<T>`** — imperative setup (`Init` / `AfterRenderKids` / `Destroy`) attaches to a node instead of being a node type of its own, so it composes with both plain and keyed, typed and untyped nodes.

See also: [Hooks](hooks.md), [Function components](function-components.md), [Diffing](diffing.md), [API reference](api-reference.md).
