using System.Collections.Immutable;

namespace Flare.Tree.HighLevel.Patterns
{
    sealed class TreeTuplePattern : TreePattern
    {
        public ImmutableArray<TreePattern> Components { get; }

        public TreeTuplePattern(TreeLocal? alias, ImmutableArray<TreePattern> components)
            : base(alias)
        {
            Components = components;
        }
    }
}
