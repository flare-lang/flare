namespace Flare.Tree
{
    sealed class TreeVariadicParameter : TreeVariable
    {
        public new string Name => base.Name!;

        public TreeVariadicParameter(string name)
            : base(TreeType.Array, name, false)
        {
        }
    }
}
