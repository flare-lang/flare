using System.CommandLine;
using System.CommandLine.Invocation;

namespace Flare.Cli.Commands
{
    public sealed class DocCommand : Command
    {
        sealed class Options
        {
        }

        public DocCommand()
            : base("doc", "Build documentation for a project.")
        {
            Handler = CommandHandler.Create<Options>(Run);
        }

        void Run(Options options)
        {
        }
    }
}
