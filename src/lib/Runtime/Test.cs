using Flare.Syntax;
using Flare.Tree;

namespace Flare.Runtime
{
    public sealed class Test : Declaration
    {
        internal TreeContext Tree { get; }

        internal Test(Module module, TestDeclarationNode node)
            : base(module, node)
        {
            Tree = TreeContext.CreateTest(this);
        }
    }
}
