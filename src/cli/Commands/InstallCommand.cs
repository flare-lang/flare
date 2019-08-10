using System.Threading.Tasks;

namespace Flare.Cli.Commands
{
    sealed class InstallCommand : BaseCommand
    {
        sealed class Options
        {
        }

        public InstallCommand()
            : base("install", "Install an executable project. [NYI]")
        {
            RegisterHandler<Options>(Run);
        }

        async Task<int> Run(Options options)
        {
            var project = Project.Instance;

            if (project == null)
            {
                Log.ErrorLine("No '{0}' file found in the current directory.", Project.ProjectFileName);
                return 1;
            }

            if (project.Type != ProjectType.Executable)
            {
                Log.ErrorLine("Project '{0}' is not executable.", project.Name);
                return 1;
            }

            // TODO
            Log.WarningLine("This command is not yet implemented.");

            return await Task.FromResult(0);
        }
    }
}
