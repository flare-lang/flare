using Flare.Metadata;
using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeExternalNode : TreeNode
    {
        public External External { get; }

        public override TreeType Type => TreeType.Function;

        public TreeExternalNode(TreeContext context, SourceLocation location, External external)
            : base(context, location)
        {
            External = external;
        }
    }
}
