using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeLogicalNotNode : TreeNode
    {
        public TreeReference Operand { get; }

        public override TreeType Type => TreeType.Boolean;

        public TreeLogicalNotNode(TreeContext context, SourceLocation location, TreeReference operand)
            : base(context, location)
        {
            Operand = operand;
        }
    }
}
