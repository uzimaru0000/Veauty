namespace Veauty
{
    public enum VTreeType
    {
        Node,
        KeyedNode,
        FunctionComponent
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

    public interface IVTreeWrapper : IVTree
    {
        IVTree Unwrap();
    }
}
