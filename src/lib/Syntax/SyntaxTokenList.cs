using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Flare.Syntax
{
    [SuppressMessage("Microsoft.Performance", "CA1815", Justification = "Unnecessary.")]
    public readonly struct SyntaxTokenList : IReadOnlyList<SyntaxToken>
    {
        readonly ImmutableArray<SyntaxToken> _tokens;

        public SyntaxToken this[int index] => _tokens[index];

        public int Count => _tokens.Length;

        internal SyntaxTokenList(ImmutableArray<SyntaxToken> tokens)
        {
            _tokens = tokens;
        }

        public SyntaxTokenList DeepClone()
        {
            return new SyntaxTokenList(_tokens.Select(x => x.DeepClone()).ToImmutableArray());
        }

        public ImmutableArray<SyntaxToken>.Enumerator GetEnumerator()
        {
            return _tokens.GetEnumerator();
        }

        IEnumerator<SyntaxToken> IEnumerable<SyntaxToken>.GetEnumerator()
        {
            return ((IEnumerable<SyntaxToken>)_tokens).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_tokens).GetEnumerator();
        }
    }
}
