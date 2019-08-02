using System.Collections.Immutable;
using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeBlockNode : TreeNode
    {
        public ImmutableArray<TreeReference> Expressions { get; }

        public override TreeType Type => Expressions[Expressions.Length - 1].Value.Type;

        public TreeBlockNode(TreeContext context, SourceLocation location, ImmutableArray<TreeReference> expressions)
            : base(context, location)
        {
            Expressions = expressions;
        }
    }
}
