using Flare.Syntax;

namespace Flare.Metadata
{
    public abstract class Declaration : Metadata
    {
        public Module Module { get; }

        public string Name { get; }

        public bool IsPublic { get; }

        private protected Declaration(Module module, TestDeclarationNode node)
            : base(node.Attributes)
        {
            Module = module;
            Name = node.NameToken.Text;
        }

        private protected Declaration(Module module, NamedDeclarationNode node)
            : base(node.Attributes)
        {
            Module = module;
            Name = node.NameToken.Text;
            IsPublic = node.VisibilityKeywordToken?.Kind == SyntaxTokenKind.PubKeyword;
        }

        public override string ToString()
        {
            return $"{Module}.{Name}";
        }
    }
}
