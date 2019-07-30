using System.CommandLine;
using System.CommandLine.Invocation;

namespace Flare.Cli.Commands
{
    public sealed class CheckCommand : Command
    {
        sealed class Options
        {
        }

        public CheckCommand()
            : base("check", "Run syntax and lint checks on a project.")
        {
            Handler = CommandHandler.Create<Options>(Run);
        }

        void Run(Options options)
        {
        }
    }
}
