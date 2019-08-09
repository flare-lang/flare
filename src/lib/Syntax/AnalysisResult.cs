using System.Collections.Immutable;
using System.Linq;

namespace Flare.Syntax
{
    public sealed class AnalysisResult
    {
        public ParseResult Parse { get; }

        public bool HasDiagnostics => Diagnostics.Length != 0;

        public ImmutableArray<SyntaxDiagnostic> Diagnostics { get; }

        public bool IsSuccess => Parse.IsSuccess && Diagnostics.All(x => x.Severity != SyntaxDiagnosticSeverity.Error);

        internal AnalysisResult(ParseResult parse, ImmutableArray<SyntaxDiagnostic> diagnostics)
        {
            Parse = parse;
            Diagnostics = diagnostics;
        }
    }
}