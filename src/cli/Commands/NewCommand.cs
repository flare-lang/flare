using System;
using System.CommandLine;
using System.IO;
using Flare.Metadata;
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

        int Run(Options options)
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
                var (ok, output) = RunGit($"init {dir.FullName}");

                if (!ok)
                {
                    Log.WarningLine("Could not create Git repository in '{0}':", ToRelative(git));
                    Log.WarningLine(output);
                }
                else
                    Log.InfoLine("Created Git repository in '{0}'.", ToRelative(git));
            }

            WriteFileAsync(Path.Combine(dir.FullName, ".editorconfig"), sw =>
            {
                sw.WriteLine("[*]");
                sw.WriteLine("charset = utf-8");
                sw.WriteLine("indent_size = 4");
                sw.WriteLine("indent_style = space");
                sw.WriteLine("insert_final_newline = true");
                sw.WriteLine("max_line_length = off");
                sw.WriteLine("tab_width = 4");
                sw.WriteLine("trim_trailing_whitespace = true");
            });

            WriteFileAsync(Path.Combine(dir.FullName, ".gitattributes"), sw => sw.WriteLine("* text"));

            WriteFileAsync(Path.Combine(dir.FullName, ".gitignore"), sw =>
            {
                sw.WriteLine("/bin");
                sw.WriteLine("/dep");
            });

            WriteFileAsync(Path.Combine(dir.FullName, Project.ProjectFileName), sw =>
            {
                sw.WriteLine("[project]");
                sw.WriteLine();
                sw.WriteLine("# This can be `library` or `executable`.");
                sw.WriteLine($"type = \"{options.Type.ToString().ToLowerInvariant()}\"");
                sw.WriteLine();
                sw.WriteLine("# This must be a valid module identifier. All modules in your project should");
                sw.WriteLine("# start with this name. It must be unique in the package registry if the");
                sw.WriteLine("# package will be published.");
                sw.WriteLine($"name = \"{options.Name}\"");
                sw.WriteLine();
                sw.WriteLine("# See: https://semver.org");
                sw.WriteLine("version = \"0.1.0\"");
                sw.WriteLine();
                sw.WriteLine("# The following keys are only used for the package registry. You can leave them");
                sw.WriteLine("# empty if the package will not be published.");
                sw.WriteLine();
                sw.WriteLine("# If you want to use a different license, set its SPDX identifier here.");
                sw.WriteLine("license = \"ISC\"");
                sw.WriteLine();
                sw.WriteLine("# A brief description of what this project does.");
                sw.WriteLine("description = \"TODO\"");
                sw.WriteLine();
                sw.WriteLine("# Project URL (e.g. https://flare-lang.org).");
                sw.WriteLine("url = \"\"");
                sw.WriteLine();
                sw.WriteLine("# Project documentation URL, e.g. https://flare-lang.org/documentation.html.");
                sw.WriteLine("url-doc = \"\"");
                sw.WriteLine();
                sw.WriteLine("# Project source URL (e.g. https://github.com/flare-lang/flare).");
                sw.WriteLine("url-src = \"\"");
                sw.WriteLine();
                sw.WriteLine("[lints]");
                sw.WriteLine();
                sw.WriteLine("# You can use this section to configure lint severities (none, suggestion,");
                sw.WriteLine("# warning, error) for `flare check`. Default severities are listed below.");
                sw.WriteLine();

                foreach (var lint in LanguageLinter.Lints.Values)
                    sw.WriteLine($"#{lint.Name} = \"{lint.DefaultSeverity.ToString().ToLowerInvariant()}\"");
            });

            WriteFileAsync(Path.Combine(dir.FullName, "README.md"), sw =>
            {
                sw.WriteLine($"# {options.Name}");
                sw.WriteLine();
                sw.WriteLine("TODO: Write a project description.");
            });

            var src = dir.CreateSubdirectory("src");

            WriteFileAsync(Path.Combine(src.FullName, Path.ChangeExtension(options.Name,
                StandardModuleLoader.ModuleFileNameExtension)!), sw =>
            {
                sw.WriteLine($"mod {options.Name};");
                sw.WriteLine();
                sw.WriteLine("use Core;");

                if (options.Type != ProjectType.Executable)
                    return;

                sw.WriteLine();
                sw.WriteLine("pub fn main(_args, _env) {");
                sw.WriteLine("    nil;");
                sw.WriteLine("}");
            });

            return 0;
        }

        static void WriteFileAsync(string path, Action<StreamWriter> action)
        {
            using var sw = new StreamWriter(path);

            action(sw);

            Log.InfoLine("Created '{0}'.", ToRelative(path));
        }
    }
}
