using System.Collections.Generic;
using System.Linq;

namespace Flare.Syntax.Lints
{
    public sealed class UndocumentedDeclarationLint : SyntaxLint
    {
        public override string Name => "undocumented_declaration";

        public override SyntaxLintContexts Contexts => SyntaxLintContexts.Program | SyntaxLintContexts.NamedDeclaration;

        static SyntaxDiagnostic? CheckDocAttribute(SyntaxNodeList<AttributeNode> attributes, SourceLocation location,
            string error)
        {
            var doc = attributes.LastOrDefault(x => x.NameToken.Text == "doc");

            if (doc == null)
                return new SyntaxDiagnostic(location, error);

            var value = doc.ValueToken;

            return !value.IsMissing && value.Kind != SyntaxTokenKind.StringLiteral && value.Value != (object)false ?
                new SyntaxDiagnostic(value.Location,
                    $"The value of the 'doc' attribute must be a string literal or the 'false' Boolean literal") : null;
        }

        public override IEnumerable<SyntaxDiagnostic> Run(ProgramNode node)
        {
            foreach (var comp in node.Path.ComponentTokens.Tokens)
                if (comp.IsMissing)
                    yield break;

            var hasExports = false;

            foreach (var decl in node.Declarations)
            {
                if (!(decl is NamedDeclarationNode named) || decl is MissingNamedDeclarationNode)
                    continue;

                if (named.VisibilityKeywordToken?.Kind == SyntaxTokenKind.PubKeyword)
                {
                    hasExports = true;
                    break;
                }
            }

            if (!hasExports)
                yield break;

            var toks = node.Path.ComponentTokens.Tokens;
            var mod = toks[0].Text;

            foreach (var tok in toks.Skip(1))
                mod += $"::{tok.Text}";

            var diag = CheckDocAttribute(node.Attributes, toks[0].Location,
                $"Module {mod} has public declarations but is undocumented");

            if (diag != null)
                yield return diag;
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

            if (ident.IsMissing)
                yield break;

            var diag = CheckDocAttribute(node.Attributes, ident.Location, $"Public {type} {ident} is undocumented");

            if (diag != null)
               yield return diag;
        }
    }
}
