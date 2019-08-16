using Flare.Metadata;
using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeFunctionNode : TreeNode
    {
        public Function Function { get; }

        public override TreeType Type => TreeType.Function;

        public TreeFunctionNode(TreeContext context, SourceLocation location, Function function)
            : base(context, location)
        {
            Function = function;
        }
    }
}
