using System.CommandLine;
using System.CommandLine.Invocation;

namespace Flare.Cli.Commands
{
    public sealed class NewCommand : Command
    {
        sealed class Options
        {
        }

        public NewCommand()
            : base("new", "Create a new project.")
        {
            Handler = CommandHandler.Create<Options>(Run);
        }

        void Run(Options options)
        {
        }
    }
}
