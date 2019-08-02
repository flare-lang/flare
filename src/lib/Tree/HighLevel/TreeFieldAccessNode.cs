using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeFieldAccessNode : TreeNode
    {
        public TreeReference Subject { get; }

        public string Name { get; }

        public override TreeType Type => TreeType.Any;

        public TreeFieldAccessNode(TreeContext context, SourceLocation location, TreeReference subject, string name)
            : base(context, location)
        {
            Subject = subject;
            Name = name;
        }
    }
}
