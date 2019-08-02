using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeReturnNode : TreeNode
    {
        public TreeReference Operand { get; }

        public override TreeType Type => Operand.Value.Type;

        public TreeReturnNode(TreeContext context, SourceLocation location, TreeReference operand)
            : base(context, location)
        {
            Operand = operand;
        }
    }
}
