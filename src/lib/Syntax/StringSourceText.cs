using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Flare.Syntax
{
    public sealed class StringSourceText : SourceText
    {
        public string Text { get; }

        public override IEnumerable<Rune> Runes => Text.EnumerateRunes();

        StringSourceText(string fullPath, string text)
            : base(fullPath)
        {
            Text = text;
        }

        public static StringSourceText From(string fullPath, string value)
        {
            return new StringSourceText(fullPath, value);
        }

        public static StringSourceText From(string fullPath, ReadOnlySpan<char> value)
        {
            return new StringSourceText(fullPath, value.ToString());
        }

        public static StringSourceText From(string fullPath, ReadOnlySpan<byte> value)
        {
            return new StringSourceText(fullPath, Encoding.GetString(value));
        }

        public static async Task<StringSourceText> FromAsync(string fullPath, Stream stream)
        {
            using var reader = new StreamReader(stream, Encoding,
                detectEncodingFromByteOrderMarks: false, leaveOpen: true);

            return new StringSourceText(fullPath, await reader.ReadToEndAsync().ConfigureAwait(false));
        }
    }
}
