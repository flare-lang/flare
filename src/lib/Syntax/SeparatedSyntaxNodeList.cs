using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Flare.Syntax
{
    public struct SeparatedSyntaxNodeList<T> : IReadOnlyList<T>
        where T : SyntaxNode
    {
        readonly IReadOnlyList<T> _nodes;

        public IReadOnlyList<SyntaxToken> Separators { get; }

        public int Count => _nodes.Count;

        public T this[int index] => _nodes[index];

        internal SeparatedSyntaxNodeList(IReadOnlyList<T> nodes, IReadOnlyList<SyntaxToken> separators)
        {
            _nodes = nodes;
            Separators = separators;
        }

        public SeparatedSyntaxNodeList<T> DeepClone()
        {
            return new SeparatedSyntaxNodeList<T>(_nodes.Select(x => (T)x.InternalDeepClone()).ToArray(),
                Separators.Select(x => x.DeepClone()).ToArray());
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _nodes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
