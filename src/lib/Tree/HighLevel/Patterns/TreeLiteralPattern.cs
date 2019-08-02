namespace Flare.Tree.HighLevel.Patterns
{
    sealed class TreeLiteralPattern : TreePattern
    {
        public object Value { get; }

        public TreeLiteralPattern(TreeLocal? alias, object value)
            : base(alias)
        {
            Value = value;
        }
    }
}
