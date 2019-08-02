using System.Collections.Immutable;
using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeExceptionNode : TreeNode
    {
        public string Name { get; }

        public ImmutableArray<TreeRecordField> Fields { get; }

        public override TreeType Type => TreeType.Exception;

        public TreeExceptionNode(TreeContext context, SourceLocation location, string name,
            ImmutableArray<TreeRecordField> fields)
            : base(context, location)
        {
            Name = name;
            Fields = fields;
        }
    }
}
