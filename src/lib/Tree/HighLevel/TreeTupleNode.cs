using System.Collections.Immutable;
using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeTupleNode : TreeNode
    {
        public ImmutableArray<TreeReference> Components { get; }

        public override TreeType Type => TreeType.Tuple;

        public TreeTupleNode(TreeContext context, SourceLocation location, ImmutableArray<TreeReference> components)
            : base(context, location)
        {
            Components = components;
        }
    }
}
