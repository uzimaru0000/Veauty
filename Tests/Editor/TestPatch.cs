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
            Assert.AreEqual(x.GetHashCode(), y.GetHashCode());
        }

        [Test]
        public void TestEntryEqualsComparesData()
        {
            var x = new Entry(Entry.Type.Move, new Node<object>("tag", new IAttribute<object>[] {}), 1) {
                data = new Remove<object>(1)
            };
            var y = new Entry(Entry.Type.Move, new Node<object>("tag", new IAttribute<object>[] {}), 1) {
                data = new Remove<object>(1)
            };
            var z = new Entry(Entry.Type.Move, new Node<object>("tag", new IAttribute<object>[] {}), 1) {
                data = new Remove<object>(2)
            };

            Assert.AreEqual(x, y);
            Assert.AreEqual(x.GetHashCode(), y.GetHashCode());
            Assert.AreNotEqual(x, z);
        }

        [TestCaseSource("TestCase")]
        public void TestPatchEquals(Veauty.IPatch<object> x, Veauty.IPatch<object> y)
        {
            Assert.AreEqual(x, y);
            Assert.AreEqual(x.GetHashCode(), y.GetHashCode());
        }

        [Test]
        public void TestRedrawEqualsComparesNewTree()
        {
            var x = new Redraw<object>(0, new Node<object>("first", new IAttribute<object>[] {}));
            var y = new Redraw<object>(0, new Node<object>("second", new IAttribute<object>[] {}));

            Assert.AreNotEqual(x, y);
        }

        [Test]
        public void TestRemoveEqualsComparesIndexWhenPatchesExist()
        {
            var x = new Remove<object>(0, new IPatch<object>[] {
                new Attach<object>(1, typeof(int), typeof(float)),
            });
            var y = new Remove<object>(1, new IPatch<object>[] {
                new Attach<object>(1, typeof(int), typeof(float)),
            });

            Assert.AreNotEqual(x, y);
        }

        [Test]
        public void TestRemoveEqualsComparesEntriesStructurally()
        {
            var x = new Remove<object>(
                0,
                null,
                new Entry(Entry.Type.Remove, new Node<object>("tag", new IAttribute<object>[] {}), 1)
            );
            var y = new Remove<object>(
                0,
                null,
                new Entry(Entry.Type.Remove, new Node<object>("tag", new IAttribute<object>[] {}), 1)
            );

            Assert.AreEqual(x, y);
            Assert.AreEqual(x.GetHashCode(), y.GetHashCode());
        }

        [Test]
        public void TestReorderEqualsComparesMoveEntriesStructurally()
        {
            var xEntry = new Entry(Entry.Type.Move, new Node<object>("tag", new IAttribute<object>[] {}), -1);
            var yEntry = new Entry(Entry.Type.Move, new Node<object>("tag", new IAttribute<object>[] {}), -1);

            var x = new Reorder<object>(
                0,
                new IPatch<object>[] {
                    new Remove<object>(1, new IPatch<object>[] {
                        new Attrs<object>(1, new Dictionary<string, IAttribute<object>>() {
                            { "test", new TestableAttribute(20) }
                        })
                    }, xEntry)
                },
                new Reorder<object>.Insert[] {},
                new Reorder<object>.Insert[] {
                    new Reorder<object>.Insert {
                        index = -1,
                        entry = xEntry
                    }
                }
            );
            var y = new Reorder<object>(
                0,
                new IPatch<object>[] {
                    new Remove<object>(1, new IPatch<object>[] {
                        new Attrs<object>(1, new Dictionary<string, IAttribute<object>>() {
                            { "test", new TestableAttribute(20) }
                        })
                    }, yEntry)
                },
                new Reorder<object>.Insert[] {},
                new Reorder<object>.Insert[] {
                    new Reorder<object>.Insert {
                        index = -1,
                        entry = yEntry
                    }
                }
            );

            Assert.AreEqual(x, y);
            Assert.AreEqual(x.GetHashCode(), y.GetHashCode());
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
