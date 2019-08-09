using System.Collections.Immutable;

namespace Flare.Syntax
{
    public sealed class ParseResult
    {
        public LexResult Lex { get; }

        public SyntaxMode Mode { get; }

        public SyntaxNode Tree { get; }

        public ImmutableArray<SyntaxDiagnostic> Diagnostics { get; }

        public bool IsSuccess => Lex.IsSuccess && Diagnostics.IsEmpty;

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
