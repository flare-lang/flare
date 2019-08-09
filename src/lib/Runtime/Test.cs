using Flare.Syntax;

namespace Flare.Runtime
{
    public sealed class Test : Declaration
    {
        internal Test(Module module, TestDeclarationNode node)
            : base(module, node)
        {
        }
    }
}
