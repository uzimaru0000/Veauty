# Veauty

[日本語](ja/com.uzimaru.veauty.md)

Core package of Veauty, a Virtual DOM implementation for Unity: virtual tree types, the diff algorithm, patch types, Hooks, and the `[Component]` function-component system.

## What this package is

`com.uzimaru.veauty` is the renderer-agnostic core of the Veauty stack. It defines *what* a UI tree is and *how* two trees are compared — it does not touch `GameObject`s itself. Rendering and patch application live in [`com.uzimaru.veauty-gameobject`](https://github.com/uzimaru0000/Veauty-GameObject), and uGUI widgets live in [`com.uzimaru.veauty-ugui`](https://github.com/uzimaru0000/Veauty-uGUI).

| Concern | Type(s) |
|---------|---------|
| Tree structure | `IVTree`, `Node<T>` / `Node<T,U>`, `KeyedNode<T>` / `KeyedNode<T,U>`, `HostLifecycleNode<...>` |
| Attributes | `IAttribute<T>`, `Attribute<T,U>`, `Attributes<T>` |
| Function components | `FunctionComponentNode`, `FunctionComponents`, `[Component]`, `HookRuntime` |
| State & lifecycle | `Hooks.UseState` / `UseRef` / `UseEffect`, `State<T>`, `Ref<T>` |
| Diffing | `Diff<T>.Calc`, `IPatch<T>`, `Append` / `Attach` / `Attrs` / `Redraw` / `Remove` / `RemoveLast` / `Reorder` |

## Requirements

- Unity 6000.5 or later (Unity 6)
- Depends on `com.unity.nuget.mono-cecil` (used by the bundled IL post-processor)

## Install

Add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.uzimaru.veauty": "https://github.com/uzimaru0000/Veauty.git"
  }
}
```

## Quick start

```csharp
using Veauty;
using Veauty.VTree;

[Component]
static IVTree Counter()
{
    var count = Hooks.UseState(0);
    return new Node<UnityEngine.GameObject>("counter",
        new IAttribute<UnityEngine.GameObject>[] { },
        new Node<UnityEngine.GameObject>($"count: {count.Value}",
            new IAttribute<UnityEngine.GameObject>[] { }));
}
```

Mount it with `VeautyObject` from `com.uzimaru.veauty-gameobject`, or drive the cycle yourself with `HookRuntime` + `Diff<T>` (see [Architecture](architecture.md)).

## Documentation

- [Getting started](getting-started.md) — install and a minimal end-to-end example.
- [Architecture](architecture.md) — the resolve → diff → patch → commit cycle.
- [Hooks](hooks.md) — `UseState` / `UseRef` / `UseEffect`, rules of hooks, effect timing.
- [Function components](function-components.md) — `[Component]`, `FunctionComponents`, how the IL post-processor works.
- [Diffing](diffing.md) — the diff algorithm, patch types, the pre-order index contract, keyed diffing.
- [API reference](api-reference.md) — every public type and member.

Also see the [Agent guide](../AGENTS.md) and the [Changelog](../CHANGELOG.md).
