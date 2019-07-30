using System;
using System.Collections.Generic;

namespace Flare.Syntax
{
    public abstract class SyntaxLint
    {
        public abstract string Name { get; }

        public abstract SyntaxLintContexts Contexts { get; }

        public virtual SyntaxDiagnosticSeverity DefaultSeverity => SyntaxDiagnosticSeverity.Warning;

        public virtual IEnumerable<SyntaxDiagnostic> Run(ProgramNode node)
        {
            return Array.Empty<SyntaxDiagnostic>();
        }

        public virtual IEnumerable<SyntaxDiagnostic> Run(DeclarationNode node)
        {
            return Array.Empty<SyntaxDiagnostic>();
        }

        public virtual IEnumerable<SyntaxDiagnostic> Run(NamedDeclarationNode node)
        {
            return Array.Empty<SyntaxDiagnostic>();
        }

        public virtual IEnumerable<SyntaxDiagnostic> Run(StatementNode node)
        {
            return Array.Empty<SyntaxDiagnostic>();
        }

        public virtual IEnumerable<SyntaxDiagnostic> Run(ExpressionNode node)
        {
            return Array.Empty<SyntaxDiagnostic>();
        }

        public virtual IEnumerable<SyntaxDiagnostic> Run(PatternNode node)
        {
            return Array.Empty<SyntaxDiagnostic>();
        }
    }
}
