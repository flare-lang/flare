using System.Collections.Immutable;
using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeIndexNode : TreeNode
    {
        public TreeReference Subject { get; }

        public ImmutableArray<TreeReference> Arguments { get; }

        public TreeReference? VariadicArgument { get; }

        public override TreeType Type => TreeType.Any;

        public TreeIndexNode(TreeContext context, SourceLocation location, TreeReference subject,
            ImmutableArray<TreeReference> arguments, TreeReference? variadicArgument)
            : base(context, location)
        {
            Subject = subject;
            Arguments = arguments;
            VariadicArgument = variadicArgument;
        }
    }
}
