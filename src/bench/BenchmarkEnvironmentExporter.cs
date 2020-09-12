using System.Text.Json;
using System.Text.Json.Serialization;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;

namespace Flare.Benchmarks
{
    sealed class BenchmarkEnvironmentExporter : ExporterBase
    {
        protected override string FileExtension => "json";

        protected override string FileCaption => "environment";

        public override void ExportToLog(Summary summary, ILogger logger)
        {
            var host = summary.HostEnvironmentInfo;
            var cpu = host.CpuInfo.Value;

            logger.WriteLine(JsonSerializer.Serialize(new
            {
                Hardware = new
                {
                    Architecture = host.Architecture,
                    ProcessorName = cpu.ProcessorName,
                    PhysicalProcessorCount = cpu.PhysicalProcessorCount,
                    PhysicalCoreCount = cpu.PhysicalCoreCount,
                    LogicalCoreCount = cpu.LogicalCoreCount,
                    HardwareTimerKind = host.HardwareTimerKind.ToString(),
                    ChronometerResolution = host.ChronometerResolution.Nanoseconds,
                    ChronometerFrequency = host.ChronometerFrequency.Hertz,
                },
                OperatingSystem = new
                {
                    Version = host.OsVersion.Value,
                    Hypervisor = host.VirtualMachineHypervisor.Value?.Name ?? "None",
                },
                Build = new
                {
                    DotNetSdkVersion = host.DotNetSdkVersion.Value,
                    BenchmarkDotNetVersion = host.BenchmarkDotNetVersion,
                    Configuration = host.Configuration,
                },
                Runtime = new
                {
                    Version = host.RuntimeVersion,
                    IsServerGC = host.IsServerGC,
                    IsConcurrentGC = host.IsConcurrentGC,
                    GCAllocationQuantum = host.GCAllocationQuantum,
                },
            }, new JsonSerializerOptions
            {
                NumberHandling = JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowNamedFloatingPointLiterals,
                WriteIndented = true,
            }));
        }
    }
}
