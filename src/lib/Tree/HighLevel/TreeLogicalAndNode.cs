using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeLogicalAndNode : TreeNode
    {
        public TreeReference Left { get; }

        public TreeReference Right { get; }

        public override TreeType Type => TreeType.Boolean;

        public TreeLogicalAndNode(TreeContext context, SourceLocation location, TreeReference left, TreeReference right)
            : base(context, location)
        {
            Left = left;
            Right = right;
        }

        public override TreeReference Reduce()
        {
            // Rewrite a && b to if (!a) { false; } else { !!b; }.

            return new TreeIfNode(Context, Location,
                new TreeLogicalNotNode(Context, Location.WithMissing(), Left),
                new TreeLiteralNode(Context, Location.WithMissing(), false),
                new TreeLogicalNotNode(Context, Location.WithMissing(),
                    new TreeLogicalNotNode(Context, Location.WithMissing(), Right)));
        }
    }
}
