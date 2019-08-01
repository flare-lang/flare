using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Flare.Syntax
{
    public sealed class LexResult
    {
        public SourceText Source { get; }

        public ImmutableArray<SyntaxToken> Tokens { get; }

        public bool HasDiagnostics => Diagnostics.Length != 0;

        public ImmutableArray<SyntaxDiagnostic> Diagnostics { get; }

        public bool IsSuccess => Diagnostics.All(x => x.Severity != SyntaxDiagnosticSeverity.Error);

        internal LexResult(SourceText source, ImmutableArray<SyntaxToken> tokens,
            ImmutableArray<SyntaxDiagnostic> diagnostics)
        {
            Source = source;
            Tokens = tokens;
            Diagnostics = diagnostics;
        }
    }
}
