using Flare.Runtime;

namespace Flare.Syntax
{
    sealed class SyntaxSymbol
    {
        public SyntaxSymbolKind Kind { get; }

        public ModulePath? Module { get; }

        public SyntaxNode? Definition { get; }

        public string Name { get; }

        public SyntaxSymbol(SyntaxSymbolKind kind, ModulePath? module, SyntaxNode? definition, string name)
        {
            Kind = kind;
            Module = module;
            Definition = definition;
            Name = name;
        }
    }
}
