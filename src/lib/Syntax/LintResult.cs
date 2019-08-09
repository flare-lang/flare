using System.Collections.Immutable;
using System.Linq;

namespace Flare.Syntax
{
    public sealed class LintResult
    {
        public ParseResult Parse { get; }

        public bool HasDiagnostics => !Diagnostics.IsEmpty;

        public ImmutableArray<SyntaxDiagnostic> Diagnostics { get; }

        public bool IsSuccess => Parse.IsSuccess && Diagnostics.All(x => x.Severity != SyntaxDiagnosticSeverity.Error);

        internal LintResult(ParseResult parse, ImmutableArray<SyntaxDiagnostic> diagnostics)
        {
            Parse = parse;
            Diagnostics = diagnostics;
        }
    }
}
