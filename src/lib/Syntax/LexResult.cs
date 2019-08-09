using System.Collections.Immutable;

namespace Flare.Syntax
{
    public sealed class LexResult
    {
        public SourceText Source { get; }

        public ImmutableArray<SyntaxToken> Tokens { get; }

        public ImmutableArray<SyntaxDiagnostic> Diagnostics { get; }

        public bool IsSuccess => Diagnostics.IsEmpty;

        internal LexResult(SourceText source, ImmutableArray<SyntaxToken> tokens,
            ImmutableArray<SyntaxDiagnostic> diagnostics)
        {
            Source = source;
            Tokens = tokens;
            Diagnostics = diagnostics;
        }
    }
}
