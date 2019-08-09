namespace Flare.Tree
{
    abstract class TreeVariable
    {
        public TreeType Type { get; }

        public string? Name { get; }

        public bool IsMutable { get; }

        protected TreeVariable(TreeType type, string? name, bool mutable)
        {
            Type = type;
            Name = name;
            IsMutable = mutable;
        }
    }
}
