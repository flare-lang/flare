using System.Collections.Immutable;

namespace Flare.Tree.HighLevel.Patterns
{
    sealed class TreeRecordPattern : TreePattern
    {
        public string? Name { get; }

        public ImmutableArray<TreeRecordPatternField> Fields { get; }

        public TreeRecordPattern(TreeLocal? alias, string? name, ImmutableArray<TreeRecordPatternField> fields)
            : base(alias)
        {
            Name = name;
            Fields = fields;
        }
    }
}
