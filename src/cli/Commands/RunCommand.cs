using System.CommandLine;
using System.CommandLine.Invocation;

namespace Flare.Cli.Commands
{
    public sealed class RunCommand : Command
    {
        sealed class Options
        {
        }

        public RunCommand()
            : base("run", "Run an executable project.")
        {
            Handler = CommandHandler.Create<Options>(Run);
        }

        void Run(Options options)
        {
        }
    }
}
