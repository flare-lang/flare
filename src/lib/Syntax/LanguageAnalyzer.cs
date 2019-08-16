using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Flare.Runtime;

namespace Flare.Syntax
{
    public static class LanguageAnalyzer
    {
        sealed class Analyzer
        {
            sealed class Walker : SyntaxWalker
            {
                class Scope
                {
                    public Scope? Previous { get; }

                    public FunctionScope? Function => this is FunctionScope s ? s : Previous?.Function;

                    public BlockScope? Block => this is BlockScope s ? s : Previous?.Block;

                    protected Dictionary<string, SyntaxSymbol> Symbols { get; } =
                        new Dictionary<string, SyntaxSymbol>();

                    public Scope(Scope? previous)
                    {
                        Previous = previous;
                    }

                    public virtual SyntaxSymbol? Resolve(string name)
                    {
                        return Symbols.TryGetValue(name, out var sym) ? sym : Previous?.Resolve(name);
                    }

                    public virtual bool IsMutable(string name)
                    {
                        return Symbols.TryGetValue(name, out var sym) ? sym.Kind == SyntaxSymbolKind.Mutable :
                            Previous?.IsMutable(name) ?? false;
                    }

                    public void Define(SyntaxSymbolKind kind, ModulePath? module, SyntaxNode? definition, string name)
                    {
                        // This method is used to shadow variables, too.
                        Symbols[name] = new SyntaxVariableSymbol(kind, module, definition, name);
                    }
                }

                sealed class FunctionScope : Scope
                {
                    public IReadOnlyDictionary<SyntaxSymbol, SyntaxUpvalueSymbol> Upvalues => _upvalues;

                    readonly Dictionary<BlockExpressionNode, int> _depths = new Dictionary<BlockExpressionNode, int>();

                    int _depth;

                    readonly Stack<(int, UseStatementNode)> _uses = new Stack<(int, UseStatementNode)>();

                    readonly Stack<PrimaryExpressionNode> _loops = new Stack<PrimaryExpressionNode>();

                    readonly Dictionary<SyntaxSymbol, SyntaxUpvalueSymbol> _upvalues =
                        new Dictionary<SyntaxSymbol, SyntaxUpvalueSymbol>();

                    int _slot;

                    public FunctionScope(Scope previous)
                        : base(previous)
                    {
                    }

                    public override SyntaxSymbol? Resolve(string name)
                    {
                        // Is it a local variable?
                        if (Symbols.TryGetValue(name, out var sym))
                            return sym;

                        sym = Previous?.Resolve(name);

                        // Is it an upvalue?
                        if (sym != null && (sym.Kind == SyntaxSymbolKind.Immutable ||
                            sym.Kind == SyntaxSymbolKind.Mutable))
                        {
                            if (!_upvalues.TryGetValue(sym, out var up))
                            {
                                sym = up = new SyntaxUpvalueSymbol(sym, _slot++);

                                _upvalues.Add(sym, up);
                            }
                            else
                                sym = up;
                        }

                        return sym;
                    }

                    public override bool IsMutable(string name)
                    {
                        // Lambdas can't mutate upvalues.
                        return Symbols.TryGetValue(name, out var sym) ? sym.Kind == SyntaxSymbolKind.Mutable : false;
                    }

                    public void Descend(BlockExpressionNode block)
                    {
                        _depths.Add(block, _depth++);
                    }

                    public void Ascend(BlockScope scope)
                    {
                        _depth--;

                        for (var i = 0; i < scope.Uses; i++)
                            _ = _uses.Pop();
                    }

                    public int GetDepth(BlockExpressionNode block)
                    {
                        return _depths[block];
                    }

                    public void PushUse(BlockScope scope, UseStatementNode use)
                    {
                        _uses.Push((GetDepth(scope.Node), use));

                        scope.Uses++;
                    }

                    public ImmutableArray<UseStatementNode> GetUses(int depth)
                    {
                        var uses = ImmutableArray<UseStatementNode>.Empty;

                        foreach (var use in _uses)
                            if (use.Item1 >= depth)
                                uses = uses.Add(use.Item2);

                        return uses;
                    }

                    public void PushLoop(PrimaryExpressionNode loop)
                    {
                        _loops.Push(loop);
                    }

                    public void PopLoop()
                    {
                        _ = _loops.Pop();
                    }

                    public PrimaryExpressionNode? GetLoop()
                    {
                        return _loops.TryPeek(out var loop) ? loop : null;
                    }
                }

                sealed class BlockScope : Scope
                {
                    public BlockExpressionNode Node { get; }

                    public int Uses { get; set; }

                    public BlockScope(Scope previous, BlockExpressionNode node)
                        : base(previous)
                    {
                        Node = node;
                    }
                }

                public IReadOnlyList<SyntaxDiagnostic> Diagnostics => _diagnostics;

                readonly List<SyntaxDiagnostic> _diagnostics = new List<SyntaxDiagnostic>();

                readonly ModuleLoader _loader;

                readonly SyntaxContext _context;

                ModulePath? _path;

                readonly Dictionary<string, ModulePath> _aliases = new Dictionary<string, ModulePath>();

                Scope _scope = new Scope(null);

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

                void PushScope()
                {
                    _scope = new Scope(_scope);
                }

                FunctionScope PushFunctionScope()
                {
                    var func = new FunctionScope(_scope);

                    _scope = func;

                    return func;
                }

                void PushBlockScope(BlockExpressionNode node)
                {
                    _scope = new BlockScope(_scope, node);
                    _scope.Function!.Descend(node);
                }

                void PopScope()
                {
                    _scope = _scope.Previous!;
                }

                public void PopBlockScope()
                {
                    _scope.Function!.Ascend(_scope.Block!);
                    PopScope();
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
                        }

                        if (!(LoadModule(use, use.Path.ComponentTokens.Tokens[0].Location, path) is Module mod))
                            continue;

                        // Don't import symbols if the module is aliased.
                        if (use.Alias != null)
                            continue;

                        foreach (var mdecl in mod.Declarations)
                        {
                            if (!mdecl.IsPublic)
                                continue;

                            var sym = mdecl switch
                            {
                                Constant _ => SyntaxSymbolKind.Constant,
                                Function _ => SyntaxSymbolKind.Function,
                                External _ => SyntaxSymbolKind.External,
                                Macro _ => SyntaxSymbolKind.Macro,
                                _ => (SyntaxSymbolKind?)null,
                            };

                            if (sym is SyntaxSymbolKind kind)
                                _scope.Define(kind, path, null, mdecl.Name);
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
                            MacroDeclarationNode _ => SyntaxSymbolKind.Macro,
                            _ => (SyntaxSymbolKind?)null,
                        };

                        if (sym is SyntaxSymbolKind kind)
                            _scope.Define(kind, _path, decl, name.Text);
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
                    _ = PushFunctionScope();

                    base.Visit(node);

                    PopScope();
                }

                public override void Visit(FunctionDeclarationNode node)
                {
                    _ = PushFunctionScope();

                    foreach (var param in node.ParameterList.Parameters.Nodes)
                    {
                        var name = param.NameToken;

                        if (name.IsMissing)
                            continue;

                        _scope.Define(SyntaxSymbolKind.Immutable, null, param, name.Text);

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
                    _ = PushFunctionScope();

                    foreach (var param in node.ParameterList.Parameters.Nodes)
                    {
                        var name = param.NameToken;

                        if (name.IsMissing)
                            continue;

                        _scope.Define(SyntaxSymbolKind.Fragment, null, param, name.Text);

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

                    PopScope();
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

                    _scope.Function!.PushUse(_scope.Block!, node);
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
                            var sym = _scope.Resolve(ident.Text);

                            if (sym != null && sym.Kind != SyntaxSymbolKind.Macro)
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

                public override void Visit(MacroCallExpressionNode node)
                {
                    var ident = node.IdentifierToken;

                    if (!ident.IsMissing)
                    {
                        var sym = _scope.Resolve(ident.Text);

                        if (sym != null && sym.Kind == SyntaxSymbolKind.Macro)
                            node.SetAnnotation("Symbol", sym);
                        else
                            Error(node, SyntaxDiagnosticKind.UnknownValueName, ident.Location,
                                $"Unknown macro name '{ident}'");
                    }

                    base.Visit(node);
                }

                public override void Visit(FragmentExpressionNode node)
                {
                    var ident = node.IdentifierToken;

                    if (!ident.IsMissing)
                    {
                        if (!ident.Text.StartsWith("$_"))
                        {
                            var sym = _scope.Resolve(ident.Text);

                            if (sym != null)
                                node.SetAnnotation("Symbol", sym);
                            else
                                Error(node, SyntaxDiagnosticKind.UnknownValueName, ident.Location,
                                    $"Unknown fragment name '{ident}'");
                        }
                        else
                            Error(node, SyntaxDiagnosticKind.DiscardedVariableUsed, ident.Location,
                                $"Use of discarded fragment name '{ident}'");
                    }

                    base.Visit(node);
                }

                public override void Visit(LambdaExpressionNode node)
                {
                    var scope = PushFunctionScope();

                    foreach (var param in node.ParameterList.Parameters.Nodes)
                    {
                        var name = param.NameToken;

                        if (name.IsMissing)
                            continue;

                        _scope.Define(SyntaxSymbolKind.Immutable, null, param, name.Text);

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

                    // Unwrap the upvalue symbols by one level. Some of them could still be upvalue
                    // symbols after this, in the case of nested lambdas, and that's OK. The lowerer
                    // will handle both cases.
                    var upvalues = ImmutableArray<SyntaxSymbol>.Empty;

                    // Avoid LINQ allocations if there are no upvalues.
                    if (scope.Upvalues.Count != 0)
                        foreach (var upvalue in scope.Upvalues.Values.OrderBy(x => x.Slot))
                            upvalues = upvalues.Add(upvalue.Symbol);

                    node.SetAnnotation("Upvalues", upvalues);

                    PopScope();
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
                    PushBlockScope(node);

                    base.Visit(node);

                    PopBlockScope();
                }

                public override void Visit(ConditionExpressionArmNode node)
                {
                    PushScope();

                    base.Visit(node);

                    PopScope();
                }

                public override void Visit(ForExpressionNode node)
                {
                    _scope.Function!.PushLoop(node);

                    base.Visit(node);

                    _scope.Function.PopLoop();
                }

                public override void Visit(WhileExpressionNode node)
                {
                    _scope.Function!.PushLoop(node);

                    base.Visit(node);

                    _scope.Function.PopLoop();
                }

                public override void Visit(LoopExpressionNode node)
                {
                    var kw = node.LoopKeywordToken;

                    if (!kw.IsMissing)
                    {
                        var func = _scope.Function!;

                        if (func.GetLoop() is PrimaryExpressionNode loop)
                        {
                            var block = loop switch
                            {
                                ForExpressionNode f => f.Body,
                                WhileExpressionNode w => w.Body,
                                _ => throw DebugAssert.Unreachable(),
                            };

                            node.SetAnnotation("Target", loop);
                            node.SetAnnotation("Uses", func.GetUses(func.GetDepth(block)));
                        }
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
                        var func = _scope.Function!;

                        if (func.GetLoop() is PrimaryExpressionNode loop)
                        {
                            var block = loop switch
                            {
                                ForExpressionNode f => f.Body,
                                WhileExpressionNode w => w.Body,
                                _ => throw DebugAssert.Unreachable(),
                            };

                            node.SetAnnotation("Target", loop);
                            node.SetAnnotation("Uses", func.GetUses(func.GetDepth(block)));
                        }
                        else
                            Error(node, SyntaxDiagnosticKind.InvalidLoopTarget, kw.Location,
                                "No enclosing 'while' or 'for' expression for this 'break' expression");
                    }

                    base.Visit(node);
                }

                public override void Visit(RaiseExpressionNode node)
                {
                    node.SetAnnotation("Uses", _scope.Function!.GetUses(0));

                    base.Visit(node);
                }

                public override void Visit(ReturnExpressionNode node)
                {
                    node.SetAnnotation("Uses", _scope.Function!.GetUses(0));

                    base.Visit(node);
                }

                public override void Visit(IdentifierPatternNode node)
                {
                    if (!node.IdentifierToken.IsMissing)
                        _scope.Define(node.MutKeywordToken != null ? SyntaxSymbolKind.Mutable :
                            SyntaxSymbolKind.Immutable, null, node, node.IdentifierToken.Text);

                    base.Visit(node);

                    var alias = node.Alias;

                    if (alias != null && !alias.NameToken.IsMissing)
                        _scope.Define(SyntaxSymbolKind.Immutable, null, alias, alias.NameToken.Text);
                }

                public override void Visit(LiteralPatternNode node)
                {
                    base.Visit(node);

                    var alias = node.Alias;

                    if (alias != null && !alias.NameToken.IsMissing)
                        _scope.Define(SyntaxSymbolKind.Immutable, null, alias, alias.NameToken.Text);
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

                    var alias = node.Alias;

                    if (alias != null && !alias.NameToken.IsMissing)
                        _scope.Define(SyntaxSymbolKind.Immutable, null, alias, alias.NameToken.Text);
                }

                public override void Visit(TuplePatternNode node)
                {
                    base.Visit(node);

                    var alias = node.Alias;

                    if (alias != null && !alias.NameToken.IsMissing)
                        _scope.Define(SyntaxSymbolKind.Immutable, null, alias, alias.NameToken.Text);
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

                    var alias = node.Alias;

                    if (alias != null && !alias.NameToken.IsMissing)
                        _scope.Define(SyntaxSymbolKind.Immutable, null, alias, alias.NameToken.Text);
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

                    var alias = node.Alias;

                    if (alias != null && !alias.NameToken.IsMissing)
                        _scope.Define(SyntaxSymbolKind.Immutable, null, alias, alias.NameToken.Text);
                }

                public override void Visit(ArrayPatternNode node)
                {
                    base.Visit(node);

                    var alias = node.Alias;

                    if (alias != null && !alias.NameToken.IsMissing)
                        _scope.Define(SyntaxSymbolKind.Immutable, null, alias, alias.NameToken.Text);
                }

                public override void Visit(SetPatternNode node)
                {
                    base.Visit(node);

                    var alias = node.Alias;

                    if (alias != null && !alias.NameToken.IsMissing)
                        _scope.Define(SyntaxSymbolKind.Immutable, null, alias, alias.NameToken.Text);
                }

                public override void Visit(MapPatternNode node)
                {
                    base.Visit(node);

                    var alias = node.Alias;

                    if (alias != null && !alias.NameToken.IsMissing)
                        _scope.Define(SyntaxSymbolKind.Immutable, null, alias, alias.NameToken.Text);
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
