using System;

namespace Veauty
{
    /// <summary>
    /// A handle to a state hook slot created by <see cref="Hooks.UseState{T}(T)"/>.
    /// Reading <see cref="Value"/> returns the current value; calling <see cref="Set(T)"/>
    /// stores a new value and asks the host to re-render.
    /// </summary>
    /// <typeparam name="T">The stored value type.</typeparam>
    public readonly struct State<T>
    {
        private readonly HookRuntime runtime;
        private readonly HookRuntime.HookSlot slot;

        internal State(HookRuntime runtime, HookRuntime.HookSlot slot)
        {
            this.runtime = runtime;
            this.slot = slot;
        }

        /// <summary>Gets the current value stored in the hook slot.</summary>
        public T Value => (T)this.slot.Value;

        /// <summary>
        /// Stores a new value and invokes the runtime's re-render request callback.
        /// </summary>
        /// <param name="value">The value to store.</param>
        /// <remarks>
        /// The value is written synchronously (updates are not batched) and a re-render is
        /// requested even when the new value equals the old one — no equality check is performed.
        /// </remarks>
        public void Set(T value)
        {
            this.runtime.SetState(this.slot, value);
        }

        /// <summary>
        /// Computes a new value from the current one, stores it, and requests a re-render.
        /// </summary>
        /// <param name="update">A function receiving the current value and returning the new value.</param>
        /// <exception cref="ArgumentNullException"><paramref name="update"/> is <c>null</c>.</exception>
        public void Set(Func<T, T> update)
        {
            if (update == null)
            {
                throw new ArgumentNullException(nameof(update));
            }

            this.runtime.SetState(this.slot, update((T)this.slot.Value));
        }
    }

    /// <summary>
    /// A mutable box returned by <see cref="Hooks.UseRef{T}(T)"/>. The same instance is
    /// returned on every render of a component; mutating <see cref="Current"/> does NOT
    /// trigger a re-render.
    /// </summary>
    /// <typeparam name="T">The stored value type.</typeparam>
    public sealed class Ref<T>
    {
        /// <summary>Initializes the box with an initial value.</summary>
        /// <param name="current">The initial value.</param>
        public Ref(T current)
        {
            this.Current = current;
        }

        /// <summary>The current value. Freely mutable; changes never cause a re-render.</summary>
        public T Current { get; set; }
    }

    /// <summary>
    /// Hook entry points for function components. Hooks may only be called while a function
    /// component is being rendered by <see cref="HookRuntime.Resolve{T}(IVTree)"/>; calling them
    /// anywhere else throws <see cref="InvalidOperationException"/>.
    /// </summary>
    /// <remarks>
    /// Rules of hooks (enforced at runtime per component instance):
    /// <list type="bullet">
    /// <item><description>Call hooks in the same order on every render — a kind mismatch throws "Hook order changed between renders".</description></item>
    /// <item><description>Do not change the value type of a slot between renders — this throws "Hook slot type changed between renders".</description></item>
    /// <item><description>Do not call hooks conditionally or in loops with varying iteration counts.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// [Component]
    /// static IVTree Counter()
    /// {
    ///     var count = Hooks.UseState(0);
    ///     var clicks = Hooks.UseRef(0);
    ///
    ///     Hooks.UseEffect(() =>
    ///     {
    ///         Debug.Log($"count is now {count.Value}");
    ///         return () => Debug.Log("cleanup before next run / unmount");
    ///     }, new object[] { count.Value });
    ///
    ///     return new Node&lt;GameObject&gt;("counter", new IAttribute&lt;GameObject&gt;[] { });
    /// }
    /// </code>
    /// </example>
    public static class Hooks
    {
        /// <summary>
        /// Declares a state slot. The initial value is stored on the first render only;
        /// subsequent renders return the current stored value.
        /// </summary>
        /// <typeparam name="T">The stored value type.</typeparam>
        /// <param name="initialValue">The value used when the slot is first created.</param>
        /// <returns>A <see cref="State{T}"/> handle for reading and updating the value.</returns>
        /// <exception cref="InvalidOperationException">Called outside a function component render, or the slot's kind/type changed between renders.</exception>
        public static State<T> UseState<T>(T initialValue)
            => HookRuntime.Current.UseState(initialValue);

        /// <summary>
        /// Declares a ref slot holding a mutable <see cref="Ref{T}"/> box that persists across
        /// renders. Mutating the box never triggers a re-render.
        /// </summary>
        /// <typeparam name="T">The stored value type.</typeparam>
        /// <param name="initialValue">The value used when the slot is first created.</param>
        /// <returns>The same <see cref="Ref{T}"/> instance on every render of this component.</returns>
        /// <exception cref="InvalidOperationException">Called outside a function component render, or the slot's kind/type changed between renders.</exception>
        public static Ref<T> UseRef<T>(T initialValue = default(T))
            => HookRuntime.Current.UseRef(initialValue);

        /// <summary>
        /// Declares an effect with no cleanup that runs after every render.
        /// </summary>
        /// <param name="effect">The side effect to run during <see cref="HookRuntime.CommitEffects"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="effect"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Called outside a function component render.</exception>
        /// <remarks>Effects never run during render; they are staged and executed when the host calls <see cref="HookRuntime.CommitEffects"/> after patching.</remarks>
        public static void UseEffect(Action effect)
            => HookRuntime.Current.UseEffect(ToEffect(effect), null);

        /// <summary>
        /// Declares an effect with no cleanup that runs when any dependency changed since the
        /// previous run (compared with <see cref="object.Equals(object, object)"/>).
        /// </summary>
        /// <param name="effect">The side effect to run during <see cref="HookRuntime.CommitEffects"/>.</param>
        /// <param name="dependencies">Values compared element-wise against the previous render. An empty array means "run on mount only".</param>
        /// <exception cref="ArgumentNullException"><paramref name="effect"/> is <c>null</c>.</exception>
        /// <exception cref="InvalidOperationException">Called outside a function component render.</exception>
        /// <remarks>
        /// Passing an explicit <c>null</c> array is coerced to an empty array (mount-only), unlike
        /// the overload without a <paramref name="dependencies"/> parameter which runs every render.
        /// </remarks>
        public static void UseEffect(Action effect, params object[] dependencies)
            => HookRuntime.Current.UseEffect(ToEffect(effect), dependencies ?? new object[] {});

        /// <summary>
        /// Declares an effect that runs after every render and returns a cleanup action.
        /// The cleanup runs before the next execution of the effect and on unmount/dispose.
        /// </summary>
        /// <param name="effect">The side effect; may return a cleanup <see cref="Action"/> or <c>null</c>.</param>
        /// <exception cref="InvalidOperationException">Called outside a function component render.</exception>
        public static void UseEffect(Func<Action> effect)
            => HookRuntime.Current.UseEffect(effect, null);

        /// <summary>
        /// Declares an effect with cleanup that runs when any dependency changed since the
        /// previous run (compared with <see cref="object.Equals(object, object)"/>).
        /// </summary>
        /// <param name="effect">The side effect; may return a cleanup <see cref="Action"/> or <c>null</c>.</param>
        /// <param name="dependencies">Values compared element-wise against the previous render. An empty array means "run on mount only".</param>
        /// <exception cref="InvalidOperationException">Called outside a function component render.</exception>
        /// <remarks>
        /// Passing an explicit <c>null</c> array is coerced to an empty array (mount-only), unlike
        /// the overload without a <paramref name="dependencies"/> parameter which runs every render.
        /// </remarks>
        public static void UseEffect(Func<Action> effect, params object[] dependencies)
            => HookRuntime.Current.UseEffect(effect, dependencies ?? new object[] {});

        private static Func<Action> ToEffect(Action effect)
        {
            if (effect == null)
            {
                throw new ArgumentNullException(nameof(effect));
            }

            return () =>
            {
                effect();
                return null;
            };
        }
    }
}
