namespace Flare.Tests.TestAdapter
{
    sealed class FlareTestFilter
    {
        public FlareTestFilterKind Kind { get; }

        public string Value { get; }

        public FlareTestFilter(FlareTestFilterKind kind, string value)
        {
            Kind = kind;
            Value = value;
        }
    }
}
