namespace Flare.Tree
{
    sealed class TreeFragment : TreeParameter
    {
        public new string Name => base.Name!;

        public TreeFragment(string name)
            : base(name)
        {
        }
    }
}
