using System.CommandLine;
using System.CommandLine.Invocation;

namespace Flare.Cli.Commands
{
    public sealed class BuildCommand : Command
    {
        sealed class Options
        {
        }

        public BuildCommand()
            : base("build", "Build binaries of a project.")
        {
            Handler = CommandHandler.Create<Options>(Run);
        }

        void Run(Options options)
        {
        }
    }
}
