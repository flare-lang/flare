using System.Collections.Immutable;
using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeMapNode : TreeNode
    {
        public ImmutableArray<TreeMapPair> Pairs { get; }

        public bool IsMutable { get; }

        public override TreeType Type => TreeType.Map;

        public TreeMapNode(TreeContext context, SourceLocation location, ImmutableArray<TreeMapPair> pairs,
            bool mutable)
            : base(context, location)
        {
            Pairs = pairs;
            IsMutable = mutable;
        }
    }
}
