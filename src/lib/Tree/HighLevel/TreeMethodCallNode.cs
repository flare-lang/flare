using System.Collections.Immutable;
using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeMethodCallNode : TreeNode
    {
        public TreeReference Subject { get; }

        public string Name { get; }

        public ImmutableArray<TreeReference> Arguments { get; }

        public TreeReference? VariadicArgument { get; }

        public TreeCallTry? Try { get; }

        public override TreeType Type => TreeType.Any;

        public TreeMethodCallNode(TreeContext context, SourceLocation location, TreeReference subject, string name,
            ImmutableArray<TreeReference> arguments, TreeReference? variadicArgument, TreeCallTry? @try)
            : base(context, location)
        {
            Subject = subject;
            Name = name;
            Arguments = arguments;
            VariadicArgument = variadicArgument;
            Try = @try;
        }
    }
}
