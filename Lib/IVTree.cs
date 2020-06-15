namespace Veauty
{
    public enum VTreeType
    {
        Node,
        KeyedNode,
        Widget
    }

    public interface IVTree
    {
        VTreeType GetNodeType();
        int GetDescendantsCount();
    }

    public interface IParent
    {
        IVTree[] GetKids();
    }
}