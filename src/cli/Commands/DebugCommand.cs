using System.CommandLine;
using System.CommandLine.Invocation;

namespace Flare.Cli.Commands
{
    public sealed class DebugCommand : Command
    {
        sealed class Options
        {
        }

        public DebugCommand()
            : base("debug", "Interactively debug a project.")
        {
            Handler = CommandHandler.Create<Options>(Run);
        }

        void Run(Options options)
        {
        }
    }
}
