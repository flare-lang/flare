using System.Collections.Immutable;
using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeSetNode : TreeNode
    {
        public ImmutableArray<TreeReference> Elements { get; }

        public bool IsMutable { get; }

        public override TreeType Type => TreeType.Set;

        public TreeSetNode(TreeContext context, SourceLocation location, ImmutableArray<TreeReference> elements,
            bool mutable)
            : base(context, location)
        {
            Elements = elements;
            IsMutable = mutable;
        }
    }
}
