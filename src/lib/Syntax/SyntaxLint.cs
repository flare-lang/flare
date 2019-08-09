using System;
using System.Collections.Generic;

namespace Flare.Syntax
{
    public abstract class SyntaxLint
    {
        public abstract string Name { get; }

        public abstract SyntaxLintContexts Contexts { get; }

        public virtual SyntaxDiagnosticSeverity DefaultSeverity => SyntaxDiagnosticSeverity.Warning;

        public virtual IEnumerable<(SyntaxNode, SyntaxDiagnostic)> Run(ProgramNode node)
        {
            return Array.Empty<(SyntaxNode, SyntaxDiagnostic)>();
        }

        public virtual IEnumerable<(SyntaxNode, SyntaxDiagnostic)> Run(DeclarationNode node)
        {
            return Array.Empty<(SyntaxNode, SyntaxDiagnostic)>();
        }

        public virtual IEnumerable<(SyntaxNode, SyntaxDiagnostic)> Run(NamedDeclarationNode node)
        {
            return Array.Empty<(SyntaxNode, SyntaxDiagnostic)>();
        }

        public virtual IEnumerable<(SyntaxNode, SyntaxDiagnostic)> Run(StatementNode node)
        {
            return Array.Empty<(SyntaxNode, SyntaxDiagnostic)>();
        }

        public virtual IEnumerable<(SyntaxNode, SyntaxDiagnostic)> Run(ExpressionNode node)
        {
            return Array.Empty<(SyntaxNode, SyntaxDiagnostic)>();
        }

        public virtual IEnumerable<(SyntaxNode, SyntaxDiagnostic)> Run(PatternNode node)
        {
            return Array.Empty<(SyntaxNode, SyntaxDiagnostic)>();
        }
    }
}
