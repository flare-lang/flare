using Flare.Runtime;
using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeConstantNode : TreeNode
    {
        public Constant Constant { get; }

        public override TreeType Type => TreeType.Any;

        public TreeConstantNode(TreeContext context, SourceLocation location, Constant constant)
            : base(context, location)
        {
            Constant = constant;
        }
    }
}
