using Flare.Metadata;
using Flare.Syntax;
using Flare.Syntax.Lints;

namespace Flare.Cli.Commands
{
    sealed class CheckCommand : BaseCommand
    {
        sealed class Options
        {
        }

        public CheckCommand()
            : base("check", "Run syntax and lint checks on a project.")
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

            _ = project.LoadModules(ModuleLoaderMode.Reflection, context);

            foreach (var diag in context.Diagnostics)
                LogDiagnostic(diag);

            foreach (var (path, parse) in context.Parses)
            {
                // TODO: This check is a bit brittle. It works fine for the current module loader
                // setup, but might not in the future.
                if (!path.StartsWith(project.SourceDirectory.FullName))
                    continue;

                var lint = LanguageLinter.Lint(parse, project.Lints, new SyntaxLint[]
                {
                    new UndocumentedDeclarationLint(),
                });

                foreach (var diag in lint.Diagnostics)
                    LogDiagnostic(diag);
            }

            return context.HasDiagnostics ? 1 : 0;
        }
    }
}
