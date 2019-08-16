using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Text;

namespace Flare.Tests.TestAdapter
{
    sealed class FlareRunTest : FlareTest
    {
        public DirectoryInfo Directory { get; }

        public ImmutableArray<FlareTestFilter> Filters { get; }

        public ImmutableDictionary<string, string> Variables { get; }

        public string Arguments { get; }

        public bool ShouldSucceed { get; }

        public string? StandardOut { get; }

        public string? StandardError { get; }

        public override string FullPath => Directory.FullName;

        public override int LineNumber => 1;

        public FlareRunTest(string name, DirectoryInfo directory, ImmutableArray<FlareTestFilter> filters,
            ImmutableDictionary<string, string> variables, string arguments, bool succeed, string? stdout,
            string? stderr)
            : base("run", name)
        {
            Directory = directory;
            Filters = filters;
            Variables = variables;
            Arguments = arguments;
            ShouldSucceed = succeed;
            StandardOut = stdout;
            StandardError = stderr;
        }

        public override FlareTestResult Run()
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

            var outcome = Process.ExecuteAsync("dotnet", $"{Executable.FullName} run {Arguments}",
                Directory.FullName, s => Write(stdout, s), s => Write(stderr, s),
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

            var stdout2 = stdout.ToString().Trim();
            var stderr2 = stderr.ToString().Trim();

            if (StandardOut != null && stdout2 != StandardOut)
            {
                error = "Standard output text did not match the expected text.";
                outcome = FlareTestOutcome.Failed;
            }

            if (StandardError != null && stderr2 != StandardError)
            {
                error = "Standard error text did not match the expected text.";
                outcome = FlareTestOutcome.Failed;
            }

            return new FlareTestResult(outcome, error, stdout2, stderr2);
        }
    }
}
