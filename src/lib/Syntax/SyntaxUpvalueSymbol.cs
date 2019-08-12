using Flare.Runtime;

namespace Flare.Syntax
{
    sealed class SyntaxUpvalueSymbol : SyntaxSymbol
    {
        public SyntaxSymbol Symbol { get; }

        public int Slot { get; }

        public override SyntaxSymbolKind Kind => Symbol.Kind;

        public override ModulePath? Module => Symbol.Module;

        public override SyntaxNode? Definition => Symbol.Definition;

        public override string Name => Symbol.Name;

        public SyntaxUpvalueSymbol(SyntaxSymbol symbol, int slot)
        {
            Symbol = symbol;
            Slot = slot;
        }
    }
}
