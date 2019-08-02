namespace Flare.Tree.HighLevel.Patterns
{
    abstract class TreePattern
    {
        public TreeLocal? Alias { get; }

        public TreePattern(TreeLocal? alias)
        {
            Alias = alias;
        }
    }
}
