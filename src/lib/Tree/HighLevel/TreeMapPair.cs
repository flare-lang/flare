namespace Flare.Tree.HighLevel
{
    sealed class TreeMapPair
    {
        public TreeReference Key { get; }

        public TreeReference Value { get; }

        public TreeMapPair(TreeReference key, TreeReference value)
        {
            Key = key;
            Value = value;
        }
    }
}
