using Flare.Syntax;
using Flare.Tree.HighLevel.Patterns;

namespace Flare.Tree.HighLevel
{
    sealed class TreeUseNode : TreeNode
    {
        public TreePattern Pattern { get; }

        public TreeReference Initializer { get; }

        public override TreeType Type => Initializer.Value.Type;

        public TreeUseNode(TreeContext context, SourceLocation location, TreePattern pattern, TreeReference initializer)
            : base(context, location)
        {
            Pattern = pattern;
            Initializer = initializer;
        }
    }
}
