namespace Flare.Syntax
{
    public readonly struct SourceLocation
    {
        public static int MissingLine => -1;

        public static int MissingColumn => -1;

        public SourceText Source { get; }

        public int Line { get; }

        public int Column { get; }

        internal SourceLocation(SourceText source)
            : this(source, MissingLine, MissingColumn)
        {
        }

        internal SourceLocation(SourceText source, int line, int column)
        {
            Source = source;
            Line = line;
            Column = column;
        }

        public override string ToString()
        {
            return $"{Source.FullPath}({Line},{Column})";
        }
    }
}
