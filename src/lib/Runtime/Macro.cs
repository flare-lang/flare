using System.Collections.Immutable;
using Flare.Syntax;

namespace Flare.Runtime
{
    public sealed class Macro : Declaration
    {
        public bool HasParameters => !Parameters.IsEmpty;

        public ImmutableArray<Parameter> Parameters { get; }

        internal Macro(Module module, MacroDeclarationNode node)
            : base(module, node)
        {
            var parms = ImmutableArray<Parameter>.Empty;
            var i = 0;

            foreach (var param in node.ParameterList.Parameters.Nodes)
            {
                parms = parms.Add(new Parameter(this, param.Attributes, param.NameToken.Text, i, false));

                i++;
            }

            Parameters = parms;
        }
    }
}
