using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Adapter;

namespace Flare.Tests.TestAdapter
{
    [ExtensionUri(ExecutorUri)]
    public sealed class FlareTestExecutor : ITestExecutor
    {
        public const string ExecutorUri = "executor://" + nameof(FlareTestExecutor);

        readonly CancellationTokenSource _cts = new CancellationTokenSource();

        public void Cancel()
        {
            _cts.Cancel();
        }

        public void RunTests(IEnumerable<TestCase> tests, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            // TODO: Do we need to process filters in runContext?
            RunTests(tests.Select(x => (FlareTestLoader.Tests[x.FullyQualifiedName], x)).ToArray(), frameworkHandle);
        }

        public void RunTests(IEnumerable<string> sources, IRunContext runContext, IFrameworkHandle frameworkHandle)
        {
            RunTests(FlareTestLoader.Tests.Values.Select(x => (x, x.Convert())).ToArray(), frameworkHandle);
        }

        void RunTests((FlareTest, TestCase)[] tests, IFrameworkHandle frameworkHandle)
        {
            Task.Run(() =>
            {
                foreach (var (tc, vstc) in tests)
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    frameworkHandle.RecordStart(vstc);

                    var start = DateTimeOffset.Now;
                    var tr = tc.Run();
                    var end = DateTimeOffset.Now;
                    var outcome = tr.Outcome switch
                    {
                        FlareTestOutcome.Passed => TestOutcome.Passed,
                        FlareTestOutcome.Failed => TestOutcome.Failed,
                        FlareTestOutcome.Skipped => TestOutcome.Skipped,
                        _ => throw DebugAssert.Unreachable(),
                    };

                    frameworkHandle.RecordEnd(vstc, outcome);

                    var span = end - start;

                    // Work around a VS bug.
                    if (span.TotalMilliseconds == 0.0)
                        span = TimeSpan.FromMilliseconds(1.0);

                    var vstr = new TestResult(vstc)
                    {
                        ComputerName = Environment.MachineName,
                        Duration = span,
                        EndTime = end,
                        ErrorMessage = tr.Error,
                        Outcome = outcome,
                        StartTime = start,
                    };

                    vstr.Messages.Add(new TestResultMessage(TestResultMessage.StandardOutCategory, tr.StandardOut));
                    vstr.Messages.Add(new TestResultMessage(TestResultMessage.StandardErrorCategory, tr.StandardError));

                    frameworkHandle.RecordResult(vstr);
                }
            }, _cts.Token).Wait();
        }
    }
}
