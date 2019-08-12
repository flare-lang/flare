using Flare.Runtime;

namespace Flare.Syntax
{
    abstract class SyntaxSymbol
    {
        public abstract SyntaxSymbolKind Kind { get; }

        public abstract ModulePath? Module { get; }

        public abstract SyntaxNode? Definition { get; }

        public abstract string Name { get; }
    }
}
