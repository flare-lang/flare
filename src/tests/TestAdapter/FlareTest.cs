using System;
using System.IO;
using System.Reflection;
using Flare.Cli;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Flare.Tests.TestAdapter
{
    abstract class FlareTest
    {
        static readonly string _source = Assembly.GetExecutingAssembly().Location;

        public static FileInfo Executable { get; }

        public string Category { get; }

        public string Name { get; }

        public string FullName => $"{Category}/{Name}";

        public abstract string FullPath { get; }

        public abstract int LineNumber { get; }

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

        protected FlareTest(string category, string name)
        {
            Category = category;
            Name = name;
        }

        public TestCase Convert()
        {
            return new TestCase(FullName, new Uri(FlareTestRunner.ExecutorUri), _source)
            {
                CodeFilePath = FullPath,
                LineNumber = LineNumber,
            };
        }

        public abstract FlareTestResult Run();
    }
}
