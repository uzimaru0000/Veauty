using NUnit.Framework;
using Veauty;
using Veauty.VTree;

namespace Tests
{
    public class TestHostLifecycleNode
    {
        [Test]
        public void TestDescendantsCountIncludesChildrenAndTheirDescendants()
        {
            var node = new HostLifecycleNode<object>(
                "parent",
                new IAttribute<object>[] {},
                new IHostLifecycle<object>[] {},
                new Node<object>(
                    "child",
                    new IAttribute<object>[] {},
                    new Node<object>("grandchild", new IAttribute<object>[] {})
                )
            );

            Assert.AreEqual(2, node.GetDescendantsCount());
        }
    }
}
