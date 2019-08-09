using System.Collections.Immutable;
using Flare.Runtime;

namespace Flare.Syntax
{
    public sealed class AnalysisResult
    {
        public ParseResult Parse { get; }

        public ModuleLoader Loader { get; }

        public SyntaxContext Context { get; }

        public ImmutableArray<SyntaxDiagnostic> Diagnostics { get; }

        public bool IsSuccess => Parse.IsSuccess && Diagnostics.IsEmpty;

        internal AnalysisResult(ParseResult parse, ModuleLoader loader, SyntaxContext context,
            ImmutableArray<SyntaxDiagnostic> diagnostics)
        {
            Parse = parse;
            Loader = loader;
            Context = context;
            Diagnostics = diagnostics;
        }
    }
}
