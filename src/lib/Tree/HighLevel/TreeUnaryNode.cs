using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeUnaryNode : TreeNode
    {
        public string Operator { get; }

        public TreeReference Operand { get; }

        public override TreeType Type
        {
            get
            {
                var type = Operand.Value.Type;

                switch (Operator)
                {
                    case "+":
                    case "-":
                        switch (type)
                        {
                            case TreeType.Integer:
                            case TreeType.Real:
                                return type;
                        }

                        break;
                    case "~":
                        if (type == TreeType.Integer)
                            return type;

                        break;
                }

                return TreeType.Any;
            }
        }

        public TreeUnaryNode(TreeContext context, SourceLocation location, string @operator, TreeReference operand)
            : base(context, location)
        {
            Operator = @operator;
            Operand = operand;
        }
    }
}
