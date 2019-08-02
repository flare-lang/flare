namespace Flare.Tree.HighLevel.Patterns
{
    sealed class TreeMapPatternPair
    {
        public TreeReference Key { get; }

        public TreePattern Value { get; }

        public TreeMapPatternPair(TreeReference key, TreePattern value)
        {
            Key = key;
            Value = value;
        }
    }
}
