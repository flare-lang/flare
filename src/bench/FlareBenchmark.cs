using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Flare.Metadata;

namespace Flare.Benchmarks
{
    [SuppressMessage("Microsoft.Performance", "CA1822", Justification = "Required for benchmarks.")]
    public class FlareBenchmark
    {
        [Benchmark]
        public void LoadCoreModules()
        {
            _ = new StandardModuleLoader(ModuleLoaderMode.Reflection);
        }
    }
}
