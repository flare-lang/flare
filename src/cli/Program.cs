using System;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Flare.Cli.Commands;

namespace Flare.Cli
{
    static class Program
    {
        public static DirectoryInfo StartDirectory { get; }

        static Program()
        {
            StartDirectory = new DirectoryInfo(Environment.CurrentDirectory);
        }

        static async Task FallbackMiddleware(InvocationContext context, Func<InvocationContext, Task> next)
        {
            var result = context.ParseResult;

            // Fall back to the implicit script command if no other command is matched.
            if (result.CommandResult.Command is RootCommand cmd)
            {
                var parser = result.Parser;

                context.ParseResult = parser.Parse(parser.Configuration.RootCommand.Children.OfType<ScriptCommand>()
                    .Select(x => x.Name).Concat(result.Tokens.Select(x => x.Value)).ToArray());
            }

            await next(context).ConfigureAwait(false);
        }

        static int Main(string[] args)
        {
            return new CommandLineBuilder(new RootCommand())
                .UseVersionOption()
                .UseHelp()
                .UseParseDirective()
                .UseDebugDirective()
                .UseSuggestDirective()
                .RegisterWithDotnetSuggest()
                .UseTypoCorrections()
                .UseParseErrorReporting()
                .UseExceptionHandler()
                .CancelOnProcessTermination()
                .UseMiddleware(FallbackMiddleware, MiddlewareOrder.ErrorReporting - 1)
                .Build()
                .Invoke(args);
        }
    }
}
