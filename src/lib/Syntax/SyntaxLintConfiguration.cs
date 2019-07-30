using System.Collections.Immutable;

namespace Flare.Syntax
{
    public sealed class SyntaxLintConfiguration
    {
        ImmutableDictionary<string, SyntaxDiagnosticSeverity?> _severities;

        public SyntaxLintConfiguration(ImmutableDictionary<string, SyntaxDiagnosticSeverity?>? severities = null)
        {
            if (severities != null)
                foreach (var severity in severities.Values)
                    if (severity is SyntaxDiagnosticSeverity s)
                        _ = s.Check(nameof(severities));

            _severities = severities ?? ImmutableDictionary<string, SyntaxDiagnosticSeverity?>.Empty;
        }

        public bool Get(string name, out SyntaxDiagnosticSeverity? severity)
        {
            return _severities.TryGetValue(name, out severity);
        }

        public SyntaxLintConfiguration Set(string name, SyntaxDiagnosticSeverity? severity)
        {
            if (severity is SyntaxDiagnosticSeverity s)
                _ = s.Check(nameof(severity));

            return new SyntaxLintConfiguration
            {
                _severities = _severities.SetItem(name, severity),
            };
        }

        public SyntaxLintConfiguration Remove(string name)
        {
            return new SyntaxLintConfiguration
            {
                _severities = _severities.Remove(name),
            };
        }
    }
}
