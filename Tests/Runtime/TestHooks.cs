using System;
using System.Collections.Generic;
using NUnit.Framework;
using Veauty;
using Veauty.VTree;

namespace Tests
{
    public class TestHooks
    {
        [Test]
        public void TestUseStateInitializesOnceAndPersists()
        {
            var runtime = new HookRuntime();
            State<int> count = default(State<int>);
            var component = FunctionComponents.Create(() =>
            {
                count = Hooks.UseState(1);
                return new Node<object>("root", NoAttrs(), Kid(count.Value.ToString()));
            });

            var first = (BaseNode<object>)runtime.Resolve<object>(component);
            runtime.CommitEffects();
            Assert.AreEqual("1", ((BaseNode<object>)first.kids[0]).tag);

            count.Set(x => x + 1);
            var second = (BaseNode<object>)runtime.Resolve<object>(component);
            runtime.CommitEffects();
            Assert.AreEqual("2", ((BaseNode<object>)second.kids[0]).tag);
        }

        [Test]
        public void TestUseStateSetterRequestsRender()
        {
            var requested = 0;
            var runtime = new HookRuntime(() => requested++);
            State<int> count = default(State<int>);
            var component = FunctionComponents.Create(() =>
            {
                count = Hooks.UseState(1);
                return Kid(count.Value.ToString());
            });

            runtime.Resolve<object>(component);
            runtime.CommitEffects();

            count.Set(2);

            Assert.AreEqual(1, requested);
        }

        [Test]
        public void TestMultipleHooksPreserveSlotOrder()
        {
            var runtime = new HookRuntime();
            State<int> first = default(State<int>);
            State<int> second = default(State<int>);
            var component = FunctionComponents.Create(() =>
            {
                first = Hooks.UseState(1);
                second = Hooks.UseState(10);
                return new Node<object>("root", NoAttrs(),
                    Kid(first.Value.ToString()),
                    Kid(second.Value.ToString()));
            });

            runtime.Resolve<object>(component);
            runtime.CommitEffects();

            first.Set(2);
            second.Set(20);

            var resolved = (BaseNode<object>)runtime.Resolve<object>(component);
            runtime.CommitEffects();

            Assert.AreEqual("2", ((BaseNode<object>)resolved.kids[0]).tag);
            Assert.AreEqual("20", ((BaseNode<object>)resolved.kids[1]).tag);
        }

        [Test]
        public void TestKeyedFunctionComponentsPreserveStateDuringReorder()
        {
            var runtime = new HookRuntime();
            var states = new Dictionary<int, State<int>>();
            FunctionComponent<int> item = id =>
            {
                var state = Hooks.UseState(id);
                states[id] = state;
                return Kid(id + ":" + state.Value);
            };

            var firstTree = new KeyedNode<object>(
                "root",
                NoAttrs(),
                ("a", FunctionComponents.Create(item, 1)),
                ("b", FunctionComponents.Create(item, 2))
            );

            runtime.Resolve<object>(firstTree);
            runtime.CommitEffects();
            states[1].Set(10);
            states[2].Set(20);

            var secondTree = new KeyedNode<object>(
                "root",
                NoAttrs(),
                ("b", FunctionComponents.Create(item, 2)),
                ("a", FunctionComponents.Create(item, 1))
            );

            var resolved = (BaseKeyedNode<object>)runtime.Resolve<object>(secondTree);
            runtime.CommitEffects();

            Assert.AreEqual("2:20", ((BaseNode<object>)resolved.GetKids()[0]).tag);
            Assert.AreEqual("1:10", ((BaseNode<object>)resolved.GetKids()[1]).tag);
        }

        [Test]
        public void TestUseRefReturnsStableReferenceWithoutRequestingRender()
        {
            var requested = 0;
            var runtime = new HookRuntime(() => requested++);
            Ref<int> firstRef = null;
            Ref<int> secondRef = null;
            var component = FunctionComponents.Create(() =>
            {
                var value = Hooks.UseRef(1);
                if (firstRef == null)
                {
                    firstRef = value;
                }
                else
                {
                    secondRef = value;
                }

                value.Current++;
                return Kid(value.Current.ToString());
            });

            runtime.Resolve<object>(component);
            runtime.CommitEffects();
            var resolved = (BaseNode<object>)runtime.Resolve<object>(component);
            runtime.CommitEffects();

            Assert.AreSame(firstRef, secondRef);
            Assert.AreEqual("3", resolved.tag);
            Assert.AreEqual(0, requested);
        }

        [Test]
        public void TestUseEffectRunsAfterCommitAndCleansUpOnDependencyChange()
        {
            var runtime = new HookRuntime();
            var dep = 1;
            var effectRuns = 0;
            var cleanups = 0;
            FunctionComponent componentFunc = () =>
            {
                Hooks.UseEffect(() =>
                {
                    effectRuns++;
                    return () => cleanups++;
                }, dep);
                return Kid("root");
            };
            var component = FunctionComponents.Create(componentFunc);

            runtime.Resolve<object>(component);
            Assert.AreEqual(0, effectRuns);
            runtime.CommitEffects();
            Assert.AreEqual(1, effectRuns);
            Assert.AreEqual(0, cleanups);

            runtime.Resolve<object>(component);
            runtime.CommitEffects();
            Assert.AreEqual(1, effectRuns);
            Assert.AreEqual(0, cleanups);

            dep = 2;
            runtime.Resolve<object>(component);
            runtime.CommitEffects();
            Assert.AreEqual(2, effectRuns);
            Assert.AreEqual(1, cleanups);
        }

        [Test]
        public void TestUseEffectCleansUpWhenComponentUnmounts()
        {
            var runtime = new HookRuntime();
            var showChild = true;
            var effectRuns = 0;
            var cleanups = 0;
            var child = FunctionComponents.Create(() =>
            {
                Hooks.UseEffect(() =>
                {
                    effectRuns++;
                    return () => cleanups++;
                }, new object[] {});
                return Kid("child");
            });
            var root = FunctionComponents.Create(() =>
                showChild
                    ? new Node<object>("root", NoAttrs(), child)
                    : new Node<object>("root", NoAttrs()));

            runtime.Resolve<object>(root);
            runtime.CommitEffects();
            Assert.AreEqual(1, effectRuns);
            Assert.AreEqual(0, cleanups);

            showChild = false;
            runtime.Resolve<object>(root);
            runtime.CommitEffects();
            Assert.AreEqual(1, cleanups);
        }

        private static IAttribute<object>[] NoAttrs()
        {
            return new IAttribute<object>[] {};
        }

        private static Node<object> Kid(string name)
        {
            return new Node<object>(name, NoAttrs());
        }
    }
}
