using System.Collections.Generic;
using System.Text;

namespace Flare.Syntax
{
    public abstract class SourceText
    {
        public static Encoding Encoding { get; } = Encoding.UTF8;

        public string FullPath { get; }

        public abstract IEnumerable<Rune> Runes { get; }

        protected SourceText(string fullPath)
        {
            FullPath = fullPath;
        }
    }
}
