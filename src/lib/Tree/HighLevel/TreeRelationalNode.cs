using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeRelationalNode : TreeNode
    {
        public TreeReference Left { get; }

        public TreeRelationalOperator Operator { get; }

        public TreeReference Right { get; }

        public override TreeType Type
        {
            get
            {
                switch (Left.Value.Type)
                {
                    case TreeType.Nil:
                    case TreeType.Boolean:
                    case TreeType.Atom:
                    case TreeType.Integer:
                    case TreeType.Real:
                    case TreeType.String:
                    case TreeType.Module:
                    case TreeType.Function:
                    case TreeType.Tuple:
                    case TreeType.Array:
                    case TreeType.Set:
                    case TreeType.Map:
                        return TreeType.Boolean;
                    default:
                        return TreeType.Any;
                }
            }
        }

        public TreeRelationalNode(TreeContext context, SourceLocation location, TreeReference left,
            TreeRelationalOperator @operator, TreeReference right)
            : base(context, location)
        {
            Left = left;
            Operator = @operator;
            Right = right;
        }
    }
}
