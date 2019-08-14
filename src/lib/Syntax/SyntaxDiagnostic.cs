using System.Collections.Immutable;

namespace Flare.Syntax
{
    public sealed class SyntaxDiagnostic
    {
        public SyntaxDiagnosticKind Kind { get; }

        public SyntaxDiagnosticSeverity Severity { get; }

        public SourceLocation Location { get; }

        public string Message { get; }

        public bool HasNotes => !Notes.IsEmpty;

        public ImmutableArray<(SourceLocation Location, string Message)> Notes { get; }

        public SyntaxDiagnostic(SourceLocation location, string message,
            ImmutableArray<(SourceLocation Location, string Message)> notes = default)
            : this(SyntaxDiagnosticKind.Lint, SyntaxDiagnosticSeverity.Warning, location, message,
                !notes.IsDefault ? notes : ImmutableArray<(SourceLocation, string)>.Empty)
        {
        }

        internal SyntaxDiagnostic(SyntaxDiagnosticKind kind, SyntaxDiagnosticSeverity severity, SourceLocation location,
            string message, ImmutableArray<(SourceLocation Location, string Message)> notes)
        {
            Kind = kind;
            Severity = severity;
            Location = location;
            Message = message;
            Notes = notes;
        }

        public override string ToString()
        {
            return $"{Location}: {Severity}: {Message}";
        }
    }
}
