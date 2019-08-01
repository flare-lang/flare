namespace Flare.Syntax
{
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
