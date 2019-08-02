namespace Flare.Tree.HighLevel
{
    sealed class TreeRecordField
    {
        public string Name { get; }

        public TreeReference Initializer { get; }

        public bool IsMutable { get; }

        public TreeRecordField(string name, TreeReference initializer, bool mutable)
        {
            Name = name;
            Initializer = initializer;
            IsMutable = mutable;
        }
    }
}
