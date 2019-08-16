using System.CommandLine;
using Flare.Metadata;
using Flare.Syntax;

namespace Flare.Cli.Commands
{
    sealed class RunCommand : BaseCommand
    {
        sealed class Options
        {
            public string[] Arguments { get; set; } = null!;
        }

        public RunCommand()
            : base("run", "Run an executable project.")
        {
            AddArgument<string[]>("arguments", "Arguments to be passed to the program.", ArgumentArity.ZeroOrMore);

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

            if (project.Type != ProjectType.Executable)
            {
                Log.ErrorLine("Project '{0}' is not executable.", project.Name);
                return 1;
            }

            var context = new SyntaxContext();

            _ = project.LoadModules(ModuleLoaderMode.Normal, context);

            foreach (var diag in context.Diagnostics)
                LogDiagnostic(diag);

            // TODO

            return context.HasDiagnostics ? 1 : 0;
        }
    }
}
