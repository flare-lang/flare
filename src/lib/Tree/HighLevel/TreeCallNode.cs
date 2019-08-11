using System.Collections.Immutable;
using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeCallNode : TreeNode
    {
        public TreeReference Subject { get; }

        public ImmutableArray<TreeReference> Arguments { get; }

        public TreeReference? VariadicArgument { get; }

        public TreeCallTry? Try { get; }

        public override TreeType Type => TreeType.Any;

        public TreeCallNode(TreeContext context, SourceLocation location, TreeReference subject,
            ImmutableArray<TreeReference> arguments, TreeReference? variadicArgument, TreeCallTry? @try)
            : base(context, location)
        {
            Subject = subject;
            Arguments = arguments;
            VariadicArgument = variadicArgument;
            Try = @try;
        }
    }
}
