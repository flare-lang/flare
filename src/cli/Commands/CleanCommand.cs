using System.CommandLine;
using System.CommandLine.Invocation;

namespace Flare.Cli.Commands
{
    public sealed class CleanCommand : Command
    {
        sealed class Options
        {
        }

        public CleanCommand()
            : base("clean", "Clean build artifacts of a project.")
        {
            Handler = CommandHandler.Create<Options>(Run);
        }

        void Run(Options options)
        {
        }
    }
}
