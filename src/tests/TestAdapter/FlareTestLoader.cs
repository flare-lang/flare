using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Flare.Cli;

namespace Flare.Tests.TestAdapter
{
    static class FlareTestLoader
    {
        const RegexOptions Options = RegexOptions.Compiled | RegexOptions.Singleline;

        public static ImmutableDictionary<string, FlareTest> Tests { get; }

        static readonly Regex _ifSet = new Regex("^test:if-set (.*)$", Options);

        static readonly Regex _ifUnset = new Regex("^test:if-unset (.*)$", Options);

        static readonly Regex _env = new Regex("^test:env (.*)=(.*)$", Options);

        static readonly Regex _args = new Regex("^test:args (.*)$", Options);

        static FlareTestLoader()
        {
            var tests = new Dictionary<string, FlareTest>();
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "..", "..");

            foreach (var test in LoadCheckTests(path))
                tests.Add(test.FullName, test);

            foreach (var test in LoadRunTests(path))
                tests.Add(test.FullName, test);

            foreach (var test in LoadScriptTests(path))
                tests.Add(test.FullName, test);

            Tests = tests.ToImmutableDictionary();
        }

        static IEnumerable<FlareCheckTest> LoadCheckTests(string path)
        {
            var script = Path.Combine(path, "check");

            foreach (var dir in new DirectoryInfo(script).EnumerateDirectories())
            {
                ReadHeader(Path.Combine(dir.FullName, Project.ProjectFileName), '#', out var filters, out var vars,
                    out var args, out var succeed);

                var stdout = TryReadFile(Path.Combine(dir.FullName, "check.out"));
                var stderr = TryReadFile(Path.Combine(dir.FullName, "check.err"));

                yield return new FlareCheckTest(Path.GetFileName(dir.FullName)!, dir, filters, vars, args, succeed, stdout,
                    stderr);
            }
        }

        static IEnumerable<FlareRunTest> LoadRunTests(string path)
        {
            var script = Path.Combine(path, "run");

            foreach (var dir in new DirectoryInfo(script).EnumerateDirectories())
            {
                ReadHeader(Path.Combine(dir.FullName, Project.ProjectFileName), '#', out var filters, out var vars,
                    out var args, out var succeed);

                var stdout = TryReadFile(Path.Combine(dir.FullName, "run.out"));
                var stderr = TryReadFile(Path.Combine(dir.FullName, "run.err"));

                yield return new FlareRunTest(Path.GetFileName(dir.FullName)!, dir, filters, vars, args, succeed, stdout,
                    stderr);
            }
        }

        static IEnumerable<FlareScriptTest> LoadScriptTests(string path)
        {
            var script = Path.Combine(path, "script");

            foreach (var file in new DirectoryInfo(script).EnumerateFiles("*.fl"))
            {
                ReadHeader(file.FullName, '\'', out var filters, out var vars, out var args, out var succeed);

                var stdout = TryReadFile(Path.ChangeExtension(file.FullName, "out")!);
                var stderr = TryReadFile(Path.ChangeExtension(file.FullName, "err")!);

                yield return new FlareScriptTest(Path.GetFileNameWithoutExtension(file.FullName)!, file, filters, vars,
                    args, succeed, stdout, stderr);
            }
        }

        static void ReadHeader(string path, char prefix, out ImmutableArray<FlareTestFilter> filters,
            out ImmutableDictionary<string, string> variables, out string arguments, out bool succeed)
        {
            filters = ImmutableArray<FlareTestFilter>.Empty;
            variables = ImmutableDictionary<string, string>.Empty;
            arguments = string.Empty;
            succeed = true;

            foreach (var line in File.ReadAllLines(path))
            {
                if (!line.StartsWith(prefix))
                    break;

                var directive = line.Substring(1);

                Match m;

                if (directive == "test:pass")
                    succeed = true;
                else if (directive == "test:fail")
                    succeed = false;
                else if ((m = _args.Match(directive)).Success)
                    arguments += m.Groups[1].Value;
                else if ((m = _ifSet.Match(directive)).Success)
                    filters = filters.Add(new FlareTestFilter(FlareTestFilterKind.IfSet, m.Groups[1].Value));
                else if ((m = _ifUnset.Match(directive)).Success)
                    filters = filters.Add(new FlareTestFilter(FlareTestFilterKind.IfUnset, m.Groups[1].Value));
                else if ((m = _env.Match(directive)).Success)
                    variables = variables.SetItem(m.Groups[1].Value, m.Groups[2].Value);
           }
        }

        static string TryReadFile(string path)
        {
            try
            {
                return File.ReadAllText(path).Trim();
            }
            catch (FileNotFoundException)
            {
                return string.Empty;
            }
        }
    }
}
