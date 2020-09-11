namespace Flare.Benchmarks
{
    sealed class BenchmarkOptions
    {
        public string? Filter { get; private set; }

        public bool Test { get; private set; }

        public bool Json { get; private set; }

        public bool Xml { get; private set; }

        public bool Plots { get; private set; }
    }
}
