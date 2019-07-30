namespace Flare.Syntax
{
    public sealed class SyntaxTrivia
    {
        public SyntaxToken Parent { get; internal set; }

        public SourceLocation Location { get; }

        public SyntaxTriviaKind Kind { get; }

        public string Text { get; }

        internal SyntaxTrivia(SourceLocation location, SyntaxTriviaKind kind, string text)
        {
            Parent = null!;
            Location = location;
            Kind = kind;
            Text = text;
        }

        public SyntaxTrivia DeepClone()
        {
            return new SyntaxTrivia(Location, Kind, Text)
            {
                Parent = Parent,
            };
        }

        public override string ToString()
        {
            return Text;
        }
    }
}
