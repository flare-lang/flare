using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Flare.Cli;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Flare.Tests.TestAdapter
{
    abstract class FlareTest
    {
        static readonly string _source = Assembly.GetExecutingAssembly().Location;

        static readonly Regex _paths = new Regex("^(.*)(.fl\\(\\d+,\\d+\\): .*: .*)",
            RegexOptions.Compiled | RegexOptions.Multiline);

        public static FileInfo Executable { get; }

        public string Category { get; }

        public string Name { get; }

        public string FullName => $"{Category}/{Name}";

        public abstract string FullPath { get; }

        public abstract int LineNumber { get; }

        public ImmutableArray<FlareTestFilter> Filters { get; }

        public ImmutableDictionary<string, string> Variables { get; }

        public bool ShouldSucceed { get; }

        public string StandardOut { get; }

        public string StandardError { get; }

        static FlareTest()
        {
#if DEBUG
            const string cfg = "Debug";
#else
            const string cfg = "Release";
#endif

            // We need to use the executable in src/cli rather than src/tests.
            Executable = new FileInfo(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
                "..", "..", "..", "cli", "bin", cfg, Path.GetFileName(typeof(Program).Assembly.Location)!));
        }

        protected FlareTest(string category, string name, ImmutableArray<FlareTestFilter> filters,
            ImmutableDictionary<string, string> variables, bool succeed, string stdout, string stderr)
        {
            Category = category;
            Name = name;
            Filters = filters;
            Variables = variables;
            ShouldSucceed = succeed;
            StandardOut = stdout;
            StandardError = stderr;
        }

        public TestCase Convert()
        {
            return new TestCase(FullName, new Uri(FlareTestExecutor.ExecutorUri), _source)
            {
                CodeFilePath = FullPath,
                LineNumber = LineNumber,
            };
        }

        protected abstract void GetArguments(out string arguments, out string directory);

        public FlareTestResult Run()
        {
            var syms = FlareTestEnvironment.Symbols;

            foreach (var filter in Filters)
            {
                var ok = filter.Kind switch
                {
                    FlareTestFilterKind.IfSet => syms.Contains(filter.Value),
                    FlareTestFilterKind.IfUnset => !syms.Contains(filter.Value),
                    _ => throw DebugAssert.Unreachable(),
                };

                if (!ok)
                    return new FlareTestResult();
            }

            var vars = new Dictionary<string, string>();

            foreach (var elem in Environment.GetEnvironmentVariables())
            {
                var (key, value) = (DictionaryEntry)elem!;

                vars.Add((string)key, (string)value!);
            }

            // Note that we might need to overwrite existing variables here.
            foreach (var (key, value) in Variables)
                vars[key] = value;

            var stdout = new StringBuilder();
            var stderr = new StringBuilder();

            static void Write(StringBuilder builder, string str)
            {
                lock (builder)
                    _ = builder.AppendLine(str);
            }

            GetArguments(out var args, out var dir);

            var outcome = Process.ExecuteAsync("dotnet", $"{Executable.FullName} {args}",
                dir, s => Write(stdout, s), s => Write(stderr, s),
                vars.Select(kvp => (kvp.Key, kvp.Value)).ToArray()).Result == 0 ? FlareTestOutcome.Passed :
                FlareTestOutcome.Failed;
            var error = (string?)null;

            if (outcome == FlareTestOutcome.Passed && !ShouldSucceed)
            {
                error = "Test passed and was expected to fail.";
                outcome = FlareTestOutcome.Failed;
            }
            else if (outcome == FlareTestOutcome.Failed && ShouldSucceed)
            {
                error = "Test failed and was expected to pass.";
                outcome = FlareTestOutcome.Failed;
            }

            static string Normalize(StringBuilder output)
            {
                return _paths.Replace(output.ToString().Trim().Replace("\r\n", "\n"),
                    m => m.Groups[1].Value.Replace("\\", "/") + m.Groups[2].Value);
            }

            var stdout2 = Normalize(stdout);
            var stderr2 = Normalize(stderr);

            if (stdout2 != StandardOut)
            {
                error = "Standard output text did not match the expected text.";
                outcome = FlareTestOutcome.Failed;
            }

            if (stderr2 != StandardError)
            {
                error = "Standard error text did not match the expected text.";
                outcome = FlareTestOutcome.Failed;
            }

            return new FlareTestResult(outcome, error, stdout2, stderr2);
        }
    }
}
