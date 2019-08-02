using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeIdentifierNode : TreeNode
    {
        public string Identifier { get; }

        public override TreeType Type => TreeType.Nil;

        public TreeIdentifierNode(TreeContext context, SourceLocation location, string identifier)
            : base(context, location)
        {
            Identifier = identifier;
        }
    }
}
