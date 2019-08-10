using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.ComponentModel;
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

        protected static void LogDiagnostic(SyntaxDiagnostic diagnostic)
        {
            switch (diagnostic.Severity)
            {
                case SyntaxDiagnosticSeverity.Suggestion:
                    Log.SuggestionLine("{0}", diagnostic);
                    break;
                case SyntaxDiagnosticSeverity.Warning:
                    Log.WarningLine("{0}", diagnostic);
                    break;
                case SyntaxDiagnosticSeverity.Error:
                    Log.ErrorLine("{0}", diagnostic);
                    break;
            }
        }

        protected static async Task<(bool, string)> RunGitAsync(string args)
        {
            var result = new StringBuilder();

            void write(string str)
            {
                lock (result)
                    _ = result.AppendLine(str);
            }

            try
            {
                return (await Process.ExecuteAsync("git", args, null, write, write) == 0, result.ToString());
            }
            catch (Win32Exception)
            {
                return (false, "Could not execute 'git'.");
            }
        }
    }
}
