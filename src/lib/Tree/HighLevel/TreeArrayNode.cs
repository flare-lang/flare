using System.Collections.Immutable;
using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeArrayNode : TreeNode
    {
        public ImmutableArray<TreeReference> Elements { get; }

        public bool IsMutable { get; }

        public override TreeType Type => TreeType.Array;

        public TreeArrayNode(TreeContext context, SourceLocation location, ImmutableArray<TreeReference> elements,
            bool mutable)
            : base(context, location)
        {
            Elements = elements;
            IsMutable = mutable;
        }
    }
}
