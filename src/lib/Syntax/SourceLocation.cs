namespace Flare.Syntax
{
    public readonly struct SourceLocation
    {
        public static int MissingLine => -1;

        public static int MissingColumn => -1;

        public string FullPath { get; }

        public int Line { get; }

        public int Column { get; }

        internal SourceLocation(string fullPath)
            : this(fullPath, MissingLine, MissingColumn)
        {
        }

        internal SourceLocation(string fullPath, int line, int column)
        {
            FullPath = fullPath;
            Line = line;
            Column = column;
        }

        internal SourceLocation WithMissing()
        {
            return new SourceLocation(FullPath);
        }

        public override string ToString()
        {
            return $"{FullPath}({Line},{Column})";
        }
    }
}
