using System;
using System.Collections.Generic;

namespace Flare.Syntax
{
    public sealed class SyntaxDiagnostic
    {
        public SyntaxDiagnosticKind Kind { get; }

        public SyntaxDiagnosticSeverity Severity { get; }

        public SourceLocation Location { get; }

        public string Message { get; }

        public IReadOnlyList<(SourceLocation Location, string Message)> Notes { get; }

        public SyntaxDiagnostic(SourceLocation location, string message,
            IReadOnlyList<(SourceLocation, string)>? notes = null)
            : this(SyntaxDiagnosticKind.Lint, SyntaxDiagnosticSeverity.Warning, location, message, notes)
        {
        }

        internal SyntaxDiagnostic(SyntaxDiagnosticKind kind, SyntaxDiagnosticSeverity severity, SourceLocation location,
            string message, IReadOnlyList<(SourceLocation, string)>? notes = null)
        {
            Kind = kind;
            Severity = severity;
            Location = location;
            Message = message;
            Notes = notes ?? Array.Empty<(SourceLocation, string)>();
        }

        public override string ToString()
        {
            return $"{Location}: {Message}";
        }
    }
}
