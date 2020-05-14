namespace Veauty.VTree
{
    public enum VTreeType
    {
        Node,
        KeyedNode,
        Widget
    }

    public interface IVTree
    {
        VTreeType GetType();
        int GetDescendantsCount();
    }

    public interface IParent
    {
        IVTree[] GetKids();
    }
}