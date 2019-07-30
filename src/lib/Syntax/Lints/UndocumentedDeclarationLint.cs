using System.Collections.Generic;
using System.Linq;

namespace Flare.Syntax.Lints
{
    public sealed class UndocumentedDeclarationLint : SyntaxLint
    {
        public override string Name => "undocumented_declaration";

        public override SyntaxLintContexts Contexts => SyntaxLintContexts.NamedDeclaration;

        static SyntaxDiagnostic Diagnostic(SourceLocation location, string message)
        {
            return new SyntaxDiagnostic(location, message);
        }

        public override IEnumerable<SyntaxDiagnostic> Run(NamedDeclarationNode node)
        {
            if (node.VisibilityKeywordToken?.Kind != SyntaxTokenKind.PubKeyword)
                yield break;

            var type = node switch
            {
                ConstantDeclarationNode _ => "constant",
                FunctionDeclarationNode _ => "function",
                ExternalDeclarationNode _ => "external function",
                MacroDeclarationNode _ => "macro",
                _ => throw Assert.Unreachable(),
            };

            var ident = node.NameToken;
            var doc = node.Attributes.LastOrDefault(x => x.NameToken.Text == "doc");

            if (doc == null)
            {
                yield return Diagnostic(ident.Location, $"Public {type} {ident} is undocumented");
                yield break;
            }

            var value = doc.ValueToken;

            if (value.Kind != SyntaxTokenKind.StringLiteral && value.Value != (object)false)
                yield return Diagnostic(value.Location,
                    $"The 'doc' attribute's value must be a string literal or the 'false' Boolean literal");
        }
    }
}
