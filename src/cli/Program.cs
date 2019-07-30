using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Flare.Cli.Commands;

namespace Flare.Cli
{
    static class Program
    {
        static async Task<int> Main(string[] args)
        {
            return await new RootCommand
            {
                new BuildCommand(),
                new CheckCommand(),
                new CleanCommand(),
                new DebugCommand(),
                new DocCommand(),
                new FormatCommand(),
                new InstallCommand(),
                new NewCommand(),
                new ReplCommand(),
                new RestoreCommand(),
                new RunCommand(),
                new SearchCommand(),
                new TestCommand(),
                new UninstallCommand(),
            }.InvokeAsync(args);
        }
    }
}
