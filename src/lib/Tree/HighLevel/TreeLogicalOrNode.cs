using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeLogicalOrNode : TreeNode
    {
        public TreeReference Left { get; }

        public TreeReference Right { get; }

        public override TreeType Type => TreeType.Boolean;

        public TreeLogicalOrNode(TreeContext context, SourceLocation location, TreeReference left, TreeReference right)
            : base(context, location)
        {
            Left = left;
            Right = right;
        }

        public override TreeReference Reduce()
        {
            // Rewrite a || b to !(!a && !b).

            return new TreeLogicalNotNode(Context, Location,
                new TreeLogicalAndNode(Context, Location.WithMissing(),
                    new TreeLogicalNotNode(Context, Location.WithMissing(), Left),
                    new TreeLogicalNotNode(Context, Location.WithMissing(), Right)));
        }
    }
}
