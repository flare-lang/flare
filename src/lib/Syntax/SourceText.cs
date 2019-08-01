using System.Collections.Generic;
using System.Text;

namespace Flare.Syntax
{
    public abstract class SourceText
    {
        public static Encoding Encoding { get; } = Encoding.UTF8;

        public string FullPath { get; }

        protected SourceText(string fullPath)
        {
            FullPath = fullPath;
        }

        public abstract IEnumerable<Rune> GetRunes();
    }
}
