using System.Collections.Immutable;
using System.IO;
using Flare.Cli;

namespace Flare.Tests.TestAdapter
{
    sealed class FlareRunTest : FlareTest
    {
        public DirectoryInfo Directory { get; }

        public string Arguments { get; }

        public override string FullPath { get; }

        public override int LineNumber => 1;

        public FlareRunTest(string name, ImmutableArray<FlareTestFilter> filters,
            ImmutableDictionary<string, string> variables, bool succeed, string stdout, string stderr,
            DirectoryInfo directory, string arguments)
            : base("run", name, filters, variables, succeed, stdout, stderr)
        {
            Directory = directory;
            Arguments = arguments;
            FullPath = Path.Combine(Directory.FullName, Project.ProjectFileName);
        }

        protected override void GetArguments(out string arguments, out string directory)
        {
            arguments = $"run {Arguments}";
            directory = Directory.FullName;
        }
    }
}
