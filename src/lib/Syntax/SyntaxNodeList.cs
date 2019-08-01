using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Flare.Syntax
{
    public readonly struct SyntaxNodeList<T> : IReadOnlyList<T>
        where T : SyntaxNode
    {
        readonly ImmutableArray<T> _nodes;

        public T this[int index] => _nodes[index];

        public int Count => _nodes.Length;

        internal SyntaxNodeList(ImmutableArray<T> nodes)
        {
            _nodes = nodes;
        }

        public SyntaxNodeList<T> DeepClone()
        {
            return new SyntaxNodeList<T>(_nodes.Select(x => (T)x.InternalDeepClone()).ToImmutableArray());
        }

        public ImmutableArray<T>.Enumerator GetEnumerator()
        {
            return _nodes.GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return ((IEnumerable<T>)_nodes).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_nodes).GetEnumerator();
        }
    }
}
