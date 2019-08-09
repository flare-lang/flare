using System.CommandLine.Invocation;

namespace Flare.Cli
{
    sealed class ParseErrorResult : IInvocationResult
    {
        public void Apply(InvocationContext context)
        {
            foreach (var err in context.ParseResult.Errors)
                Log.ErrorLine(err.Message);

            context.ResultCode = 1;
        }
    }
}
