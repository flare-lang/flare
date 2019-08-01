using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Linq;
using System.Text;
using Flare.Syntax;

namespace Flare.Cli.Commands
{
    public sealed class ReplCommand : Command
    {
        sealed class Options
        {
        }

        public ReplCommand()
            : base("repl", "Run an interactive evaluator.")
        {
            Handler = CommandHandler.Create<Options>(Run);
        }

        void Run(Options options)
        {
            ReadLine.AutoCompletionHandler = null;

            var i = 0;

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write($"flare({i})> ");
                Console.ResetColor();

                var text = new StringBuilder();
                var continued = false;

                while (true)
                {
                    var input = ReadLine.Read();

                    if (input != string.Empty)
                        ReadLine.AddHistory(input);

                    var broken = false;

                    if (continued && input == "'repl:break")
                        broken = true;
                    else
                        _ = text.AppendLine(input);

                    var lex = LanguageLexer.Lex(StringSourceText.From("<repl>", text.ToString()));

                    foreach (var diag in lex.Diagnostics)
                        PrintDiagnostic(diag);

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
                        PrintDiagnostic(diag);

                    break;
                }

                i++;
            }
        }

        static bool IsIncomplete(Syntax.ParseResult parse)
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

        static void PrintDiagnostic(SyntaxDiagnostic diagnostic)
        {
            Console.ForegroundColor = diagnostic.Severity switch
            {
                SyntaxDiagnosticSeverity.Suggestion => ConsoleColor.White,
                SyntaxDiagnosticSeverity.Warning => ConsoleColor.Yellow,
                SyntaxDiagnosticSeverity.Error => ConsoleColor.Red,
                _ => throw Assert.Unreachable(),
            };
            Console.WriteLine(diagnostic);
            Console.ResetColor();
        }
    }
}
