# Hooks

[日本語](ja/hooks.md)

State, refs and effects for function components: the `Hooks` API, the rules the runtime enforces, and exactly when effects and cleanups run.

## Where hooks may be called

All `Hooks.*` methods delegate to `HookRuntime.Current`, a `[ThreadStatic]` field that is only non-null **while a function component's render function is executing** inside `HookRuntime.Resolve<T>()`. Calling a hook anywhere else throws:

```
InvalidOperationException: Hooks can only be used while rendering a function component.
```

The field is saved and restored with `try/finally` around each component render, so components nested inside other components work, and an exception in one render cannot leak the runtime pointer.

## UseState

```csharp
State<T> Hooks.UseState<T>(T initialValue)
```

- First render: stores `initialValue` in a new slot.
- Later renders: returns the current stored value; `initialValue` is ignored.
- `state.Value` reads the value; `state.Set(value)` / `state.Set(v => ...)` writes it **synchronously** and invokes the runtime's `requestRender` callback.
- `Set` performs **no equality check** — setting the same value still requests a render. Updates are not batched by the core; batching (if any) is the host's job.
- Changing `T` for the same slot position between renders throws `InvalidOperationException("Hook slot type changed between renders.")`.

## UseRef

```csharp
Ref<T> Hooks.UseRef<T>(T initialValue = default)
```

- Returns the **same `Ref<T>` instance** on every render of a component instance.
- Mutating `refBox.Current` never triggers a re-render — use it for render counters, cached objects, or capturing host objects.
- The slot type check applies as with `UseState`.

## UseEffect

Four overloads:

```csharp
void Hooks.UseEffect(Action effect)                              // no cleanup, runs after EVERY render
void Hooks.UseEffect(Action effect, params object[] deps)        // no cleanup, runs when deps change
void Hooks.UseEffect(Func<Action> effect)                        // cleanup,   runs after EVERY render
void Hooks.UseEffect(Func<Action> effect, params object[] deps)  // cleanup,   runs when deps change
```

The `Func<Action>` form returns the cleanup action (or `null` for none).

### Dependency semantics

`ShouldRun` decides whether the effect is (re)staged, in this order:

1. Never ran before → run (mount).
2. `deps == null` on either side → run (the no-deps overloads pass `null`, meaning "every render").
3. Length differs from the previous deps → run.
4. Otherwise compare element-wise with **`object.Equals`** — any inequality → run.

Consequences:

- `new object[] { }` (empty array) → effect runs **on mount only**.
- Value types are boxed and compared by value (`1.Equals(1)` → equal). Reference types compare by their own `Equals`; types without a meaningful `Equals` (e.g. lambdas, plain arrays) compare by reference and will re-run the effect whenever a new instance is passed.
- **Gotcha:** because `deps` is `params`, `Hooks.UseEffect(fn, null)` passes a `null` array which the wrapper coerces to an *empty* array → mount-only. Only the overloads *without* a deps parameter give the "every render" behavior.

### Commit timing

Effects never run during render. `UseEffect` only *stages* the effect (`Pending = true`) in its slot; execution happens when the host calls `HookRuntime.CommitEffects()`, which should be **after patches are applied** so the effect observes the updated host tree. For each pending effect:

1. The previous cleanup (if any) is invoked.
2. The effect runs; its return value becomes the new cleanup.
3. The staged deps become the current deps.

### Cleanup semantics

- Before every re-run of the same effect: previous cleanup runs first.
- On **unmount**: an instance not visited by the latest `Resolve` (detected by comparing its `LastSeenVersion` with the runtime's `renderVersion`) has *all* of its effects' cleanups invoked in `CommitEffects()`, then the instance and its state are discarded.
- On **`HookRuntime.Dispose()`**: every instance's cleanups run and all state is cleared. Call this when tearing down the mounted tree.

## Rules of hooks (runtime-enforced)

Hook slots are matched **by call order** within a component instance. The runtime verifies on each render:

1. **Same order, same count** — if the slot kind at a position changes (e.g. a `UseState` where a `UseEffect` used to be), it throws `InvalidOperationException("Hook order changed between renders.")`. Don't call hooks inside `if`/loops with varying counts.
2. **Same value type** — a slot's `T` may not change: `"Hook slot type changed between renders."`
3. **Only during render** — see above.

Note: calling *more* hooks than the previous render appends new slots without error; rely on rule 1 and keep hook calls unconditional anyway.

## Component identity — when does state survive?

Hook state belongs to a *component instance*, identified by render path + component token (see [Function components](function-components.md#identity)). State survives re-renders while the component stays at the same position — or keeps the same `key` — under the same parent chain. If the path changes (e.g. an unkeyed component shifts index because a sibling was inserted), the old instance unmounts (cleanups run) and a fresh instance mounts.

## Full example

```csharp
[Component]
static IVTree Timer(float interval)
{
    var elapsed = Hooks.UseState(0f);
    var running = Hooks.UseRef(true);

    // restart the ticker whenever `interval` changes; stop it on unmount
    Hooks.UseEffect(() =>
    {
        var handle = Ticker.Start(interval, dt =>
        {
            if (running.Current) elapsed.Set(x => x + dt);
        });
        return () => Ticker.Stop(handle);
    }, new object[] { interval });

    return new Node<GameObject>($"elapsed:{elapsed.Value:F1}",
        new IAttribute<GameObject>[] { });
}
```

See also: [Function components](function-components.md), [Architecture](architecture.md), [API reference](api-reference.md).
