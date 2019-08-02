using Flare.Syntax;

namespace Flare.Tree
{
    abstract class TreeNode
    {
        public TreeReference Reference { get; }

        public TreeContext Context => Reference.Context;

        public SourceLocation Location { get; }

        public abstract TreeType Type { get; }

        public TreeNode(TreeContext context, SourceLocation location)
        {
            Reference = context.RegisterNode(this);
            Location = location;
        }

        public virtual TreeReference Reduce()
        {
            return this;
        }
    }
}
