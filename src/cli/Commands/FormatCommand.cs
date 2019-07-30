using System.CommandLine;
using System.CommandLine.Invocation;

namespace Flare.Cli.Commands
{
    public sealed class FormatCommand : Command
    {
        sealed class Options
        {
        }

        public FormatCommand()
            : base("format", "Format source text files of a project.")
        {
            Handler = CommandHandler.Create<Options>(Run);
        }

        void Run(Options options)
        {
        }
    }
}
