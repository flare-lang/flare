using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Flare.Syntax
{
    public struct SyntaxNodeList<T> : IReadOnlyList<T>
        where T : SyntaxNode
    {
        readonly IReadOnlyList<T> _nodes;

        public T this[int index] => _nodes[index];

        public int Count => _nodes.Count;

        internal SyntaxNodeList(IReadOnlyList<T> nodes)
        {
            _nodes = nodes;
        }

        public SyntaxNodeList<T> DeepClone()
        {
            return new SyntaxNodeList<T>(_nodes.Select(x => (T)x.InternalDeepClone()).ToArray());
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
