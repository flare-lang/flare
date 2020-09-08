using System;
using Flare.Metadata;
using Flare.Syntax;

namespace Flare.Cli.Commands
{
    sealed class CheckCommand : BaseCommand
    {
        sealed class CheckOptions
        {
        }

        public CheckCommand()
            : base("check", "Run syntax and lint checks on a project.")
        {
            RegisterHandler<CheckOptions>(Run);
        }

        int Run(CheckOptions options)
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
                if (!path.StartsWith(project.SourceDirectory.FullName, StringComparison.InvariantCulture))
                    continue;

                var lint = LanguageLinter.Lint(parse, project.Lints, LanguageLinter.Lints.Values);

                foreach (var diag in lint.Diagnostics)
                    LogDiagnostic(diag);
            }

            return context.HasDiagnostics ? 1 : 0;
        }
    }
}
