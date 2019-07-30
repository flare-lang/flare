using System;

namespace Flare.Syntax
{
    [Flags]
    public enum SyntaxLintContexts : long
    {
        None = 0b000000,
        Program = 0b000001,
        Declaration = 0b000010,
        NamedDeclaration = 0b000100,
        Statement = 0b001000,
        Expression = 0b010000,
        Pattern = 0b100000,
    }
}
