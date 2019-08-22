using System.Collections.Immutable;
using Flare.Syntax;

namespace Flare.Metadata
{
    public sealed class Module : Metadata
    {
        public ModuleLoader Loader { get; }

        public ModulePath Path { get; }

        public bool HasDeclarations => !Declarations.IsEmpty;

        public ImmutableArray<Declaration> Declarations { get; }

        internal Module(ModuleLoader loader, ModulePath path, ProgramNode node)
            : base(node.Attributes)
        {
            Loader = loader;
            Path = path;

            var decls = ImmutableArray<Declaration>.Empty;

            foreach (var decl in node.Declarations)
            {
                if (decl is UseDeclarationNode)
                    continue;

                decls = decls.Add(decl switch
                {
                    ConstantDeclarationNode c => (Declaration)new Constant(this, c),
                    FunctionDeclarationNode f => new Function(this, f),
                    ExternalDeclarationNode e => new External(this, e),
                    TestDeclarationNode t => new Test(this, t),
                    _ => throw DebugAssert.Unreachable(),
                });
            }

            Declarations = decls;
        }

        public override string ToString()
        {
            return Path.ToString();
        }
    }
}
