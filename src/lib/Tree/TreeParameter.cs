namespace Flare.Tree
{
    class TreeParameter : TreeVariable
    {
        public new string Name => base.Name!;

        public TreeParameter(string name)
            : base(TreeType.Any, name, false)
        {
        }
    }
}
