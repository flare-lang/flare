using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flare.Syntax;

namespace Flare.Cli.Commands
{
    abstract class BaseCommand : Command
    {
        public BaseCommand(string name, string description)
            : base(name, description)
        {
        }

        protected void AddArgument<T>(string name, string description, IArgumentArity arity)
        {
            AddArgument(new Argument<T>(name)
            {
                Arity = arity,
                Description = description,
            });
        }

        protected void AddOption<T>(string alias1, string alias2, string description)
        {
            AddOption(new Option(new[] { alias1, alias2 }, description)
            {
                Argument = new Argument<T>(),
            });
        }

        protected void RegisterHandler<T>(Func<T, Task<int>> handler)
        {
            Handler = CommandHandler.Create(handler);
        }

        protected static string ToRelative(string path)
        {
            return Path.GetRelativePath(Program.StartDirectory.FullName, path);
        }

        protected static void LogDiagnostic(SyntaxDiagnostic diagnostic)
        {
            var loc = diagnostic.Location;
            var msg = $"{ToRelative(loc.FullPath)}({loc.Line},{loc.Column}): {diagnostic.Severity}: " +
                diagnostic.Message;

            switch (diagnostic.Severity)
            {
                case SyntaxDiagnosticSeverity.Suggestion:
                    Log.SuggestionLine("{0}", msg);
                    break;
                case SyntaxDiagnosticSeverity.Warning:
                    Log.WarningLine("{0}", msg);
                    break;
                case SyntaxDiagnosticSeverity.Error:
                    Log.ErrorLine("{0}", msg);
                    break;
            }

            LogCaretMarker(loc);

            foreach (var (nloc, note) in diagnostic.Notes)
            {
                Log.NoteLine("{0}({1},{2}): Note: {3}", ToRelative(nloc.FullPath), nloc.Line, nloc.Column, note);
                LogCaretMarker(nloc);
            }
        }

        static void LogCaretMarker(SourceLocation location)
        {
            string[] lines;

            try
            {
                // TODO: We should store some kind of source file map in memory and refer to that
                // rather than reading files from disk again. The SyntaxContext would probably be a
                // good place to do this.
                lines = File.ReadAllLines(location.FullPath);
            }
            catch (Exception)
            {
                return;
            }


            var indexed = lines.Select((str, ln) => (ln: ln + 1, str));
            var actual = indexed.Where(x => x.ln == location.Line).Select(x => x.str).SingleOrDefault();

            if (actual == null)
                return;

            const int context = 3;

            var lead = indexed.Where(x => x.ln >= location.Line - context && x.ln < location.Line);
            var trail = indexed.Where(x => x.ln > location.Line && x.ln <= location.Line + context);
            var llen = indexed.Last().ln.ToString().Length;

            void PrintLine(int line, string text)
            {
                Log.Marker("{0} | ", line.ToString().PadLeft(llen));
                Log.ContextLine("{0}", text);
            }

            if (!lead.All(x => string.IsNullOrWhiteSpace(x.str)))
                foreach (var (ln, str) in lead)
                    PrintLine(ln, str);

            PrintLine(location.Line, actual);

            Log.Marker("{0} | ", string.Empty.PadLeft(llen));

            foreach (var (col, ch) in actual.Select((ch, col) => (col + 1, ch)))
            {
                if (col == location.Column)
                {
                    Log.MarkerLine("^");
                    break;
                }
                else
                    Log.Marker("{0}", char.IsWhiteSpace(ch) ? ch : ' ');
            }

            if (!trail.All(x => string.IsNullOrWhiteSpace(x.str)))
                foreach (var (ln, str) in trail)
                    PrintLine(ln, str);
        }

        protected static async Task<(bool, string)> RunGitAsync(string args)
        {
            var result = new StringBuilder();

            void Write(string str)
            {
                lock (result)
                    _ = result.AppendLine(str);
            }

            try
            {
                var success = await Process.ExecuteAsync("git", args, null, Write, Write) == 0;

                lock (result)
                    return (success, result.ToString());
            }
            catch (Win32Exception)
            {
                return (false, "Could not execute 'git'.");
            }
        }
    }
}
