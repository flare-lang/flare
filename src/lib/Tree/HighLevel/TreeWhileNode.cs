using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeWhileNode : TreeNode
    {
        public TreeReference Condition { get; }

        public TreeReference Body { get; }

        public override TreeType Type => TreeType.Nil;

        public TreeWhileNode(TreeContext context, SourceLocation location, TreeReference condition, TreeReference body)
            : base(context, location)
        {
            Condition = condition;
            Body = body;
        }
    }
}
