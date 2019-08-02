using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeAssertNode : TreeNode
    {
        public TreeReference Operand { get; }

        public string Message { get; }

        public override TreeType Type => TreeType.Nil;

        public TreeAssertNode(TreeContext context, SourceLocation location, TreeReference operand, string message)
            : base(context, location)
        {
            Operand = operand;
            Message = message;
        }
    }
}
