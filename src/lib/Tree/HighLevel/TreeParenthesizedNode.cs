using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeParenthesizedNode : TreeNode
    {
        public TreeReference Expression { get; }

        public override TreeType Type => Expression.Value.Type;

        public TreeParenthesizedNode(TreeContext context, SourceLocation location, TreeReference expression)
            : base(context, location)
        {
            Expression = expression;
        }

        public override TreeReference Reduce()
        {
            return Expression;
        }
    }
}
