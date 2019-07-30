using System;
using System.Collections.Generic;

namespace Flare.Syntax
{
    public static class LanguageLinter
    {
        sealed class Linter
        {
            sealed class Walker : SyntaxWalker
            {
                public Linter Linter { get; }

                public SyntaxLintConfiguration? Configuration { get; set; }

                public Walker(Linter linter)
                {
                    Linter = linter;
                }

                protected override void DefaultVisit(SyntaxNode node)
                {
                    var cfg = Configuration!;

                    if (node is StatementNode stmt)
                        Linter.RunLints(SyntaxLintContexts.Statement, cfg, lint => lint.Run(stmt));
                    else if (node is ExpressionNode expr)
                        Linter.RunLints(SyntaxLintContexts.Expression, cfg, lint => lint.Run(expr));
                    else if (node is PatternNode pat)
                        Linter.RunLints(SyntaxLintContexts.Pattern, cfg, lint => lint.Run(pat));

                    base.DefaultVisit(node);
                }
            }

            readonly ParseResult _parse;

            SyntaxLintConfiguration _configuration;

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

            SyntaxLintConfiguration Configure(SyntaxNodeList<AttributeNode> attributes,
                SyntaxLintConfiguration configuration)
            {
                foreach (var attr in attributes)
                {
                    var ident = attr.NameToken.Text;

                    if (!ident.StartsWith(AttributePrefix))
                        continue;

                    var name = ident.Substring(AttributePrefix.Length);
                    var value = attr.ValueToken;

                    if (!(value.Value is string s))
                    {
                        _diagnostics.Add(new SyntaxDiagnostic(SyntaxDiagnosticKind.InvalidLintAttribute,
                            SyntaxDiagnosticSeverity.Warning, value.Location,
                            "Lint severity level is not a string literal; ignoring attribute"));
                        continue;
                    }

                    SyntaxDiagnosticSeverity? severity;

                    switch (s)
                    {
                        case NoneIdentifier:
                            severity = null;
                            break;
                        case SuggestionIdentifier:
                            severity = SyntaxDiagnosticSeverity.Suggestion;
                            break;
                        case WarningIdentifier:
                            severity = SyntaxDiagnosticSeverity.Warning;
                            break;
                        case ErrorIdentifier:
                            severity = SyntaxDiagnosticSeverity.Error;
                            break;
                        default:
                            _diagnostics.Add(new SyntaxDiagnostic(SyntaxDiagnosticKind.InvalidLintAttribute,
                                SyntaxDiagnosticSeverity.Warning, value.Location,
                                "Invalid lint severity level; ignoring attribute"));
                            continue;
                    }

                    configuration = configuration.Set(name, severity);
                }

                return configuration;
            }

            void RunLints(SyntaxLintContexts context, SyntaxLintConfiguration configuration,
                Func<SyntaxLint, IEnumerable<SyntaxDiagnostic>> runner)
            {
                foreach (var lint in _lints)
                {
                    if (!lint.Contexts.HasFlag(context))
                        continue;

                    if (!configuration.Get(lint.Name, out var severity))
                        severity = lint.DefaultSeverity;

                    if (severity is SyntaxDiagnosticSeverity sev)
                        foreach (var diag in runner(lint))
                            _diagnostics.Add(diag.Severity == sev ? diag : new SyntaxDiagnostic(diag.Kind, sev,
                                diag.Location, diag.Message, diag.Notes));
                }
            }

            public LintResult Lint()
            {
                var tree = (ProgramNode)_parse.Tree;

                _configuration = Configure(tree.Attributes, _configuration);

                RunLints(SyntaxLintContexts.Program, _configuration, lint => lint.Run(tree));

                foreach (var decl in tree.Declarations)
                {
                    var attrs = decl.Attributes;

                    if (attrs.Count == 0)
                        continue;

                    var cfg = Configure(attrs, _configuration);

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
                        !(decl is MacroDeclarationNode))
                        break;

                    _walker.Configuration = cfg;
                    _walker.Visit(decl);
                }

                return new LintResult(_parse, _diagnostics);
            }
        }

        public const string AttributePrefix = "lint_";

        public const string NoneIdentifier = "none";

        public const string SuggestionIdentifier = "suggestion";

        public const string WarningIdentifier = "warning";

        public const string ErrorIdentifier = "error";

        public static LintResult Lint(ParseResult parse, SyntaxLintConfiguration configuration,
            IEnumerable<SyntaxLint> lints)
        {
            if (parse.Mode != SyntaxMode.Normal)
                throw new ArgumentException("The given parse result cannot be linted due to its syntax mode.",
                    nameof(parse));

            if (!parse.IsSuccess)
                throw new ArgumentException("Unsuccessful parse result given.", nameof(parse));

            return new Linter(parse, configuration, lints).Lint();
        }
    }
}
