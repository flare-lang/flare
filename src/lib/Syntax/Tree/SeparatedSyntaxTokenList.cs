using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Flare.Syntax.Tree
{
    public struct SeparatedSyntaxTokenList : IReadOnlyList<SyntaxToken>
    {
        readonly IReadOnlyList<SyntaxToken> _tokens;

        public IReadOnlyList<SyntaxToken> Separators { get; }

        public int Count => _tokens.Count;

        public SyntaxToken this[int index] => _tokens[index];

        internal SeparatedSyntaxTokenList(IReadOnlyList<SyntaxToken> tokens, IReadOnlyList<SyntaxToken> separators)
        {
            _tokens = tokens;
            Separators = separators;
        }

        public SeparatedSyntaxTokenList DeepClone()
        {
            return new SeparatedSyntaxTokenList(_tokens.Select(x => x.DeepClone()).ToArray(),
                Separators.Select(x => x.DeepClone()).ToArray());
        }

        public IEnumerator<SyntaxToken> GetEnumerator()
        {
            return _tokens.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
