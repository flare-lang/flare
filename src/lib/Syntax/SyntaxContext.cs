using System.Collections.Immutable;

namespace Flare.Syntax
{
    public sealed class SyntaxContext
    {
        public bool HasParses => !Parses.IsEmpty;

        public ImmutableDictionary<string, ParseResult> Parses { get; private set; } =
            ImmutableDictionary<string, ParseResult>.Empty;

        public bool HasDiagnostics => !Diagnostics.IsEmpty;

        public ImmutableArray<SyntaxDiagnostic> Diagnostics { get; private set; } =
            ImmutableArray<SyntaxDiagnostic>.Empty;

        internal void AddParse(string path, ParseResult parse)
        {
            Parses = Parses.Add(path, parse);
        }

        internal void AddDiagnostic(SyntaxDiagnostic diagnostic)
        {
            Diagnostics = Diagnostics.Add(diagnostic);
        }
    }
}
