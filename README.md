# Veauty

A Virtual DOM implementation for Unity. Build UIs declaratively with a React-like API — virtual tree diffing and patching keep your GameObjects in sync efficiently.

## Requirements

- Unity 6000.5 or later (Unity 6)

## Install

Add to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.uzimaru.veauty": "https://github.com/uzimaru0000/Veauty.git"
  }
}
```

## Package structure

Veauty is split into four packages:

| Package | Role |
|---------|------|
| `com.uzimaru.veauty` (this package) | VTree types, Diff algorithm, Hooks, `[Component]` attribute |
| [`com.uzimaru.veauty-gameobject`](https://github.com/uzimaru0000/Veauty-GameObject) | Renderer, Patch applicator, `VeautyObject` entry point |
| [`com.uzimaru.veauty-ugui`](https://github.com/uzimaru0000/Veauty-uGUI) | uGUI widgets, Presets API (`V.Text`, `V.Button`, etc.) |
| [`com.uzimaru.veauty-uitoolkit`](https://github.com/uzimaru0000/Veauty-UIToolkit) | UI Toolkit elements, `VeautyElement` entry point, style attributes |

## VTree types

- **`Node<T>`** / **`Node<T,U>`** — basic node with tag, attributes, and children. `U` is the Unity component type.
- **`KeyedNode<T,U>`** — keyed node for efficient list diffing.
- **`FunctionComponentNode`** — function component, created via `[Component]` attribute or `FunctionComponents.Create()`.

## Hooks

APIs for managing state and lifecycle inside function components.

```csharp
// State — triggers re-render on change
var count = Hooks.UseState(0);
count.Set(x => x + 1);

// Ref — mutable value that does NOT trigger re-render
var renderCount = Hooks.UseRef(0);
renderCount.Current++;

// Effect — runs on mount/unmount or when dependencies change
Hooks.UseEffect(() =>
{
    Debug.Log("mounted");
    return () => Debug.Log("unmounted");  // cleanup
}, new object[] { /* dependencies */ });
```

## `[Component]` attribute

Mark a static method with `[Component]` to turn it into a function component. An ILPostProcessor wraps the method body in `FunctionComponents.Create()` at compile time, so callers invoke it like a regular method.

```csharp
// Definition
[Component]
static IVTree Timer(float interval)
{
    var elapsed = Hooks.UseState(0f);
    Hooks.UseEffect(() => { /* ... */ }, new object[] { interval });
    return new Node<GameObject>("Timer", /* ... */);
}

// Call site — no wrapping needed
Timer(1.0f)
```

Without `[Component]`, hooks would run in the caller's scope and would not have their own lifecycle (mount/unmount).

### Building the ILPostProcessor

Source lives in `SourceGenerator~/`. To rebuild after making changes:

```bash
cd SourceGenerator~
dotnet build -c Release
cp bin/Release/netstandard2.1/Veauty.CodeGen.dll ../CodeGen/
```

## Diff / Patch

`Diff<T>.Calc(oldTree, newTree)` computes patches; `Patch.Apply()` applies them to the existing GameObject tree.

| Patch type | Behavior |
|-----------|----------|
| `Redraw` | Replace the node |
| `Attrs` | Apply attribute diff |
| `Attach` | Change the typed component |
| `Append` | Append children |
| `RemoveLast` | Remove trailing children |
| `Remove` | Remove the node |
| `Reorder` | Reorder keyed children |

## Documentation

- [Manual](Documentation~/com.uzimaru.veauty.md) — getting started, architecture, hooks, function components, diffing
- [API reference](Documentation~/api-reference.md) — every public type and member
- [Agent guide](AGENTS.md) — rules, patterns and pitfalls for coding agents
- [Changelog](CHANGELOG.md)
