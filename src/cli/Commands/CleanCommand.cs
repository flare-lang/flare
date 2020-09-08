namespace Flare.Cli.Commands
{
    sealed class CleanCommand : BaseCommand
    {
        sealed class CleanOptions
        {
            public bool Deps { get; set; }
        }

        public CleanCommand()
            : base("clean", "Clean build artifacts of a project.")
        {
            AddOption<bool>("-d", "--deps", "Clean locally restored dependencies.");

            RegisterHandler<CleanOptions>(Run);
        }

        int Run(CleanOptions options)
        {
            var project = Project.Instance;

            if (project == null)
            {
                Log.ErrorLine("No '{0}' file found in the current directory.", Project.ProjectFileName);
                return 1;
            }

            if (project.BuildDirectory.Exists)
                project.BuildDirectory.Delete();

            if (options.Deps && project.DependencyDirectory.Exists)
                project.DependencyDirectory.Delete();

            return 0;
        }
    }
}
