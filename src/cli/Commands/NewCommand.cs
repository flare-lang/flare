using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Flare.Runtime;
using Flare.Syntax;

namespace Flare.Cli.Commands
{
    sealed class NewCommand : BaseCommand
    {
        sealed class Options
        {
            public ProjectType Type { get; set; }

            public string Name { get; set; } = null!;

            public DirectoryInfo Directory { get; set; } = null!;

            public bool Force { get; set; }
        }

        public NewCommand()
            : base("new", "Create a new project.")
        {
            AddArgument<ProjectType>("type", "The project type (library or executable).", ArgumentArity.ExactlyOne);
            AddArgument<string>("name", "Name of the new project.", ArgumentArity.ExactlyOne);
            AddArgument<string>("directory", "Directory to add generated files to.", ArgumentArity.ExactlyOne);

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

            if (dir.Exists && !options.Force)
            {
                Log.ErrorLine("Output directory '{0}' already exists.", dir.FullName);
                return 1;
            }

            dir.Create();

            var git = Path.Combine(dir.FullName, ".git");

            if (!Directory.Exists(git))
            {
                var (ok, output) = await RunGitAsync($"init {dir.FullName}");

                if (!ok)
                {
                    Log.WarningLine("Could not create Git repository in '{0}':", git);
                    Log.WarningLine(output);
                }
                else
                    Log.InfoLine("Created Git repository in '{0}'.", git);
            }

            await WriteFileAsync(Path.Combine(dir.FullName, ".editorconfig"), async sw =>
            {
                await sw.WriteLineAsync("[*]");
                await sw.WriteLineAsync("charset = utf-8");
                await sw.WriteLineAsync("indent_size = 4");
                await sw.WriteLineAsync("indent_style = space");
                await sw.WriteLineAsync("insert_final_newline = true");
                await sw.WriteLineAsync("max_line_length = off");
                await sw.WriteLineAsync("tab_width = 4");
                await sw.WriteLineAsync("trim_trailing_whitespace = true");
            });

            await WriteFileAsync(Path.Combine(dir.FullName, ".gitattributes"),
                async sw => await sw.WriteLineAsync("* text"));

            await WriteFileAsync(Path.Combine(dir.FullName, ".gitignore"), async sw =>
            {
                await sw.WriteLineAsync("/bin");
                await sw.WriteLineAsync("/dep");
            });

            await WriteFileAsync(Path.Combine(dir.FullName, Project.ProjectFileName), async sw =>
            {
                await sw.WriteLineAsync("[project]");
                await sw.WriteLineAsync();
                await sw.WriteLineAsync("# This can be `library` or `executable`.");
                await sw.WriteLineAsync($"type = \"{options.Type.ToString().ToLowerInvariant()}\"");
                await sw.WriteLineAsync();
                await sw.WriteLineAsync("# This must be a valid module identifier. All modules in your project should");
                await sw.WriteLineAsync("# start with this name. It must be unique in the package registry if the ");
                await sw.WriteLineAsync("# package will be published.");
                await sw.WriteLineAsync($"name = {options.Name}");
                await sw.WriteLineAsync();
                await sw.WriteLineAsync("# See: https://semver.org");
                await sw.WriteLineAsync("version = \"0.1.0\"");
                await sw.WriteLineAsync();
                await sw.WriteLineAsync("# The following keys are only used for the package registry. You can leave");
                await sw.WriteLineAsync("# them empty if the package will not be published.");
                await sw.WriteLineAsync();
                await sw.WriteLineAsync("# If you want to use a different license, set its SPDX identifier here.");
                await sw.WriteLineAsync("license = \"ISC\"");
                await sw.WriteLineAsync();
                await sw.WriteLineAsync("# A brief description of what this project does.");
                await sw.WriteLineAsync("description = \"TODO\"");
                await sw.WriteLineAsync();
                await sw.WriteLineAsync("# Project URL (e.g. https://flare-lang.org).");
                await sw.WriteLineAsync("url = \"\"");
                await sw.WriteLineAsync();
                await sw.WriteLineAsync("# Project documentation URL, e.g. https://flare-lang.org/documentation.html.");
                await sw.WriteLineAsync("url-doc = \"\"");
                await sw.WriteLineAsync();
                await sw.WriteLineAsync("# Project source URL (e.g. https://github.com/flare-lang/flare).");
                await sw.WriteLineAsync("url-src = \"\"");
                await sw.WriteLineAsync();
                await sw.WriteLineAsync("[lints]");
                await sw.WriteLineAsync();
                await sw.WriteLineAsync("# You can use this section to configure lint severities (none, suggestion,");
                await sw.WriteLineAsync("# warning, error) for `flare check`. Default severities are listed below.");
                await sw.WriteLineAsync();

                foreach (var lint in LanguageLinter.Lints.Values)
                    await sw.WriteLineAsync($"#{lint.Name} = \"{lint.DefaultSeverity.ToString().ToLowerInvariant()}\"");
            });

            await WriteFileAsync(Path.Combine(dir.FullName, "README.md"), async sw =>
            {
                await sw.WriteLineAsync($"# {options.Name}");
                await sw.WriteLineAsync();
                await sw.WriteLineAsync("TODO: Write a project description.");
            });

            var src = dir.CreateSubdirectory("src");

            await WriteFileAsync(Path.Combine(src.FullName, Path.ChangeExtension(options.Name,
                StandardModuleLoader.ModuleFileNameExtension)!), async sw =>
            {
                await sw.WriteLineAsync($"mod {options.Name};");
                await sw.WriteLineAsync();
                await sw.WriteLineAsync("use Core;");
                await sw.WriteLineAsync();
                await sw.WriteLineAsync("pub fn main(_args, _env) {");
                await sw.WriteLineAsync("    nil;");
                await sw.WriteLineAsync("}");
            });

            return await Task.FromResult(0);
        }

        static async Task WriteFileAsync(string path, Func<StreamWriter, Task> action)
        {
            using var sw = new StreamWriter(path);

            await action(sw);

            Log.InfoLine("Created '{0}'.", path);
        }
    }
}
