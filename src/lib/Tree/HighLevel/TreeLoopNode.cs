using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeLoopNode : TreeNode
    {
        public TreeReference Target { get; }

        public override TreeType Type => TreeType.Nil;

        public TreeLoopNode(TreeContext context, SourceLocation location, TreeReference target)
            : base(context, location)
        {
            Target = target;
        }
    }
}
