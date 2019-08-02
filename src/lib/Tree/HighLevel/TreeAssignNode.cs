using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeAssignNode : TreeNode
    {
        public TreeReference Left { get; }

        public TreeReference Right { get; }

        public override TreeType Type => Right.Value.Type;

        public TreeAssignNode(TreeContext context, SourceLocation location, TreeReference left, TreeReference right)
            : base(context, location)
        {
            Left = left;
            Right = right;
        }
    }
}
