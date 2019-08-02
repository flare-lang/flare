using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeRaiseNode : TreeNode
    {
        public TreeReference Operand { get; }

        public override TreeType Type => Operand.Value.Type;

        public TreeRaiseNode(TreeContext context, SourceLocation location, TreeReference operand)
            : base(context, location)
        {
            Operand = operand;
        }
    }
}
