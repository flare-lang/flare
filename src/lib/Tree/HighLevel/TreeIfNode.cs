using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeIfNode : TreeNode
    {
        public TreeReference Condition { get; }

        public TreeReference Then { get; }

        public TreeReference Else { get; }

        public override TreeType Type
        {
            get
            {
                var then = Then.Value.Type;

                return then == Else.Value.Type ? then : TreeType.Any;
            }
        }

        public TreeIfNode(TreeContext context, SourceLocation location, TreeReference condition, TreeReference then,
            TreeReference @else)
            : base(context, location)
        {
            Condition = condition;
            Then = then;
            Else = @else;
        }
    }
}
