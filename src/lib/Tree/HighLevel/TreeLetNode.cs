using System;
using Flare.Syntax;
using Flare.Tree.HighLevel.Patterns;

namespace Flare.Tree.NodHighLeveles
{
    sealed class TreeLetNode : TreeNode
    {
        public TreePattern Pattern { get; }

        public TreeReference Initializer { get; }

        public override TreeType Type => Initializer.Value.Type;

        public TreeLetNode(TreeContext context, SourceLocation location, TreePattern pattern, TreeReference initializer)
            : base(context, location)
        {
            Pattern = pattern;
            Initializer = initializer;
        }
    }
}
