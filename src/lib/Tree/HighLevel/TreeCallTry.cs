using System.Collections.Immutable;

namespace Flare.Tree.HighLevel
{
    sealed class TreeCallTry
    {
        public ImmutableArray<TreePatternArm> Arms { get; }

        public TreeCallTry(ImmutableArray<TreePatternArm> arms)
        {
            Arms = arms;
        }
    }
}
