namespace Flare.Syntax
{
    public enum SyntaxDiagnosticKind
    {
        UnexpectedCharacter,
        IncompleteOperator,
        IncompleteFragmentIdentifier,
        IncompleteIntegerLiteral,
        IncompleteRealLiteral,
        IncompleteStringLiteral,
        IncompleteEscapeSequence,
        MissingToken,
        SkippedToken,
        MissingNamedDeclaration,
        MissingExpression,
        MissingPattern,
        InvalidLintAttribute,
        Lint,
    }
}
