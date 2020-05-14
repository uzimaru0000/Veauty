namespace Veauty.VTree
{
    public struct VText : IVTree
    {
        public readonly string text;
        public VText(string text)
        {
            this.text = text;
        }

        public VTreeType GetType() => VTreeType.Text;
        public int GetDescendantsCount() => 0;

    }
}