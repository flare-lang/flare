using System.Collections.Immutable;
using Flare.Syntax;

namespace Flare.Runtime
{
    public abstract class Metadata
    {
        public bool HasAttributes => !Attributes.IsEmpty;

        public ImmutableArray<Attribute> Attributes { get; }

        private protected Metadata(SyntaxNodeList<AttributeNode> nodes)
        {
            var attrs = ImmutableArray<Attribute>.Empty;

            foreach (var attr in nodes)
                attrs = attrs.Add(new Attribute(attr.NameToken.Text, attr.ValueToken.Value!));

            Attributes = attrs;
        }
    }
}
