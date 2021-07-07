using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Veauty;
using Veauty.VTree;
using Veauty.Patch;

namespace Tests
{
    public class TestableAttribute : Attribute<GameObject, int>
    {
        public TestableAttribute(int value) : base("test", value) {}
        public override void Apply(GameObject go) {}
    }

    public class PatchComparer<T> : IEqualityComparer<IPatch<T>>
    {
        public bool Equals(IPatch<T> x, IPatch<T> y)
        {
            if (x is Append<T> appendX && y is Append<T> appendY)
            {
                return appendX.length == appendY.length && appendX.kids.Equals(appendY);
            }
            else if (x is Attach<T> attachX && y is Attach<T> attachY)
            {
                return attachX.newComponent == attachY.newComponent && attachX.oldComponent == attachY.oldComponent;
            }
            else if (x is Attrs<T> attrsX && y is Attrs<T> attrsY)
            {
                return attrsX.index == attrsY.index && attrsX.attrs.Equals(attrsY.attrs);
            }
            else if (x is Redraw<T> redrawX && y is Redraw<T> redrawY)
            {
                return redrawX.index == redrawY.index;
            }
            else if (x is Remove<T> removeX && y is Remove<T> removeY)
            {
                return removeX.entry == removeY.entry;
            }
            else if (x is RemoveLast<T> removeLastX && y is RemoveLast<T> removeLastY)
            {
                return removeLastX.diff == removeLastY.diff && removeLastX.length == removeLastY.length;
            }
            else if (x is Reorder<T> reorderX && y is Reorder<T> reorderY)
            {
                return reorderX.endInserts.Equals(reorderY.endInserts);
            }

            return false;
        }

        public int GetHashCode(IPatch<T> x)
        {
            return x.GetHashCode();
        }
    }

    public class TestVTree
    {
        [TestCaseSource("TestCase")]
        public void TestDiff(Veauty.IVTree oldTree, Veauty.IVTree newTree, Veauty.IPatch<GameObject>[] expected)
        {
            var actual = Veauty.Diff<GameObject>.Calc(oldTree, newTree);

            Assert.AreEqual(expected.Length, actual.Length);
            for (var i = 0; i < actual.Length; i++)
            {
                Assert.That(actual[i], Is.EqualTo(expected[i]).Using(new PatchComparer<GameObject>()));
            }
        }

        static object[] TestCase =
        {
            new object[] { 
                new Node<GameObject>("tag", new IAttribute<GameObject>[] {}, new IVTree[] {}),
                new Node<GameObject>("tag", new IAttribute<GameObject>[] {}, new IVTree[] {}),
                new IPatch<GameObject>[] {}
            },
            new object[] {
                new Node<GameObject>("tag", new IAttribute<GameObject>[] {
                    new TestableAttribute(10)
                }, new IVTree[] {}),
                new Node<GameObject>("tag", new IAttribute<GameObject>[] {
                    new TestableAttribute(10)
                }, new IVTree[] {}),
                new IPatch<GameObject>[] {}
            },
            new object[] {
                new Node<GameObject>("tag", new IAttribute<GameObject>[] {}, new IVTree[] {
                    new Node<GameObject>("child", new IAttribute<GameObject>[] {}, new IVTree[] {})
                }),
                new Node<GameObject>("tag", new IAttribute<GameObject>[] {}, new IVTree[] {
                    new Node<GameObject>("child", new IAttribute<GameObject>[] {}, new IVTree[] {})
                }),
                new IPatch<GameObject>[] {}
            },
            new object[] {
                new Node<GameObject>("tag", new IAttribute<GameObject>[] {}, new IVTree[] {}),
                new Node<GameObject>("gat", new IAttribute<GameObject>[] {}, new IVTree[] {}),
                new IPatch<GameObject>[] {
                    new Redraw<GameObject>(0, new Node<GameObject>("gat", new IAttribute<GameObject>[] {}, new IVTree[] {}))
                },
            }
        };
    }
}
