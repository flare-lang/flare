using System.Collections.Immutable;

namespace Flare.Tree.HighLevel.Patterns
{
    sealed class TreeModulePattern : TreePattern
    {
        public ImmutableArray<string> Path { get; }

        public TreeModulePattern(TreeLocal? alias, ImmutableArray<string> path)
            : base(alias)
        {
            Path = path;
        }
    }
}
