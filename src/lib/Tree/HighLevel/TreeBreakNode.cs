using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeBreakNode : TreeNode
    {
        public TreeReference Target { get; }

        public override TreeType Type => TreeType.Nil;

        public TreeBreakNode(TreeContext context, SourceLocation location, TreeReference target)
            : base(context, location)
        {
            Target = target;
        }
    }
}
