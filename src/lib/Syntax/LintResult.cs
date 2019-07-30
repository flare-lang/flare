using System.Collections.Generic;
using System.Linq;

namespace Flare.Syntax
{
    public sealed class LintResult
    {
        public ParseResult Parse { get; }

        public bool HasDiagnostics => Diagnostics.Count != 0;

        public IReadOnlyList<SyntaxDiagnostic> Diagnostics { get; }

        public bool IsSuccess => Parse.IsSuccess && Diagnostics.All(x => x.Severity != SyntaxDiagnosticSeverity.Error);

        internal LintResult(ParseResult parse, IReadOnlyList<SyntaxDiagnostic> diagnostics)
        {
            Parse = parse;
            Diagnostics = diagnostics;
        }
    }
}
