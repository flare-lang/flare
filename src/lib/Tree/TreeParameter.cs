using Flare.Runtime;

namespace Flare.Tree
{
    class TreeParameter : TreeVariable
    {
        public Parameter Parameter { get; }

        public new string Name => base.Name!;

        public TreeParameter(Parameter parameter)
            : base(TreeType.Any, parameter.Name, false)
        {
            Parameter = parameter;
        }
    }
}
