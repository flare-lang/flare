using System.Threading.Tasks;

namespace Flare.Cli.Commands
{
    sealed class CleanCommand : BaseCommand
    {
        sealed class Options
        {
        }

        public CleanCommand()
            : base("clean", "Clean build artifacts of a project.")
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

            if (project.BuildDirectory.Exists)
                project.BuildDirectory.Delete();

            return await Task.FromResult(0);
        }
    }
}
