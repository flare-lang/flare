using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Flare.Syntax.Lints;

namespace Flare.Syntax
{
    public static class LanguageLinter
    {
        sealed class Linter
        {
            sealed class Walker : SyntaxWalker
            {
                public Linter Linter { get; }

                public SyntaxLintConfiguration Configuration { get; set; } = null!;

                public Walker(Linter linter)
                {
                    Linter = linter;
                }

                protected override void DefaultVisit(SyntaxNode node)
                {
                    var cfg = Configuration;

                    if (node is StatementNode stmt &&
                        !(node is ExpressionStatementNode e && e.Expression is MissingExpressionNode))
                        Linter.RunLints(SyntaxLintContexts.Statement, cfg, lint => lint.Run(stmt));
                    else if (node is ExpressionNode expr && !(node is MissingExpressionNode))
                        Linter.RunLints(SyntaxLintContexts.Expression, cfg, lint => lint.Run(expr));
                    else if (node is PatternNode pat && !(node is MissingPatternNode))
                        Linter.RunLints(SyntaxLintContexts.Pattern, cfg, lint => lint.Run(pat));

                    base.DefaultVisit(node);
                }
            }

            readonly ParseResult _parse;

            readonly SyntaxLintConfiguration _configuration;

            readonly IEnumerable<SyntaxLint> _lints;

            readonly Walker _walker;

            readonly List<SyntaxDiagnostic> _diagnostics = new List<SyntaxDiagnostic>();

            public Linter(ParseResult parse, SyntaxLintConfiguration configuration, IEnumerable<SyntaxLint> lints)
            {
                _parse = parse;
                _configuration = configuration;
                _lints = lints;
                _walker = new Walker(this);
            }

            void Warning(SyntaxNode node, SourceLocation location, string message)
            {
                var diag = new SyntaxDiagnostic(SyntaxDiagnosticKind.InvalidLintAttribute,
                    SyntaxDiagnosticSeverity.Warning, location, message,
                    ImmutableArray<(SourceLocation, string)>.Empty);

                node.AddDiagnostic(diag);
                _diagnostics.Add(diag);
            }

            SyntaxLintConfiguration Configure(SyntaxNodeList<AttributeNode> attributes,
                SyntaxLintConfiguration configuration)
            {
                foreach (var attr in attributes)
                {
                    var ident = attr.NameToken.Text;

                    if (!ident.StartsWith(AttributePrefix))
                        continue;

                    var name = ident.Substring(AttributePrefix.Length);

                    if (name == string.Empty)
                        continue;

                    var value = attr.ValueToken;

                    if (value.IsMissing)
                        continue;

                    if (!(value.Value is ReadOnlyMemory<byte> str))
                    {
                        Warning(attr, value.Location,
                            "Lint severity level is not a string literal; ignoring attribute");
                        continue;
                    }

                    SyntaxDiagnosticSeverity? severity;

                    switch (Encoding.UTF8.GetString(str.Span).ToLowerInvariant())
                    {
                        case NoneSeverityName:
                            severity = null;
                            break;
                        case SuggestionSeverityName:
                            severity = SyntaxDiagnosticSeverity.Suggestion;
                            break;
                        case WarninngSeverityName:
                            severity = SyntaxDiagnosticSeverity.Warning;
                            break;
                        case ErrorSeverityName:
                            severity = SyntaxDiagnosticSeverity.Error;
                            break;
                        default:
                            Warning(attr, value.Location, "Invalid lint severity level; ignoring attribute");
                            continue;
                    }

                    configuration = configuration.Set(name, severity);
                }

                return configuration;
            }

            void RunLints(SyntaxLintContexts context, SyntaxLintConfiguration configuration,
                Func<SyntaxLint, IEnumerable<(SyntaxNode, SyntaxDiagnostic)>> runner)
            {
                foreach (var lint in _lints)
                {
                    if (!lint.Contexts.HasFlag(context))
                        continue;

                    if (!configuration.Get(lint.Name, out var severity))
                        severity = lint.DefaultSeverity;

                    if (severity is SyntaxDiagnosticSeverity sev)
                    {
                        foreach (var (node, diag) in runner(lint))
                        {
                            node.AddDiagnostic(diag);
                            _diagnostics.Add(diag.Severity == sev ? diag : new SyntaxDiagnostic(
                                diag.Kind, sev, diag.Location, diag.Message, diag.Notes));
                        }
                    }
                }
            }

            public LintResult Lint()
            {
                var tree = (ProgramNode)_parse.Tree;
                var cfg = Configure(tree.Attributes, _configuration);

                RunLints(SyntaxLintContexts.Program, cfg, lint => lint.Run(tree));

                foreach (var decl in tree.Declarations)
                {
                    if (decl is MissingNamedDeclarationNode)
                        continue;

                    var attrs = decl.Attributes;

                    if (attrs.Count != 0)
                        cfg = Configure(attrs, cfg);

                    switch (decl)
                    {
                        case NamedDeclarationNode node:
                            RunLints(SyntaxLintContexts.NamedDeclaration, cfg, lint => lint.Run(node));
                            break;
                        case DeclarationNode node:
                            RunLints(SyntaxLintContexts.Declaration, cfg, lint => lint.Run(node));
                            break;
                    }

                    if (!(decl is ConstantDeclarationNode) &&
                        !(decl is FunctionDeclarationNode) &&
                        !(decl is TestDeclarationNode))
                        break;

                    _walker.Configuration = cfg;
                    _walker.Visit(decl);
                }

                return new LintResult(_parse, _diagnostics.ToImmutableArray());
            }
        }

        public const string AttributePrefix = "lint_";

        public const string NoneSeverityName = "none";

        public const string SuggestionSeverityName = "suggestion";

        public const string WarninngSeverityName = "warning";

        public const string ErrorSeverityName = "error";

        public static ImmutableDictionary<string, SyntaxLint> Lints = new SyntaxLint[]
        {
            new UndocumentedDeclarationLint(),
        }.ToImmutableDictionary(x => x.Name);

        public static LintResult Lint(ParseResult parse, SyntaxLintConfiguration configuration,
            IEnumerable<SyntaxLint> lints)
        {
            if (parse.Mode != SyntaxMode.Normal)
                throw new ArgumentException("The given parse result cannot be linted due to its syntax mode.",
                    nameof(parse));

            return new Linter(parse, configuration, lints).Lint();
        }
    }
}
