using System.CommandLine;
using System.CommandLine.Invocation;

namespace Flare.Cli.Commands
{
    public sealed class InstallCommand : Command
    {
        sealed class Options
        {
        }

        public InstallCommand()
            : base("install", "Install binaries of a project.")
        {
            Handler = CommandHandler.Create<Options>(Run);
        }

        void Run(Options options)
        {
        }
    }
}
