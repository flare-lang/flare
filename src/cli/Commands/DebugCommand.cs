using System.Threading.Tasks;

namespace Flare.Cli.Commands
{
    sealed class DebugCommand : BaseCommand
    {
        sealed class Options
        {
        }

        public DebugCommand()
            : base("debug", "Interactively debug a project.")
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

            return await Task.FromResult(0);
        }
    }
}
