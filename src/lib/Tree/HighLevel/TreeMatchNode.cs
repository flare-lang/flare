using System;
using System.Collections.Immutable;
using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeMatchNode : TreeNode
    {
        public TreeReference Operand { get; }

        public ImmutableArray<TreePatternArm> Arms { get; }

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

        public TreeMatchNode(TreeContext context, SourceLocation location, TreeReference operand,
            ImmutableArray<TreePatternArm> arms)
            : base(context, location)
        {
            Operand = operand;
            Arms = arms;
        }
    }
}
