# Getting started

[日本語](ja/getting-started.md)

Install the core package and run one full render → update cycle by hand to understand what the higher-level packages automate.

## Install

Add the package to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.uzimaru.veauty": "https://github.com/uzimaru0000/Veauty.git"
  }
}
```

Requires Unity 6000.5+. In real projects you will usually also install `com.uzimaru.veauty-gameobject` (renderer) and `com.uzimaru.veauty-ugui` (widgets); this page uses only the core so every step is visible.

## A minimal working example

The core is host-agnostic: the `T` in `Node<T>`, `Diff<T>` etc. is whatever object type your renderer manages (`GameObject` in Unity, `object` in tests). The snippet below uses `object` and drives the cycle manually — this is exactly what `VeautyObject<State>` does for you in the GameObject package.

```csharp
using System;
using Veauty;
using Veauty.VTree;

public static class Example
{
    // 1. A function component with state.
    [Component]
    static IVTree Counter()
    {
        var count = Hooks.UseState(0);

        Hooks.UseEffect(() =>
        {
            Console.WriteLine($"count is now {count.Value}");
            return () => Console.WriteLine("cleanup");
        }, new object[] { count.Value });

        return new Node<object>("counter",
            new IAttribute<object>[] { },
            new Node<object>($"label:{count.Value}", new IAttribute<object>[] { }));
    }

    public static void Run()
    {
        // 2. One runtime per mounted tree. The callback fires when State.Set is called.
        HookRuntime runtime = null;
        IVTree currentTree = null;

        runtime = new HookRuntime(() =>
        {
            // 3. Re-render: resolve the new tree, diff against the old, apply, commit.
            var nextTree = runtime.Resolve<object>(Counter());
            var patches = Diff<object>.Calc(currentTree, nextTree);
            // ... hand `patches` to your patch applicator here ...
            currentTree = nextTree;
            runtime.CommitEffects();
        });

        // 4. First mount: resolve, render, commit.
        currentTree = runtime.Resolve<object>(Counter());
        // ... first render of `currentTree` here ...
        runtime.CommitEffects();

        // 5. Teardown: run every effect cleanup.
        runtime.Dispose();
    }
}
```

## Line by line

- **`[Component]`** — marks a static method as a function component. At compile time an IL post-processor rewrites the method so that calling `Counter()` returns a `FunctionComponentNode` instead of executing the body immediately. This gives the body its own hook state and mount/unmount lifecycle. See [Function components](function-components.md).
- **`Hooks.UseState(0)`** — declares a state slot. The initial value is used only on the first render; `count.Set(...)` stores a new value and invokes the runtime's `requestRender` callback.
- **`Hooks.UseEffect(..., new object[] { count.Value })`** — stages a side effect. It runs inside `CommitEffects()` (never during render), and only when a dependency changed compared to the previous run (`object.Equals` element-wise). The returned action is the cleanup, invoked before the next run and on unmount. See [Hooks](hooks.md).
- **`new Node<object>(tag, attrs, kids)`** — a plain virtual node. `tag` identifies the node kind (nodes with different tags are redrawn, not updated); `attrs` are applied to the host object; `kids` are diffed by position. Use `KeyedNode` for lists.
- **`new HookRuntime(requestRender)`** — owns all component instances and hook slots for one mounted tree. The callback is invoked (synchronously) whenever any `State<T>.Set` runs.
- **`runtime.Resolve<object>(tree)`** — expands every `FunctionComponentNode` into concrete nodes by calling the component functions with hooks enabled. **Both** trees given to `Diff.Calc` must be resolved; diffing an unresolved component throws `InvalidOperationException`.
- **`Diff<object>.Calc(old, new)`** — produces the patch list (`Redraw`, `Attrs`, `Append`, ...) addressed by pre-order index. See [Diffing](diffing.md).
- **`runtime.CommitEffects()`** — runs staged effects and cleans up components that disappeared in the last `Resolve` call. Call it *after* patches were applied so effects observe the updated host tree.
- **`runtime.Dispose()`** — runs all outstanding cleanups; call when the tree is unmounted for good.

## Next steps

- [Architecture](architecture.md) — how these pieces fit together.
- [Hooks](hooks.md) — full hooks API and rules.
- [API reference](api-reference.md) — all public types.
