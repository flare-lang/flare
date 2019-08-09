namespace Flare.Runtime
{
    public sealed class Attribute
    {
        public string Name { get; }

        public object Value { get; }

        internal Attribute(string name, object value)
        {
            Name = name;
            Value = value;
        }
    }
}
