namespace Flare.Cli.Commands
{
    sealed class RootCommand : System.CommandLine.RootCommand
    {
        public RootCommand()
        {
            AddCommand(new CheckCommand());
            AddCommand(new CleanCommand());
            AddCommand(new DebugCommand());
            AddCommand(new DocCommand());
            AddCommand(new FormatCommand());
            AddCommand(new InstallCommand());
            AddCommand(new NewCommand());
            AddCommand(new ReplCommand());
            AddCommand(new RestoreCommand());
            AddCommand(new RunCommand());
            AddCommand(new SearchCommand());
            AddCommand(new TestCommand());
            AddCommand(new UninstallCommand());
        }
    }
}
