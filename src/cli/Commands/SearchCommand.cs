namespace Flare.Cli.Commands
{
    sealed class SearchCommand : BaseCommand
    {
        sealed class SearchOptions
        {
        }

        public SearchCommand()
            : base("search", "Search the package registry. [NYI]")
        {
            RegisterHandler<SearchOptions>(Run);
        }

        int Run(SearchOptions options)
        {
            // TODO
            Log.WarningLine("This command is not yet implemented.");

            return 0;
        }
    }
}
