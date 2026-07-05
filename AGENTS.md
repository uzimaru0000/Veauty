# Veauty (core) — Agent Guide

`com.uzimaru.veauty` is the renderer-agnostic core of the Veauty Virtual DOM stack for Unity: virtual tree types (`Node`, `KeyedNode`, host-lifecycle nodes), the diff algorithm (`Diff<T>.Calc` → `IPatch<T>[]`), the hooks engine (`HookRuntime`, `Hooks`), and the `[Component]` function-component system rewritten at compile time by a bundled ILPostProcessor. It never touches `GameObject`s — rendering/patch application live in `com.uzimaru.veauty-gameobject`, uGUI widgets in `com.uzimaru.veauty-ugui`.

## Package map

| Path | Role |
|------|------|
| `Lib/IVTree.cs` | `IVTree`, `IParent`, `IVTreeWrapper`, `VTreeType` |
| `Lib/VTree/Node.cs` | `NodeBase/BaseNode/Node<T>/Node<T,U>`, `BaseKeyedNode/KeyedNode<T>/KeyedNode<T,U>`, `ITypedNode` |
| `Lib/VTree/HostLifecycleNode.cs` | `IHostLifecycle<T>`, `IHostLifecycleTree<T>`, 4 lifecycle node variants (replacement for removed `Widget<T>`) |
| `Lib/VTree/FunctionComponentNode.cs` | `FunctionComponentNode`, `FunctionComponents` factory, `FunctionComponent` delegates |
| `Lib/Attribute.cs` | `Attributes<T>`, `IAttribute<T>`, `Attribute<T,U>` |
| `Lib/ComponentAttribute.cs` | `[Component]` marker attribute |
| `Lib/Hooks.cs` | `Hooks`, `State<T>`, `Ref<T>` |
| `Lib/HookRuntime.cs` | `HookRuntime` — component instances, hook slots, effect commit |
| `Lib/Diff.cs` | `Diff<T>.Calc` and keyed diff internals |
| `Lib/IPatch.cs` | `IPatch<T>`, `Entry` (keyed-diff bookkeeping) |
| `Lib/Patch/*.cs` | `Append`, `Attach`, `Attrs`, `Redraw`, `Remove`, `RemoveLast`, `Reorder` |
| `CodeGen/Veauty.CodeGen.dll` | Prebuilt ILPostProcessor plugin (Editor-enabled) that rewrites `[Component]` methods |
| `SourceGenerator~/` | Source of the DLL. `ComponentILPostProcessor.cs` is ACTIVE; `ComponentGenerator.cs` (Roslyn) is EXCLUDED from the build |
| `Tests/Editor/`, `Tests/Runtime/` | NUnit tests (`Veauty.Editor.Tests`, `Veauty.Runtime.Tests`) |
| `Documentation~/` | Manual (English + `ja/`) |

## Public API surface

```csharp
using Veauty;         // Hooks, State<T>, Ref<T>, HookRuntime, Diff<T>, IPatch<T>,
                      // IVTree, IAttribute<T>, Attribute<T,U>, Attributes<T>, ComponentAttribute, Entry
using Veauty.VTree;   // Node<T>, Node<T,U>, KeyedNode<T>, KeyedNode<T,U>, ITypedNode,
                      // FunctionComponentNode, FunctionComponents, FunctionComponent,
                      // IHostLifecycle<T>, HostLifecycleNode<...>, HostLifecycleKeyedNode<...>
using Veauty.Patch;   // Append<T>, Attach<T>, Attrs<T>, Redraw<T>, Remove<T>, RemoveLast<T>, Reorder<T>
```

Primary entry points: `Hooks.*` inside components, `HookRuntime.Resolve<T>` / `CommitEffects` / `Dispose`, `Diff<T>.Calc`, `FunctionComponents.Create/Keyed`, node constructors.

## Usage rules

1. Hooks (`Hooks.UseState/UseRef/UseEffect`) may only be called while a function component renders inside `HookRuntime.Resolve<T>()`; anywhere else throws `InvalidOperationException`.
2. Call hooks unconditionally, in the same order, with the same slot value types on every render — order/kind/type changes throw at runtime.
3. Never pass a tree containing a `FunctionComponentNode` to `Diff<T>.Calc`; resolve BOTH trees with the same `HookRuntime` instance first.
4. `[Component]` methods must be `static`, should return `IVTree`, and must not be generic. Do not rely on the attribute at runtime — the IL rewrite strips it.
5. Call `CommitEffects()` after applying patches (never before), and `Dispose()` when unmounting the tree for good.
6. Give keyed children stable, unique keys; never index-derived keys and never duplicates (duplicates are silently disambiguated with a `"UniVDOM"` suffix and degrade into remove/insert).
7. Use one `HookRuntime` per mounted tree; the `requestRender` callback you pass is invoked synchronously by every `State<T>.Set`.
8. Attribute values must have meaningful `Equals`; otherwise the diff emits an `Attrs` patch every render.
9. Do not edit `CodeGen/Veauty.CodeGen.dll` by hand; rebuild it from `SourceGenerator~/` (see below). `SourceGenerator~/ComponentGenerator.cs` is dead code — don't "fix" it expecting behavior changes.
10. Nodes are immutable; to change UI, build a new tree and diff — never mutate `kids`/`attrs` arrays of an already-diffed tree.

## Common patterns

Manual mount/update cycle (what `VeautyObject` automates):

```csharp
HookRuntime runtime = null; IVTree current = null;
runtime = new HookRuntime(() => {
    var next = runtime.Resolve<GameObject>(App());
    var patches = Diff<GameObject>.Calc(current, next);
    // Patch.Apply(root, current, patches);   // renderer package
    current = next;
    runtime.CommitEffects();
});
current = runtime.Resolve<GameObject>(App());
// Renderer.Render(current) ...
runtime.CommitEffects();
```

State update inside a component:

```csharp
[Component]
static IVTree Counter()
{
    var count = Hooks.UseState(0);
    // count.Set(count.Value + 1) or count.Set(x => x + 1) from an event handler
    return new Node<GameObject>($"count:{count.Value}", new IAttribute<GameObject>[] { });
}
```

Effect with cleanup, re-run when a prop changes:

```csharp
Hooks.UseEffect(() => {
    var sub = Subscribe(id);
    return () => sub.Dispose();
}, new object[] { id });
```

List rendering with keys (keyed node + keyed components):

```csharp
new KeyedNode<GameObject>("list", new IAttribute<GameObject>[] { },
    items.Select(item =>
        (item.Id, (IVTree)FunctionComponents.Keyed(item.Id, () => Row(item)))
    ).ToArray());
```

Typed node attaching a Unity component:

```csharp
new Node<GameObject, UnityEngine.UI.Image>("icon", attrs); // component-type change => Attach patch
```

## Pitfalls

- **`UseEffect(fn, null)` is NOT "every render".** The `params` overload coerces an explicit `null` deps array to an EMPTY array → mount-only. Wrong: `Hooks.UseEffect(fn, null)` expecting every render. Right: `Hooks.UseEffect(fn)` (no deps argument).
- **`State<T>.Set` has no equality check.** Setting the same value still triggers `requestRender`; guard yourself if that matters.
- **Effects don't run during render.** They run in `CommitEffects()`; a `Debug.Log` in an effect that never appears usually means the host forgot to call `CommitEffects()`.
- **Unkeyed component position = identity.** Inserting a sibling above an unkeyed function component remounts it (state lost, cleanups run). Right: `FunctionComponents.Keyed(key, ...)` in dynamic lists.
- **`[Component]` gives no key parameter.** The rewrite always calls key-less `Create`; wrap with `FunctionComponents.Keyed` for list items.
- **Duplicate attribute keys are silently collapsed** in `Attributes<T>` (last wins) — not an error.
- **Attrs patch removal is `key -> null`.** When handling `Attrs<T>` in an applicator, a `null` dictionary value means "detach attribute", not a bug.
- **Patch indices target the OLD tree.** Applying patches after the host tree changed out-of-band corrupts targeting; keep old tree + host tree in lockstep.
- **`KeyedNode<T>` ctor takes `IAttribute<T>[]`**, not `IEnumerable` — call `.ToArray()` when needed.
- **Thread affinity:** `HookRuntime.Current` is `[ThreadStatic]`; resolve and hooks must happen on the same thread.

## Extending

- **New attribute:** subclass `Attribute<T, TValue>` with a unique key and implement `Apply(T obj)`. Ensure `TValue` has value equality. No registration needed — attributes are data.
- **New node variant:** derive from `BaseNode<T>` / `BaseKeyedNode<T>` (constructors are `protected`). Implement `ITypedNode` if it should carry a component type, `IHostLifecycleTree<T>` for lifecycle callbacks. Caution: `HookRuntime.Resolve` rebuilds nodes and only preserves `ITypedNode` + `IHostLifecycleTree<T>` — extra state on custom node classes is dropped during resolution; prefer `IVTreeWrapper` for decorators.
- **New patch type:** implement `IPatch<T>` with structural `Equals`/`GetHashCode`, emit it from `Diff<T>`, and add handling to the applicator in `com.uzimaru.veauty-gameobject` — both sides must agree on the pre-order index contract.
- **Changing the IL rewrite:** edit `SourceGenerator~/ComponentILPostProcessor.cs`, then `cd SourceGenerator~ && dotnet build -c Release && cp bin/Release/netstandard2.1/Veauty.CodeGen.dll ../CodeGen/` (fix Unity `HintPath`s in the csproj for your local Unity).

## Build & test

Run from the repo root (requires a running Unity Editor with uloop):

```bash
npx --yes uloop-cli@2.2.0 compile --force-recompile true --wait-for-domain-reload true
npx --yes uloop-cli@2.2.0 run-tests --test-mode EditMode   # Veauty.Editor.Tests (diff/patch/vtree)
npx --yes uloop-cli@2.2.0 run-tests --test-mode PlayMode   # Veauty.Runtime.Tests (hooks)
```

This package's test assemblies: `Veauty.Editor.Tests` (Tests/Editor), `Veauty.Runtime.Tests` (Tests/Runtime). Git operations happen inside this package directory (it is its own repository).
