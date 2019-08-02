using System;
using System.Collections.Immutable;
using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeMacroCallNode : TreeNode
    {
        public ImmutableArray<TreeReference> Arguments { get; }

        public override TreeType Type => TreeType.Any;

        public TreeMacroCallNode(TreeContext context, SourceLocation location, ImmutableArray<TreeReference> arguments)
            : base(context, location)
        {
            Arguments = arguments;
        }
    }
}
