using System.Collections.Generic;
using System.Linq;

namespace Flare.Syntax
{
    public sealed class LexResult
    {
        public SourceText Source { get; }

        public IReadOnlyList<SyntaxToken> Tokens { get; }

        public bool HasDiagnostics => Diagnostics.Count != 0;

        public IReadOnlyList<SyntaxDiagnostic> Diagnostics { get; }

        public bool IsSuccess => Diagnostics.All(x => x.Severity != SyntaxDiagnosticSeverity.Error);

        internal LexResult(SourceText source, IReadOnlyList<SyntaxToken> tokens,
            IReadOnlyList<SyntaxDiagnostic> diagnostics)
        {
            Source = source;
            Tokens = tokens;
            Diagnostics = diagnostics;
        }
    }
}
