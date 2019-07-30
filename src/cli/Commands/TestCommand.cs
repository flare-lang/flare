using System.CommandLine;
using System.CommandLine.Invocation;

namespace Flare.Cli.Commands
{
    public sealed class TestCommand : Command
    {
        sealed class Options
        {
        }

        public TestCommand()
            : base("test", "Run unit tests of a project.")
        {
            Handler = CommandHandler.Create<Options>(Run);
        }

        void Run(Options options)
        {
        }
    }
}
