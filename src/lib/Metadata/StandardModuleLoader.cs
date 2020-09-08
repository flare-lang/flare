using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Flare.Syntax;

namespace Flare.Metadata
{
    public class StandardModuleLoader : ModuleLoader
    {
        public const string ModuleFileNameExtension = "fl";

        public const string EnvironmentVariableName = "FLARE_PATH";

        static readonly string _assemblyPath;

        public bool UseEnvironmentVariable { get; set; } = true;

        public HashSet<string> SearchPaths { get; } = new HashSet<string>();

        static StandardModuleLoader()
        {
            var loc = Assembly.GetExecutingAssembly().Location;

            _assemblyPath = loc.Length != 0 ? loc : "<libflare>";
        }

        public StandardModuleLoader(ModuleLoaderMode mode)
            : base(mode)
        {
        }

        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "TODO")]
        static SourceText? TryLoadSourceText(string directory, string path)
        {
            var full = Path.GetFullPath(Path.Combine(directory, Path.ChangeExtension(path, ModuleFileNameExtension)!));

            try
            {
                return StringSourceText.FromAsync(full, File.OpenRead(full)).Result;
            }
            catch (Exception)
            {
                // TODO: Catch more specific exceptions?
                return null;
            }
        }

        static string[] GetSearchPaths(EnvironmentVariableTarget target)
        {
            var value = Environment.GetEnvironmentVariable(EnvironmentVariableName, target);

            return value != null ? value.Split(Path.PathSeparator) : Array.Empty<string>();
        }

        protected override SourceText? GetSourceText(ModulePath path)
        {
            // Core modules are not subject to any security checks, so make sure they're not loaded
            // from arbitrary file system locations.
            if (path.IsCore)
            {
                try
                {
                    var rsc = $"{nameof(Flare)}.{string.Join('.', path.Components)}.{ModuleFileNameExtension}";
                    var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(rsc);

                    return stream != null ? StringSourceText.FromAsync(Path.Combine(_assemblyPath, rsc),
                        stream).Result : null;
                }
                catch (Exception e)
                {
                    throw new ModuleLoadException($"Core module '{path}' failed to load: {e.Message}", e);
                }
            }

            var str = string.Join(Path.DirectorySeparatorChar, path.Components);

            foreach (var dir in SearchPaths)
                if (TryLoadSourceText(dir, str) is SourceText source)
                    return source;

            if (!UseEnvironmentVariable)
                return null;

            foreach (var dir in GetSearchPaths(EnvironmentVariableTarget.Process))
                if (TryLoadSourceText(dir, str) is SourceText source)
                    return source;

            foreach (var dir in GetSearchPaths(EnvironmentVariableTarget.User))
                if (TryLoadSourceText(dir, str) is SourceText source)
                    return source;

            foreach (var dir in GetSearchPaths(EnvironmentVariableTarget.Machine))
                if (TryLoadSourceText(dir, str) is SourceText source)
                    return source;

            return null;
        }
    }
}
