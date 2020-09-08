using System.Diagnostics.CodeAnalysis;

namespace Flare.Syntax
{
    [SuppressMessage("Microsoft.Performance", "CA1815", Justification = "Unnecessary.")]
    public readonly struct SeparatedSyntaxNodeList<T>
        where T : SyntaxNode
    {
        public SyntaxNodeList<T> Nodes { get; }

        public SyntaxTokenList Separators { get; }

        internal SeparatedSyntaxNodeList(SyntaxNodeList<T> nodes, SyntaxTokenList separators)
        {
            Nodes = nodes;
            Separators = separators;
        }

        public SeparatedSyntaxNodeList<T> DeepClone()
        {
            return new SeparatedSyntaxNodeList<T>(Nodes.DeepClone(), Separators.DeepClone());
        }
    }
}
