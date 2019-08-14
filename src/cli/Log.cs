using System;
using System.IO;

namespace Flare.Cli
{
    static class Log
    {
        static readonly object _lock = new object();

        static void Output(ConsoleColor? color, TextWriter writer, bool line, string format, object[] args)
        {
            lock (_lock)
            {
                if (color is ConsoleColor c)
                    Console.ForegroundColor = c;

                try
                {
                    var msg = args.Length != 0 ? string.Format(format, args) : format;

                    writer.Write(msg);

                    if (line)
                        writer.WriteLine();
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
            Output(null, Console.Out, false, format, args);
        }

        public static void InfoLine(string format, params object[] args)
        {
            Output(null, Console.Out, true, format, args);
        }

        public static void Suggestion(string format, params object[] args)
        {
            Output(ConsoleColor.Green, Console.Error, false, format, args);
        }

        public static void SuggestionLine(string format, params object[] args)
        {
            Output(ConsoleColor.Green, Console.Error, true, format, args);
        }

        public static void Warning(string format, params object[] args)
        {
            Output(ConsoleColor.Yellow, Console.Error, false, format, args);
        }

        public static void WarningLine(string format, params object[] args)
        {
            Output(ConsoleColor.Yellow, Console.Error, true, format, args);
        }

        public static void Error(string format, params object[] args)
        {
            Output(ConsoleColor.Red, Console.Error, false, format, args);
        }

        public static void ErrorLine(string format, params object[] args)
        {
            Output(ConsoleColor.Red, Console.Error, true, format, args);
        }

        public static void Note(string format, params object[] args)
        {
            Output(ConsoleColor.White, Console.Error, false, format, args);
        }

        public static void NoteLine(string format, params object[] args)
        {
            Output(ConsoleColor.White, Console.Error, true, format, args);
        }

        public static void Context(string format, params object[] args)
        {
            Output(null, Console.Error, false, format, args);
        }

        public static void ContextLine(string format, params object[] args)
        {
            Output(null, Console.Error, true, format, args);
        }

        public static void Marker(string format, params object[] args)
        {
            Output(ConsoleColor.Cyan, Console.Error, false, format, args);
        }

        public static void MarkerLine(string format, params object[] args)
        {
            Output(ConsoleColor.Cyan, Console.Error, true, format, args);
        }

        public static void Debug(string format, params object[] args)
        {
            Output(ConsoleColor.Magenta, Console.Error, false, format, args);
        }

        public static void DebugLine(string format, params object[] args)
        {
            Output(ConsoleColor.Magenta, Console.Error, true, format, args);
        }
    }
}
