namespace Flare.Cli.Commands
{
    sealed class SearchCommand : BaseCommand
    {
        sealed class Options
        {
        }

        public SearchCommand()
            : base("search", "Search the package registry. [NYI]")
        {
            RegisterHandler<Options>(Run);
        }

        int Run(Options options)
        {
            // TODO
            Log.WarningLine("This command is not yet implemented.");

            return 0;
        }
    }
}
