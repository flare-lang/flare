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
                if (!(decl is NamedDeclarationNode))
                    continue;

                decls = decls.Add(decl switch
                {
                    ConstantDeclarationNode c => (Declaration)new Constant(this, c),
                    FunctionDeclarationNode f => new Function(this, f),
                    ExternalDeclarationNode e => new External(this, e),
                    MacroDeclarationNode m => new Macro(this, m),
                    _ => throw Assert.Unreachable(),
                });
            }

            Declarations = decls;
        }
    }
}
