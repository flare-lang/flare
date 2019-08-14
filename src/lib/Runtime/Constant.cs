using Flare.Syntax;
using Flare.Tree;

namespace Flare.Runtime
{
    public sealed class Constant : Declaration
    {
        internal TreeContext Tree { get; }

        internal Constant(Module module, ConstantDeclarationNode node)
            : base(module, node)
        {
            Tree = TreeContext.CreateConstant(this);
        }
    }
}
