using System.Collections.Immutable;
using System.Linq;

namespace Flare.Syntax
{
    public sealed class SyntaxToken
    {
        public SyntaxNode? Parent { get; internal set; }

        public SourceLocation Location { get; }

        public SyntaxTokenKind Kind { get; }

        public bool IsMissing => Kind == SyntaxTokenKind.Missing;

        public bool IsEndOfInput => Kind == SyntaxTokenKind.EndOfInput;

        public string Text { get; }

        public object? Value { get; }

        public bool HasLeadingTrivia => LeadingTrivia.Length != 0;

        public ImmutableArray<SyntaxTrivia> LeadingTrivia { get; }

        public bool HasTrailingTrivia => TrailingTrivia.Length != 0;

        public ImmutableArray<SyntaxTrivia> TrailingTrivia { get; }

        public bool HasDiagnostics => Diagnostics.Length != 0;

        public ImmutableArray<SyntaxDiagnostic> Diagnostics { get; }

        internal SyntaxToken(string fullPath)
            : this(new SourceLocation(fullPath), SyntaxTokenKind.Missing, string.Empty, null,
                ImmutableArray<SyntaxTrivia>.Empty, ImmutableArray<SyntaxTrivia>.Empty,
                ImmutableArray<SyntaxDiagnostic>.Empty)
        {
        }

        internal SyntaxToken(SourceLocation location, SyntaxTokenKind kind, string text, object? value,
            ImmutableArray<SyntaxTrivia> leading, ImmutableArray<SyntaxTrivia> trailing,
            ImmutableArray<SyntaxDiagnostic> diagnostics)
        {
            Location = location;
            Kind = kind;
            Text = text;
            Value = value;
            LeadingTrivia = leading;
            TrailingTrivia = trailing;
            Diagnostics = diagnostics;

            if (HasLeadingTrivia)
                foreach (var trivia in leading)
                    trivia.Parent = this;

            if (HasTrailingTrivia)
                foreach (var trivia in trailing)
                    trivia.Parent = this;
        }

        public SyntaxToken DeepClone()
        {
            return new SyntaxToken(Location, Kind, Text, Value,
                LeadingTrivia.Select(x => x.DeepClone()).ToImmutableArray(),
                TrailingTrivia.Select(x => x.DeepClone()).ToImmutableArray(), Diagnostics)
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
