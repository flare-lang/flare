using System;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace Flare.Metadata
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

        public override string ToString()
        {
            var value = Value switch
            {
                null => "nil",
                bool b => b ? "true" : "false",
                string a => $":{a}",
                BigInteger i => i.ToString(CultureInfo.InvariantCulture),
                double d => d.ToString(CultureInfo.InvariantCulture),
                ReadOnlyMemory<byte> s => $"\"{Encoding.UTF8.GetString(s.Span)}\"",
                _ => throw DebugAssert.Unreachable(),
            };

            return $"{Name} = {value}";
        }
    }
}
