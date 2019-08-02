namespace Flare.Tree.HighLevel.Patterns
{
    sealed class TreeIdentifierPattern : TreePattern
    {
        public TreeLocal Identifier { get; }

        public TreeIdentifierPattern(TreeLocal? alias, TreeLocal identifier)
            : base(alias)
        {
            Identifier = identifier;
        }
    }
}
