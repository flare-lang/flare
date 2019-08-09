using Flare.Runtime;

namespace Flare.Tree
{
    sealed class TreeFragment : TreeParameter
    {
        public new string Name => base.Name!;

        public TreeFragment(Parameter parameter)
            : base(parameter)
        {
        }
    }
}
