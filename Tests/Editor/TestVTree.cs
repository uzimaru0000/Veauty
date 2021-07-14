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
        [TestCaseSource("TestCase")]
        public void TestDiff(Veauty.IVTree oldTree, Veauty.IVTree newTree, Veauty.IPatch<object>[] expected)
        {
            var actual = Veauty.Diff<object>.Calc(oldTree, newTree);
            CollectionAssert.AreEqual(actual, expected);
        }

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
