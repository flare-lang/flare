using System.Collections.Generic;
using System.Linq;
using Flare.Syntax.Tree;

namespace Flare.Syntax
{
    public sealed class ParseResult
    {
        public LexResult Lex { get; }

        public SyntaxMode Mode { get; }

        public SyntaxNode Tree { get; }

        public bool HasDiagnostics => Diagnostics.Count != 0;

        public IReadOnlyList<SyntaxDiagnostic> Diagnostics { get; }

        public bool IsSuccess => Lex.IsSuccess && Diagnostics.All(x => x.Severity != SyntaxDiagnosticSeverity.Error);

        internal ParseResult(LexResult lex, SyntaxMode mode, SyntaxNode tree,
            IReadOnlyList<SyntaxDiagnostic> diagnostics)
        {
            Lex = lex;
            Mode = mode;
            Tree = tree;
            Diagnostics = diagnostics;
        }
    }
}
