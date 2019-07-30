using System.CommandLine;
using System.CommandLine.Invocation;

namespace Flare.Cli.Commands
{
    public sealed class UninstallCommand : Command
    {
        sealed class Options
        {
        }

        public UninstallCommand()
            : base("uninstall", "Uninstall binaries of a project.")
        {
            Handler = CommandHandler.Create<Options>(Run);
        }

        void Run(Options options)
        {
        }
    }
}
