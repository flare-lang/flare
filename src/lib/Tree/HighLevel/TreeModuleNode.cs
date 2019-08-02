using System.Collections.Immutable;
using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeModuleNode : TreeNode
    {
        public ImmutableArray<string> Path { get; }

        public override TreeType Type => TreeType.Module;

        public TreeModuleNode(TreeContext context, SourceLocation location, ImmutableArray<string> path)
            : base(context, location)
        {
            Path = path;
        }
    }
}
