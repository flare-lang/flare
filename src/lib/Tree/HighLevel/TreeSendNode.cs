using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeSendNode : TreeNode
    {
        public TreeReference Left { get; }

        public TreeReference Right { get; }

        public override TreeType Type => Right.Value.Type;

        public TreeSendNode(TreeContext context, SourceLocation location, TreeReference left, TreeReference right)
            : base(context, location)
        {
            Left = left;
            Right = right;
        }
    }
}
