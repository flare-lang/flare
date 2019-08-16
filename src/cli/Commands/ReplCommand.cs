using System;
using System.Linq;
using System.Text;
using Flare.Metadata;
using Flare.Syntax;

namespace Flare.Cli.Commands
{
    sealed class ReplCommand : BaseCommand
    {
        sealed class Options
        {
        }

        public ReplCommand()
            : base("repl", "Run an interactive evaluator.")
        {
            RegisterHandler<Options>(Run);
        }

        int Run(Options options)
        {
            ReadLine.AutoCompletionHandler = null;

            var loader = new StandardModuleLoader(ModuleLoaderMode.Interactive);
            var i = 0;

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"flare({i})> ");
                Console.ResetColor();

                var text = new StringBuilder();
                var quit = false;
                var continued = false;

                while (true)
                {
                    var input = ReadLine.Read();

                    if (input != string.Empty)
                        ReadLine.AddHistory(input);

                    var broken = false;

                    if (input == "'repl:quit")
                    {
                        quit = true;
                        break;
                    }
                    if (continued && input == "'repl:break")
                        broken = true;
                    else
                        _ = text.AppendLine(input);

                    var lex = LanguageLexer.Lex(StringSourceText.From("<repl>", text.ToString()));

                    foreach (var diag in lex.Diagnostics)
                        LogDiagnostic(diag);

                    if (!lex.IsSuccess)
                        break;

                    var parse = LanguageParser.Parse(lex, SyntaxMode.Interactive);

                    if (!broken && IsIncomplete(parse))
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.Write($".....({i})> ");
                        Console.ResetColor();

                        continued = true;

                        continue;
                    }

                    foreach (var diag in parse.Diagnostics)
                        LogDiagnostic(diag);

                    var analysis = LanguageAnalyzer.Analyze(parse, loader, new SyntaxContext());

                    foreach (var diag in analysis.Diagnostics)
                        LogDiagnostic(diag);

                    break;
                }

                if (quit)
                    break;

                i++;
            }

            return 0;
        }

        static bool IsIncomplete(ParseResult parse)
        {
            // TODO: The heuristics here could probably be better.

            if (parse.IsSuccess)
                return false;

            var tree = parse.Tree;

            if (tree.HasSkippedTokens)
                return false;

            var last = tree.Children().LastOrDefault();

            if (last == null)
                return false;

            if (last is MissingNamedDeclarationNode || last is MissingExpressionNode || last is MissingPatternNode)
                return true;

            var tokens = last.Tokens();

            return tokens.Any() && tokens.Last().Kind == SyntaxTokenKind.Missing;
        }
    }
}
