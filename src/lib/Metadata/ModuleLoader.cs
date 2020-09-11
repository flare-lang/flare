using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Flare.Syntax;

namespace Flare.Metadata
{
    public abstract class ModuleLoader
    {
        public ModuleLoaderMode Mode { get; }

        readonly object _lock = new object();

        readonly HashSet<string> _loading = new HashSet<string>();

        readonly Dictionary<ModulePath, Module> _modules = new Dictionary<ModulePath, Module>();

        protected ModuleLoader(ModuleLoaderMode mode)
        {
            Mode = mode.Check(nameof(mode));

            var context = new SyntaxContext();

            void LoadCore(params string[] components)
            {
                _ = LoadModule(new ModulePath(new[] { ModulePath.CoreModuleName }.Concat(components)
                    .ToImmutableArray()), context);
            }

            LoadCore();
            LoadCore("Agent");
            LoadCore("Array");
            LoadCore("GC");
            LoadCore("IO");
            LoadCore("Map");
            LoadCore("Set");
            LoadCore("String");
            LoadCore("Time");
        }

        public Module? GetModule(ModulePath path)
        {
            lock (_lock)
                return _modules.TryGetValue(path, out var mod) ? mod : null;
        }

        public Module[] GetModules()
        {
            lock (_lock)
                return _modules.Values.ToArray();
        }

        ProgramNode ParseModule(ModulePath? path, SourceText source, SyntaxContext context)
        {
            var msg = path != null ? $" with path '{path}'" : string.Empty;

            if (!_loading.Add(source.FullPath))
                throw new ModuleLoadException($"Module{msg} could not be loaded due to circular 'use' declarations.");

            try
            {
                if (context.Parses.ContainsKey(source.FullPath))
                    throw new ModuleLoadException($"Module{msg} was already loaded previously.");

                var lex = LanguageLexer.Lex(source);
                var parse = LanguageParser.Parse(lex, SyntaxMode.Normal);

                context.AddParse(source.FullPath, parse);

                foreach (var diag in lex.Diagnostics)
                    context.AddDiagnostic(diag);

                foreach (var diag in parse.Diagnostics)
                    context.AddDiagnostic(diag);

                var analysis = LanguageAnalyzer.Analyze(parse, this, context);

                foreach (var diag in analysis.Diagnostics)
                    context.AddDiagnostic(diag);

                return analysis.IsSuccess ? (ProgramNode)parse.Tree : throw new ModuleLoadException(
                    $"Module{msg} failed to load due to syntax and/or semantic errors.");
            }
            finally
            {
                _ = _loading.Remove(source.FullPath);
            }
        }

        public Module LoadModule(SourceText source, SyntaxContext context)
        {
            lock (_lock)
            {
                var tree = ParseModule(null, source, context);
                var path = new ModulePath(tree.Path.ComponentTokens.Tokens.Select(x => x.Text).ToImmutableArray());

                if (GetModule(path) is Module existing)
                    throw new ModuleLoadException($"Module with path '{path}' was already loaded previously.");

                var mod = new Module(this, path, tree);

                _modules.Add(path, mod);

                return mod;
            }
        }

        protected abstract SourceText? GetSourceText(ModulePath path);

        public Module LoadModule(ModulePath path, SyntaxContext context)
        {
            lock (_lock)
            {
                if (GetModule(path) is Module existing)
                    return existing;

                var source = GetSourceText(path);

                if (source == null)
                    throw new ModuleLoadException($"Source file for module path '{path}' could not be located.");

                var mod = new Module(this, path, ParseModule(path, source, context));

                _modules.Add(path, mod);

                return mod;
            }
        }
    }
}
