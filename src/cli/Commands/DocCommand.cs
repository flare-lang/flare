namespace Flare.Cli.Commands
{
    sealed class DocCommand : BaseCommand
    {
        sealed class Options
        {
        }

        public DocCommand()
            : base("doc", "Build documentation for a project. [NYI]")
        {
            RegisterHandler<Options>(Run);
        }

        int Run(Options options)
        {
            var project = Project.Instance;

            if (project == null)
            {
                Log.ErrorLine("No '{0}' file found in the current directory.", Project.ProjectFileName);
                return 1;
            }

            // TODO
            Log.WarningLine("This command is not yet implemented.");

            return 0;
        }
    }
}
