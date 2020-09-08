using System.Diagnostics.CodeAnalysis;

namespace Flare.Syntax
{
    [SuppressMessage("Microsoft.Performance", "CA1815", Justification = "Unnecessary.")]
    public readonly struct SeparatedSyntaxTokenList
    {
        public SyntaxTokenList Tokens { get; }

        public SyntaxTokenList Separators { get; }

        internal SeparatedSyntaxTokenList(SyntaxTokenList tokens, SyntaxTokenList separators)
        {
            Tokens = tokens;
            Separators = separators;
        }

        public SeparatedSyntaxTokenList DeepClone()
        {
            return new SeparatedSyntaxTokenList(Tokens.DeepClone(), Separators.DeepClone());
        }
    }
}
