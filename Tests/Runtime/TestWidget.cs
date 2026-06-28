using NUnit.Framework;
using Veauty;
using Veauty.VTree;

namespace Tests
{
    public class TestWidget
    {
        [Test]
        public void TestDescendantsCountIncludesChildrenAndTheirDescendants()
        {
            var widget = new TestableWidget(
                new IAttribute<object>[] {},
                new Node<object>(
                    "parent",
                    new IAttribute<object>[] {},
                    new Node<object>("child", new IAttribute<object>[] {})
                )
            );

            Assert.AreEqual(2, widget.GetDescendantsCount());
        }

        class TestableWidget : Widget<object>
        {
            public TestableWidget(IAttribute<object>[] attrs, params IVTree[] kids) : base(attrs, kids) {}

            public override object Init(object obj) => obj;

            public override IVTree Render() => new Node<object>("widget", new IAttribute<object>[] {}, kids);

            public override void Destroy(object obj) {}
        }
    }
}
