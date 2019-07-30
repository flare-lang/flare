using System.CommandLine;
using System.CommandLine.Invocation;

namespace Flare.Cli.Commands
{
    public sealed class SearchCommand : Command
    {
        sealed class Options
        {
        }

        public SearchCommand()
            : base("search", "Search the package registry.")
        {
            Handler = CommandHandler.Create<Options>(Run);
        }

        void Run(Options options)
        {
        }
    }
}
