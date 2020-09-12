namespace Flare.Benchmarks
{
    sealed class BenchmarkOptions
    {
        public string? Filter { get; private set; }

        public bool Test { get; private set; }

        public bool Export { get; private set; }
    }
}
