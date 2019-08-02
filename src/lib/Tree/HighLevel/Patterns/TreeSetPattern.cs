using System.Collections.Immutable;

namespace Flare.Tree.HighLevel.Patterns
{
    sealed class TreeSetPattern : TreePattern
    {
        public ImmutableArray<TreeReference> Elements { get; }

        public TreePattern? Remainder { get; }

        public TreeSetPattern(TreeLocal? alias, ImmutableArray<TreeReference> elements, TreePattern? remainder)
            : base(alias)
        {
            Elements = elements;
            Remainder = remainder;
        }
    }
}
