using Flare.Metadata;

namespace Flare.Tree
{
    sealed class TreeVariadicParameter : TreeVariable
    {
        public Parameter Parameter { get; }

        public new string Name => base.Name!;

        public TreeVariadicParameter(Parameter parameter)
            : base(TreeType.Array, parameter.Name, false)
        {
            Parameter = parameter;
        }
    }
}
