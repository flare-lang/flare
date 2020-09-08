using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Flare.Metadata;
using Flare.Syntax;
using Nett;
using NuGet.Versioning;

namespace Flare.Cli
{
    sealed class Project
    {
        public static Project? Instance { get; }

        public const string ProjectFileName = nameof(Flare) + Toml.FileExtension;

        public DirectoryInfo ProjectDirectory { get; }

        public DirectoryInfo DependencyDirectory { get; }

        public DirectoryInfo SourceDirectory { get; }

        public FileInfo MainModule { get; }

        public DirectoryInfo BuildDirectory { get; }

        public ProjectType Type { get; }

        public string Name { get; }

        public string? ExecutableFileName { get; }

        public SemanticVersion Version { get; }

        public string? License { get; }

        public string? Description { get; }

        public Uri? ProjectUri { get; }

        public Uri? DocumentationUri { get; }

        public Uri? SourceUri { get; }

        public SyntaxLintConfiguration Lints { get; }

        static Project()
        {
            var path = Path.GetFullPath(ProjectFileName);

            if (File.Exists(path))
                Instance = new Project(path);
        }

        [SuppressMessage("Microsoft.Globalization", "CA1308", Justification = "Values are well-known.")]
        Project(string path)
        {
            ProjectDirectory = new DirectoryInfo(Path.GetDirectoryName(path)!);
            DependencyDirectory = new DirectoryInfo(Path.Combine(ProjectDirectory.FullName, "dep"));
            SourceDirectory = new DirectoryInfo(Path.Combine(ProjectDirectory.FullName, "src"));
            BuildDirectory = new DirectoryInfo(Path.Combine(ProjectDirectory.FullName, "bin"));

            // TODO: make this parsing more robust in general, and add error reporting.

            var table = Toml.ReadFile(path);
            var project = (TomlTable)table.Get("project");

            Type = (ProjectType)Enum.Parse(typeof(ProjectType), project.Get<string>("type"), true);
            Name = project.Get<string>("name");
            MainModule = new FileInfo(Path.Combine(SourceDirectory.FullName, Path.ChangeExtension(Name,
                StandardModuleLoader.ModuleFileNameExtension)!));
            ExecutableFileName = table.TryGetValue("executable")?.Get<string>();
            Version = SemanticVersion.Parse(project.Get<string>("version"));
            License = project.TryGetValue("license")?.Get<string>();
            Description = project.TryGetValue("description")?.Get<string>();
            ProjectUri = project.TryGetValue("url") is TomlObject o1 ? new Uri(o1.Get<string>()) : null;
            DocumentationUri = project.TryGetValue("url-doc") is TomlObject o2 ? new Uri(o2.Get<string>()) : null;
            SourceUri = project.TryGetValue("url-src") is TomlObject o3 ? new Uri(o3.Get<string>()) : null;

            var cfg = new SyntaxLintConfiguration();

            if (table.TryGetValue("lints") is TomlTable lints)
            {
                foreach (var kvp in lints)
                {
                    cfg = cfg.Set(kvp.Key, kvp.Value.Get<string>().ToLowerInvariant() switch
                    {
                        LanguageLinter.NoneSeverityName => (SyntaxDiagnosticSeverity?)null,
                        LanguageLinter.SuggestionSeverityName => SyntaxDiagnosticSeverity.Suggestion,
                        LanguageLinter.WarninngSeverityName => SyntaxDiagnosticSeverity.Warning,
                        LanguageLinter.ErrorSeverityName => SyntaxDiagnosticSeverity.Error,
                        _ => throw DebugAssert.Unreachable(),
                    });
                }
            }

            Lints = cfg;
        }

        public ModuleLoader LoadModules(ModuleLoaderMode mode, SyntaxContext context)
        {
            var loader = new StandardModuleLoader(mode)
            {
                UseEnvironmentVariable = false,
            };

            _ = loader.SearchPaths.Add(SourceDirectory.FullName);

            // TODO: Add dependency paths.

            var iter = SourceDirectory.EnumerateFiles(Path.ChangeExtension("*",
                StandardModuleLoader.ModuleFileNameExtension)!, new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                });

            foreach (var file in iter)
            {
                try
                {
                    _ = loader.LoadModule(StringSourceText.FromAsync(file.FullName, file.OpenRead()).Result, context);
                }
                catch (ModuleLoadException)
                {
                    // All errors will be reported in the context.
                }
            }

            return loader;
        }
    }
}
