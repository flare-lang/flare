using System.CommandLine;
using System.CommandLine.Invocation;

namespace Flare.Cli.Commands
{
    public sealed class RestoreCommand : Command
    {
        sealed class Options
        {
        }

        public RestoreCommand()
            : base("restore", "Restore remote dependencies of a project.")
        {
            Handler = CommandHandler.Create<Options>(Run);
        }

        void Run(Options options)
        {
        }
    }
}
