using System.CommandLine;
using System.CommandLine.Invocation;

namespace Flare.Cli.Commands
{
    public sealed class ReplCommand : Command
    {
        sealed class Options
        {
        }

        public ReplCommand()
            : base("repl", "Run an interactive evaluator.")
        {
            Handler = CommandHandler.Create<Options>(Run);
        }

        void Run(Options options)
        {
        }
    }
}
