using System.Collections.Generic;
using System.Collections.Immutable;
using Flare.Runtime;

namespace Flare.Syntax
{
    public static class LanguageAnalyzer
    {
        sealed class Analyzer
        {
            sealed class Walker : SyntaxWalker
            {
                sealed class FunctionContext
                {
                    public FunctionContext? Previous { get; }

                    readonly Stack<PrimaryExpressionNode> _loops = new Stack<PrimaryExpressionNode>();

                    public FunctionContext(FunctionContext? previous)
                    {
                        Previous = previous;
                    }

                    public void Push(PrimaryExpressionNode loop)
                    {
                        _loops.Push(loop);
                    }

                    public void Pop()
                    {
                        _ = _loops.Pop();
                    }

                    public PrimaryExpressionNode? Get()
                    {
                        return _loops.TryPeek(out var loop) ? loop : null;
                    }
                }

                class Scope
                {
                    public Scope? Previous { get; }

                    protected Dictionary<string, SyntaxSymbol> Symbols { get; } =
                        new Dictionary<string, SyntaxSymbol>();

                    public Scope(Scope? previous)
                    {
                        Previous = previous;
                    }

                    public SyntaxSymbol? Get(string name)
                    {
                        return Symbols.TryGetValue(name, out var sym) ? sym : Previous?.Get(name);
                    }

                    public virtual bool IsMutable(string name)
                    {
                        var sym = Get(name);

                        return sym != null ? sym.Kind == SyntaxSymbolKind.Mutable : Previous?.IsMutable(name) ?? false;
                    }

                    public void Define(SyntaxSymbolKind kind, ModulePath? module, string name)
                    {
                        // This method is used to shadow variables, too.
                        Symbols[name] = new SyntaxSymbol(kind, module, name);
                    }
                }

                sealed class LambdaScope : Scope
                {
                    public LambdaScope(Scope previous)
                        : base(previous)
                    {
                    }

                    public override bool IsMutable(string name)
                    {
                        // Lambdas can't mutate upvalues.
                        return Symbols.TryGetValue(name, out var sym) ? sym.Kind == SyntaxSymbolKind.Mutable : false;
                    }
                }

                public IReadOnlyList<SyntaxDiagnostic> Diagnostics => _diagnostics;

                readonly List<SyntaxDiagnostic> _diagnostics = new List<SyntaxDiagnostic>();

                readonly ModuleLoader _loader;

                readonly SyntaxContext _context;

                ModulePath? _path;

                readonly Dictionary<string, ModulePath> _aliases = new Dictionary<string, ModulePath>();

                FunctionContext? _function;

                Scope _scope = new Scope(null!);

                readonly HashSet<string> _fragments = new HashSet<string>();

                public Walker(ModuleLoader loader, SyntaxContext context)
                {
                    _loader = loader;
                    _context = context;
                }

                static ModulePath? CreatePath(ModulePathNode node)
                {
                    var comps = ImmutableArray<string>.Empty;

                    foreach (var comp in node.ComponentTokens.Tokens)
                        if (!comp.IsMissing)
                            comps = comps.Add(comp.Text);

                    return comps.IsEmpty ? null : new ModulePath(comps);
                }

                static ModulePath? CreateCorrectPath(ModulePathNode node)
                {
                    foreach (var sep in node.ComponentTokens.Separators)
                        if (sep.IsMissing)
                            return null;

                    var comps = ImmutableArray<string>.Empty;

                    foreach (var comp in node.ComponentTokens.Tokens)
                    {
                        if (comp.IsMissing)
                            return null;

                        comps = comps.Add(comp.Text);
                    }

                    return new ModulePath(comps);
                }

                Module? LoadModule(SyntaxNode node, SourceLocation location, ModulePath path)
                {
                    try
                    {
                        return _loader.LoadModule(path, _context);
                    }
                    catch (ModuleLoadException e)
                    {
                        Error(node, SyntaxDiagnosticKind.ModuleLoadFailed, location,
                            $"Module load failed: {e.Message}");

                        return null;
                    }
                }

                void PushFunction()
                {
                    _function = new FunctionContext(_function);
                }

                void PopFunction()
                {
                    _function = _function!.Previous;
                }

                void PushScope()
                {
                    _scope = new Scope(_scope);
                }

                void PushLambdaScope()
                {
                    _scope = new LambdaScope(_scope);
                }

                void PopScope()
                {
                    _scope = _scope.Previous!;
                }

                void Error(SyntaxNode node, SyntaxDiagnosticKind kind, SourceLocation location, string message)
                {
                    var diag = new SyntaxDiagnostic(kind, SyntaxDiagnosticSeverity.Error, location, message,
                        ImmutableArray<(SourceLocation, string)>.Empty);

                    node.AddDiagnostic(diag);
                    _diagnostics.Add(diag);
                }

                public override void Visit(ProgramNode node)
                {
                    _path = CreatePath(node.Path);

                    foreach (var decl in node.Declarations)
                    {
                        if (!(decl is UseDeclarationNode use))
                            continue;

                        var path = CreateCorrectPath(use.Path);

                        if (path == null)
                            continue;

                        if (use.Alias is UseDeclarationAliasNode alias && !alias.NameToken.IsMissing)
                        {
                            var name = alias.NameToken;
                            var duplicate = false;

                            foreach (var decl2 in node.Declarations)
                            {
                                if (decl2 is UseDeclarationNode use2 && use2.Alias is UseDeclarationAliasNode alias2 &&
                                    alias2 != alias && alias2.NameToken.Text == name.Text)
                                {
                                    duplicate = true;
                                    break;
                                }
                            }

                            if (!duplicate)
                                _aliases.Add(name.Text, path);
                            else
                                Error(alias, SyntaxDiagnosticKind.DuplicateUseDeclarationAlias, name.Location,
                                    $"Module alias '{name}' declared multiple times");

                            continue;
                        }

                        if (!(LoadModule(node, node.Path.ComponentTokens.Tokens[0].Location, path) is Module mod))
                            continue;

                        foreach (var mdecl in mod.Declarations)
                        {
                            if (!mdecl.IsPublic || mdecl is Macro)
                                continue;

                            var sym = mdecl switch
                            {
                                Constant _ => SyntaxSymbolKind.Constant,
                                Function _ => SyntaxSymbolKind.Function,
                                External _ => SyntaxSymbolKind.External,
                                _ => (SyntaxSymbolKind?)null,
                            };

                            if (sym is SyntaxSymbolKind kind)
                                _scope.Define(kind, path, mdecl.Name);
                        }
                    }

                    foreach (var decl in node.Declarations)
                    {
                        if (!(decl is NamedDeclarationNode named) || decl is MissingNamedDeclarationNode)
                            continue;

                        var name = named.NameToken;

                        if (name.IsMissing)
                            continue;

                        if (name.Text.StartsWith('_'))
                        {
                            Error(named, SyntaxDiagnosticKind.InvalidDeclarationName, name.Location,
                                $"Declaration '{name}' is invalid; declaration names cannot start with '_'");
                            continue;
                        }

                        var duplicate = false;

                        foreach (var decl2 in node.Declarations)
                        {
                            if (decl2 == decl || !(decl2 is NamedDeclarationNode named2) ||
                                decl2 is MissingNamedDeclarationNode)
                                continue;

                            var name2 = named2.NameToken;

                            if (name2.Text != name.Text)
                                continue;

                            duplicate = true;
                        }

                        if (duplicate)
                        {
                            Error(named, SyntaxDiagnosticKind.DuplicateDeclaration, name.Location,
                                $"Declaration name '{name}' declared multiple times");
                            continue;
                        }

                        var sym = decl switch
                        {
                            ConstantDeclarationNode _ => SyntaxSymbolKind.Constant,
                            FunctionDeclarationNode _ => SyntaxSymbolKind.Function,
                            ExternalDeclarationNode _ => SyntaxSymbolKind.External,
                            _ => (SyntaxSymbolKind?)null,
                        };

                        if (sym is SyntaxSymbolKind kind)
                            _scope.Define(kind, _path, name.Text);
                    }

                    foreach (var decl in node.Declarations)
                    {
                        if (!(decl is TestDeclarationNode test))
                            continue;

                        var name = test.NameToken;

                        if (name.Text.StartsWith('_'))
                        {
                            Error(test, SyntaxDiagnosticKind.InvalidDeclarationName, name.Location,
                                $"Test name '{name}' is invalid; test names cannot start with '_'");
                            continue;
                        }

                        var duplicate = false;

                        foreach (var decl2 in node.Declarations)
                        {
                            if (decl2 != decl && decl2 is TestDeclarationNode test2 &&
                                test2.NameToken.Text == name.Text)
                            {
                                duplicate = true;
                                break;
                            }
                        }

                        if (duplicate)
                        {
                            Error(test, SyntaxDiagnosticKind.DuplicateDeclaration, name.Location,
                                $"Test '{name}' declared multiple times");
                            continue;
                        }
                    }

                    base.Visit(node);
                }

                public override void Visit(ConstantDeclarationNode node)
                {
                    PushFunction();

                    base.Visit(node);

                    PopFunction();
                }

                public override void Visit(FunctionDeclarationNode node)
                {
                    PushFunction();
                    PushScope();

                    foreach (var param in node.ParameterList.Parameters.Nodes)
                    {
                        var name = param.NameToken;

                        if (name.IsMissing)
                            continue;

                        _scope.Define(SyntaxSymbolKind.Immutable, _path, name.Text);

                        var duplicate = false;

                        foreach (var param2 in node.ParameterList.Parameters.Nodes)
                        {
                            if (param2 != param && param2.NameToken.Text == name.Text)
                            {
                                duplicate = true;
                                break;
                            }
                        }

                        if (!duplicate)
                            continue;

                        Error(param, SyntaxDiagnosticKind.DuplicateParameter, name.Location,
                            $"Function parameter '{name}' (of '{node.NameToken}') declared multiple times");
                    }

                    base.Visit(node);

                    PopScope();
                    PopFunction();
                }

                public override void Visit(ExternalDeclarationNode node)
                {
                    foreach (var param in node.ParameterList.Parameters.Nodes)
                    {
                        var name = param.NameToken;

                        if (name.IsMissing)
                            continue;

                        var duplicate = false;

                        foreach (var param2 in node.ParameterList.Parameters.Nodes)
                        {
                            if (param2 != param && param2.NameToken.Text == name.Text)
                            {
                                duplicate = true;
                                break;
                            }
                        }

                        if (!duplicate)
                            continue;

                        Error(param, SyntaxDiagnosticKind.DuplicateParameter, name.Location,
                            $"Function parameter '{name}' (of '{node.NameToken}') declared multiple times");
                    }

                    base.Visit(node);
                }

                public override void Visit(MacroDeclarationNode node)
                {
                    PushFunction();

                    _fragments.Clear();

                    foreach (var param in node.ParameterList.Parameters.Nodes)
                    {
                        var name = param.NameToken;

                        if (name.IsMissing)
                            continue;

                        _ = _fragments.Add(name.Text);

                        var duplicate = false;

                        foreach (var param2 in node.ParameterList.Parameters.Nodes)
                        {
                            if (param2 != param && param2.NameToken.Text == name.Text)
                            {
                                duplicate = true;
                                break;
                            }
                        }

                        if (!duplicate)
                            continue;

                        Error(param, SyntaxDiagnosticKind.DuplicateParameter, name.Location,
                            $"Macro parameter '{name}' (of '{node.NameToken}') declared multiple times");
                    }

                    base.Visit(node);

                    PopFunction();
                }

                public override void Visit(LetStatementNode node)
                {
                    // We need to visit the initializer first so that it can't refer to variables
                    // introduced in the pattern.
                    Visit(node.Initializer);
                    Visit(node.Pattern);
                }

                public override void Visit(UseStatementNode node)
                {
                    // We need to visit the initializer first so that it can't refer to variables
                    // introduced in the pattern.
                    Visit(node.Initializer);
                    Visit(node.Pattern);
                }

                public override void Visit(AssignExpressionNode node)
                {
                    var left = node.LeftOperand;
                    var op = node.OperatorToken;

                    if (!op.IsMissing)
                    {
                        switch (left)
                        {
                            case IndexExpressionNode _:
                            case FieldAccessExpressionNode _:
                                break;
                            case IdentifierExpressionNode inode:
                                var ident = inode.IdentifierToken;

                                if (!ident.IsMissing && !_scope.IsMutable(ident.Text))
                                    Error(node, SyntaxDiagnosticKind.InvalidAssignmentTarget, op.Location,
                                        $"'{ident}' does not represent a mutable variable");
                                break;
                            default:
                                Error(node, SyntaxDiagnosticKind.InvalidAssignmentTarget, op.Location,
                                    "Assignment target must be an index, field access, or identifier expression");
                                break;
                        }
                    }

                    base.Visit(node);
                }

                public override void Visit(PatternArmNode node)
                {
                    PushScope();

                    base.Visit(node);

                    PopScope();
                }

                public override void Visit(IdentifierExpressionNode node)
                {
                    var ident = node.IdentifierToken;

                    if (!ident.IsMissing)
                    {
                        if (!ident.Text.StartsWith('_'))
                        {
                            var sym = _scope.Get(ident.Text);

                            if (sym != null)
                                node.SetAnnotation("Symbol", sym);
                            else
                                Error(node, SyntaxDiagnosticKind.UnknownValueName, ident.Location,
                                    $"Unknown declaration or value name '{ident}'");
                        }
                        else
                            Error(node, SyntaxDiagnosticKind.DiscardedVariableUsed, ident.Location,
                                $"Use of discarded variable name '{ident}'");
                    }

                    base.Visit(node);
                }

                public override void Visit(FragmentExpressionNode node)
                {
                    var ident = node.IdentifierToken;

                    if (!ident.IsMissing && !_fragments.Contains(ident.Text))
                        Error(node, SyntaxDiagnosticKind.UnknownValueName, ident.Location,
                            $"Unknown fragment name '{ident}'");

                    base.Visit(node);
                }

                public override void Visit(LambdaExpressionNode node)
                {
                    PushFunction();
                    PushLambdaScope();

                    foreach (var param in node.ParameterList.Parameters.Nodes)
                    {
                        var name = param.NameToken;

                        if (name.IsMissing)
                            continue;

                        _scope.Define(SyntaxSymbolKind.Immutable, _path, name.Text);

                        var duplicate = false;

                        foreach (var param2 in node.ParameterList.Parameters.Nodes)
                        {
                            if (param2 != param && param2.NameToken.Text == name.Text)
                            {
                                duplicate = true;
                                break;
                            }
                        }

                        if (!duplicate)
                            continue;

                        Error(param, SyntaxDiagnosticKind.DuplicateParameter, name.Location,
                            $"Lambda parameter '{name}' declared multiple times");
                    }

                    base.Visit(node);

                    PopScope();
                    PopFunction();
                }

                public override void Visit(ModuleExpressionNode node)
                {
                    // Only try to load the module if the entire path is syntactically correct.
                    var path = CreateCorrectPath(node.Path);

                    if (path != null)
                    {
                        if (path.Components.Length == 1 && _aliases.TryGetValue(path.Components[0], out var actual))
                            path = actual;

                        if (path == _path || LoadModule(node, node.Path.ComponentTokens.Tokens[0].Location, path) != null)
                            node.SetAnnotation("Path", path);
                    }

                    base.Visit(node);
                }

                public override void Visit(RecordExpressionNode node)
                {
                    foreach (var field in node.Fields.Nodes)
                    {
                        var name = field.NameToken;

                        if (name.IsMissing)
                            continue;

                        var duplicate = false;

                        foreach (var field2 in node.Fields.Nodes)
                        {
                            if (field2 != field && field2.NameToken.Text == name.Text)
                            {
                                duplicate = true;
                                break;
                            }
                        }

                        if (!duplicate)
                            continue;

                        Error(field, SyntaxDiagnosticKind.DuplicateExpressionField, name.Location,
                            $"Record field '{name}' is assigned multiple times");
                    }

                    base.Visit(node);
                }

                public override void Visit(ExceptionExpressionNode node)
                {
                    foreach (var field in node.Fields.Nodes)
                    {
                        var name = field.NameToken;

                        if (name.IsMissing)
                            continue;

                        var duplicate = false;

                        foreach (var field2 in node.Fields.Nodes)
                        {
                            if (field2 != field && field2.NameToken.Text == name.Text)
                            {
                                duplicate = true;
                                break;
                            }
                        }

                        if (!duplicate)
                            continue;

                        Error(field, SyntaxDiagnosticKind.DuplicateExpressionField, name.Location,
                            $"Exception field '{name}' is assigned multiple times");
                    }

                    base.Visit(node);
                }

                public override void Visit(BlockExpressionNode node)
                {
                    PushScope();

                    base.Visit(node);

                    PopScope();
                }

                public override void Visit(ConditionExpressionArmNode node)
                {
                    PushScope();

                    base.Visit(node);

                    PopScope();
                }

                public override void Visit(ForExpressionNode node)
                {
                    _function!.Push(node);

                    base.Visit(node);

                    _function!.Pop();
                }

                public override void Visit(WhileExpressionNode node)
                {
                    _function!.Push(node);

                    base.Visit(node);

                    _function!.Pop();
                }

                public override void Visit(LoopExpressionNode node)
                {
                    var kw = node.LoopKeywordToken;

                    if (!kw.IsMissing)
                    {
                        if (_function!.Get() is PrimaryExpressionNode loop)
                            node.SetAnnotation("Target", loop);
                        else
                            Error(node, SyntaxDiagnosticKind.InvalidLoopTarget, kw.Location,
                                "No enclosing 'while' or 'for' expression for this 'loop' expression");
                    }

                    base.Visit(node);
                }

                public override void Visit(BreakExpressionNode node)
                {
                    var kw = node.BreakKeywordToken;

                    if (!kw.IsMissing)
                    {
                        if (_function!.Get() is PrimaryExpressionNode loop)
                            node.SetAnnotation("Target", loop);
                        else
                            Error(node, SyntaxDiagnosticKind.InvalidLoopTarget, kw.Location,
                                "No enclosing 'while' or 'for' expression for this 'break' expression");
                    }

                    base.Visit(node);
                }

                public override void Visit(IdentifierPatternNode node)
                {
                    if (!node.IdentifierToken.IsMissing)
                        _scope.Define(node.MutKeywordToken != null ? SyntaxSymbolKind.Mutable :
                            SyntaxSymbolKind.Immutable, _path, node.IdentifierToken.Text);

                    base.Visit(node);

                    var alias = node.Alias?.NameToken;

                    if (alias != null && !alias.IsMissing)
                        _scope.Define(SyntaxSymbolKind.Immutable, _path, alias.Text);
                }

                public override void Visit(LiteralPatternNode node)
                {
                    base.Visit(node);

                    var alias = node.Alias?.NameToken;

                    if (alias != null && !alias.IsMissing)
                        _scope.Define(SyntaxSymbolKind.Immutable, _path, alias.Text);
                }

                public override void Visit(ModulePatternNode node)
                {
                    // Only try to load the module if the entire path is syntactically correct.
                    var path = CreateCorrectPath(node.Path);

                    if (path != null)
                    {
                        if (path.Components.Length == 1 && _aliases.TryGetValue(path.Components[0], out var actual))
                            path = actual;

                        if (path == _path || LoadModule(node, node.Path.ComponentTokens.Tokens[0].Location, path) != null)
                            node.SetAnnotation("Path", path);
                    }

                    base.Visit(node);

                    var alias = node.Alias?.NameToken;

                    if (alias != null && !alias.IsMissing)
                        _scope.Define(SyntaxSymbolKind.Immutable, _path, alias.Text);
                }

                public override void Visit(TuplePatternNode node)
                {
                    base.Visit(node);

                    var alias = node.Alias?.NameToken;

                    if (alias != null && !alias.IsMissing)
                        _scope.Define(SyntaxSymbolKind.Immutable, _path, alias.Text);
                }

                public override void Visit(RecordPatternNode node)
                {
                    foreach (var field in node.Fields.Nodes)
                    {
                        var name = field.NameToken;

                        if (name.IsMissing)
                            continue;

                        var duplicate = false;

                        foreach (var field2 in node.Fields.Nodes)
                        {
                            if (field2 != field && field2.NameToken.Text == name.Text)
                            {
                                duplicate = true;
                                break;
                            }
                        }

                        if (!duplicate)
                            continue;

                        Error(field, SyntaxDiagnosticKind.DuplicatePatternField, name.Location,
                            $"Record field '{name}' is matched multiple times");
                    }

                    base.Visit(node);

                    var alias = node.Alias?.NameToken;

                    if (alias != null && !alias.IsMissing)
                        _scope.Define(SyntaxSymbolKind.Immutable, _path, alias.Text);
                }

                public override void Visit(ExceptionPatternNode node)
                {
                    foreach (var field in node.Fields.Nodes)
                    {
                        var name = field.NameToken;

                        if (name.IsMissing)
                            continue;

                        var duplicate = false;

                        foreach (var field2 in node.Fields.Nodes)
                        {
                            if (field2 != field && field2.NameToken.Text == name.Text)
                            {
                                duplicate = true;
                                break;
                            }
                        }

                        if (!duplicate)
                            continue;

                        Error(field, SyntaxDiagnosticKind.DuplicatePatternField, name.Location,
                            $"Exception field '{name}' is matched multiple times");
                    }

                    base.Visit(node);

                    var alias = node.Alias?.NameToken;

                    if (alias != null && !alias.IsMissing)
                        _scope.Define(SyntaxSymbolKind.Immutable, _path, alias.Text);
                }

                public override void Visit(ArrayPatternNode node)
                {
                    base.Visit(node);

                    var alias = node.Alias?.NameToken;

                    if (alias != null && !alias.IsMissing)
                        _scope.Define(SyntaxSymbolKind.Immutable, _path, alias.Text);
                }

                public override void Visit(SetPatternNode node)
                {
                    base.Visit(node);

                    var alias = node.Alias?.NameToken;

                    if (alias != null && !alias.IsMissing)
                        _scope.Define(SyntaxSymbolKind.Immutable, _path, alias.Text);
                }

                public override void Visit(MapPatternNode node)
                {
                    base.Visit(node);

                    var alias = node.Alias?.NameToken;

                    if (alias != null && !alias.IsMissing)
                        _scope.Define(SyntaxSymbolKind.Immutable, _path, alias.Text);
                }
            }

            readonly ParseResult _parse;

            readonly ModuleLoader _loader;

            readonly SyntaxContext _context;

            public Analyzer(ParseResult parse, ModuleLoader loader, SyntaxContext context)
            {
                _parse = parse;
                _loader = loader;
                _context = context;
            }

            public AnalysisResult Analyze()
            {
                var walker = new Walker(_loader, _context);

                walker.Visit(_parse.Tree);

                return new AnalysisResult(_parse, _loader, _context, walker.Diagnostics.ToImmutableArray());
            }
        }

        public static AnalysisResult Analyze(ParseResult parse, ModuleLoader loader, SyntaxContext context)
        {
            return new Analyzer(parse, loader, context).Analyze();
        }
    }
}
