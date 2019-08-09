using System;

namespace Flare.Cli
{
    static class Log
    {
        static readonly object _lock = new object();

        static void Output(ConsoleColor? color, bool line, string format, object[] args)
        {
            lock (_lock)
            {
                if (color is ConsoleColor c)
                    Console.ForegroundColor = c;

                try
                {
                    var msg = args.Length != 0 ? string.Format(format, args) : format;

                    Console.Write(msg);

                    if (line)
                        Console.WriteLine();
                }
                finally
                {
                    if (color != null)
                        Console.ResetColor();
                }
            }
        }

        public static void Info(string format, params object[] args)
        {
            Output(null, false, format, args);
        }

        public static void InfoLine(string format, params object[] args)
        {
            Output(null, true, format, args);
        }

        public static void Suggestion(string format, params object[] args)
        {
            Output(ConsoleColor.Green, false, format, args);
        }

        public static void SuggestionLine(string format, params object[] args)
        {
            Output(ConsoleColor.Green, true, format, args);
        }

        public static void Warning(string format, params object[] args)
        {
            Output(ConsoleColor.Yellow, false, format, args);
        }

        public static void WarningLine(string format, params object[] args)
        {
            Output(ConsoleColor.Yellow, true, format, args);
        }

        public static void Error(string format, params object[] args)
        {
            Output(ConsoleColor.Red, false, format, args);
        }

        public static void ErrorLine(string format, params object[] args)
        {
            Output(ConsoleColor.Red, true, format, args);
        }

        public static void Debug(string format, params object[] args)
        {
            Output(ConsoleColor.Cyan, false, format, args);
        }

        public static void DebugLine(string format, params object[] args)
        {
            Output(ConsoleColor.Cyan, true, format, args);
        }

        public static void Line()
        {
            Output(null, true, string.Empty, Array.Empty<object>());
        }
    }
}
