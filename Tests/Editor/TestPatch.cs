using System.Collections.Generic;
using NUnit.Framework;
using Veauty;
using Veauty.VTree;
using Veauty.Patch;

namespace Tests
{
    public class TestPatch
    {
        [Test]
        public void TestEntry()
        {
            var x = new Entry(Entry.Type.Insert, new Node<object>("tag", new IAttribute<object>[]{}), 1);
            var y = new Entry(Entry.Type.Insert, new Node<object>("tag", new IAttribute<object>[]{}), 1);

            Assert.AreEqual(x, y);
        }

        [TestCaseSource("TestCase")]
        public void TestPatchEquals(Veauty.IPatch<object> x, Veauty.IPatch<object> y)
        {
            Assert.AreEqual(x, y);
        }

        static object[] TestCase =
        {
            new object[] {
                new Append<object>(0, 1, new IVTree[] {}),
                new Append<object>(0, 1, new IVTree[] {}),
            },
            new object[] {
                new Append<object>(0, 1, new IVTree[] {
                    new Node<object>("tag", new IAttribute<object>[] {})
                }),
                new Append<object>(0, 1, new IVTree[] {
                    new Node<object>("tag", new IAttribute<object>[] {})
                }),
            },
            new object[] {
                new Attach<object>(0, typeof(int), typeof(float)),
                new Attach<object>(0, typeof(int), typeof(float)),
            },
            new object[] {
                new Attrs<object>(0, new Dictionary<string, IAttribute<object>>()),
                new Attrs<object>(0, new Dictionary<string, IAttribute<object>>()),
            },
            new object[] {
                new Attrs<object>(0, new Dictionary<string, IAttribute<object>>() {
                    {"test", new TestableAttribute(10)}
                }),
                new Attrs<object>(0, new Dictionary<string, IAttribute<object>>() {
                    {"test", new TestableAttribute(10)}
                }),
            },
            new object[] {
                new Redraw<object>(0, new Node<object>("tag", new IAttribute<object>[] {})),
                new Redraw<object>(0, new Node<object>("tag", new IAttribute<object>[] {})),
            },
            new object[] {
                new Remove<object>(0),
                new Remove<object>(0),
            },
            new object[] {
                new Remove<object>(0, new IPatch<object>[] {
                    new Attach<object>(1, typeof(int), typeof(float)),
                }),
                new Remove<object>(0, new IPatch<object>[] {
                    new Attach<object>(1, typeof(int), typeof(float)),
                }),
            },
            new object[] {
                new RemoveLast<object>(0, 2, 3),
                new RemoveLast<object>(0, 2, 3),
            },
            new object[] {
                new Reorder<object>(
                    0,
                    new IPatch<object>[] {},
                    new Reorder<object>.Insert[] {},
                    new Reorder<object>.Insert[] {}
                ),
                new Reorder<object>(
                    0,
                    new IPatch<object>[] {},
                    new Reorder<object>.Insert[] {},
                    new Reorder<object>.Insert[] {}
                )
            },
            new object[] {
                new Reorder<object>(
                    0,
                    new IPatch<object>[] {
                        new Attach<object>(1, typeof(int), typeof(float)),
                    },
                    new Reorder<object>.Insert[] {},
                    new Reorder<object>.Insert[] {}
                ),
                new Reorder<object>(
                    0,
                    new IPatch<object>[] {
                        new Attach<object>(1, typeof(int), typeof(float)),
                    },
                    new Reorder<object>.Insert[] {},
                    new Reorder<object>.Insert[] {}
                ),
            },
            new object[] {
                new Reorder<object>(
                    0,
                    new IPatch<object>[] {
                        new Attach<object>(1, typeof(int), typeof(float)),
                    },
                    new Reorder<object>.Insert[] {
                        new Reorder<object>.Insert {
                            index = 2, 
                            entry = new Entry(Entry.Type.Remove, new Node<object>("tag", new IAttribute<object>[] {}), 3)
                        }
                    },
                    new Reorder<object>.Insert[] {}
                ),
                new Reorder<object>(
                    0,
                    new IPatch<object>[] {
                        new Attach<object>(1, typeof(int), typeof(float)),
                    },
                    new Reorder<object>.Insert[] {
                        new Reorder<object>.Insert {
                            index = 2, 
                            entry = new Entry(Entry.Type.Remove, new Node<object>("tag", new IAttribute<object>[] {}), 3)
                        }
                    },
                    new Reorder<object>.Insert[] {}
                ),
            }
        };
    }
}
