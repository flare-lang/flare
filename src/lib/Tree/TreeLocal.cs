namespace Flare.Tree
{
    sealed class TreeLocal : TreeVariable
    {
        public TreeLocal(TreeType type, string? name, bool mutable)
            : base(type, name, mutable)
        {
        }
    }
}
