using Flare.Syntax;

namespace Flare.Runtime
{
    public sealed class Constant : Declaration
    {
        internal Constant(Module module, ConstantDeclarationNode node)
            : base(module, node)
        {
        }
    }
}
