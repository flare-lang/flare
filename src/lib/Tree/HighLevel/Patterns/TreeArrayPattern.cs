using System.Collections.Immutable;

namespace Flare.Tree.HighLevel.Patterns
{
    sealed class TreeArrayPattern : TreePattern
    {
        public ImmutableArray<TreePattern> Elements { get; }

        public TreePattern? Remainder { get; }

        public TreeArrayPattern(TreeLocal? alias, ImmutableArray<TreePattern> elements, TreePattern? remainder)
            : base(alias)
        {
            Elements = elements;
            Remainder = remainder;
        }
    }
}
