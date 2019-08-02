using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeBinaryNode : TreeNode
    {
        public TreeReference Left { get; }

        public string Operator { get; }

        public TreeReference Right { get; }

        public override TreeType Type
        {
            get
            {
                var type = Left.Value.Type;

                switch (Operator)
                {
                    case "%":
                    case "*":
                    case "+":
                    case "-":
                    case "/":
                        switch (type)
                        {
                            case TreeType.Integer:
                            case TreeType.Real:
                                return type;
                        }

                        break;
                    case "&":
                    case "^":
                    case "|":
                        switch (type)
                        {
                            case TreeType.Boolean:
                            case TreeType.Integer:
                                return type;
                        }

                        break;
                    case "<<":
                    case ">>":
                        if (type == TreeType.Integer)
                            return type;

                        break;
                    case "~":
                        switch (type)
                        {
                            case TreeType.String:
                            case TreeType.Tuple:
                            case TreeType.Array:
                                return type;
                        }

                        break;
                }

                return TreeType.Any;
            }
        }

        public TreeBinaryNode(TreeContext context, SourceLocation location, TreeReference left, string @operator,
            TreeReference right)
            : base(context, location)
        {
            Left = left;
            Operator = @operator;
            Right = right;
        }
    }
}
