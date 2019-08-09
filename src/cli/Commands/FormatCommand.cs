using System.Threading.Tasks;

namespace Flare.Cli.Commands
{
    sealed class FormatCommand : BaseCommand
    {
        sealed class Options
        {
        }

        public FormatCommand()
            : base("format", "Format source text files of a project.")
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

            // TODO

            return await Task.FromResult(0);
        }
    }
}
