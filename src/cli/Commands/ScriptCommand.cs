using System.CommandLine;
using System.IO;
using Flare.Metadata;
using Flare.Syntax;

namespace Flare.Cli.Commands
{
    sealed class ScriptCommand : BaseCommand
    {
        sealed class ScriptOptions
        {
            public FileInfo Module { get; set; } = null!;

            public string[] Arguments { get; set; } = null!;
        }

        public ScriptCommand()
            : base("script", "Run a script file.")
        {
            IsHidden = true;

            AddArgument<FileInfo>("module", "Entry point module.", ArgumentArity.ExactlyOne);
            AddArgument<string[]>("arguments", "Arguments to be passed to the program.", ArgumentArity.ZeroOrMore);

            RegisterHandler<ScriptOptions>(Run);
        }

        int Run(ScriptOptions options)
        {
            var path = options.Module.FullName;

            if (!options.Module.Exists)
            {
                Log.ErrorLine("Module '{0}' could not be found.", path);
                return 1;
            }

            var loader = new StandardModuleLoader(ModuleLoaderMode.Normal);

            _ = loader.SearchPaths.Add(Path.GetDirectoryName(path)!);

            using var file = File.OpenRead(path);

            var text = StringSourceText.FromAsync(path, file).Result;
            var context = new SyntaxContext();

            try
            {
                _ = loader.LoadModule(text, context);
            }
            catch (ModuleLoadException)
            {
                // All errors will be reported in the context.
            }

            foreach (var diag in context.Diagnostics)
                LogDiagnostic(diag);

            // TODO

            return context.HasDiagnostics ? 1 : 0;
        }
    }
}
