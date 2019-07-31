namespace Flare.Syntax
{
    public enum SyntaxDiagnosticKind
    {
        UnrecognizedCharacter,
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
