using System.Collections.Immutable;
using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeConditionNode : TreeNode
    {
        public TreeReference Operand { get; }

        public ImmutableArray<TreeConditionArm> Arms { get; }

        public override TreeType Type
        {
            get
            {
                var type = Arms[0].Body.Value.Type;

                foreach (var arm in Arms)
                    if (arm.Body.Value.Type != type)
                        return TreeType.Any;

                return type;
            }
        }

        public TreeConditionNode(TreeContext context, SourceLocation location, TreeReference operand,
            ImmutableArray<TreeConditionArm> arms)
            : base(context, location)
        {
            Operand = operand;
            Arms = arms;
        }
    }
}
