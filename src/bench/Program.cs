using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;

namespace Flare.Benchmarks
{
    static class Program
    {
        static Task<int> Main(string[] args)
        {
            var root = new RootCommand()
            {
                new Argument<string?>("filter", "Benchmark filter pattern.")
                {
                    Arity = ArgumentArity.ZeroOrOne,
                },
                new Option<bool>(new[] { "-t", "--test" }, "Run in test mode."),
                new Option<bool>(new[] { "-e", "--export" }, "Export JSON data."),
            };

            root.Handler = CommandHandler.Create<BenchmarkOptions>(opts =>
                _ = BenchmarkSwitcher.FromAssembly(Assembly.GetExecutingAssembly()).RunAll(new BenchmarkConfig(opts)));

            return root.InvokeAsync(args);
        }
    }
}
