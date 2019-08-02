using System;
using System.Collections.Immutable;
using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeReceiveNode : TreeNode
    {
        public ImmutableArray<TreePatternArm> Arms { get; }

        public TreeReference? Else { get; }

        public override TreeType Type
        {
            get
            {
                var type = Arms[0].Body.Value.Type;

                foreach (var arm in Arms)
                    if (arm.Body.Value.Type != type)
                        return TreeType.Any;

                return Else is TreeReference @else && @else.Value.Type == type ? type : TreeType.Any;
            }
        }

        public TreeReceiveNode(TreeContext context, SourceLocation location, ImmutableArray<TreePatternArm> arms,
            TreeReference @else)
            : base(context, location)
        {
            Arms = arms;
            Else = @else;
        }
    }
}
