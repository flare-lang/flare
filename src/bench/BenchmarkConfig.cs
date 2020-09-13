using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Validators;

namespace Flare.Benchmarks
{
    sealed class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig(BenchmarkOptions options)
        {
            var job = Job.InProcess;

            if (options.Test)
                job = job.WithStrategy(RunStrategy.ColdStart).WithIterationCount(1);
            else
                _ = AddValidator(JitOptimizationsValidator.FailOnError);

            if (options.Filter is string filter)
                _ = AddFilter(new GlobFilter(new[] { filter }));

            if (options.Export)
                _ = AddExporter(new BenchmarkEnvironmentExporter(), new BenchmarkReportExporter());

            _ = WithOptions(ConfigOptions.JoinSummary | ConfigOptions.StopOnFirstError)
                .WithSummaryStyle(new BenchmarkSummaryStyle(true))
                .AddAnalyser(
                    EnvironmentAnalyser.Default,
                    MinIterationTimeAnalyser.Default,
                    OutliersAnalyser.Default,
                    RuntimeErrorAnalyser.Default,
                    ZeroMeasurementAnalyser.Default)
                .AddValidator(
                    BaselineValidator.FailOnError,
                    CompilationValidator.FailOnError,
                    ConfigCompatibilityValidator.FailOnError,
                    DeferredExecutionValidator.FailOnError,
                    DiagnosersValidator.Composite,
                    ExecutionValidator.FailOnError,
                    GenericBenchmarksValidator.DontFailOnError,
                    ParamsAllValuesValidator.FailOnError,
                    ReturnValueValidator.FailOnError,
                    RunModeValidator.FailOnError,
                    SetupCleanupValidator.FailOnError,
                    ShadowCopyValidator.DontFailOnError)
                .AddColumn(
                    TargetMethodColumn.Method,
                    StatisticColumn.Mean,
                    StatisticColumn.StdErr,
                    StatisticColumn.StdDev,
                    StatisticColumn.Min,
                    StatisticColumn.Median,
                    StatisticColumn.Max,
                    StatisticColumn.Iterations,
                    StatisticColumn.OperationsPerSecond)
                .AddLogger(ConsoleLogger.Unicode)
                .AddJob(job);
        }
    }
}
