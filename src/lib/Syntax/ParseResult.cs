using System.Collections.Immutable;
using System.Linq;

namespace Flare.Syntax
{
    public sealed class ParseResult
    {
        public LexResult Lex { get; }

        public SyntaxMode Mode { get; }

        public SyntaxNode Tree { get; }

        public bool HasDiagnostics => Diagnostics.Length != 0;

        public ImmutableArray<SyntaxDiagnostic> Diagnostics { get; }

        public bool IsSuccess => Lex.IsSuccess && Diagnostics.All(x => x.Severity != SyntaxDiagnosticSeverity.Error);

        internal ParseResult(LexResult lex, SyntaxMode mode, SyntaxNode tree,
            ImmutableArray<SyntaxDiagnostic> diagnostics)
        {
            Lex = lex;
            Mode = mode;
            Tree = tree;
            Diagnostics = diagnostics;
        }
    }
}
