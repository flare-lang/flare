using Flare.Metadata;
using Flare.Syntax;

namespace Flare.Cli.Commands
{
    sealed class TestCommand : BaseCommand
    {
        sealed class Options
        {
        }

        public TestCommand()
            : base("test", "Run unit tests of a project.")
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

            var context = new SyntaxContext();

            _ = project.LoadModules(ModuleLoaderMode.Normal, context);

            foreach (var diag in context.Diagnostics)
                LogDiagnostic(diag);

            // TODO

            return context.HasDiagnostics ? 1 : 0;
        }
    }
}
