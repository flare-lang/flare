using System.Collections.Immutable;
using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeRecordNode : TreeNode
    {
        public string? Name { get; }

        public ImmutableArray<TreeRecordField> Fields { get; }

        public override TreeType Type => TreeType.Record;

        public TreeRecordNode(TreeContext context, SourceLocation location, string? name,
            ImmutableArray<TreeRecordField> fields)
            : base(context, location)
        {
            Name = name;
            Fields = fields;
        }
    }
}
