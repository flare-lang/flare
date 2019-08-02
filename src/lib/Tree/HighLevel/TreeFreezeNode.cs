using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeFreezeNode : TreeNode
    {
        public TreeReference Operand { get; }

        public bool IsCollection { get; }

        public override TreeType Type => Operand.Value.Type;

        public TreeFreezeNode(TreeContext context, SourceLocation location, TreeReference operand, bool collection)
            : base(context, location)
        {
            Operand = operand;
            IsCollection = collection;
        }
    }
}
