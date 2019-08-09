using Flare.Runtime;

namespace Flare.Syntax
{
    sealed class SyntaxSymbol
    {
        public SyntaxSymbolKind Kind { get; }

        public ModulePath? Module { get; }

        public string Name { get; }

        public SyntaxSymbol(SyntaxSymbolKind kind, ModulePath? module, string name)
        {
            Kind = kind;
            Module = module;
            Name = name;
        }
    }
}
