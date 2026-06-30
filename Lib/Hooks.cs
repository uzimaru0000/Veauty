using System;

namespace Veauty
{
    public readonly struct State<T>
    {
        private readonly HookRuntime runtime;
        private readonly HookRuntime.HookSlot slot;

        internal State(HookRuntime runtime, HookRuntime.HookSlot slot)
        {
            this.runtime = runtime;
            this.slot = slot;
        }

        public T Value => (T)this.slot.Value;

        public void Set(T value)
        {
            this.runtime.SetState(this.slot, value);
        }

        public void Set(Func<T, T> update)
        {
            if (update == null)
            {
                throw new ArgumentNullException(nameof(update));
            }

            this.runtime.SetState(this.slot, update((T)this.slot.Value));
        }
    }

    public sealed class Ref<T>
    {
        public Ref(T current)
        {
            this.Current = current;
        }

        public T Current { get; set; }
    }

    public static class Hooks
    {
        public static State<T> UseState<T>(T initialValue)
            => HookRuntime.Current.UseState(initialValue);

        public static Ref<T> UseRef<T>(T initialValue = default(T))
            => HookRuntime.Current.UseRef(initialValue);

        public static void UseEffect(Action effect)
            => HookRuntime.Current.UseEffect(ToEffect(effect), null);

        public static void UseEffect(Action effect, params object[] dependencies)
            => HookRuntime.Current.UseEffect(ToEffect(effect), dependencies ?? new object[] {});

        public static void UseEffect(Func<Action> effect)
            => HookRuntime.Current.UseEffect(effect, null);

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
