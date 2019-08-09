using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Flare.Runtime;

namespace Flare.Cli.Commands
{
    sealed class NewCommand : BaseCommand
    {
        sealed class Options
        {
            public string Name { get; set; } = null!;

            public DirectoryInfo Directory { get; set; } = null!;

            public bool Force { get; set; }
        }

        public NewCommand()
            : base("new", "Create a new project.")
        {
            AddArgument<string>("name", "Name of the new project.", ArgumentArity.ExactlyOne);
            AddArgument<string[]>("directory", "Directory to add generated files to.", ArgumentArity.ExactlyOne);

            AddOption<bool>("-f", "--force", "Proceed even if existing files would be overwritten.");

            RegisterHandler<Options>(Run);
        }

        async Task<int> Run(Options options)
        {
            if (!ModulePath.IsValidComponent(options.Name))
            {
                Log.ErrorLine("'{0}' is not a valid project name.", options.Name);
                return 1;
            }

            var dir = options.Directory;

            if (dir.Exists)
            {
                Log.ErrorLine("Output directory '{0}' already exists.", dir.FullName);
                return 1;
            }

            dir.Create();

            // TODO

            return await Task.FromResult(0);
        }
    }
}
