using System.Collections.Generic;
using System.Collections.Immutable;

namespace Flare.Syntax
{
    public static class LanguageAnalyzer
    {
        sealed class Analyzer
        {
            sealed class Walker : SyntaxWalker
            {
                enum SymbolKind
                {
                    Declaration,
                    Immutable,
                    Mutable,
                }

                class Scope
                {
                    public Scope Previous { get; }

                    protected Dictionary<string, SymbolKind> Symbols { get; } = new Dictionary<string, SymbolKind>();

                    public Scope(Scope previous)
                    {
                        Previous = previous;
                    }

                    public bool IsDefined(string name)
                    {
                        return Symbols.ContainsKey(name) ? true : Previous?.IsDefined(name) ?? false;
                    }

                    public virtual bool IsMutable(string name)
                    {
                        return Symbols.TryGetValue(name, out var kind) ? kind == SymbolKind.Mutable :
                            Previous?.IsMutable(name) ?? false;
                    }

                    public void Define(string name, SymbolKind kind)
                    {
                        // This method is used to shadow variables, too.
                        Symbols[name] = kind;
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
                        return Symbols.TryGetValue(name, out var kind) ? kind == SymbolKind.Mutable : false;
                    }
                }

                public Analyzer Analyzer { get; }

                public Walker(Analyzer analyzer)
                {
                    Analyzer = analyzer;
                }

                Scope _scope = new Scope(null!);

                readonly HashSet<string> _currentFragments = new HashSet<string>();

                readonly Stack<PrimaryExpressionNode> _loops = new Stack<PrimaryExpressionNode>();

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

                void Error(SyntaxDiagnosticKind kind, SourceLocation location, string message,
                    ImmutableArray<(SourceLocation, string)> notes = default)
                {
                    Analyzer._diagnostics.Add(new SyntaxDiagnostic(kind, SyntaxDiagnosticSeverity.Error, location,
                        message, notes));
                }

                public override void Visit(ProgramNode node)
                {
                    foreach (var decl in node.Declarations)
                    {
                        if (decl is UseDeclarationNode || decl is MissingNamedDeclarationNode)
                            continue;

                        var named = (NamedDeclarationNode)decl;
                        var name = named.NameToken;

                        if (name.IsMissing)
                            continue;

                        if (name.Text.StartsWith('_'))
                        {
                            Error(SyntaxDiagnosticKind.InvalidDeclarationName, name.Location,
                                $"Declaration name '{name}' is invalid; declaration names cannot start with '_'");
                            continue;
                        }

                        _scope.Define(name.Text, SymbolKind.Declaration);

                        var notes = ImmutableArray<(SourceLocation, string)>.Empty;

                        foreach (var decl2 in node.Declarations)
                        {
                            if (decl2 == decl || decl2 is UseDeclarationNode || decl2 is MissingNamedDeclarationNode)
                                continue;

                            var named2 = (NamedDeclarationNode)decl;
                            var name2 = named2.NameToken;

                            if (name2.Text != name.Text)
                                continue;

                            notes = notes.Add((name2.Location, "Previous declaration here"));
                        }

                        if (notes.Length == 0)
                            continue;

                        Error(SyntaxDiagnosticKind.DuplicateDeclaration, name.Location,
                            $"Declaration '{name}' already declared earlier", notes);
                    }

                    base.Visit(node);
                }

                public override void Visit(FunctionDeclarationNode node)
                {
                    PushScope();

                    foreach (var param in node.ParameterList.Parameters.Nodes)
                    {
                        var name = param.NameToken;

                        if (name.IsMissing)
                            continue;

                        _scope.Define(name.Text, SymbolKind.Immutable);

                        var notes = ImmutableArray<(SourceLocation, string)>.Empty;

                        foreach (var param2 in node.ParameterList.Parameters.Nodes)
                            if (param2 != param && param2.NameToken.Text == name.Text)
                                notes = notes.Add((param2.NameToken.Location, "Previous declaration here"));

                        if (notes.Length == 0)
                            continue;

                        Error(SyntaxDiagnosticKind.DuplicateParameter, name.Location,
                            $"Function parameter '{name}' already declared earlier", notes);
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

                        var notes = ImmutableArray<(SourceLocation, string)>.Empty;

                        foreach (var param2 in node.ParameterList.Parameters.Nodes)
                            if (param2 != param && param2.NameToken.Text == name.Text)
                                notes = notes.Add((param2.NameToken.Location, "Previous declaration here"));

                        if (notes.Length == 0)
                            continue;

                        Error(SyntaxDiagnosticKind.DuplicateParameter, name.Location,
                            $"Function parameter '{name}' already declared earlier", notes);
                    }

                    base.Visit(node);
                }

                public override void Visit(MacroDeclarationNode node)
                {
                    _currentFragments.Clear();

                    foreach (var param in node.ParameterList.Parameters.Nodes)
                    {
                        var name = param.NameToken;

                        if (name.IsMissing)
                            continue;

                        _ = _currentFragments.Add(name.Text);

                        var notes = ImmutableArray<(SourceLocation, string)>.Empty;

                        foreach (var param2 in node.ParameterList.Parameters.Nodes)
                            if (param2 != param && param2.NameToken.Text == name.Text)
                                notes = notes.Add((param2.NameToken.Location, "Previous declaration here"));

                        if (notes.Length == 0)
                            continue;

                        Error(SyntaxDiagnosticKind.DuplicateParameter, name.Location,
                            $"Macro parameter '{name}' already declared earlier", notes);
                    }

                    base.Visit(node);
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
                                    Error(SyntaxDiagnosticKind.InvalidAssignmentTarget, op.Location,
                                        $"Identifier '{ident}' does not represent a mutable variable");
                                break;
                            default:
                                Error(SyntaxDiagnosticKind.InvalidAssignmentTarget, op.Location,
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

                    // TODO: Check symbol table as well.
                    if (ident.Text.StartsWith('_'))
                        Error(SyntaxDiagnosticKind.DiscardedVariableUsed, ident.Location,
                            $"Use of discarded variable '{ident}'");

                    base.Visit(node);
                }

                public override void Visit(FragmentExpressionNode node)
                {
                    var ident = node.IdentifierToken;

                    if (!ident.IsMissing && !_currentFragments.Contains(ident.Text))
                        Error(SyntaxDiagnosticKind.UnknownValueName, ident.Location,
                            $"Unknown fragment name '{ident}'");

                    base.Visit(node);
                }

                public override void Visit(LambdaExpressionNode node)
                {
                    PushLambdaScope();

                    foreach (var param in node.ParameterList.Parameters.Nodes)
                    {
                        var name = param.NameToken;

                        if (name.IsMissing)
                            continue;

                        _scope.Define(name.Text, SymbolKind.Immutable);

                        var notes = ImmutableArray<(SourceLocation, string)>.Empty;

                        foreach (var param2 in node.ParameterList.Parameters.Nodes)
                            if (param2 != param && param2.NameToken.Text == name.Text)
                                notes = notes.Add((param2.NameToken.Location, "Previous declaration here"));

                        if (notes.Length == 0)
                            continue;

                        Error(SyntaxDiagnosticKind.DuplicateParameter, name.Location,
                            $"Lambda parameter '{name}' already declared earlier", notes);
                    }

                    base.Visit(node);

                    PopScope();
                }

                public override void Visit(RecordExpressionNode node)
                {
                    foreach (var field in node.Fields.Nodes)
                    {
                        var name = field.NameToken;

                        if (name.IsMissing)
                            continue;

                        var notes = ImmutableArray<(SourceLocation, string)>.Empty;

                        foreach (var field2 in node.Fields.Nodes)
                            if (field2 != field && field2.NameToken.Text == name.Text)
                                notes = notes.Add((field2.NameToken.Location, "Previous assignment here"));

                        if (notes.Length == 0)
                            continue;

                        Error(SyntaxDiagnosticKind.DuplicateExpressionField, name.Location,
                            $"Record field '{name}' is assigned multiple times", notes);
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

                        var notes = ImmutableArray<(SourceLocation, string)>.Empty;

                        foreach (var field2 in node.Fields.Nodes)
                            if (field2 != field && field2.NameToken.Text == name.Text)
                                notes = notes.Add((field2.NameToken.Location, "Previous assignment here"));

                        if (notes.Length == 0)
                            continue;

                        Error(SyntaxDiagnosticKind.DuplicateExpressionField, name.Location,
                            $"Exception field '{name}' is assigned multiple times", notes);
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
                    _loops.Push(node);

                    base.Visit(node);

                    _ = _loops.Pop();
                }

                public override void Visit(WhileExpressionNode node)
                {
                    _loops.Push(node);

                    base.Visit(node);

                    _ = _loops.Pop();
                }

                public override void Visit(LoopExpressionNode node)
                {
                    var kw = node.LoopKeywordToken;

                    if (!kw.IsMissing)
                    {
                        if (_loops.TryPeek(out var loop))
                            node.Set("Target", loop);
                        else
                            Error(SyntaxDiagnosticKind.InvalidLoopTarget, kw.Location,
                                $"'loop' expression appears outside 'for' or 'while' expression");
                    }

                    base.Visit(node);
                }

                public override void Visit(BreakExpressionNode node)
                {
                    var kw = node.BreakKeywordToken;

                    if (!kw.IsMissing)
                    {
                        if (_loops.TryPeek(out var loop))
                            node.Set("Target", loop);
                        else
                            Error(SyntaxDiagnosticKind.InvalidLoopTarget, kw.Location,
                                $"'break' expression appears outside 'for' or 'while' expression");
                    }

                    base.Visit(node);
                }

                public override void Visit(IdentifierPatternNode node)
                {
                    if (!node.IdentifierToken.IsMissing)
                        _scope.Define(node.IdentifierToken.Text, node.MutKeywordToken != null ? SymbolKind.Mutable :
                            SymbolKind.Immutable);

                    base.Visit(node);

                    var alias = node.Alias?.NameToken;

                    if (alias != null && !alias.IsMissing)
                        _scope.Define(alias.Text, SymbolKind.Immutable);
                }

                public override void Visit(LiteralPatternNode node)
                {
                    base.Visit(node);

                    var alias = node.Alias?.NameToken;

                    if (alias != null && !alias.IsMissing)
                        _scope.Define(alias.Text, SymbolKind.Immutable);
                }

                public override void Visit(ModulePatternNode node)
                {
                    base.Visit(node);

                    var alias = node.Alias?.NameToken;

                    if (alias != null && !alias.IsMissing)
                        _scope.Define(alias.Text, SymbolKind.Immutable);
                }

                public override void Visit(TuplePatternNode node)
                {
                    base.Visit(node);

                    var alias = node.Alias?.NameToken;

                    if (alias != null && !alias.IsMissing)
                        _scope.Define(alias.Text, SymbolKind.Immutable);
                }

                public override void Visit(RecordPatternNode node)
                {
                    foreach (var field in node.Fields.Nodes)
                    {
                        var name = field.NameToken;

                        if (name.IsMissing)
                            continue;

                        _scope.Define(name.Text, SymbolKind.Immutable);

                        var notes = ImmutableArray<(SourceLocation, string)>.Empty;

                        foreach (var field2 in node.Fields.Nodes)
                            if (field2 != field && field2.NameToken.Text == name.Text)
                                notes = notes.Add((field2.NameToken.Location, "Previous pattern here"));

                        if (notes.Length == 0)
                            continue;

                        Error(SyntaxDiagnosticKind.DuplicatePatternField, name.Location,
                            $"Record field '{name}' is matched multiple times", notes);
                    }

                    base.Visit(node);

                    var alias = node.Alias?.NameToken;

                    if (alias != null && !alias.IsMissing)
                        _scope.Define(alias.Text, SymbolKind.Immutable);
                }

                public override void Visit(ExceptionPatternNode node)
                {
                    foreach (var field in node.Fields.Nodes)
                    {
                        var name = field.NameToken;

                        if (name.IsMissing)
                            continue;

                        var notes = ImmutableArray<(SourceLocation, string)>.Empty;

                        foreach (var field2 in node.Fields.Nodes)
                            if (field2 != field && field2.NameToken.Text == name.Text)
                                notes = notes.Add((field2.NameToken.Location, "Previous pattern here"));

                        if (notes.Length == 0)
                            continue;

                        Error(SyntaxDiagnosticKind.DuplicatePatternField, name.Location,
                            $"Exception field '{name}' is matched multiple times", notes);
                    }

                    base.Visit(node);

                    var alias = node.Alias?.NameToken;

                    if (alias != null && !alias.IsMissing)
                        _scope.Define(alias.Text, SymbolKind.Immutable);
                }

                public override void Visit(ArrayPatternNode node)
                {
                    base.Visit(node);

                    var alias = node.Alias?.NameToken;

                    if (alias != null && !alias.IsMissing)
                        _scope.Define(alias.Text, SymbolKind.Immutable);
                }

                public override void Visit(SetPatternNode node)
                {
                    base.Visit(node);

                    var alias = node.Alias?.NameToken;

                    if (alias != null && !alias.IsMissing)
                        _scope.Define(alias.Text, SymbolKind.Immutable);
                }

                public override void Visit(MapPatternNode node)
                {
                    base.Visit(node);

                    var alias = node.Alias?.NameToken;

                    if (alias != null && !alias.IsMissing)
                        _scope.Define(alias.Text, SymbolKind.Immutable);
                }
            }

            readonly ParseResult _parse;

            readonly List<SyntaxDiagnostic> _diagnostics = new List<SyntaxDiagnostic>();

            public Analyzer(ParseResult parse)
            {
                _parse = parse;
            }

            public AnalysisResult Analyze()
            {
                new Walker(this).Visit(_parse.Tree);

                return new AnalysisResult(_parse, _diagnostics.ToImmutableArray());
            }
        }

        public static AnalysisResult Analyze(ParseResult parse)
        {
            return new Analyzer(parse).Analyze();
        }
    }
}
