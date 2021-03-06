using System.Collections.Immutable;
using Flare.Syntax;
using Flare.Tree;

namespace Flare.Metadata
{
    public sealed class Function : Declaration
    {
        public bool HasParameters => !Parameters.IsEmpty;

        public ImmutableArray<Parameter> Parameters { get; }

        internal TreeContext Tree { get; }

        internal Function(Module module, FunctionDeclarationNode node)
            : base(module, node)
        {
            var parms = ImmutableArray<Parameter>.Empty;
            var i = 0;

            foreach (var param in node.ParameterList.Parameters.Nodes)
            {
                parms = parms.Add(new Parameter(this, param.Attributes, param.NameToken.Text, i,
                    param.DotDotToken != null));

                i++;
            }

            Parameters = parms;
            Tree = TreeContext.CreateFunction(this);
        }
    }
}
