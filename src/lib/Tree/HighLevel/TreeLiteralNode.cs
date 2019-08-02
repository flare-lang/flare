using System;
using System.Numerics;
using Flare.Syntax;

namespace Flare.Tree.HighLevel
{
    sealed class TreeLiteralNode : TreeNode
    {
        public object Value { get; }

        public override TreeType Type
        {
            get
            {
                return Value switch
                {
                    null => TreeType.Nil,
                    bool _ => TreeType.Boolean,
                    string _ => TreeType.Atom,
                    BigInteger _ => TreeType.Integer,
                    double _ => TreeType.Real,
                    ReadOnlyMemory<byte> _ => TreeType.String,
                    _ => throw Assert.Unreachable(),
                };
            }
        }

        public TreeLiteralNode(TreeContext context, SourceLocation location, object value)
            : base(context, location)
        {
            Value = value;
        }
    }
}
