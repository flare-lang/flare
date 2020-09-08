using System.Collections.Immutable;

namespace Flare.Tree.HighLevel.Patterns
{
    sealed class TreeMapPattern : TreePattern
    {
        public ImmutableArray<TreeMapPatternPair> Pairs { get; }

        public TreePattern? Remainder { get; }

        public TreeMapPattern(TreeLocal? alias, ImmutableArray<TreeMapPatternPair> pairs, TreePattern? remainder)
            : base(alias)
        {
            Pairs = pairs;
            Remainder = remainder;
        }
    }
}
