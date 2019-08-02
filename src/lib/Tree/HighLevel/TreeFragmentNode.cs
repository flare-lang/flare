using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeFragmentNode : TreeNode
    {
        public TreeFragment Fragment { get; }

        public override TreeType Type => TreeType.Any;

        public TreeFragmentNode(TreeContext context, SourceLocation location, TreeFragment fragment)
            : base(context, location)
        {
            Fragment = fragment;
        }
    }
}
