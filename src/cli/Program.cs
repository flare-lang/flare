using System;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Flare.Cli.Commands;

namespace Flare.Cli
{
    public static class Program
    {
        public static DirectoryInfo StartDirectory { get; }

        static Program()
        {
            StartDirectory = new DirectoryInfo(Environment.CurrentDirectory);
        }

        static async Task FallbackMiddleware(InvocationContext context, Func<InvocationContext, Task> next)
        {
            // Fall back to the implicit script command if no other command is matched.
            if (context.ParseResult.CommandResult.Command is RootCommand cmd)
            {
                var scmd = new ScriptCommand();

                // TODO: It would be nice if we could just add this ahead of time and retrieve it
                // from the context somehow.
                cmd.AddCommand(scmd);

                context.ParseResult = context.Parser.Parse(new[] { scmd.Name }.Concat(
                    context.ParseResult.Tokens.Select(x => x.Value)).ToArray());
            }

            await next(context);
        }

        static async Task ErrorMiddleware(InvocationContext context, Func<InvocationContext, Task> next)
        {
            if (context.ParseResult.Errors.Count != 0)
                context.InvocationResult = new ParseErrorResult();
            else
                await next(context);
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
                .UseExceptionHandler()
                .CancelOnProcessTermination()
                .UseMiddleware(FallbackMiddleware)
                .UseMiddleware(ErrorMiddleware)
                .Build()
                .Invoke(args);
        }
    }
}
