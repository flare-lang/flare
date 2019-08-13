using System.Collections.Immutable;
using Flare.Syntax;

namespace Flare.Runtime
{
    public sealed class Module : Metadata
    {
        public ModulePath Path { get; }

        public bool HasDeclarations => !Declarations.IsEmpty;

        public ImmutableArray<Declaration> Declarations { get; }

        internal Module(ModulePath path, ProgramNode node)
            : base(node.Attributes)
        {
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
                    MacroDeclarationNode m => new Macro(this, m),
                    TestDeclarationNode t => new Test(this, t),
                    _ => throw DebugAssert.Unreachable(),
                });
            }

            Declarations = decls;
        }
    }
}
