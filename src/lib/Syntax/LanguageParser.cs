using System.Collections.Generic;
using System.Collections.Immutable;

namespace Flare.Syntax
{
    public sealed class LanguageParser
    {
        sealed class Parser
        {
            sealed class TokenStream
            {
                public int Position { get; set; } = -1;

                readonly IReadOnlyList<SyntaxToken> _tokens;

                public TokenStream(IReadOnlyList<SyntaxToken> tokens)
                {
                    _tokens = tokens;
                }

                public SyntaxToken Peek(int offset = 1)
                {
                    var idx = Position + offset;

                    if (idx >= _tokens.Count)
                        idx = _tokens.Count - 1;

                    return _tokens[idx];
                }

                public SyntaxToken Move(int offset = 1)
                {
                    var idx = Position + offset;

                    if (idx >= _tokens.Count)
                        idx = _tokens.Count - 1;

                    return _tokens[Position = idx];
                }
            }

            abstract class ParseContext
            {
                public abstract bool CanParse(SyntaxToken token);
            }

            sealed class DeclarationParseContext : ParseContext
            {
                public static DeclarationParseContext Instance { get; } = new DeclarationParseContext();

                DeclarationParseContext()
                {
                }

                public override bool CanParse(SyntaxToken token)
                {
                    switch (token.Kind)
                    {
                        // All the tokens that a declaration can start with.
                        case SyntaxTokenKind.At:
                        case SyntaxTokenKind.ConstKeyword:
                        case SyntaxTokenKind.ExternKeyword:
                        case SyntaxTokenKind.FnKeyword:
                        case SyntaxTokenKind.MacroKeyword:
                        case SyntaxTokenKind.PrivKeyword:
                        case SyntaxTokenKind.PubKeyword:
                        case SyntaxTokenKind.TestKeyword:
                        case SyntaxTokenKind.UseKeyword:
                            return true;
                        default:
                            return false;
                    }
                }
            }

            sealed class StatementParseContext : ParseContext
            {
                public static StatementParseContext Instance { get; } = new StatementParseContext();

                StatementParseContext()
                {
                }

                public override bool CanParse(SyntaxToken token)
                {
                    switch (token.Kind)
                    {
                        // All the tokens that a statement can start with.
                        case SyntaxTokenKind.MultiplicativeOperator:
                        case SyntaxTokenKind.AdditiveOperator:
                        case SyntaxTokenKind.ShiftOperator:
                        case SyntaxTokenKind.BitwiseOperator:
                        case SyntaxTokenKind.Hash:
                        case SyntaxTokenKind.OpenParen:
                        case SyntaxTokenKind.OpenBracket:
                        case SyntaxTokenKind.OpenBrace:
                        case SyntaxTokenKind.AssertKeyword:
                        case SyntaxTokenKind.BreakKeyword:
                        case SyntaxTokenKind.CondKeyword:
                        case SyntaxTokenKind.ExcKeyword:
                        case SyntaxTokenKind.FnKeyword:
                        case SyntaxTokenKind.ForKeyword:
                        case SyntaxTokenKind.FreezeKeyword:
                        case SyntaxTokenKind.IfKeyword:
                        case SyntaxTokenKind.LetKeyword:
                        case SyntaxTokenKind.LoopKeyword:
                        case SyntaxTokenKind.MatchKeyword:
                        case SyntaxTokenKind.MutKeyword:
                        case SyntaxTokenKind.NotKeyword:
                        case SyntaxTokenKind.RaiseKeyword:
                        case SyntaxTokenKind.RecKeyword:
                        case SyntaxTokenKind.RecvKeyword:
                        case SyntaxTokenKind.ReturnKeyword:
                        case SyntaxTokenKind.TryKeyword:
                        case SyntaxTokenKind.UseKeyword:
                        case SyntaxTokenKind.WhileKeyword:
                        case SyntaxTokenKind.ModuleIdentifier:
                        case SyntaxTokenKind.ValueIdentifier:
                        case SyntaxTokenKind.FragmentIdentifier:
                        case SyntaxTokenKind.NilLiteral:
                        case SyntaxTokenKind.BooleanLiteral:
                        case SyntaxTokenKind.AtomLiteral:
                        case SyntaxTokenKind.IntegerLiteral:
                        case SyntaxTokenKind.RealLiteral:
                        case SyntaxTokenKind.StringLiteral:
                            return true;
                        default:
                            return false;
                    }
                }
            }

            sealed class InteractiveParseContext : ParseContext
            {
                public static InteractiveParseContext Instance { get; } = new InteractiveParseContext();

                public DeclarationParseContext Declarations { get; } = DeclarationParseContext.Instance;

                public StatementParseContext Statements { get; } = StatementParseContext.Instance;

                InteractiveParseContext()
                {
                }

                public override bool CanParse(SyntaxToken token)
                {
                    return Declarations.CanParse(token) || Statements.CanParse(token);
                }
            }

            readonly ref struct ContextWrapper<T>
                where T : ParseContext
            {
                public T Context { get; }

                readonly Parser _parser;

                public ContextWrapper(Parser parser, T context)
                {
                    Context = context;
                    _parser = parser;

                    parser._contexts.Push(context);
                }

                public void Dispose()
                {
                    _ = _parser._contexts.Pop();
                }
            }

            readonly LexResult _lex;

            readonly SyntaxMode _mode;

            readonly TokenStream _stream;

            readonly Stack<ParseContext> _contexts = new Stack<ParseContext>();

            readonly List<SyntaxDiagnostic> _diagnostics = new List<SyntaxDiagnostic>();

            public Parser(LexResult lex, SyntaxMode mode)
            {
                _lex = lex;
                _mode = mode;
                _stream = new TokenStream(lex.Tokens);
            }

            SyntaxToken Missing()
            {
                return new SyntaxToken(_lex.Source.FullPath);
            }

            SyntaxToken Expect(SyntaxTokenKind kind, string expected, ref ImmutableArray<SyntaxDiagnostic> diagnostics)
            {
                var tok = _stream.Peek();

                if (tok.Kind == kind)
                    return _stream.Move();

                Error(ref diagnostics, SyntaxDiagnosticKind.MissingToken, tok.Location,
                    $"Expected {expected}, but found {(tok.IsEndOfInput ? "end of input" : $"'{tok}'")}");

                return Missing();
            }

            SyntaxToken ExpectNumericLiteral(ref ImmutableArray<SyntaxDiagnostic> diagnostics)
            {
                var tok = _stream.Peek();

                switch (tok.Kind)
                {
                    case SyntaxTokenKind.IntegerLiteral:
                    case SyntaxTokenKind.RealLiteral:
                        return _stream.Move();
                    default:
                        Error(ref diagnostics, SyntaxDiagnosticKind.MissingToken, tok.Location,
                            $"Expected numeric literal value, but found {(tok.IsEndOfInput ? "end of input" : $"'{tok}'")}");

                        return Missing();
                }
            }

            SyntaxToken ExpectLiteral(ref ImmutableArray<SyntaxDiagnostic> diagnostics)
            {
                var tok = _stream.Peek();

                switch (tok.Kind)
                {
                    case SyntaxTokenKind.NilLiteral:
                    case SyntaxTokenKind.BooleanLiteral:
                    case SyntaxTokenKind.AtomLiteral:
                    case SyntaxTokenKind.IntegerLiteral:
                    case SyntaxTokenKind.RealLiteral:
                    case SyntaxTokenKind.StringLiteral:
                        return _stream.Move();
                    default:
                        Error(ref diagnostics, SyntaxDiagnosticKind.MissingToken, tok.Location,
                            $"Expected literal value, but found {(tok.IsEndOfInput ? "end of input" : $"'{tok}'")}");

                        return Missing();
                }
            }

            SyntaxToken? Optional1(SyntaxTokenKind kind)
            {
                return _stream.Peek().Kind == kind ? _stream.Move() : null;
            }

            SyntaxToken? Optional2(SyntaxTokenKind kind1, SyntaxTokenKind kind2)
            {
                var cur = _stream.Peek().Kind;

                return cur == kind1 || cur == kind2 ? _stream.Move() : null;
            }

            static SyntaxNodeList<T> List<T>(ImmutableArray<T> nodes)
                where T : SyntaxNode
            {
                return new SyntaxNodeList<T>(nodes);
            }

            static SyntaxTokenList List(ImmutableArray<SyntaxToken> tokens)
            {
                return new SyntaxTokenList(tokens);
            }

            static SeparatedSyntaxNodeList<T> List<T>(ImmutableArray<T> nodes, ImmutableArray<SyntaxToken> separators)
                where T : SyntaxNode
            {
                return new SeparatedSyntaxNodeList<T>(List(nodes), List(separators));
            }

            static SeparatedSyntaxTokenList List(ImmutableArray<SyntaxToken> nodes,
                ImmutableArray<SyntaxToken> separators)
            {
                return new SeparatedSyntaxTokenList(List(nodes), List(separators));
            }

            ContextWrapper<T> Context<T>(T context)
                where T : ParseContext
            {
                return new ContextWrapper<T>(this, context);
            }

            bool CanOuterParse(SyntaxToken token)
            {
                // Avoid LINQ for performance reasons.
                foreach (var ctx in _contexts)
                    if (ctx.CanParse(token))
                        return true;

                return false;
            }

            ImmutableArray<SyntaxToken> Skipped()
            {
                return ImmutableArray<SyntaxToken>.Empty;
            }

            void Skip(ref ImmutableArray<SyntaxToken> skipped)
            {
                skipped = skipped.Add(_stream.Move());
            }

            ImmutableArray<SyntaxDiagnostic> Diagnostics()
            {
                return ImmutableArray<SyntaxDiagnostic>.Empty;
            }

            void Error(ref ImmutableArray<SyntaxDiagnostic> diagnostics, SyntaxDiagnosticKind kind,
                SourceLocation location, string message)
            {
                var diag = new SyntaxDiagnostic(kind, SyntaxDiagnosticSeverity.Error, location, message,
                    ImmutableArray<(SourceLocation, string)>.Empty);

                diagnostics = diagnostics.Add(diag);
                _diagnostics.Add(diag);
            }

            public ParseResult Parse()
            {
                var node = _mode switch
                {
                    SyntaxMode.Normal => (SyntaxNode)ParseProgram(),
                    SyntaxMode.Interactive => ParseInteractive(),
                    _ => throw Assert.Unreachable(),
                };

                var diags = Diagnostics(); // Discarded.

                _ = Expect(SyntaxTokenKind.EndOfInput, "end of input", ref diags);

                return new ParseResult(_lex, _mode, node, _diagnostics.ToImmutableArray());
            }

            ProgramNode ParseProgram()
            {
                var skipped = Skipped();
                var diags = Diagnostics();

                var attrs = ParseAttributes();
                var mod = Expect(SyntaxTokenKind.ModKeyword, "'mod' keyword", ref diags);
                var path = ParseModulePath();
                var semi = Expect(SyntaxTokenKind.Semicolon, "';'", ref diags);
                var decls = ImmutableArray<DeclarationNode>.Empty;

                using var ctx = Context(DeclarationParseContext.Instance);

                while (_stream.Peek() is var tok && !tok.IsEndOfInput)
                {
                    if (ctx.Context.CanParse(tok))
                        decls = decls.Add(ParseDeclaration());
                    else
                        Skip(ref skipped);
                }

                return new ProgramNode(skipped, diags, List(attrs), mod, path, semi, List(decls));
            }

            InteractiveNode ParseInteractive()
            {
                var skipped = Skipped();
                var diags = Diagnostics();

                var decls = ImmutableArray<DeclarationNode>.Empty;
                var stmts = ImmutableArray<StatementNode>.Empty;

                using var ctx = Context(InteractiveParseContext.Instance);

                while (_stream.Peek() is var tok && !tok.IsEndOfInput)
                {
                    if (ctx.Context.Declarations.CanParse(tok))
                        decls = decls.Add(ParseDeclaration());
                    else if (ctx.Context.Statements.CanParse(tok))
                        stmts = stmts.Add(ParseStatement());
                    else
                        Skip(ref skipped);
                }

                return new InteractiveNode(skipped, diags, List(decls), List(stmts));
            }

            ModulePathNode ParseModulePath()
            {
                var diags = Diagnostics();

                var idents = ImmutableArray<SyntaxToken>.Empty;
                var seps = ImmutableArray<SyntaxToken>.Empty;

                idents = idents.Add(Expect(SyntaxTokenKind.ModuleIdentifier, "module identifier", ref diags));

                while (_stream.Peek().Kind == SyntaxTokenKind.ColonColon)
                {
                    seps = seps.Add(Expect(SyntaxTokenKind.ColonColon, "'::'", ref diags));
                    idents = idents.Add(Expect(SyntaxTokenKind.ModuleIdentifier, "module identifier", ref diags));
                }

                return new ModulePathNode(Skipped(), diags, List(idents, seps));
            }

            AttributeNode ParseAttribute()
            {
                var diags = Diagnostics();

                var at = Expect(SyntaxTokenKind.At, "'@'", ref diags);
                var open = Expect(SyntaxTokenKind.OpenBracket, "'['", ref diags);
                var name = Expect(SyntaxTokenKind.ValueIdentifier, "value identifier", ref diags);
                var eq = Expect(SyntaxTokenKind.Equals, "'='", ref diags);
                var value = ExpectLiteral(ref diags);
                var close = Expect(SyntaxTokenKind.CloseBracket, "']'", ref diags);

                return new AttributeNode(Skipped(), diags, at, open, name, eq, value, close);
            }

            ImmutableArray<AttributeNode> ParseAttributes()
            {
                var attrs = ImmutableArray<AttributeNode>.Empty;

                while (_stream.Peek().Kind == SyntaxTokenKind.At)
                    attrs = attrs.Add(ParseAttribute());

                return attrs;
            }

            DeclarationNode ParseDeclaration()
            {
                var saved = _stream.Position;

                var attrs = ParseAttributes();

                if (_stream.Peek().Kind == SyntaxTokenKind.UseKeyword)
                    return ParseUseDeclaration(attrs);

                if (_stream.Peek().Kind == SyntaxTokenKind.TestKeyword)
                    return ParseTestDeclaration(attrs);

                var vis = Optional2(SyntaxTokenKind.PrivKeyword, SyntaxTokenKind.PubKeyword);
                var tok = _stream.Peek();

                switch (tok.Kind)
                {
                    case SyntaxTokenKind.ConstKeyword:
                        return ParseConstantDeclaration(attrs, vis);
                    case SyntaxTokenKind.ExternKeyword:
                        return ParseExternalDeclaration(attrs, vis);
                    case SyntaxTokenKind.FnKeyword:
                        return ParseFunctionDeclaration(attrs, vis);
                    case SyntaxTokenKind.MacroKeyword:
                        return ParseMacroDeclaration(attrs, vis);
                    default:
                        var skipped = Skipped();
                        var diags = Diagnostics();

                        var diff = _stream.Position - saved;

                        // Skip the tokens we parsed for attributes and visibility.
                        _stream.Position = saved;

                        for (var i = 0; i < diff; i++)
                            Skip(ref skipped);

                        Error(ref diags, SyntaxDiagnosticKind.MissingNamedDeclaration, tok.Location,
                            $"Expected declaration, but found {(tok.IsEndOfInput ? "end of input" : $"'{tok}'")}");

                        return new MissingNamedDeclarationNode(skipped, diags,
                            List(ImmutableArray<AttributeNode>.Empty), Missing(), Missing(), Missing());
                }
            }

            TestDeclarationNode ParseTestDeclaration(ImmutableArray<AttributeNode> attributes)
            {
                var diags = Diagnostics();

                var test = Expect(SyntaxTokenKind.TestKeyword, "'test' keyword", ref diags);
                var name = Expect(SyntaxTokenKind.ValueIdentifier, "value identifier", ref diags);
                var body = ParseBlockExpression();

                return new TestDeclarationNode(Skipped(), diags, List(attributes), test, name, body);
            }

            UseDeclarationNode ParseUseDeclaration(ImmutableArray<AttributeNode> attributes)
            {
                var diags = Diagnostics();

                var use = Expect(SyntaxTokenKind.UseKeyword, "'use' keyword", ref diags);
                var path = ParseModulePath();
                var alias = _stream.Peek().Kind == SyntaxTokenKind.AsKeyword ? ParseUseDeclarationAlias() : null;
                var semi = Expect(SyntaxTokenKind.Semicolon, "';'", ref diags);

                return new UseDeclarationNode(Skipped(), diags, List(attributes), use, path, alias, semi);
            }

            UseDeclarationAliasNode ParseUseDeclarationAlias()
            {
                var diags = Diagnostics();

                var @as = Expect(SyntaxTokenKind.AsKeyword, "'as' keyword", ref diags);
                var name = Expect(SyntaxTokenKind.ModuleIdentifier, "module identifier", ref diags);

                return new UseDeclarationAliasNode(Skipped(), diags, @as, name);
            }

            ConstantDeclarationNode ParseConstantDeclaration(ImmutableArray<AttributeNode> attributes,
                SyntaxToken? visibility)
            {
                var diags = Diagnostics();

                var @const = Expect(SyntaxTokenKind.ConstKeyword, "'const' keyword", ref diags);
                var name = Expect(SyntaxTokenKind.ValueIdentifier, "value identifier", ref diags);
                var eq = Expect(SyntaxTokenKind.Equals, "'='", ref diags);
                var init = ParseExpression();
                var semi = Expect(SyntaxTokenKind.Semicolon, "';'", ref diags);

                return new ConstantDeclarationNode(Skipped(), diags, List(attributes), visibility, @const, name, eq,
                    init, semi);
            }

            ExternalDeclarationNode ParseExternalDeclaration(ImmutableArray<AttributeNode> attributes,
                SyntaxToken? visibility)
            {
                var diags = Diagnostics();

                var @extern = Expect(SyntaxTokenKind.ExternKeyword, "'extern' keyword", ref diags);
                var name = Expect(SyntaxTokenKind.ValueIdentifier, "value identifier", ref diags);
                var parms = ParseFunctionParameterList();
                var semi = Expect(SyntaxTokenKind.Semicolon, "';'", ref diags);

                return new ExternalDeclarationNode(Skipped(), diags, List(attributes), visibility, @extern, name, parms,
                    semi);
            }

            FunctionParameterListNode ParseFunctionParameterList()
            {
                var diags = Diagnostics();

                var open = Expect(SyntaxTokenKind.OpenParen, "'('", ref diags);
                var parms = ImmutableArray<FunctionParameterNode>.Empty;
                var seps = ImmutableArray<SyntaxToken>.Empty;
                var first = true;

                while (_stream.Peek() is var tok && !tok.IsEndOfInput && tok.Kind != SyntaxTokenKind.CloseParen)
                {
                    if (!first)
                    {
                        if (_stream.Peek().Kind != SyntaxTokenKind.Comma)
                            break;

                        seps = seps.Add(_stream.Move());
                    }

                    var param = ParseFunctionParameter();
                    var stop = param.NameToken.IsMissing && _stream.Peek().Kind != SyntaxTokenKind.Comma;

                    if (first && stop)
                        break;

                    parms = parms.Add(param);
                    first = false;

                    if (stop || param.DotDotToken != null)
                        break;
                }

                var close = Expect(SyntaxTokenKind.CloseParen, "')'", ref diags);

                return new FunctionParameterListNode(Skipped(), diags, open, List(parms, seps), close);
            }

            FunctionParameterNode ParseFunctionParameter()
            {
                var diags = Diagnostics();

                var dotDot = Optional1(SyntaxTokenKind.DotDot);
                var attrs = ParseAttributes();
                var name = Expect(SyntaxTokenKind.ValueIdentifier, "value identifier", ref diags);

                return new FunctionParameterNode(Skipped(), diags, dotDot, List(attrs), name);
            }

            FunctionDeclarationNode ParseFunctionDeclaration(ImmutableArray<AttributeNode> attributes,
                SyntaxToken? visibility)
            {
                var diags = Diagnostics();

                var fn = Expect(SyntaxTokenKind.FnKeyword, "'fn' keyword", ref diags);
                var name = Expect(SyntaxTokenKind.ValueIdentifier, "value identifier", ref diags);
                var parms = ParseFunctionParameterList();
                var body = ParseBlockExpression();

                return new FunctionDeclarationNode(Skipped(), diags, List(attributes), visibility, fn, name, parms,
                    body);
            }

            MacroDeclarationNode ParseMacroDeclaration(ImmutableArray<AttributeNode> attributes,
                SyntaxToken? visibility)
            {
                var diags = Diagnostics();

                var macro = Expect(SyntaxTokenKind.MacroKeyword, "'macro' keyword", ref diags);
                var name = Expect(SyntaxTokenKind.ValueIdentifier, "value identifier", ref diags);
                var parms = ParseMacroParameterList();
                var body = ParseBlockExpression();

                return new MacroDeclarationNode(Skipped(), diags, List(attributes), visibility, macro, name, parms,
                    body);
            }

            MacroParameterListNode ParseMacroParameterList()
            {
                var diags = Diagnostics();

                var open = Expect(SyntaxTokenKind.OpenParen, "'('", ref diags);
                var parms = ImmutableArray<MacroParameterNode>.Empty;
                var seps = ImmutableArray<SyntaxToken>.Empty;
                var first = true;

                while (_stream.Peek() is var tok && !tok.IsEndOfInput && tok.Kind != SyntaxTokenKind.CloseParen)
                {
                    if (!first)
                    {
                        if (_stream.Peek().Kind != SyntaxTokenKind.Comma)
                            break;

                        seps = seps.Add(_stream.Move());
                    }

                    var param = ParseMacroParameter();
                    var stop = param.NameToken.IsMissing && _stream.Peek().Kind != SyntaxTokenKind.Comma;

                    if (first && stop)
                        break;

                    parms = parms.Add(param);
                    first = false;

                    if (stop)
                        break;
                }

                var close = Expect(SyntaxTokenKind.CloseParen, "')'", ref diags);

                return new MacroParameterListNode(Skipped(), diags, open, List(parms, seps), close);
            }

            MacroParameterNode ParseMacroParameter()
            {
                var diags = Diagnostics();

                var attrs = ParseAttributes();
                var name = Expect(SyntaxTokenKind.FragmentIdentifier, "fragment identifier", ref diags);

                return new MacroParameterNode(Skipped(), diags, List(attrs), name);
            }

            StatementNode ParseStatement()
            {
                return _stream.Peek().Kind switch
                {
                    SyntaxTokenKind.LetKeyword => ParseLetStatement(),
                    SyntaxTokenKind.UseKeyword => ParseUseStatement(),
                    _ => (StatementNode)ParseExpressionStatement(),
                };
            }

            LetStatementNode ParseLetStatement()
            {
                var diags = Diagnostics();

                var let = Expect(SyntaxTokenKind.LetKeyword, "'let' keyword", ref diags);
                var pat = ParsePattern();
                var eq = Expect(SyntaxTokenKind.Equals, "'='", ref diags);
                var init = ParseExpression();
                var semi = Expect(SyntaxTokenKind.Semicolon, "';'", ref diags);

                return new LetStatementNode(Skipped(), diags, let, pat, eq, init, semi);
            }

            UseStatementNode ParseUseStatement()
            {
                var diags = Diagnostics();

                var use = Expect(SyntaxTokenKind.UseKeyword, "'use' keyword", ref diags);
                var pat = ParsePattern();
                var eq = Expect(SyntaxTokenKind.Equals, "'='", ref diags);
                var init = ParseExpression();
                var semi = Expect(SyntaxTokenKind.Semicolon, "';'", ref diags);

                return new UseStatementNode(Skipped(), diags, use, pat, eq, init, semi);
            }

            ExpressionStatementNode ParseExpressionStatement()
            {
                var diags = Diagnostics();

                var expr = ParseExpression();
                var semi = Expect(SyntaxTokenKind.Semicolon, "';'", ref diags);

                return new ExpressionStatementNode(Skipped(), diags, expr, semi);
            }

            ExpressionNode ParseExpression()
            {
                return ParseSendExpression();
            }

            ExpressionNode ParseSendExpression()
            {
                var expr = ParseAssignExpression();

                while (_stream.Peek().Kind == SyntaxTokenKind.OpenAngleMinus)
                {
                    var op = _stream.Move();
                    var right = ParseSendExpression();

                    expr = new SendExpressionNode(Skipped(), Diagnostics(), expr, op, right);
                }

                return expr;
            }

            ExpressionNode ParseAssignExpression()
            {
                var expr = ParseLogicalExpression();

                while (_stream.Peek().Kind == SyntaxTokenKind.Equals)
                {
                    var op = _stream.Move();
                    var right = ParseAssignExpression();

                    expr = new AssignExpressionNode(Skipped(), Diagnostics(), expr, op, right);
                }

                return expr;
            }

            ExpressionNode ParseLogicalExpression()
            {
                var expr = ParseRelationalExpression();

                while (_stream.Peek() is var tok && !tok.IsEndOfInput)
                {
                    switch (tok.Kind)
                    {
                        case SyntaxTokenKind.AndKeyword:
                        case SyntaxTokenKind.OrKeyword:
                            var op = _stream.Move();
                            var right = ParseRelationalExpression();

                            expr = new LogicalExpressionNode(Skipped(), Diagnostics(), expr, op, right);
                            continue;
                        default:
                            break;
                    }

                    break;
                }

                return expr;
            }

            ExpressionNode ParseRelationalExpression()
            {
                var expr = ParseBitwiseExpression();

                while (_stream.Peek() is var tok && !tok.IsEndOfInput)
                {
                    switch (tok.Kind)
                    {
                        case SyntaxTokenKind.ExclamationEquals:
                        case SyntaxTokenKind.OpenAngle:
                        case SyntaxTokenKind.OpenAngleEquals:
                        case SyntaxTokenKind.EqualsEquals:
                        case SyntaxTokenKind.CloseAngle:
                        case SyntaxTokenKind.CloseAngleEquals:
                            var op = _stream.Move();
                            var right = ParseBitwiseExpression();

                            expr = new RelationalExpressionNode(Skipped(), Diagnostics(), expr, op, right);
                            continue;
                        default:
                            break;
                    }

                    break;
                }

                return expr;
            }

            ExpressionNode ParseBitwiseExpression()
            {
                var expr = ParseShiftExpression();

                while (_stream.Peek().Kind == SyntaxTokenKind.BitwiseOperator)
                {
                    var op = _stream.Move();
                    var right = ParseShiftExpression();

                    expr = new BitwiseExpressionNode(Skipped(), Diagnostics(), expr, op, right);
                }

                return expr;
            }

            ExpressionNode ParseShiftExpression()
            {
                var expr = ParseAdditiveExpression();

                while (_stream.Peek().Kind == SyntaxTokenKind.ShiftOperator)
                {
                    var op = _stream.Move();
                    var right = ParseAdditiveExpression();

                    expr = new ShiftExpressionNode(Skipped(), Diagnostics(), expr, op, right);
                }

                return expr;
            }

            ExpressionNode ParseAdditiveExpression()
            {
                var expr = ParseMultiplicativeExpression();

                while (_stream.Peek().Kind == SyntaxTokenKind.AdditiveOperator)
                {
                    var op = _stream.Move();
                    var right = ParseMultiplicativeExpression();

                    expr = new AdditiveExpressionNode(Skipped(), Diagnostics(), expr, op, right);
                }

                return expr;
            }

            ExpressionNode ParseMultiplicativeExpression()
            {
                var expr = ParsePrefixExpression();

                while (_stream.Peek().Kind == SyntaxTokenKind.MultiplicativeOperator)
                {
                    var op = _stream.Move();
                    var right = ParsePrefixExpression();

                    expr = new MultiplicativeExpressionNode(Skipped(), Diagnostics(), expr, op, right);
                }

                return expr;
            }

            ExpressionNode ParsePrefixExpression()
            {
                switch (_stream.Peek().Kind)
                {
                    case SyntaxTokenKind.MultiplicativeOperator:
                    case SyntaxTokenKind.AdditiveOperator:
                    case SyntaxTokenKind.ShiftOperator:
                    case SyntaxTokenKind.BitwiseOperator:
                        var op = _stream.Move();
                        var oper = ParsePrefixExpression();

                        return new UnaryExpressionNode(Skipped(), Diagnostics(), op, oper);
                    case SyntaxTokenKind.AssertKeyword:
                        return ParseAssertExpression();
                    default:
                        return ParsePrimaryExpression();
                }
            }

            AssertExpressionNode ParseAssertExpression()
            {
                var diags = Diagnostics();

                var assert = Expect(SyntaxTokenKind.AssertKeyword, "'assert' keyword", ref diags);
                var oper = ParsePrefixExpression();

                return new AssertExpressionNode(Skipped(), diags, assert, oper);
            }

            ExpressionNode ParsePrimaryExpression()
            {
                var diags = Diagnostics();

                var tok = _stream.Peek();

                ExpressionNode expr;

                switch (tok.Kind)
                {
                    case SyntaxTokenKind.Hash:
                        var htok2 = _stream.Peek(2);

                        switch (htok2.Kind)
                        {
                            case SyntaxTokenKind.OpenBracket:
                                expr = ParseMapExpression();
                                break;
                            case SyntaxTokenKind.OpenBrace:
                                expr = ParseSetExpression();
                                break;
                            default:
                                Error(ref diags, SyntaxDiagnosticKind.MissingExpression, htok2.Location,
                                    $"Expected set or map expression, but found {(htok2.IsEndOfInput ? "end of input" : $"'{htok2}'")}");

                                return new MissingExpressionNode(Skipped(), diags);
                        }

                        break;
                    case SyntaxTokenKind.OpenParen:
                        expr = ParseParenthesizedOrTupleExpression();
                        break;
                    case SyntaxTokenKind.OpenBracket:
                        expr = ParseArrayExpression();
                        break;
                    case SyntaxTokenKind.OpenBrace:
                        expr = ParseBlockExpression();
                        break;
                    case SyntaxTokenKind.BreakKeyword:
                        expr = ParseBreakExpression();
                        break;
                    case SyntaxTokenKind.CondKeyword:
                        expr = ParseConditionExpression();
                        break;
                    case SyntaxTokenKind.ExcKeyword:
                        expr = ParseExceptionExpression();
                        break;
                    case SyntaxTokenKind.FnKeyword:
                        expr = ParseLambdaExpression();
                        break;
                    case SyntaxTokenKind.ForKeyword:
                        expr = ParseForExpression();
                        break;
                    case SyntaxTokenKind.FreezeKeyword:
                        expr = ParseFreezeExpression();
                        break;
                    case SyntaxTokenKind.IfKeyword:
                        expr = ParseIfExpression();
                        break;
                    case SyntaxTokenKind.LoopKeyword:
                        expr = ParseLoopExpression();
                        break;
                    case SyntaxTokenKind.MatchKeyword:
                        expr = ParseMatchExpression();
                        break;
                    case SyntaxTokenKind.MutKeyword:
                        var mtok2 = _stream.Peek(2);

                        switch (mtok2.Kind)
                        {
                            case SyntaxTokenKind.Hash:
                                var mtok3 = _stream.Peek(3);

                                switch (mtok3.Kind)
                                {
                                    case SyntaxTokenKind.OpenBracket:
                                        expr = ParseMapExpression();
                                        break;
                                    case SyntaxTokenKind.OpenBrace:
                                        expr = ParseSetExpression();
                                        break;
                                    default:
                                        Error(ref diags, SyntaxDiagnosticKind.MissingExpression, mtok3.Location,
                                            $"Expected set or map expression, but found {(mtok3.IsEndOfInput ? "end of input" : $"'{mtok3}'")}");

                                        return new MissingExpressionNode(Skipped(), diags);
                                }

                                break;
                            case SyntaxTokenKind.OpenBracket:
                                expr = ParseArrayExpression();
                                break;
                            default:
                                Error(ref diags, SyntaxDiagnosticKind.MissingExpression, mtok2.Location,
                                    $"Expected array, set, or map expression, but found {(mtok2.IsEndOfInput ? "end of input" : $"'{mtok2}'")}");

                                return new MissingExpressionNode(Skipped(), diags);
                        }

                        break;
                    case SyntaxTokenKind.RaiseKeyword:
                        expr = ParseRaiseExpression();
                        break;
                    case SyntaxTokenKind.RecKeyword:
                        expr = ParseRecordExpression();
                        break;
                    case SyntaxTokenKind.RecvKeyword:
                        expr = ParseReceiveExpression();
                        break;
                    case SyntaxTokenKind.ReturnKeyword:
                        expr = ParseReturnExpression();
                        break;
                    case SyntaxTokenKind.WhileKeyword:
                        expr = ParseWhileExpression();
                        break;
                    case SyntaxTokenKind.ModuleIdentifier:
                        expr = ParseModuleExpression();
                        break;
                    case SyntaxTokenKind.ValueIdentifier:
                        expr = ParseIdentifierOrMacroCallExpression();
                        break;
                    case SyntaxTokenKind.FragmentIdentifier:
                        expr = ParseFragmentExpression();
                        break;
                    case SyntaxTokenKind.NilLiteral:
                    case SyntaxTokenKind.BooleanLiteral:
                    case SyntaxTokenKind.AtomLiteral:
                    case SyntaxTokenKind.IntegerLiteral:
                    case SyntaxTokenKind.RealLiteral:
                    case SyntaxTokenKind.StringLiteral:
                        expr = ParseLiteralExpression();
                        break;
                    default:
                        Error(ref diags, SyntaxDiagnosticKind.MissingExpression, tok.Location,
                            $"Expected expression, but found {(tok.IsEndOfInput ? "end of input" : $"'{tok}'")}");

                        return new MissingExpressionNode(Skipped(), diags);
                }

                return ParsePostfixExpression(expr);
            }

            MapExpressionNode ParseMapExpression()
            {
                var diags = Diagnostics();

                var mut = Optional1(SyntaxTokenKind.MutKeyword);
                var hash = Expect(SyntaxTokenKind.Hash, "'#'", ref diags);
                var open = Expect(SyntaxTokenKind.OpenBracket, "'['", ref diags);
                var elems = ImmutableArray<MapExpressionPairNode>.Empty;
                var seps = ImmutableArray<SyntaxToken>.Empty;
                var first = true;

                while (_stream.Peek() is var tok && !tok.IsEndOfInput && tok.Kind != SyntaxTokenKind.CloseBracket)
                {
                    if (!first)
                    {
                        if (_stream.Peek().Kind != SyntaxTokenKind.Comma)
                            break;

                        seps = seps.Add(_stream.Move());
                    }

                    var pair = ParseMapExpressionPair();
                    var stop = pair.Value is MissingExpressionNode && _stream.Peek().Kind != SyntaxTokenKind.Comma;

                    if (first && stop)
                        break;

                    elems = elems.Add(pair);
                    first = false;

                    if (stop)
                        break;
                }

                var close = Expect(SyntaxTokenKind.CloseBracket, "']'", ref diags);

                return new MapExpressionNode(Skipped(), diags, mut, hash, open, List(elems, seps), close);
            }

            MapExpressionPairNode ParseMapExpressionPair()
            {
                var diags = Diagnostics();

                var key = ParseExpression();
                var colon = Expect(SyntaxTokenKind.Colon, "':'", ref diags);
                var value = ParseExpression();

                return new MapExpressionPairNode(Skipped(), diags, key, colon, value);
            }

            SetExpressionNode ParseSetExpression()
            {
                var diags = Diagnostics();

                var mut = Optional1(SyntaxTokenKind.MutKeyword);
                var hash = Expect(SyntaxTokenKind.Hash, "'#'", ref diags);
                var open = Expect(SyntaxTokenKind.OpenBrace, "'{'", ref diags);
                var elems = ImmutableArray<ExpressionNode>.Empty;
                var seps = ImmutableArray<SyntaxToken>.Empty;
                var first = true;

                while (_stream.Peek() is var tok && !tok.IsEndOfInput && tok.Kind != SyntaxTokenKind.CloseBrace)
                {
                    if (!first)
                    {
                        if (_stream.Peek().Kind != SyntaxTokenKind.Comma)
                            break;

                        seps = seps.Add(_stream.Move());
                    }

                    var expr = ParseExpression();
                    var stop = expr is MissingExpressionNode && _stream.Peek().Kind != SyntaxTokenKind.Comma;

                    if (first && stop)
                        break;

                    elems = elems.Add(expr);
                    first = false;

                    if (stop)
                        break;
                }

                var close = Expect(SyntaxTokenKind.CloseBrace, "'}'", ref diags);

                return new SetExpressionNode(Skipped(), diags, mut, hash, open, List(elems, seps), close);
            }

            PrimaryExpressionNode ParseParenthesizedOrTupleExpression()
            {
                var diags = Diagnostics();

                var open = Expect(SyntaxTokenKind.OpenParen, "'('", ref diags);
                var expr = ParseExpression();

                if (_stream.Peek().Kind == SyntaxTokenKind.Comma)
                {
                    var comps = ImmutableArray<ExpressionNode>.Empty;
                    var seps = ImmutableArray<SyntaxToken>.Empty;

                    comps = comps.Add(expr);

                    do
                    {
                        seps = seps.Add(Expect(SyntaxTokenKind.Comma, "','", ref diags));

                        var expr2 = ParseExpression();

                        comps = comps.Add(expr2);

                        if (expr2 is MissingExpressionNode && _stream.Peek().Kind != SyntaxTokenKind.Comma)
                            break;
                    }
                    while (_stream.Peek() is var tok && !tok.IsEndOfInput && tok.Kind != SyntaxTokenKind.CloseParen);

                    var close1 = Expect(SyntaxTokenKind.CloseParen, "')'", ref diags);

                    return new TupleExpressionNode(Skipped(), diags, open, List(comps, seps), close1);
                }

                var close2 = Expect(SyntaxTokenKind.CloseParen, "')'", ref diags);

                return new ParenthesizedExpressionNode(Skipped(), diags, open, expr, close2);
            }

            ArrayExpressionNode ParseArrayExpression()
            {
                var diags = Diagnostics();

                var mut = Optional1(SyntaxTokenKind.MutKeyword);
                var open = Expect(SyntaxTokenKind.OpenBracket, "'['", ref diags);
                var elems = ImmutableArray<ExpressionNode>.Empty;
                var seps = ImmutableArray<SyntaxToken>.Empty;
                var first = true;

                while (_stream.Peek() is var tok && !tok.IsEndOfInput && tok.Kind != SyntaxTokenKind.CloseBracket)
                {
                    if (!first)
                    {
                        if (_stream.Peek().Kind != SyntaxTokenKind.Comma)
                            break;

                        seps = seps.Add(_stream.Move());
                    }

                    var expr = ParseExpression();
                    var stop = expr is MissingExpressionNode && _stream.Peek().Kind != SyntaxTokenKind.Comma;

                    if (first && stop)
                        break;

                    elems = elems.Add(expr);
                    first = false;

                    if (stop)
                        break;
                }

                var close = Expect(SyntaxTokenKind.CloseBracket, "']'", ref diags);

                return new ArrayExpressionNode(Skipped(), diags, mut, open, List(elems, seps), close);
            }

            BlockExpressionNode ParseBlockExpression()
            {
                var skipped = Skipped();
                var diags = Diagnostics();

                var open = Expect(SyntaxTokenKind.OpenBrace, "'{'", ref diags);
                var stmts = ImmutableArray<StatementNode>.Empty;

                stmts = stmts.Add(ParseStatement());

                using var ctx = Context(StatementParseContext.Instance);

                while (_stream.Peek() is var tok && !tok.IsEndOfInput && tok.Kind != SyntaxTokenKind.CloseBrace)
                {
                    if (ctx.Context.CanParse(tok))
                        stmts = stmts.Add(ParseStatement());
                    else if (CanOuterParse(tok))
                        break;
                    else
                        Skip(ref skipped);
                }

                var close = Expect(SyntaxTokenKind.CloseBrace, "'}'", ref diags);

                return new BlockExpressionNode(skipped, diags, open, List(stmts), close);
            }

            BreakExpressionNode ParseBreakExpression()
            {
                var diags = Diagnostics();

                var brk = Expect(SyntaxTokenKind.BreakKeyword, "'break' keyword", ref diags);

                return new BreakExpressionNode(Skipped(), diags, brk);
            }

            ConditionExpressionNode ParseConditionExpression()
            {
                var diags = Diagnostics();

                var cond = Expect(SyntaxTokenKind.CondKeyword, "'cond' keyword", ref diags);
                var open = Expect(SyntaxTokenKind.OpenBrace, "'{'", ref diags);
                var arms = ImmutableArray<ConditionExpressionArmNode>.Empty;

                do
                {
                    var arm = ParseConditionExpressionArm();

                    arms = arms.Add(arm);

                    if (arm.Condition is MissingExpressionNode && arm.ArrowToken.IsMissing &&
                        arm.Body is MissingExpressionNode)
                        break;
                }
                while (_stream.Peek() is var tok && !tok.IsEndOfInput && tok.Kind != SyntaxTokenKind.CloseBrace);

                var close = Expect(SyntaxTokenKind.CloseBrace, "'}'", ref diags);

                return new ConditionExpressionNode(Skipped(), diags, cond, open, List(arms), close);
            }

            ConditionExpressionArmNode ParseConditionExpressionArm()
            {
                var diags = Diagnostics();

                var cond = ParseExpression();
                var arrow = Expect(SyntaxTokenKind.EqualsCloseAngle, "'=>'", ref diags);
                var body = ParseExpression();

                return new ConditionExpressionArmNode(Skipped(), diags, cond, arrow, body);
            }

            ExceptionExpressionNode ParseExceptionExpression()
            {
                var diags = Diagnostics();

                var exc = Expect(SyntaxTokenKind.ExcKeyword, "'exc' keyword", ref diags);
                var name = Expect(SyntaxTokenKind.ModuleIdentifier, "module identifier", ref diags);
                var open = Expect(SyntaxTokenKind.OpenBrace, "'{'", ref diags);
                var fields = ImmutableArray<ExpressionFieldNode>.Empty;
                var seps = ImmutableArray<SyntaxToken>.Empty;
                var first = true;

                while (_stream.Peek() is var tok && !tok.IsEndOfInput && tok.Kind != SyntaxTokenKind.CloseBrace)
                {
                    if (!first)
                    {
                        if (_stream.Peek().Kind != SyntaxTokenKind.Comma)
                            break;

                        seps = seps.Add(_stream.Move());
                    }

                    var field = ParseExpressionField();
                    var stop = field.Value is MissingExpressionNode && _stream.Peek().Kind != SyntaxTokenKind.Comma;

                    if (first && stop)
                        break;

                    fields = fields.Add(field);
                    first = false;

                    if (stop)
                        break;
                }

                var close = Expect(SyntaxTokenKind.CloseBrace, "'}'", ref diags);

                return new ExceptionExpressionNode(Skipped(), diags, exc, name, open, List(fields, seps), close);
            }

            ExpressionFieldNode ParseExpressionField()
            {
                var diags = Diagnostics();

                var mut = Optional1(SyntaxTokenKind.MutKeyword);
                var name = Expect(SyntaxTokenKind.ValueIdentifier, "value identifier", ref diags);
                var eq = Expect(SyntaxTokenKind.Equals, "'='", ref diags);
                var value = ParseExpression();

                return new ExpressionFieldNode(Skipped(), diags, mut, name, eq, value);
            }

            LambdaExpressionNode ParseLambdaExpression()
            {
                var diags = Diagnostics();

                var fn = Expect(SyntaxTokenKind.FnKeyword, "'fn' keyword", ref diags);
                var parms = ParseLambdaParameterList();
                var arrow = Expect(SyntaxTokenKind.EqualsCloseAngle, "'=>'", ref diags);
                var body = ParseExpression();

                return new LambdaExpressionNode(Skipped(), diags, fn, parms, arrow, body);
            }

            LambdaParameterListNode ParseLambdaParameterList()
            {
                var diags = Diagnostics();

                var open = Expect(SyntaxTokenKind.OpenParen, "'('", ref diags);
                var parms = ImmutableArray<LambdaParameterNode>.Empty;
                var seps = ImmutableArray<SyntaxToken>.Empty;
                var first = true;

                while (_stream.Peek() is var tok && !tok.IsEndOfInput && tok.Kind != SyntaxTokenKind.CloseParen)
                {
                    if (!first)
                    {
                        if (_stream.Peek().Kind != SyntaxTokenKind.Comma)
                            break;

                        seps = seps.Add(_stream.Move());
                    }

                    var param = ParseLambdaParameter();
                    var stop = param.NameToken.IsMissing && _stream.Peek().Kind != SyntaxTokenKind.Comma;

                    if (first && stop)
                        break;

                    parms = parms.Add(param);
                    first = false;

                    if (stop || param.DotDotToken != null)
                        break;
                }

                var close = Expect(SyntaxTokenKind.CloseParen, "')'", ref diags);

                return new LambdaParameterListNode(Skipped(), diags, open, List(parms, seps), close);
            }

            LambdaParameterNode ParseLambdaParameter()
            {
                var diags = Diagnostics();

                var dotDot = Optional1(SyntaxTokenKind.DotDot);
                var name = Expect(SyntaxTokenKind.ValueIdentifier, "value identifier", ref diags);

                return new LambdaParameterNode(Skipped(), diags, dotDot, name);
            }

            ForExpressionNode ParseForExpression()
            {
                var diags = Diagnostics();

                var @for = Expect(SyntaxTokenKind.ForKeyword, "'for' keyword", ref diags);
                var pat = ParsePattern();
                var @in = Expect(SyntaxTokenKind.InKeyword, "'in' keyword", ref diags);
                var col = ParseExpression();
                var body = ParseBlockExpression();

                return new ForExpressionNode(Skipped(), diags, @for, pat, @in, col, body);
            }

            FreezeExpressionNode ParseFreezeExpression()
            {
                var diags = Diagnostics();

                var freeze = Expect(SyntaxTokenKind.FreezeKeyword, "'freeze' keyword", ref diags);
                var @in = Optional1(SyntaxTokenKind.InKeyword);
                var oper = ParseExpression();

                return new FreezeExpressionNode(Skipped(), diags, freeze, @in, oper);
            }

            IfExpressionNode ParseIfExpression()
            {
                var diags = Diagnostics();

                var @if = Expect(SyntaxTokenKind.IfKeyword, "'if' keyword", ref diags);
                var cond = ParseExpression();
                var body = ParseBlockExpression();
                var @else = _stream.Peek().Kind == SyntaxTokenKind.ElseKeyword ? ParseIfExpressionElse() : null;

                return new IfExpressionNode(Skipped(), diags, @if, cond, body, @else);
            }

            IfExpressionElseNode ParseIfExpressionElse()
            {
                var diags = Diagnostics();

                var @else = Expect(SyntaxTokenKind.ElseKeyword, "'else' keyword", ref diags);
                var body = _stream.Peek().Kind == SyntaxTokenKind.IfKeyword ?
                    ParseIfExpression() : (PrimaryExpressionNode)ParseBlockExpression();

                return new IfExpressionElseNode(Skipped(), diags, @else, body);
            }

            LoopExpressionNode ParseLoopExpression()
            {
                var diags = Diagnostics();

                var loop = Expect(SyntaxTokenKind.LoopKeyword, "'loop' keyword", ref diags);

                return new LoopExpressionNode(Skipped(), diags, loop);
            }

            MatchExpressionNode ParseMatchExpression()
            {
                var skipped = Skipped();
                var diags = Diagnostics();

                var match = Expect(SyntaxTokenKind.MatchKeyword, "'match' keyword", ref diags);
                var oper = ParseExpression();
                var open = Expect(SyntaxTokenKind.OpenBrace, "'{'", ref diags);
                var arms = ImmutableArray<PatternArmNode>.Empty;

                do
                {
                    var arm = ParsePatternArm(false);

                    arms = arms.Add(arm);

                    if (arm.Pattern is MissingPatternNode && arm.ArrowToken.IsMissing &&
                        arm.Body is MissingExpressionNode)
                        break;
                }
                while (_stream.Peek() is var tok && !tok.IsEndOfInput && tok.Kind != SyntaxTokenKind.CloseBrace);

                var close = Expect(SyntaxTokenKind.CloseBrace, "'}'", ref diags);

                return new MatchExpressionNode(skipped, diags, match, oper, open, List(arms), close);
            }

            PatternArmNode ParsePatternArm(bool tryElse)
            {
                var diags = Diagnostics();

                var pat = tryElse ? ParseTryElsePattern() : ParsePattern();
                var guard = _stream.Peek().Kind == SyntaxTokenKind.IfKeyword ? ParsePatternArmGuard() : null;
                var arrow = Expect(SyntaxTokenKind.EqualsCloseAngle, "'=>'", ref diags);
                var body = ParseExpression();

                return new PatternArmNode(Skipped(), diags, pat, guard, arrow, body);
            }

            PatternArmGuardNode ParsePatternArmGuard()
            {
                var diags = Diagnostics();

                var @if = Expect(SyntaxTokenKind.IfKeyword, "'if' keyword", ref diags);
                var cond = ParseExpression();

                return new PatternArmGuardNode(Skipped(), diags, @if, cond);
            }

            RaiseExpressionNode ParseRaiseExpression()
            {
                var diags = Diagnostics();

                var raise = Expect(SyntaxTokenKind.RaiseKeyword, "'raise' keyword", ref diags);
                var oper = ParseExpression();

                return new RaiseExpressionNode(Skipped(), diags, raise, oper);
            }

            RecordExpressionNode ParseRecordExpression()
            {
                var diags = Diagnostics();

                var rec = Expect(SyntaxTokenKind.RecKeyword, "'rec' keyword", ref diags);
                var name = Optional1(SyntaxTokenKind.ModuleIdentifier);
                var open = Expect(SyntaxTokenKind.OpenBrace, "'{'", ref diags);
                var fields = ImmutableArray<ExpressionFieldNode>.Empty;
                var seps = ImmutableArray<SyntaxToken>.Empty;
                var first = true;

                while (_stream.Peek() is var tok && !tok.IsEndOfInput && tok.Kind != SyntaxTokenKind.CloseBrace)
                {
                    if (!first)
                    {
                        if (_stream.Peek().Kind != SyntaxTokenKind.Comma)
                            break;

                        seps = seps.Add(_stream.Move());
                    }

                    var field = ParseExpressionField();
                    var stop = field.Value is MissingExpressionNode && _stream.Peek().Kind != SyntaxTokenKind.Comma;

                    if (first && stop)
                        break;

                    fields = fields.Add(field);
                    first = false;

                    if (stop)
                        break;
                }

                var close = Expect(SyntaxTokenKind.CloseBrace, "'}'", ref diags);

                return new RecordExpressionNode(Skipped(), diags, rec, name, open, List(fields, seps), close);
            }

            ReceiveExpressionNode ParseReceiveExpression()
            {
                var diags = Diagnostics();

                var recv = Expect(SyntaxTokenKind.RecvKeyword, "'recv' keyword", ref diags);
                var open = Expect(SyntaxTokenKind.OpenBrace, "'{'", ref diags);
                var arms = ImmutableArray<PatternArmNode>.Empty;

                do
                {
                    var arm = ParsePatternArm(false);

                    arms = arms.Add(arm);

                    if (arm.Pattern is MissingPatternNode && arm.ArrowToken.IsMissing &&
                        arm.Body is MissingExpressionNode)
                        break;
                }
                while (_stream.Peek() is var tok && !tok.IsEndOfInput && tok.Kind != SyntaxTokenKind.CloseBrace);

                var close = Expect(SyntaxTokenKind.CloseBrace, "'}'", ref diags);
                var @else = _stream.Peek().Kind == SyntaxTokenKind.ElseKeyword ? ParseReceiveExpressionElse() : null;

                return new ReceiveExpressionNode(Skipped(), diags, recv, open, List(arms), close, @else);
            }

            ReceiveExpressionElseNode ParseReceiveExpressionElse()
            {
                var diags = Diagnostics();

                var @else = Expect(SyntaxTokenKind.ElseKeyword, "'else' keyword", ref diags);
                var body = ParseBlockExpression();

                return new ReceiveExpressionElseNode(Skipped(), diags, @else, body);
            }

            ReturnExpressionNode ParseReturnExpression()
            {
                var diags = Diagnostics();

                var ret = Expect(SyntaxTokenKind.ReturnKeyword, "'return' keyword", ref diags);
                var oper = ParseExpression();

                return new ReturnExpressionNode(Skipped(), diags, ret, oper);
            }

            WhileExpressionNode ParseWhileExpression()
            {
                var diags = Diagnostics();

                var @while = Expect(SyntaxTokenKind.WhileKeyword, "'while' keyword", ref diags);
                var cond = ParseExpression();
                var body = ParseBlockExpression();

                return new WhileExpressionNode(Skipped(), diags, @while, cond, body);
            }

            ModuleExpressionNode ParseModuleExpression()
            {
                var path = ParseModulePath();

                return new ModuleExpressionNode(Skipped(), Diagnostics(), path);
            }

            PrimaryExpressionNode ParseIdentifierOrMacroCallExpression()
            {
                var diags = Diagnostics();

                var ident = Expect(SyntaxTokenKind.ValueIdentifier, "value identifier", ref diags);

                if (_stream.Peek().Kind == SyntaxTokenKind.Exclamation)
                {
                    var bang = _stream.Move();
                    var args = ParseMacroArgumentList();

                    return new MacroCallExpressionNode(Skipped(), diags, ident, bang, args);
                }

                return new IdentifierExpressionNode(Skipped(), diags, ident);
            }

            MacroArgumentListNode ParseMacroArgumentList()
            {
                var diags = Diagnostics();

                var open = Expect(SyntaxTokenKind.OpenParen, "'('", ref diags);
                var args = ImmutableArray<ExpressionNode>.Empty;
                var seps = ImmutableArray<SyntaxToken>.Empty;
                var first = true;

                while (_stream.Peek() is var tok && !tok.IsEndOfInput && tok.Kind != SyntaxTokenKind.CloseParen)
                {
                    if (!first)
                    {
                        if (_stream.Peek().Kind != SyntaxTokenKind.Comma)
                            break;

                        seps = seps.Add(_stream.Move());
                    }

                    var arg = ParseExpression();
                    var stop = arg is MissingExpressionNode && _stream.Peek().Kind != SyntaxTokenKind.Comma;

                    if (first && stop)
                        break;

                    args = args.Add(arg);
                    first = false;

                    if (stop)
                        break;
                }

                var close = Expect(SyntaxTokenKind.CloseParen, "')'", ref diags);

                return new MacroArgumentListNode(Skipped(), diags, open, List(args, seps), close);
            }

            FragmentExpressionNode ParseFragmentExpression()
            {
                var diags = Diagnostics();

                var ident = Expect(SyntaxTokenKind.FragmentIdentifier, "fragment identifier", ref diags);

                return new FragmentExpressionNode(Skipped(), diags, ident);
            }

            LiteralExpressionNode ParseLiteralExpression()
            {
                var diags = Diagnostics();

                var value = ExpectLiteral(ref diags);

                return new LiteralExpressionNode(Skipped(), diags, value);
            }

            ExpressionNode ParsePostfixExpression(ExpressionNode subject)
            {
                while (_stream.Peek() is var tok && !tok.IsEndOfInput)
                {
                    switch (tok.Kind)
                    {
                        case SyntaxTokenKind.OpenParen:
                            subject = ParseCallExpression(subject);
                            continue;
                        case SyntaxTokenKind.Dot:
                            subject = ParseFieldAccessExpression(subject);
                            continue;
                        case SyntaxTokenKind.MinusCloseAngle:
                            subject = ParseMethodCallExpression(subject);
                            continue;
                        case SyntaxTokenKind.OpenBracket:
                            subject = ParseIndexExpression(subject);
                            continue;
                        default:
                            break;
                    }

                    break;
                }

                return subject;
            }

            CallExpressionNode ParseCallExpression(ExpressionNode subject)
            {
                var diags = Diagnostics();

                var args = ParseArgumentList();
                var @try = _stream.Peek().Kind == SyntaxTokenKind.Question ? ParseCallTry() : null;

                return new CallExpressionNode(Skipped(), diags, subject, args, @try);
            }

            CallTryNode ParseCallTry()
            {
                var diags = Diagnostics();

                var question = Expect(SyntaxTokenKind.Question, "'?'", ref diags);
                var @catch = _stream.Peek().Kind == SyntaxTokenKind.CatchKeyword ? ParseCallTryCatch() : null;

                return new CallTryNode(Skipped(), diags, question, @catch);
            }

            CallTryCatchNode ParseCallTryCatch()
            {
                var diags = Diagnostics();

                var @catch = Expect(SyntaxTokenKind.CatchKeyword, "'catch' keyword", ref diags);
                var open = Expect(SyntaxTokenKind.OpenBrace, "'{'", ref diags);
                var arms = ImmutableArray<PatternArmNode>.Empty;

                do
                {
                    var arm = ParsePatternArm(true);

                    arms = arms.Add(arm);

                    if (arm.Pattern is MissingPatternNode && arm.ArrowToken.IsMissing &&
                        arm.Body is MissingExpressionNode)
                        break;
                }
                while (_stream.Peek() is var tok && !tok.IsEndOfInput && tok.Kind != SyntaxTokenKind.CloseBrace);

                var close = Expect(SyntaxTokenKind.CloseBrace, "'}'", ref diags);

                return new CallTryCatchNode(Skipped(), diags, @catch, open, List(arms), close);
            }

            ArgumentListNode ParseArgumentList()
            {
                var diags = Diagnostics();

                var open = Expect(SyntaxTokenKind.OpenParen, "'('", ref diags);
                var args = ImmutableArray<ArgumentNode>.Empty;
                var seps = ImmutableArray<SyntaxToken>.Empty;
                var first = true;

                while (_stream.Peek() is var tok && !tok.IsEndOfInput && tok.Kind != SyntaxTokenKind.CloseParen)
                {
                    if (!first)
                    {
                        if (_stream.Peek().Kind != SyntaxTokenKind.Comma)
                            break;

                        seps = seps.Add(_stream.Move());
                    }

                    var arg = ParseArgument();
                    var stop = arg.Value is MissingExpressionNode && _stream.Peek().Kind != SyntaxTokenKind.Comma;

                    if (first && stop)
                        break;

                    args = args.Add(arg);
                    first = false;

                    if (stop || arg.DotDotToken != null)
                        break;
                }

                var close = Expect(SyntaxTokenKind.CloseParen, "')'", ref diags);

                return new ArgumentListNode(Skipped(), diags, open, List(args, seps), close);
            }

            ArgumentNode ParseArgument()
            {
                var diags = Diagnostics();

                var dotDot = Optional1(SyntaxTokenKind.DotDot);
                var value = ParseExpression();

                return new ArgumentNode(Skipped(), diags, dotDot, value);
            }

            FieldAccessExpressionNode ParseFieldAccessExpression(ExpressionNode subject)
            {
                var diags = Diagnostics();

                var dot = Expect(SyntaxTokenKind.Dot, "'.'", ref diags);
                var name = Expect(SyntaxTokenKind.ValueIdentifier, "value identifier", ref diags);

                return new FieldAccessExpressionNode(Skipped(), diags, subject, dot, name);
            }

            MethodCallExpressionNode ParseMethodCallExpression(ExpressionNode subject)
            {
                var diags = Diagnostics();

                var arrow = Expect(SyntaxTokenKind.MinusCloseAngle, "'->'", ref diags);
                var name = Expect(SyntaxTokenKind.ValueIdentifier, "value identifier", ref diags);
                var args = ParseArgumentList();
                var @try = ParseCallTry();

                return new MethodCallExpressionNode(Skipped(), diags, subject, arrow, name, args, @try);
            }

            IndexExpressionNode ParseIndexExpression(ExpressionNode subject)
            {
                var diags = Diagnostics();

                var idxs = ParseIndexList();

                return new IndexExpressionNode(Skipped(), diags, subject, idxs);
            }

            IndexListNode ParseIndexList()
            {
                var diags = Diagnostics();

                var open = Expect(SyntaxTokenKind.OpenParen, "'('", ref diags);
                var idxs = ImmutableArray<IndexNode>.Empty;
                var seps = ImmutableArray<SyntaxToken>.Empty;
                var first = true;

                while (_stream.Peek() is var tok && !tok.IsEndOfInput && tok.Kind != SyntaxTokenKind.CloseParen)
                {
                    if (!first)
                    {
                        if (_stream.Peek().Kind != SyntaxTokenKind.Comma)
                            break;

                        seps = seps.Add(_stream.Move());
                    }

                    var idx = ParseIndex();
                    var stop = idx.Value is MissingExpressionNode && _stream.Peek().Kind != SyntaxTokenKind.Comma;

                    if (first && stop)
                        break;

                    idxs = idxs.Add(idx);
                    first = false;

                    if (stop || idx.DotDotToken != null)
                        break;
                }

                var close = Expect(SyntaxTokenKind.CloseParen, "')'", ref diags);

                return new IndexListNode(Skipped(), diags, open, List(idxs, seps), close);
            }

            IndexNode ParseIndex()
            {
                var diags = Diagnostics();

                var dotDot = Optional1(SyntaxTokenKind.DotDot);
                var value = ParseExpression();

                return new IndexNode(Skipped(), diags, dotDot, value);
            }

            PatternNode ParsePattern()
            {
                var diags = Diagnostics();

                var tok = _stream.Peek();

                switch (tok.Kind)
                {
                    case SyntaxTokenKind.Hash:
                        var tok2 = _stream.Peek(2);

                        switch (tok2.Kind)
                        {
                            case SyntaxTokenKind.OpenBracket:
                                return ParseMapPattern();
                            case SyntaxTokenKind.OpenBrace:
                                return ParseSetPattern();
                            default:
                                break;
                        }

                        Error(ref diags, SyntaxDiagnosticKind.MissingPattern, tok2.Location,
                            $"Expected set or map pattern, but found {(tok2.IsEndOfInput ? "end of input" : $"'{tok2}'")}");

                        return new MissingPatternNode(Skipped(), diags, null);
                    case SyntaxTokenKind.OpenParen:
                        return ParseTuplePattern();
                    case SyntaxTokenKind.OpenBracket:
                        return ParseArrayPattern();
                    case SyntaxTokenKind.ExcKeyword:
                        return ParseExceptionPattern();
                    case SyntaxTokenKind.MutKeyword:
                    case SyntaxTokenKind.ValueIdentifier:
                        return ParseIdentifierPattern();
                    case SyntaxTokenKind.RecKeyword:
                        return ParseRecordPattern();
                    case SyntaxTokenKind.ModuleIdentifier:
                        return ParseModulePattern();
                    case SyntaxTokenKind.NilLiteral:
                    case SyntaxTokenKind.BooleanLiteral:
                    case SyntaxTokenKind.AtomLiteral:
                    case SyntaxTokenKind.IntegerLiteral:
                    case SyntaxTokenKind.RealLiteral:
                    case SyntaxTokenKind.StringLiteral:
                        return ParseLiteralPattern();
                    default:
                        Error(ref diags, SyntaxDiagnosticKind.MissingPattern, tok.Location,
                            $"Expected pattern, but found {(tok.IsEndOfInput ? "end of input" : $"'{tok}'")}");

                        return new MissingPatternNode(Skipped(), diags, null);
                }
            }

            PatternNode ParseTryElsePattern()
            {
                var tok = _stream.Peek();

                switch (tok.Kind)
                {
                    case SyntaxTokenKind.ExcKeyword:
                        return ParseExceptionPattern();
                    case SyntaxTokenKind.MutKeyword:
                    case SyntaxTokenKind.ValueIdentifier:
                        return ParseIdentifierPattern();
                    default:
                        var diags = Diagnostics();

                        Error(ref diags, SyntaxDiagnosticKind.MissingPattern, tok.Location,
                            $"Expected pattern, but found {(tok.IsEndOfInput ? "end of input" : $"'{tok}'")}");

                        return new MissingPatternNode(Skipped(), diags, null);
                }
            }

            SetPatternNode ParseSetPattern()
            {
                var diags = Diagnostics();

                var hash = Expect(SyntaxTokenKind.Hash, "'#'", ref diags);
                var open = Expect(SyntaxTokenKind.OpenBrace, "'{'", ref diags);
                var elems = ImmutableArray<ExpressionNode>.Empty;
                var seps = ImmutableArray<SyntaxToken>.Empty;
                var first = true;

                while (_stream.Peek() is var tok && !tok.IsEndOfInput && tok.Kind != SyntaxTokenKind.CloseBrace)
                {
                    if (!first)
                    {
                        if (_stream.Peek().Kind != SyntaxTokenKind.Comma)
                            break;

                        seps = seps.Add(_stream.Move());
                    }

                    var expr = ParseExpression();
                    var stop = expr is MissingExpressionNode && _stream.Peek().Kind != SyntaxTokenKind.Comma;

                    if (first && stop)
                        break;

                    elems = elems.Add(expr);
                    first = false;

                    if (stop)
                        break;
                }

                var close = Expect(SyntaxTokenKind.CloseBrace, "'}'", ref diags);
                var remain = _stream.Peek().Kind == SyntaxTokenKind.ColonColon ? ParsePatternRemainder() : null;
                var alias = _stream.Peek().Kind == SyntaxTokenKind.AsKeyword ? ParsePatternAlias() : null;

                return new SetPatternNode(Skipped(), diags, hash, open, List(elems, seps), close, remain, alias);
            }

            PatternRemainderNode ParsePatternRemainder()
            {
                var diags = Diagnostics();

                var colonColon = Expect(SyntaxTokenKind.ColonColon, "'::'", ref diags);
                var pat = ParsePattern();

                return new PatternRemainderNode(Skipped(), diags, colonColon, pat);
            }

            MapPatternNode ParseMapPattern()
            {
                var diags = Diagnostics();

                var hash = Expect(SyntaxTokenKind.Hash, "'#'", ref diags);
                var open = Expect(SyntaxTokenKind.OpenBracket, "'['", ref diags);
                var elems = ImmutableArray<MapPatternPairNode>.Empty;
                var seps = ImmutableArray<SyntaxToken>.Empty;
                var first = true;

                while (_stream.Peek() is var tok && !tok.IsEndOfInput && tok.Kind != SyntaxTokenKind.CloseBracket)
                {
                    if (!first)
                    {
                        if (_stream.Peek().Kind != SyntaxTokenKind.Comma)
                            break;

                        seps = seps.Add(_stream.Move());
                    }

                    var pair = ParseMapPatternPair();
                    var stop = pair.Value is MissingPatternNode && _stream.Peek().Kind != SyntaxTokenKind.Comma;

                    if (first && stop)
                        break;

                    elems = elems.Add(pair);
                    first = false;

                    if (stop)
                        break;
                }

                var close = Expect(SyntaxTokenKind.CloseBracket, "']'", ref diags);
                var remain = _stream.Peek().Kind == SyntaxTokenKind.ColonColon ? ParsePatternRemainder() : null;
                var alias = _stream.Peek().Kind == SyntaxTokenKind.AsKeyword ? ParsePatternAlias() : null;

                return new MapPatternNode(Skipped(), diags, hash, open, List(elems, seps), close, remain, alias);
            }

            MapPatternPairNode ParseMapPatternPair()
            {
                var diags = Diagnostics();

                var key = ParseExpression();
                var colon = Expect(SyntaxTokenKind.Colon, "':'", ref diags);
                var value = ParsePattern();

                return new MapPatternPairNode(Skipped(), diags, key, colon, value);
            }

            PatternAliasNode ParsePatternAlias()
            {
                var diags = Diagnostics();

                var @as = Expect(SyntaxTokenKind.AsKeyword, "'as' keyword", ref diags);
                var mut = Optional1(SyntaxTokenKind.MutKeyword);
                var name = Expect(SyntaxTokenKind.ValueIdentifier, "value identifier", ref diags);

                return new PatternAliasNode(Skipped(), diags, @as, mut, name);
            }

            TuplePatternNode ParseTuplePattern()
            {
                var diags = Diagnostics();

                var open = Expect(SyntaxTokenKind.OpenParen, "'('", ref diags);
                var pats = ImmutableArray<PatternNode>.Empty;
                var seps = ImmutableArray<SyntaxToken>.Empty;

                pats = pats.Add(ParsePattern());

                do
                {
                    seps = seps.Add(Expect(SyntaxTokenKind.Comma, "','", ref diags));

                    var pat = ParsePattern();

                    pats = pats.Add(pat);

                    if (pat is MissingPatternNode && _stream.Peek().Kind != SyntaxTokenKind.Comma)
                        break;
                }
                while (_stream.Peek() is var tok && !tok.IsEndOfInput && tok.Kind != SyntaxTokenKind.CloseParen);

                var close = Expect(SyntaxTokenKind.CloseParen, "')'", ref diags);
                var alias = _stream.Peek().Kind == SyntaxTokenKind.AsKeyword ? ParsePatternAlias() : null;

                return new TuplePatternNode(Skipped(), diags, open, List(pats, seps), close, alias);
            }

            ArrayPatternNode ParseArrayPattern()
            {
                var diags = Diagnostics();

                var open = Expect(SyntaxTokenKind.OpenBracket, "'['", ref diags);
                var elems = ImmutableArray<PatternNode>.Empty;
                var seps = ImmutableArray<SyntaxToken>.Empty;
                var first = true;

                while (_stream.Peek() is var tok && !tok.IsEndOfInput && tok.Kind != SyntaxTokenKind.CloseBracket)
                {
                    if (!first)
                    {
                        if (_stream.Peek().Kind != SyntaxTokenKind.Comma)
                            break;

                        seps = seps.Add(_stream.Move());
                    }

                    var pat = ParsePattern();
                    var stop = pat is MissingPatternNode && _stream.Peek().Kind != SyntaxTokenKind.Comma;

                    if (first && stop)
                        break;

                    elems = elems.Add(pat);
                    first = false;

                    if (stop)
                        break;
                }

                var close = Expect(SyntaxTokenKind.CloseBracket, "']'", ref diags);
                var remain = _stream.Peek().Kind == SyntaxTokenKind.ColonColon ? ParsePatternRemainder() : null;
                var alias = _stream.Peek().Kind == SyntaxTokenKind.AsKeyword ? ParsePatternAlias() : null;

                return new ArrayPatternNode(Skipped(), diags, open, List(elems, seps), close, remain, alias);
            }

            ExceptionPatternNode ParseExceptionPattern()
            {
                var diags = Diagnostics();

                var exc = Expect(SyntaxTokenKind.ExcKeyword, "'exc' keyword", ref diags);
                var name = Expect(SyntaxTokenKind.ModuleIdentifier, "module identifier", ref diags);
                var open = Expect(SyntaxTokenKind.OpenBrace, "'{'", ref diags);
                var fields = ImmutableArray<PatternFieldNode>.Empty;
                var seps = ImmutableArray<SyntaxToken>.Empty;
                var first = true;

                while (_stream.Peek() is var tok && !tok.IsEndOfInput && tok.Kind != SyntaxTokenKind.CloseBrace)
                {
                    if (!first)
                    {
                        if (_stream.Peek().Kind != SyntaxTokenKind.Comma)
                            break;

                        seps = seps.Add(_stream.Move());
                    }

                    var field = ParsePatternField();
                    var stop = field.Pattern is MissingPatternNode && _stream.Peek().Kind != SyntaxTokenKind.Comma;

                    if (first && stop)
                        break;

                    fields = fields.Add(field);
                    first = false;

                    if (stop)
                        break;
                }

                var close = Expect(SyntaxTokenKind.CloseBrace, "'}'", ref diags);
                var alias = _stream.Peek().Kind == SyntaxTokenKind.AsKeyword ? ParsePatternAlias() : null;

                return new ExceptionPatternNode(Skipped(), diags, exc, name, open, List(fields, seps), close, alias);
            }

            PatternFieldNode ParsePatternField()
            {
                var diags = Diagnostics();

                var name = Expect(SyntaxTokenKind.ValueIdentifier, "value identifier", ref diags);
                var eq = Expect(SyntaxTokenKind.Equals, "'='", ref diags);
                var pat = ParsePattern();

                return new PatternFieldNode(Skipped(), diags, name, eq, pat);
            }

            IdentifierPatternNode ParseIdentifierPattern()
            {
                var diags = Diagnostics();

                var mut = Optional1(SyntaxTokenKind.MutKeyword);
                var name = Expect(SyntaxTokenKind.ValueIdentifier, "value identifier", ref diags);
                var alias = _stream.Peek().Kind == SyntaxTokenKind.AsKeyword ? ParsePatternAlias() : null;

                return new IdentifierPatternNode(Skipped(), diags, mut, name, alias);
            }

            RecordPatternNode ParseRecordPattern()
            {
                var diags = Diagnostics();

                var rec = Expect(SyntaxTokenKind.RecKeyword, "'rec' keyword", ref diags);
                var name = Optional1(SyntaxTokenKind.ModuleIdentifier);
                var open = Expect(SyntaxTokenKind.OpenBrace, "'{'", ref diags);
                var fields = ImmutableArray<PatternFieldNode>.Empty;
                var seps = ImmutableArray<SyntaxToken>.Empty;
                var first = true;

                while (_stream.Peek() is var tok && !tok.IsEndOfInput && tok.Kind != SyntaxTokenKind.CloseBrace)
                {
                    if (!first)
                    {
                        if (_stream.Peek().Kind != SyntaxTokenKind.Comma)
                            break;

                        seps = seps.Add(_stream.Move());
                    }

                    var field = ParsePatternField();
                    var stop = field.Pattern is MissingPatternNode && _stream.Peek().Kind != SyntaxTokenKind.Comma;

                    if (first && stop)
                        break;

                    fields = fields.Add(field);
                    first = false;

                    if (stop)
                        break;
                }

                var close = Expect(SyntaxTokenKind.CloseBrace, "'}'", ref diags);
                var alias = _stream.Peek().Kind == SyntaxTokenKind.AsKeyword ? ParsePatternAlias() : null;

                return new RecordPatternNode(Skipped(), diags, rec, name, open, List(fields, seps), close, alias);
            }

            ModulePatternNode ParseModulePattern()
            {
                var diags = Diagnostics();

                var path = ParseModulePath();
                var alias = _stream.Peek().Kind == SyntaxTokenKind.AsKeyword ? ParsePatternAlias() : null;

                return new ModulePatternNode(Skipped(), diags, path, alias);
            }

            LiteralPatternNode ParseLiteralPattern()
            {
                var diags = Diagnostics();

                var cur = _stream.Peek();
                var minus = cur.Kind == SyntaxTokenKind.AdditiveOperator && cur.Text == "-" ? _stream.Move() : null;
                var value = minus != null ? ExpectNumericLiteral(ref diags) : ExpectLiteral(ref diags);
                var alias = _stream.Peek().Kind == SyntaxTokenKind.AsKeyword ? ParsePatternAlias() : null;

                return new LiteralPatternNode(Skipped(), diags, minus, value, alias);
            }
        }

        public static ParseResult Parse(LexResult lex, SyntaxMode mode)
        {
            return new Parser(lex, mode.Check(nameof(mode))).Parse();
        }
    }
}
