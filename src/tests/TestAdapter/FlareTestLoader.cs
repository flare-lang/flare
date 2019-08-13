using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Flare.Tests.TestAdapter
{
    static class FlareTestLoader
    {
        const RegexOptions Options = RegexOptions.Compiled | RegexOptions.Singleline;

        public static ImmutableDictionary<string, FlareTest> Tests { get; }

        static readonly Regex _ifSet = new Regex("^'test:if-set (.*)$", Options);

        static readonly Regex _ifUnset = new Regex("^'test:if-unset (.*)$", Options);

        static readonly Regex _env = new Regex("^'test:env (.*)=(.*)$", Options);

        static readonly Regex _args = new Regex("^'test:args (.*)$", Options);

        static FlareTestLoader()
        {
            var tests = new Dictionary<string, FlareTest>();
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "..", "..");

            foreach (var test in LoadCliTests(path))
                tests.Add(test.FullName, test);

            Tests = tests.ToImmutableDictionary();
        }

        static IEnumerable<FlareCliTest> LoadCliTests(string path)
        {
            var cli = Path.Combine(path, "cli");

            foreach (var file in new DirectoryInfo(cli).EnumerateFiles("*.fl"))
            {
                var name = Path.GetFileNameWithoutExtension(file.FullName)!;

                ReadHeader(file, out var filters, out var vars, out var args, out var succeed);

                var stdout = TryReadFile(Path.ChangeExtension(file.FullName, "out")!);
                var stderr = TryReadFile(Path.ChangeExtension(file.FullName, "err")!);

                yield return new FlareCliTest(name, file, filters, vars, args, succeed, stdout, stderr);
            }
        }

        static void ReadHeader(FileInfo file, out ImmutableArray<FlareTestFilter> filters,
            out ImmutableDictionary<string, string> variables, out string arguments, out bool succeed)
        {
            filters = ImmutableArray<FlareTestFilter>.Empty;
            variables = ImmutableDictionary<string, string>.Empty;
            arguments = string.Empty;
            succeed = true;

            foreach (var line in File.ReadAllLines(file.FullName))
            {
                if (!line.StartsWith('\''))
                    break;

                Match m;

                if (line == "'test:pass")
                    succeed = true;
                else if (line == "'test:fail")
                    succeed = false;
                else if ((m = _args.Match(line)).Success)
                    arguments += m.Groups[1].Value;
                else if ((m = _ifSet.Match(line)).Success)
                    filters = filters.Add(new FlareTestFilter(FlareTestFilterKind.IfSet, m.Groups[1].Value));
                else if ((m = _ifUnset.Match(line)).Success)
                    filters = filters.Add(new FlareTestFilter(FlareTestFilterKind.IfUnset, m.Groups[1].Value));
                else if ((m = _env.Match(line)).Success)
                    variables = variables.SetItem(m.Groups[1].Value, m.Groups[2].Value);
           }
        }

        static string? TryReadFile(string path)
        {
            try
            {
                return File.ReadAllText(path).Trim();
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }
    }
}
