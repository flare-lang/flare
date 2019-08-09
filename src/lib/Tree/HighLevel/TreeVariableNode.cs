using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeVariableNode : TreeNode
    {
        public TreeVariable Variable { get; }

        public override TreeType Type => Variable.Type;

        public TreeVariableNode(TreeContext context, SourceLocation location, TreeVariable variable)
            : base(context, location)
        {
            Variable = variable;
        }
    }
}
