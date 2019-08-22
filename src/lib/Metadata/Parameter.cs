using Flare.Syntax;

namespace Flare.Metadata
{
    public sealed class Parameter : Metadata
    {
        public Declaration Declaration { get; }

        public string Name { get; }

        public int Position { get; }

        public bool IsVariadic { get; }

        internal Parameter(Declaration declaration, SyntaxNodeList<AttributeNode> attributes, string name, int position,
            bool variadic)
            : base(attributes)
        {
            Declaration = declaration;
            Name = name;
            Position = position;
            IsVariadic = variadic;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
