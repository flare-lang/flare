using System.Globalization;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;

namespace Flare.Benchmarks
{
    sealed class BenchmarkSummaryStyle : SummaryStyle
    {
        public BenchmarkSummaryStyle(bool printUnitsInContent)
            : base(CultureInfo.InvariantCulture, false, null, TimeUnit.Microsecond, printUnitsInContent)
        {
        }
    }
}
