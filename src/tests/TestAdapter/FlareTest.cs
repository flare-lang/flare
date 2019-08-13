using System;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Flare.Tests.TestAdapter
{
    abstract class FlareTest
    {
        static readonly string _source = Assembly.GetExecutingAssembly().Location;

        public string Category { get; }

        public string Name { get; }

        public string FullName => $"{Category}/{Name}";

        public abstract string FullPath { get; }

        public abstract int LineNumber { get; }

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
