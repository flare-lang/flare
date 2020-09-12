using System;
using System.CommandLine.Invocation;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace Flare.Benchmarks
{
    sealed class BenchmarkReportExporter : ExporterBase
    {
        sealed class ExportSummaryStyle : SummaryStyle
        {
            public ExportSummaryStyle()
                : base(CultureInfo.InvariantCulture, false, null, null, false)
            {
            }
        }

        protected override string FileExtension => "json";

        protected override string FileCaption => "report";

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            var loc = Path.Combine(Assembly.GetExecutingAssembly().Location, "..", "..");

            logger.WriteLine(JsonSerializer.Serialize(new
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                Commit = new
                {
                    Hash = GitShow(loc, "H"),
                    Subject = GitShow(loc, "s"),
                    Author = new
                    {
                        Timestamp = GitShow(loc, "at"),
                        Name = GitShow(loc, "aN"),
                        MailAddress = GitShow(loc, "aE"),
                    },
                    Committer = new
                    {
                        Timestamp = GitShow(loc, "ct"),
                        Name = GitShow(loc, "cN"),
                        MailAddress = GitShow(loc, "cE"),
                    },
                },
                Reports = summary.Reports.Select(r =>
                {
                    var bench = r.BenchmarkCase;
                    var stats = r.ResultStatistics;
                    var style = new ExportSummaryStyle();

                    return new
                    {
                        Name = bench.Descriptor.WorkloadMethod.Name,
                        Success = r.Success,
                        Statistics = !r.Success ? null : new
                        {
                            Mean = StatisticColumn.Mean.GetValue(summary, bench, style),
                            StdErr = StatisticColumn.StdErr.GetValue(summary, bench, style),
                            StdDev = StatisticColumn.StdDev.GetValue(summary, bench, style),
                            Min = StatisticColumn.Min.GetValue(summary, bench, style),
                            Median = StatisticColumn.Median.GetValue(summary, bench, style),
                            Max = StatisticColumn.Max.GetValue(summary, bench, style),
                            Iterations = StatisticColumn.Iterations.GetValue(summary, bench, style),
                            OperationsPerSecond = StatisticColumn.OperationsPerSecond.GetValue(summary, bench, style),
                        },
                    };
                }),
            }, new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
            }));
        }

        static string GitShow(string directory, string format)
        {
            var result = new StringBuilder();

            void Write(string str)
            {
                lock (result)
                    _ = result.AppendLine(str);
            }

            var code = Process.ExecuteAsync("git", $"show -s --format=%{format}", directory, Write, Write)
                .GetAwaiter().GetResult();

            if (code != 0)
                lock (result)
                    throw new Exception(
                        $"Could not retrieve Git commit information ({code}): {result.ToString().Trim()}");

            lock (result)
                return result.ToString().Trim();
        }
    }
}
