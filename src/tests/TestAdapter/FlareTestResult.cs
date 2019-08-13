namespace Flare.Tests.TestAdapter
{
    sealed class FlareTestResult
    {
        public FlareTestOutcome Outcome { get; }

        public string? Error { get; }

        public string StandardOut { get; }

        public string StandardError { get; }

        public FlareTestResult()
            : this(FlareTestOutcome.Skipped, string.Empty, string.Empty, string.Empty)
        {
        }

        public FlareTestResult(FlareTestOutcome outcome, string? error, string stdout, string stderr)
        {
            Outcome = outcome;
            Error = error;
            StandardOut = stdout;
            StandardError = stderr;
        }
    }
}
