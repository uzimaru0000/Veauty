using NUnit.Framework;
using Veauty;
using Veauty.VTree;
using Veauty.Patch;

namespace Tests
{
    public class TestableAttribute : Attribute<object, int>
    {
        public TestableAttribute(int value) : base("test", value) {}
        public override void Apply(object go) {}
    }

    public class NullableAttribute : Attribute<object, string>
    {
        public NullableAttribute(string value) : base("nullable", value) {}
        public override void Apply(object go) {}
    }

    public class TestWidget : Widget<object>
    {
        public TestWidget(IAttribute<object>[] attrs, params IVTree[] kids) : base(attrs, kids) { }

        public override void Destroy(object obj) {}

        public override object Init(object obj) => obj;

        public override IVTree Render()
            => new Node<object>("test", this.attrs, this.kids);
    }

    public class TestVTree
    {
        [Test]
        public void TestNodeEqualsComparesAttributes()
        {
            var x = new Node<object>("tag", new IAttribute<object>[] {
                new TestableAttribute(10)
            });
            var y = new Node<object>("tag", new IAttribute<object>[] {
                new TestableAttribute(20)
            });

            Assert.AreNotEqual(x, y);
        }

        [Test]
        public void TestAttributeEqualsHandlesNullValues()
        {
            var x = new NullableAttribute(null);
            var y = new NullableAttribute(null);
            var z = new NullableAttribute("value");

            Assert.AreEqual(x, y);
            Assert.AreEqual(x.GetHashCode(), y.GetHashCode());
            Assert.AreNotEqual(x, z);
        }

        [Test]
        public void TestNodeEqualsComparesChildren()
        {
            var x = new Node<object>(
                "tag",
                new IAttribute<object>[] {},
                new Node<object>("first", new IAttribute<object>[] {})
            );
            var y = new Node<object>(
                "tag",
                new IAttribute<object>[] {},
                new Node<object>("second", new IAttribute<object>[] {})
            );

            Assert.AreNotEqual(x, y);
        }

        [Test]
        public void TestKeyedNodeEqualsComparesAttributesAndKeys()
        {
            var x = new KeyedNode<object>(
                "tag",
                new IAttribute<object>[] { new TestableAttribute(10) },
                ("first", new Node<object>("child", new IAttribute<object>[] {}))
            );
            var y = new KeyedNode<object>(
                "tag",
                new IAttribute<object>[] { new TestableAttribute(20) },
                ("second", new Node<object>("child", new IAttribute<object>[] {}))
            );

            Assert.AreNotEqual(x, y);
        }

        [Test]
        public void TestTypedKeyedNodeEqualsComparesBaseKeyedNode()
        {
            var x = new KeyedNode<object, int>(
                "tag",
                new IAttribute<object>[] {},
                ("first", new Node<object>("child", new IAttribute<object>[] {}))
            );
            var y = new KeyedNode<object, int>(
                "tag",
                new IAttribute<object>[] {},
                ("first", new Node<object>("child", new IAttribute<object>[] {}))
            );

            Assert.AreEqual(x, y);
            Assert.AreEqual(x.GetHashCode(), y.GetHashCode());
        }

        [Test]
        public void TestNodeTypes()
        {
            Assert.AreEqual(VTreeType.Node, new Node<object>("tag", NoAttrs()).GetNodeType());
            Assert.AreEqual(VTreeType.KeyedNode, new KeyedNode<object>("tag", NoAttrs()).GetNodeType());
            Assert.AreEqual(VTreeType.Widget, new TestWidget(NoAttrs()).GetNodeType());
        }

        [TestCaseSource("TestCase")]
        public void TestDiff(Veauty.IVTree oldTree, Veauty.IVTree newTree, Veauty.IPatch<object>[] expected)
        {
            var actual = Veauty.Diff<object>.Calc(oldTree, newTree);
            CollectionAssert.AreEqual(actual, expected);
        }

        [Test]
        public void TestDiffKeyedChildrenInsertAtBeginning()
        {
            var actual = Veauty.Diff<object>.Calc(
                new KeyedNode<object>("todo", NoAttrs(), ("a", Kid("a"))),
                new KeyedNode<object>("todo", NoAttrs(), ("b", Kid("b")), ("a", Kid("a")))
            );

            var expected = new IPatch<object>[] {
                new Reorder<object>(
                    0,
                    new IPatch<object>[] {},
                    new Reorder<object>.Insert[] {
                        new Reorder<object>.Insert {
                            index = 0,
                            entry = new Entry(Entry.Type.Insert, Kid("b"), 0)
                        }
                    },
                    new Reorder<object>.Insert[] {}
                )
            };

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void TestDiffKeyedChildrenRemoveAtBeginning()
        {
            var actual = Veauty.Diff<object>.Calc(
                new KeyedNode<object>("todo", NoAttrs(), ("a", Kid("a")), ("b", Kid("b"))),
                new KeyedNode<object>("todo", NoAttrs(), ("b", Kid("b")))
            );

            var expected = new IPatch<object>[] {
                new Reorder<object>(
                    0,
                    new IPatch<object>[] {
                        new Remove<object>(1)
                    },
                    new Reorder<object>.Insert[] {},
                    new Reorder<object>.Insert[] {}
                )
            };

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void TestDiffKeyedChildrenAppendAtEnd()
        {
            var actual = Veauty.Diff<object>.Calc(
                new KeyedNode<object>("todo", NoAttrs(), ("a", Kid("a"))),
                new KeyedNode<object>("todo", NoAttrs(), ("a", Kid("a")), ("b", Kid("b")))
            );

            var expected = new IPatch<object>[] {
                new Reorder<object>(
                    0,
                    new IPatch<object>[] {},
                    new Reorder<object>.Insert[] {},
                    new Reorder<object>.Insert[] {
                        new Reorder<object>.Insert {
                            index = -1,
                            entry = new Entry(Entry.Type.Insert, Kid("b"), -1)
                        }
                    }
                )
            };

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void TestDiffKeyedChildrenMove()
        {
            var movedEntry = new Entry(Entry.Type.Move, Kid("b"), 0);

            var actual = Veauty.Diff<object>.Calc(
                new KeyedNode<object>("todo", NoAttrs(), ("a", Kid("a")), ("b", Kid("b"))),
                new KeyedNode<object>("todo", NoAttrs(), ("b", Kid("b")), ("a", Kid("a")))
            );

            var expected = new IPatch<object>[] {
                new Reorder<object>(
                    0,
                    new IPatch<object>[] {
                        new Remove<object>(2, new IPatch<object>[] {}, movedEntry)
                    },
                    new Reorder<object>.Insert[] {
                        new Reorder<object>.Insert {
                            index = 0,
                            entry = movedEntry
                        }
                    },
                    new Reorder<object>.Insert[] {}
                )
            };

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void TestDiffKeyedChildrenMoveToEnd()
        {
            var movedEntry = new Entry(Entry.Type.Move, Kid("a"), -1);

            var actual = Veauty.Diff<object>.Calc(
                new KeyedNode<object>("todo", NoAttrs(), ("a", Kid("a")), ("b", Kid("b")), ("c", Kid("c"))),
                new KeyedNode<object>("todo", NoAttrs(), ("b", Kid("b")), ("c", Kid("c")), ("a", Kid("a")))
            );

            var expected = new IPatch<object>[] {
                new Reorder<object>(
                    0,
                    new IPatch<object>[] {
                        new Remove<object>(1, new IPatch<object>[] {}, movedEntry)
                    },
                    new Reorder<object>.Insert[] {},
                    new Reorder<object>.Insert[] {
                        new Reorder<object>.Insert {
                            index = -1,
                            entry = movedEntry
                        }
                    }
                )
            };

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void TestDiffKeyedChildrenMoveToEndWithLocalPatch()
        {
            var oldMovedNode = new Node<object>("a", new IAttribute<object>[] {
                new TestableAttribute(10)
            });
            var newMovedNode = new Node<object>("a", new IAttribute<object>[] {
                new TestableAttribute(20)
            });
            var movedEntry = new Entry(Entry.Type.Move, oldMovedNode, -1);

            var actual = Veauty.Diff<object>.Calc(
                new KeyedNode<object>("todo", NoAttrs(), ("a", oldMovedNode), ("b", Kid("b")), ("c", Kid("c"))),
                new KeyedNode<object>("todo", NoAttrs(), ("b", Kid("b")), ("c", Kid("c")), ("a", newMovedNode))
            );

            Assert.AreEqual(1, actual.Length);
            var reorder = actual[0] as Reorder<object>;
            Assert.IsNotNull(reorder);
            Assert.AreEqual(0, reorder.GetIndex());
            Assert.AreEqual(1, reorder.patches.Length);
            Assert.AreEqual(0, reorder.inserts.Length);
            Assert.AreEqual(1, reorder.endInserts.Length);

            var remove = reorder.patches[0] as Remove<object>;
            Assert.IsNotNull(remove);
            Assert.AreEqual(1, remove.GetIndex());
            Assert.AreEqual(movedEntry, remove.entry);
            CollectionAssert.AreEqual(
                new IPatch<object>[] {
                    new Attrs<object>(1, new System.Collections.Generic.Dictionary<string, IAttribute<object>>() {
                        { "test", new TestableAttribute(20) }
                    })
                },
                remove.patches
            );

            Assert.AreEqual(-1, reorder.endInserts[0].index);
            Assert.AreEqual(movedEntry, reorder.endInserts[0].entry);
        }

        [Test]
        public void TestDiffKeyedChildContentChangeUsesLocalPatchIndex()
        {
            var actual = Veauty.Diff<object>.Calc(
                new KeyedNode<object>(
                    "todo",
                    NoAttrs(),
                    ("a", new Node<object>("item", new IAttribute<object>[] {
                        new TestableAttribute(10)
                    }))
                ),
                new KeyedNode<object>(
                    "todo",
                    NoAttrs(),
                    ("a", new Node<object>("item", new IAttribute<object>[] {
                        new TestableAttribute(20)
                    }))
                )
            );

            var expected = new IPatch<object>[] {
                new Reorder<object>(
                    0,
                    new IPatch<object>[] {
                        new Attrs<object>(1, new System.Collections.Generic.Dictionary<string, IAttribute<object>>() {
                            { "test", new TestableAttribute(20) }
                        })
                    },
                    new Reorder<object>.Insert[] {},
                    new Reorder<object>.Insert[] {}
                )
            };

            CollectionAssert.AreEqual(expected, actual);
        }

        static IAttribute<object>[] NoAttrs() => new IAttribute<object>[] {};

        static IVTree Kid(string tag) => new Node<object>(tag, NoAttrs());

        static object[] TestCase =
        {
            new object[] { 
                new Node<object>("tag", new IAttribute<object>[] {}),
                new Node<object>("tag", new IAttribute<object>[] {}),
                new IPatch<object>[] {}
            },
            new object[] {
                new Node<object>("tag", new IAttribute<object>[] {
                    new TestableAttribute(10)
                }),
                new Node<object>("tag", new IAttribute<object>[] {
                    new TestableAttribute(20)
                }),
                new IPatch<object>[] {
                    new Attrs<object>(0, new System.Collections.Generic.Dictionary<string, IAttribute<object>>() {
                        { "test", new TestableAttribute(20) }
                    })
                }
            },
            new object[] {
                new Node<object>("tag", NoAttrs()),
                new Node<object>("tag", new IAttribute<object>[] {
                    new TestableAttribute(10)
                }),
                new IPatch<object>[] {
                    new Attrs<object>(0, new System.Collections.Generic.Dictionary<string, IAttribute<object>>() {
                        { "test", new TestableAttribute(10) }
                    })
                }
            },
            new object[] {
                new Node<object>(
                    "parent",
                    NoAttrs(),
                    new Node<object>("child", new IAttribute<object>[] {
                        new TestableAttribute(10)
                    })
                ),
                new Node<object>(
                    "parent",
                    NoAttrs(),
                    new Node<object>("child", new IAttribute<object>[] {
                        new TestableAttribute(20)
                    })
                ),
                new IPatch<object>[] {
                    new Attrs<object>(1, new System.Collections.Generic.Dictionary<string, IAttribute<object>>() {
                        { "test", new TestableAttribute(20) }
                    })
                }
            },
            new object[] {
                new Node<object, int>("tag", NoAttrs()),
                new Node<object, float>("tag", NoAttrs()),
                new IPatch<object>[] {
                    new Attach<object>(0, typeof(int), typeof(float))
                }
            },
            new object[] {
                new Node<object>("tag", NoAttrs()),
                new KeyedNode<object>("tag", NoAttrs(), ("child", Kid("child"))),
                new IPatch<object>[] {
                    new Append<object>(0, 0, new IVTree[] {
                        Kid("child")
                    })
                }
            },
            new object[] {
                new KeyedNode<object>("tag", NoAttrs(), ("child", Kid("child"))),
                new Node<object>("tag", NoAttrs()),
                new IPatch<object>[] {
                    new RemoveLast<object>(0, 0, 1)
                }
            },
            new object[] {
                new Node<object, int>("tag", NoAttrs()),
                new KeyedNode<object, float>("tag", NoAttrs()),
                new IPatch<object>[] {
                    new Attach<object>(0, typeof(int), typeof(float))
                }
            },
            new object[] {
                new Node<object>("tag", new IAttribute<object>[] {
                    new TestableAttribute(10)
                }),
                new Node<object>("tag", new IAttribute<object>[] {
                    new TestableAttribute(10)
                }),
                new IPatch<object>[] {}
            },
            new object[] {
                new Node<object>("tag", new IAttribute<object>[] {}, new IVTree[] {
                    new Node<object>("child", new IAttribute<object>[] {})
                }),
                new Node<object>("tag", new IAttribute<object>[] {}, new IVTree[] {
                    new Node<object>("child", new IAttribute<object>[] {})
                }),
                new IPatch<object>[] {}
            },
            new object[] {
                new Node<object>("tag", new IAttribute<object>[] {}),
                new Node<object>("gat", new IAttribute<object>[] {}),
                new IPatch<object>[] {
                    new Redraw<object>(0, new Node<object>("gat", new IAttribute<object>[] {}))
                },
            },
            new object[] {
                new TestWidget(new IAttribute<object>[] {}),
                new TestWidget(new IAttribute<object>[] {}),
                new IPatch<object>[] {}
            },
            new object[] {
                new TestWidget(new IAttribute<object>[] {
                    new TestableAttribute(10)
                }),
                new TestWidget(new IAttribute<object>[] {}),
                new IPatch<object>[] {
                    new Attrs<object>(0, new System.Collections.Generic.Dictionary<string, IAttribute<object>>() {
                        { "test", null }
                    })
                }
            },
            new object[] {
                new Node<object>(
                    "todo",
                    new IAttribute<object>[] {},
                    new Node<object>("todo1", new IAttribute<object>[] {}),
                    new Node<object>("todo2", new IAttribute<object>[] {})
                ),
                new Node<object>(
                    "todo",
                    new IAttribute<object>[] {},
                    new Node<object>("todo1", new IAttribute<object>[] {}),
                    new Node<object>("todo2", new IAttribute<object>[] {}),
                    new Node<object>("todo3", new IAttribute<object>[] {})
                ),
                new IPatch<object>[] {
                    new Append<object>(0, 2, new IVTree[] {
                        new Node<object>("todo1", new IAttribute<object>[] {}),
                        new Node<object>("todo2", new IAttribute<object>[] {}),
                        new Node<object>("todo3", new IAttribute<object>[] {})
                    })
                }
            },
            new object[] {
                new Node<object>(
                    "todo",
                    new IAttribute<object>[] {},
                    new Node<object>("todo1", new IAttribute<object>[] {}),
                    new Node<object>("todo2", new IAttribute<object>[] {}),
                    new Node<object>("todo3", new IAttribute<object>[] {})
                ),
                new Node<object>(
                    "todo",
                    new IAttribute<object>[] {},
                    new Node<object>("todo1", new IAttribute<object>[] {}),
                    new Node<object>("todo2", new IAttribute<object>[] {})
                ),
                new IPatch<object>[] {
                    new RemoveLast<object>(0, 2, 1)
                }
            }
        };
    }
}
