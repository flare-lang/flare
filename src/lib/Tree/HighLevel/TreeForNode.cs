using System;
using Flare.Syntax;
using Flare.Tree.HighLevel.Patterns;

namespace Flare.Tree.HighLevel
{
    sealed class TreeForNode : TreeNode
    {
        public TreePattern Pattern { get; }

        public TreeReference Collection { get; }

        public TreeReference Body { get; }

        public override TreeType Type => TreeType.Nil;

        public TreeForNode(TreeContext context, SourceLocation location, TreePattern pattern, TreeReference collection,
            TreeReference body)
            : base(context, location)
        {
            Pattern = pattern;
            Collection = collection;
            Body = body;
        }
    }
}
