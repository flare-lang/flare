using System.Threading.Tasks;

namespace Flare.Cli.Commands
{
    sealed class SearchCommand : BaseCommand
    {
        sealed class Options
        {
        }

        public SearchCommand()
            : base("search", "Search the package registry.")
        {
            RegisterHandler<Options>(Run);
        }

        async Task<int> Run(Options options)
        {
            // TODO

            return await Task.FromResult(0);
        }
    }
}
