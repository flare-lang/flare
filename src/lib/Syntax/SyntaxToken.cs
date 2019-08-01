using System;
using System.Collections.Generic;
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

        public bool HasLeadingTrivia => LeadingTrivia.Count != 0;

        public IReadOnlyList<SyntaxTrivia> LeadingTrivia { get; }

        public bool HasTrailingTrivia => TrailingTrivia.Count != 0;

        public IReadOnlyList<SyntaxTrivia> TrailingTrivia { get; }

        public bool HasDiagnostics => Diagnostics.Count != 0;

        public IReadOnlyList<SyntaxDiagnostic> Diagnostics { get; }

        internal SyntaxToken(string fullPath)
            : this(new SourceLocation(fullPath), SyntaxTokenKind.Missing, string.Empty, null,
                Array.Empty<SyntaxTrivia>(), Array.Empty<SyntaxTrivia>(), null)
        {
        }

        internal SyntaxToken(SourceLocation location, SyntaxTokenKind kind, string text, object? value,
            IReadOnlyList<SyntaxTrivia>? leading, IReadOnlyList<SyntaxTrivia>? trailing,
            IReadOnlyList<SyntaxDiagnostic>? diagnostics)
        {
            Location = location;
            Kind = kind;
            Text = text;
            Value = value;
            LeadingTrivia = leading ?? Array.Empty<SyntaxTrivia>();
            TrailingTrivia = trailing ?? Array.Empty<SyntaxTrivia>();
            Diagnostics = diagnostics ?? Array.Empty<SyntaxDiagnostic>();

            if (leading != null)
                foreach (var trivia in leading)
                    trivia.Parent = this;

            if (trailing != null)
                foreach (var trivia in trailing)
                    trivia.Parent = this;
        }

        public SyntaxToken DeepClone()
        {
            return new SyntaxToken(Location, Kind, Text, Value, LeadingTrivia.Select(x => x.DeepClone()).ToArray(),
                TrailingTrivia.Select(x => x.DeepClone()).ToArray(), Diagnostics)
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
