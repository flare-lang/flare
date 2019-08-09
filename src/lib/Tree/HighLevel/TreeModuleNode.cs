using Flare.Runtime;
using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeModuleNode : TreeNode
    {
        public Module Module { get; }

        public override TreeType Type => TreeType.Module;

        public TreeModuleNode(TreeContext context, SourceLocation location, Module module)
            : base(context, location)
        {
            Module = module;
        }
    }
}
