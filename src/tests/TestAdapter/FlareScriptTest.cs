using System.Collections.Immutable;
using System.IO;

namespace Flare.Tests.TestAdapter
{
    sealed class FlareScriptTest : FlareTest
    {
        public FileInfo File { get; }

        public string Arguments { get; }

        public override string FullPath => File.FullName;

        public override int LineNumber => 1;

        public FlareScriptTest(string name, ImmutableArray<FlareTestFilter> filters,
            ImmutableDictionary<string, string> variables, bool succeed, string stdout, string stderr, FileInfo file,
            string arguments)
            : base("script", name, filters, variables, succeed, stdout, stderr)
        {
            File = file;
            Arguments = arguments;
        }

        protected override void GetArguments(out string arguments, out string directory)
        {
            arguments = $"{File.Name} {Arguments}";
            directory = File.DirectoryName;
        }
    }
}
