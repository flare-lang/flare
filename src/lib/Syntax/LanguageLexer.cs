using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Flare.Syntax
{
    public static class LanguageLexer
    {
        sealed class Lexer : IDisposable
        {
            sealed class PeekableRuneEnumerator : IEnumerator<Rune>
            {
                public Rune Current => _peeked ?? _enumerator.Current;

                object? IEnumerator.Current => throw DebugAssert.Unreachable();

                readonly IEnumerator<Rune> _enumerator;

                Rune? _peeked;

                public PeekableRuneEnumerator(IEnumerator<Rune> enumerator)
                {
                    _enumerator = enumerator;
                }

                public void Dispose()
                {
                    _enumerator.Dispose();
                }

                public bool MoveNext()
                {
                    if (_peeked != null)
                    {
                        _peeked = null;
                        return true;
                    }

                    return _enumerator.MoveNext();
                }

                public Rune? Peek()
                {
                    return _peeked != null ? _peeked : _enumerator.MoveNext() ? (_peeked = _enumerator.Current) : null;
                }

                public void Reset()
                {
                    _enumerator.Reset();
                    _peeked = null;
                }
            }

            const int UnicodeEscapeLength = 6;

            readonly SourceText _source;

            readonly PeekableRuneEnumerator _enumerator;

            SourceLocation _location;

            readonly List<Rune> _runes = new List<Rune>();

            readonly List<Rune> _trivia = new List<Rune>();

            readonly List<Rune> _string = new List<Rune>();

            byte[] _buffer8 = new byte[1024];

            char[] _buffer16 = new char[1024];

            object? _value;

            readonly List<SyntaxTrivia> _leading = new List<SyntaxTrivia>();

            readonly List<SyntaxTrivia> _trailing = new List<SyntaxTrivia>();

            readonly List<SyntaxDiagnostic> _currentDiagnostics = new List<SyntaxDiagnostic>();

            readonly List<SyntaxDiagnostic> _allDiagnostics = new List<SyntaxDiagnostic>();

            public Lexer(SourceText source)
            {
                _source = source;
                _enumerator = new PeekableRuneEnumerator(source.GetRunes().GetEnumerator());
                _location = new SourceLocation(source.FullPath, 1, 1);
            }

            public void Dispose()
            {
                _enumerator.Dispose();
            }

            Rune? PeekNext()
            {
                return _enumerator.Peek();
            }

            Rune? MoveNext(List<Rune>? runes = null)
            {
                if (!_enumerator.MoveNext())
                    return null;

                var cur = _enumerator.Current;
                var nl = cur.Value == '\n';
                var line = _location.Line + (nl ? 1 : 0);
                var column = nl ? 1 : _location.Column + 1;

                _location = new SourceLocation(_location.FullPath, line, column);
                (runes ?? _runes).Add(cur);

                return cur;
            }

            void ConsumeNext(List<Rune>? runes = null)
            {
                _ = MoveNext(runes);
            }

            static BigInteger CreateInteger(string value)
            {
                var radix = 10;

                if (value.StartsWith("0B") || value.StartsWith("0b"))
                    radix = 2;
                else if (value.StartsWith("0O") || value.StartsWith("0o"))
                    radix = 8;
                else if (value.StartsWith("0X") || value.StartsWith("0x"))
                    radix = 16;

                var str = value.AsSpan();

                // Strip base prefix.
                if (radix != 10)
                    str = str.Slice(2);

                // Avoid BigInteger allocations/calculations for some very common cases.

                if (str.Trim('0').IsEmpty)
                    return BigInteger.Zero;

                if (str.TrimStart('0').SequenceEqual("1"))
                    return BigInteger.One;

                var result = BigInteger.Zero;

                foreach (var c in str)
                {
                    int digit;

                    if (c - '0' <= 9)
                        digit = c - '0';
                    else if (c - 'A' <= 'Z' - 'A')
                        digit = c - 'A' + 10;
                    else if (c - 'a' <= 'z' - 'a')
                        digit = c - 'a' + 10;
                    else
                        throw DebugAssert.Unreachable();

                    result = result * radix + digit;
                }

                return result;
            }

            ReadOnlyMemory<byte> CreateString()
            {
                _string.Clear();

                // Strip double quotes.
                using var enumerator = _runes.Skip(1).SkipLast(1).GetEnumerator();

                while (enumerator.MoveNext())
                {
                    var cur = enumerator.Current;

                    if (cur.Value != '\\')
                    {
                        _string.Add(cur);
                        continue;
                    }

                    _ = enumerator.MoveNext();

                    var code = enumerator.Current;

                    // Note that escape sequences have already been validated at this point.
                    switch (code.Value)
                    {
                        case '0':
                            code = new Rune('\0');
                            break;
                        case 'T':
                        case 't':
                            code = new Rune('\t');
                            break;
                        case 'N':
                        case 'n':
                            code = new Rune('\n');
                            break;
                        case 'R':
                        case 'r':
                            code = new Rune('\r');
                            break;
                        case '"':
                            code = new Rune('"');
                            break;
                        case '\\':
                            code = new Rune('\\');
                            break;
                        case 'U':
                        case 'u':
                            Span<char> hex = stackalloc char[UnicodeEscapeLength];

                            for (var i = 0; i < UnicodeEscapeLength; i++)
                            {
                                _ = enumerator.MoveNext();
                                _ = enumerator.Current.EncodeToUtf16(hex.Slice(i));
                            }

                            code = new Rune(int.Parse(hex, NumberStyles.AllowHexSpecifier,
                                CultureInfo.InvariantCulture));
                            break;
                    }

                    _string.Add(code);
                }

                return RunesToUtf8(_string);
            }

            // TODO: Use Utf8String when/if it becomes available.
            ReadOnlyMemory<byte> RunesToUtf8(IEnumerable<Rune> runes)
            {
                var count = 0;

                // Avoid LINQ for performance reasons.
                foreach (var rune in runes)
                    count += rune.Utf8SequenceLength;

                if (count > _buffer8.Length)
                    Array.Resize(ref _buffer8, count);

                var offset = 0;

                foreach (var rune in runes)
                    offset += rune.EncodeToUtf8(_buffer8.AsSpan().Slice(offset));

                return new ReadOnlyMemory<byte>(_buffer8, 0, count).ToArray();
            }

            string RunesToUtf16(IEnumerable<Rune> runes)
            {
                var count = 0;

                // Avoid LINQ for performance reasons.
                foreach (var rune in runes)
                    count += rune.Utf16SequenceLength;

                if (count > _buffer16.Length)
                    Array.Resize(ref _buffer16, count);

                var offset = 0;

                foreach (var rune in runes)
                    offset += rune.EncodeToUtf16(_buffer16.AsSpan().Slice(offset));

                return new string(_buffer16, 0, count);
            }

            SyntaxTrivia Trivia(SourceLocation location, SyntaxTriviaKind kind)
            {
                var trivia = new SyntaxTrivia(location, kind, RunesToUtf16(_trivia));

                _trivia.Clear();

                return trivia;
            }

            SyntaxToken Token(SourceLocation location, SyntaxTokenKind kind)
            {
                var hasDiags = _currentDiagnostics.Count != 0;
                var text = RunesToUtf16(_runes);
                var value = _value;

                if (!hasDiags)
                {
                    switch (kind)
                    {
                        case SyntaxTokenKind.AtomLiteral:
                            value = text.Substring(1);
                            break;
                        case SyntaxTokenKind.IntegerLiteral:
                            value = CreateInteger(text);
                            break;
                        case SyntaxTokenKind.RealLiteral:
                            value = double.Parse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent,
                                CultureInfo.InvariantCulture);
                            break;
                        case SyntaxTokenKind.StringLiteral:
                            value = CreateString();
                            break;
                    }
                }

                var token = new SyntaxToken(location, kind, text, value,
                    _leading.Count != 0 ? _leading.ToImmutableArray() : ImmutableArray<SyntaxTrivia>.Empty,
                    _trailing.Count != 0 ? _trailing.ToImmutableArray() : ImmutableArray<SyntaxTrivia>.Empty,
                    hasDiags ? _currentDiagnostics.ToImmutableArray() : ImmutableArray<SyntaxDiagnostic>.Empty);

                _value = null;

                _runes.Clear();
                _leading.Clear();
                _trailing.Clear();

                _allDiagnostics.AddRange(_currentDiagnostics);
                _currentDiagnostics.Clear();

                return token;
            }

            void Error(SyntaxDiagnosticKind kind, SourceLocation location, string message)
            {
                _currentDiagnostics.Add(new SyntaxDiagnostic(kind, SyntaxDiagnosticSeverity.Error, location, message,
                    ImmutableArray<(SourceLocation, string)>.Empty));
            }

            public LexResult Lex()
            {
                return new LexResult(_source, LexTokens().ToImmutableArray(), _allDiagnostics.ToImmutableArray());
            }

            IEnumerable<SyntaxToken> LexTokens()
            {
                // Check for the shebang line first.
                if (LexShebangLine(_location) is SyntaxTrivia trivia)
                    _leading.Add(trivia);

                while (true)
                {
                    LexTrivia(_leading, false);

                    SourceLocation location;
                    SyntaxTokenKind kind;

                    if (PeekNext() is Rune cur)
                    {
                        switch (cur.Value)
                        {
                            case '!':
                            case '%':
                            case '&':
                            case '*':
                            case '+':
                            case '-':
                            case '/':
                            case '<':
                            case '=':
                            case '>':
                            case '^':
                            case '|':
                            case '~':
                                (location, kind) = LexOperator(_location);
                                break;
                            case '"':
                                (location, kind) = LexStringLiteral(_location);
                                break;
                            case '#':
                                (location, kind) = LexDelimiter(SyntaxTokenKind.Hash, _location);
                                break;
                            case '(':
                                (location, kind) = LexDelimiter(SyntaxTokenKind.OpenParen, _location);
                                break;
                            case ')':
                                (location, kind) = LexDelimiter(SyntaxTokenKind.CloseParen, _location);
                                break;
                            case ',':
                                (location, kind) = LexDelimiter(SyntaxTokenKind.Comma, _location);
                                break;
                            case '.':
                                (location, kind) = LexDelimiter(SyntaxTokenKind.Dot, _location);
                                break;
                            case ';':
                                (location, kind) = LexDelimiter(SyntaxTokenKind.Semicolon, _location);
                                break;
                            case '[':
                                (location, kind) = LexDelimiter(SyntaxTokenKind.OpenBracket, _location);
                                break;
                            case ']':
                                (location, kind) = LexDelimiter(SyntaxTokenKind.CloseBracket, _location);
                                break;
                            case '{':
                                (location, kind) = LexDelimiter(SyntaxTokenKind.OpenBrace, _location);
                                break;
                            case '}':
                                (location, kind) = LexDelimiter(SyntaxTokenKind.CloseBrace, _location);
                                break;
                            case ':':
                                (location, kind) = LexDelimiterOrAtom(SyntaxTokenKind.Colon, _location);
                                break;
                            case '0':
                            case '1':
                            case '2':
                            case '3':
                            case '4':
                            case '5':
                            case '6':
                            case '7':
                            case '8':
                            case '9':
                                (location, kind) = LexNumberLiteral(_location);
                                break;
                            case '?':
                                (location, kind) = LexDelimiter(SyntaxTokenKind.Question, _location);
                                break;
                            case '@':
                                (location, kind) = LexDelimiter(SyntaxTokenKind.At, _location);
                                break;
                            case 'A':
                            case 'B':
                            case 'C':
                            case 'D':
                            case 'E':
                            case 'F':
                            case 'G':
                            case 'H':
                            case 'I':
                            case 'J':
                            case 'K':
                            case 'L':
                            case 'M':
                            case 'N':
                            case 'O':
                            case 'P':
                            case 'Q':
                            case 'R':
                            case 'S':
                            case 'T':
                            case 'U':
                            case 'V':
                            case 'W':
                            case 'X':
                            case 'Y':
                            case 'Z':
                                (location, kind) = LexIdentifier(SyntaxTokenKind.ModuleIdentifier, _location, false);
                                break;
                            case '_':
                            case 'a':
                            case 'b':
                            case 'c':
                            case 'd':
                            case 'e':
                            case 'f':
                            case 'g':
                            case 'h':
                            case 'i':
                            case 'j':
                            case 'k':
                            case 'l':
                            case 'm':
                            case 'n':
                            case 'o':
                            case 'p':
                            case 'q':
                            case 'r':
                            case 's':
                            case 't':
                            case 'u':
                            case 'v':
                            case 'w':
                            case 'x':
                            case 'y':
                            case 'z':
                                (location, kind) = LexIdentifier(SyntaxTokenKind.ValueIdentifier, _location, true);
                                break;
                            default:
                                location = _location;
                                kind = SyntaxTokenKind.Unrecognized;

                                ConsumeNext();
                                Error(SyntaxDiagnosticKind.UnrecognizedCharacter, location,
                                    $"Unrecognized character '{cur}'");
                                break;
                        }

                        LexTrivia(_trailing, true);
                    }
                    else
                    {
                        // The EOI token gets any remaining trivia in the input as leading trivia.
                        location = _location;
                        kind = SyntaxTokenKind.EndOfInput;
                    }

                    yield return Token(location, kind);

                    if (kind == SyntaxTokenKind.EndOfInput)
                        break;
                }
            }

            SyntaxTrivia? LexShebangLine(SourceLocation location)
            {
                var r1 = MoveNext(_trivia);
                var r2 = MoveNext(_trivia);

                if (r1?.Value == '#' && r2?.Value == '!')
                {
                    while (PeekNext() is Rune cur)
                    {
                        switch (cur.Value)
                        {
                            case '\n':
                            case '\r':
                                break;
                            default:
                                ConsumeNext(_trivia);
                                continue;
                        }

                        break;
                    }

                    return Trivia(location, SyntaxTriviaKind.ShebangLine);
                }

                // No shebang line. Reset state since we probably just ate two tokens.
                _enumerator.Reset();
                _location = new SourceLocation(_location.FullPath, 1, 1);
                _trivia.Clear();

                return null;
            }

            void LexTrivia(List<SyntaxTrivia> list, bool trailing)
            {
                while (PeekNext() is Rune cur)
                {
                    switch (cur.Value)
                    {
                        case '\t':
                        case ' ':
                            LexWhiteSpace(_location, list);
                            continue;
                        case '\n':
                        case '\r':
                            LexNewLine(_location, list);

                            if (trailing)
                                break;
                            else
                                continue;
                        case '\'':
                            LexComment(_location, list);
                            continue;
                        default:
                            break;
                    }

                    break;
                }
            }

            void LexComment(SourceLocation location, List<SyntaxTrivia> list)
            {
                ConsumeNext(_trivia);

                while (PeekNext() is Rune cur)
                {
                    switch (cur.Value)
                    {
                        case '\n':
                        case '\r':
                            break;
                        default:
                            ConsumeNext(_trivia);
                            continue;
                    }

                    break;
                }

                list.Add(Trivia(location, SyntaxTriviaKind.Comment));
            }

            void LexNewLine(SourceLocation location, List<SyntaxTrivia> list)
            {
                if (((Rune)MoveNext(_trivia)!).Value == '\r' && PeekNext()?.Value == '\n')
                    ConsumeNext(_trivia);

                list.Add(Trivia(location, SyntaxTriviaKind.NewLine));
            }

            void LexWhiteSpace(SourceLocation location, List<SyntaxTrivia> list)
            {
                ConsumeNext(_trivia);

                while (PeekNext() is Rune cur)
                {
                    switch (cur.Value)
                    {
                        case '\t':
                        case ' ':
                            ConsumeNext(_trivia);
                            continue;
                        default:
                            break;
                    }

                    break;
                }

                list.Add(Trivia(location, SyntaxTriviaKind.WhiteSpace));
            }

            (SourceLocation, SyntaxTokenKind) LexOperator(SourceLocation location)
            {
                var c1 = ((Rune)MoveNext()!).Value;
                var r2 = PeekNext();
                var c2 = r2?.Value;

                // Handle most of the special operators first, where the runes can't possibly be
                // part of a longer custom operator.

                if (c1 == '!')
                {
                    if (c2 == '=')
                        ConsumeNext();
                    else
                        Error(SyntaxDiagnosticKind.IncompleteOperator, _location,
                            $"Expected '=', but found {(r2 != null ? $"'{r2}'" : "end of input")}");

                    return (location, SyntaxTokenKind.ExclamationEquals);
                }

                if (c1 == '<' && c2 == '=')
                {
                    ConsumeNext();

                    return (location, SyntaxTokenKind.OpenAngleEquals);
                }

                if (c1 == '=')
                {
                    if (c2 == '=')
                    {
                        ConsumeNext();

                        return (location, SyntaxTokenKind.EqualsEquals);
                    }
                    else if (c2 == '>')
                    {
                        ConsumeNext();

                        return (location, SyntaxTokenKind.EqualsCloseAngle);
                    }

                    return (location, SyntaxTokenKind.Equals);
                }

                if (c1 == '>' && c2 == '=')
                {
                    ConsumeNext();

                    return (location, SyntaxTokenKind.OpenAngleEquals);
                }

                var parts = 1;

                // Lex the full operator.
                while (PeekNext() is Rune cur)
                {
                    switch (cur.Value)
                    {
                        case '%':
                        case '&':
                        case '*':
                        case '+':
                        case '-':
                        case '/':
                        case '<':
                        case '>':
                        case '^':
                        case '|':
                        case '~':
                            ConsumeNext();
                            parts++;
                            continue;
                        default:
                            break;
                    }

                    break;
                }

                // Handle remaining special operators.
                switch (parts)
                {
                    case 1:
                        if (c1 == '<')
                            return (location, SyntaxTokenKind.OpenAngle);

                        if (c1 == '>')
                            return (location, SyntaxTokenKind.CloseAngle);

                        break;
                    case 2:
                        if (c1 == '-' && c2 == '>')
                            return (location, SyntaxTokenKind.MinusCloseAngle);

                        if (c1 == '<' && c2 == '-')
                            return (location, SyntaxTokenKind.OpenAngleMinus);

                        break;
                }

                SyntaxTokenKind kind;

                // At this point, it's definitely a custom operator, so determine the category.
                switch (c1)
                {
                    case '%':
                    case '*':
                    case '/':
                        kind = SyntaxTokenKind.MultiplicativeOperator;
                        break;
                    case '+':
                    case '-':
                    case '~':
                        kind = SyntaxTokenKind.AdditiveOperator;
                        break;
                    case '<':
                    case '>':
                        kind = SyntaxTokenKind.ShiftOperator;
                        break;
                    case '&':
                    case '^':
                    case '|':
                        kind = SyntaxTokenKind.BitwiseOperator;
                        break;
                    default:
                        throw DebugAssert.Unreachable();
                }

                return (location, kind);
            }

            (SourceLocation, SyntaxTokenKind) LexDelimiter(SyntaxTokenKind kind, SourceLocation location)
            {
                ConsumeNext();

                // It might be a .. token.
                if (kind == SyntaxTokenKind.Dot && PeekNext()?.Value == '.')
                {
                    ConsumeNext();

                    return (location, SyntaxTokenKind.DotDot);
                }

                return (location, kind);
            }

            (SourceLocation, SyntaxTokenKind) LexDelimiterOrAtom(SyntaxTokenKind kind, SourceLocation location)
            {
                ConsumeNext();

                // It might be a :: token or an atom.
                if (kind == SyntaxTokenKind.Colon)
                {
                    switch (PeekNext()?.Value)
                    {
                        case ':':
                            ConsumeNext();
                            kind = SyntaxTokenKind.ColonColon;
                            break;
                        case 'A':
                        case 'B':
                        case 'C':
                        case 'D':
                        case 'E':
                        case 'F':
                        case 'G':
                        case 'H':
                        case 'I':
                        case 'J':
                        case 'K':
                        case 'L':
                        case 'M':
                        case 'N':
                        case 'O':
                        case 'P':
                        case 'Q':
                        case 'R':
                        case 'S':
                        case 'T':
                        case 'U':
                        case 'V':
                        case 'W':
                        case 'X':
                        case 'Y':
                        case 'Z':
                            _ = LexIdentifier(SyntaxTokenKind.ModuleIdentifier, location, false);
                            kind = SyntaxTokenKind.AtomLiteral;
                            break;
                        case '_':
                        case 'a':
                        case 'b':
                        case 'c':
                        case 'd':
                        case 'e':
                        case 'f':
                        case 'g':
                        case 'h':
                        case 'i':
                        case 'j':
                        case 'k':
                        case 'l':
                        case 'm':
                        case 'n':
                        case 'o':
                        case 'p':
                        case 'q':
                        case 'r':
                        case 's':
                        case 't':
                        case 'u':
                        case 'v':
                        case 'w':
                        case 'x':
                        case 'y':
                        case 'z':
                            _ = LexIdentifier(SyntaxTokenKind.ValueIdentifier, location, false);
                            kind = SyntaxTokenKind.AtomLiteral;
                            break;
                    }
                }

                return (location, kind);
            }

            (SourceLocation, SyntaxTokenKind) LexIdentifier(SyntaxTokenKind kind, SourceLocation location, bool keyword)
            {
                ConsumeNext();

                switch (kind)
                {
                    case SyntaxTokenKind.ModuleIdentifier:
                        while (PeekNext() is Rune cur)
                        {
                            switch (cur.Value)
                            {
                                case '0':
                                case '1':
                                case '2':
                                case '3':
                                case '4':
                                case '5':
                                case '6':
                                case '7':
                                case '8':
                                case '9':
                                case 'A':
                                case 'B':
                                case 'C':
                                case 'D':
                                case 'E':
                                case 'F':
                                case 'G':
                                case 'H':
                                case 'I':
                                case 'J':
                                case 'K':
                                case 'L':
                                case 'M':
                                case 'N':
                                case 'O':
                                case 'P':
                                case 'Q':
                                case 'R':
                                case 'S':
                                case 'T':
                                case 'U':
                                case 'V':
                                case 'W':
                                case 'X':
                                case 'Y':
                                case 'Z':
                                case 'a':
                                case 'b':
                                case 'c':
                                case 'd':
                                case 'e':
                                case 'f':
                                case 'g':
                                case 'h':
                                case 'i':
                                case 'j':
                                case 'k':
                                case 'l':
                                case 'm':
                                case 'n':
                                case 'o':
                                case 'p':
                                case 'q':
                                case 'r':
                                case 's':
                                case 't':
                                case 'u':
                                case 'v':
                                case 'w':
                                case 'x':
                                case 'y':
                                case 'z':
                                    ConsumeNext();
                                    continue;
                                default:
                                    break;
                            }

                            break;
                        }

                        break;
                    case SyntaxTokenKind.ValueIdentifier:
                        var count = 0;

                        while (PeekNext() is Rune cur)
                        {
                            switch (cur.Value)
                            {
                                case '0':
                                case '1':
                                case '2':
                                case '3':
                                case '4':
                                case '5':
                                case '6':
                                case '7':
                                case '8':
                                case '9':
                                case '_':
                                case 'a':
                                case 'b':
                                case 'c':
                                case 'd':
                                case 'e':
                                case 'f':
                                case 'g':
                                case 'h':
                                case 'i':
                                case 'j':
                                case 'k':
                                case 'l':
                                case 'm':
                                case 'n':
                                case 'o':
                                case 'p':
                                case 'q':
                                case 'r':
                                case 's':
                                case 't':
                                case 'u':
                                case 'v':
                                case 'w':
                                case 'x':
                                case 'y':
                                case 'z':
                                    ConsumeNext();
                                    count++;
                                    continue;
                                default:
                                    break;
                            }

                            break;
                        }

                        break;
                }

                var text = RunesToUtf16(_runes);

                // If it's a value identifier, it might actually be a keyword or Boolean/nil literal.
                if (keyword && Keywords.TryGetValue(text, out var kind2))
                {
                    kind = kind2;

                    if (kind == SyntaxTokenKind.BooleanLiteral)
                        _value = text == "true";
                }

                return (location, kind);
            }

            (SourceLocation, SyntaxTokenKind) LexNumberLiteral(SourceLocation location)
            {
                var c = ((Rune)MoveNext()!).Value;
                var radix = 10;

                if (c == '0')
                {
                    switch (PeekNext()?.Value)
                    {
                        case 'B':
                        case 'b':
                            ConsumeNext();
                            radix = 2;
                            break;
                        case 'O':
                        case 'o':
                            ConsumeNext();
                            radix = 8;
                            break;
                        case 'X':
                        case 'x':
                            ConsumeNext();
                            radix = 16;
                            break;
                    }
                }

                var digits = 0;

                while (PeekNext() is Rune cur)
                {
                    digits++;

                    switch (cur.Value)
                    {
                        case '0':
                        case '1':
                            ConsumeNext();
                            continue;
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                            if (radix < 8)
                                break;

                            goto case '0';
                        case '8':
                        case '9':
                            if (radix < 10)
                                break;

                            goto case '0';
                        case 'A':
                        case 'B':
                        case 'C':
                        case 'D':
                        case 'E':
                        case 'F':
                        case 'a':
                        case 'b':
                        case 'c':
                        case 'd':
                        case 'e':
                        case 'f':
                            if (radix < 16)
                                break;

                            goto case '0';
                        default:
                            break;
                    }

                    digits--;
                    break;
                }

                if (radix != 10 && digits == 0)
                    Error(SyntaxDiagnosticKind.IncompleteIntegerLiteral, _location,
                        $"Expected base-{radix} digit, but found {(PeekNext() is Rune r ? $"'{r}'" : "end of input")}");

                var dot = PeekNext()?.Value;

                // Is it an integer or real literal?
                if (radix != 10 || dot != '.')
                    return (location, SyntaxTokenKind.IntegerLiteral);

                ConsumeNext();

                digits = 0;

                while (PeekNext() is Rune cur)
                {
                    switch (cur.Value)
                    {
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                            ConsumeNext();
                            digits++;
                            continue;
                        default:
                            break;
                    }

                    break;
                }

                if (digits == 0)
                    Error(SyntaxDiagnosticKind.IncompleteRealLiteral, _location,
                        $"Expected digit, but found {(PeekNext() is Rune r ? $"'{r}'" : "end of input")}");

                // Do we have an exponent part?
                if (!(PeekNext() is Rune exp) || (exp.Value != 'E' && exp.Value != 'e'))
                    return (_location, SyntaxTokenKind.RealLiteral);

                ConsumeNext();

                if (PeekNext() is Rune op && (op.Value == '+' || op.Value == '-'))
                    ConsumeNext();

                digits = 0;

                while (PeekNext() is Rune cur)
                {
                    switch (cur.Value)
                    {
                        case '0':
                        case '1':
                        case '2':
                        case '3':
                        case '4':
                        case '5':
                        case '6':
                        case '7':
                        case '8':
                        case '9':
                            ConsumeNext();
                            digits++;
                            continue;
                        default:
                            break;
                    }

                    break;
                }

                if (digits == 0)
                    Error(SyntaxDiagnosticKind.IncompleteRealLiteral, _location,
                        $"Expected exponent sign or digit, but found {(PeekNext() is Rune r ? $"'{r}'" : "end of input")}");

                return (_location, SyntaxTokenKind.RealLiteral);
            }

            (SourceLocation, SyntaxTokenKind) LexStringLiteral(SourceLocation location)
            {
                ConsumeNext();

                var closed = false;

                while (PeekNext() is Rune cur)
                {
                    ConsumeNext();

                    var c = cur.Value;

                    if (c == '\"')
                    {
                        closed = true;
                        break;
                    }

                    if (c == '\\')
                    {
                        var code = PeekNext();

                        switch (code?.Value)
                        {
                            case '0':
                            case 'N':
                            case 'n':
                            case 'R':
                            case 'r':
                            case 'T':
                            case 't':
                            case '"':
                            case '\\':
                                ConsumeNext();
                                break;
                            case 'U':
                            case 'u':
                                ConsumeNext();

                                for (var i = 0; i < UnicodeEscapeLength; i++)
                                {
                                    var u = MoveNext();

                                    switch (u?.Value)
                                    {
                                        case '0':
                                        case '1':
                                        case '2':
                                        case '3':
                                        case '4':
                                        case '5':
                                        case '6':
                                        case '7':
                                        case '8':
                                        case '9':
                                        case 'A':
                                        case 'B':
                                        case 'C':
                                        case 'D':
                                        case 'E':
                                        case 'F':
                                        case 'a':
                                        case 'b':
                                        case 'c':
                                        case 'd':
                                        case 'e':
                                        case 'f':
                                            break;
                                        default:
                                            Error(SyntaxDiagnosticKind.IncompleteEscapeSequence, _location,
                                                $"Expected Unicode escape sequence digit, but found {(code != null ? $"'{u}'" : "end of input")}");
                                            break;
                                    }
                                }

                                break;
                            default:
                                Error(SyntaxDiagnosticKind.IncompleteEscapeSequence, _location,
                                    $"Expected escape sequence code, but found {(code != null ? $"'{code}'" : "end of input")}");
                                break;
                        }
                    }
                }

                if (!closed)
                    Error(SyntaxDiagnosticKind.IncompleteStringLiteral, _location,
                        "Expected closing '\"', but found end of input");

                return (location, SyntaxTokenKind.StringLiteral);
            }
        }

        public static IReadOnlyDictionary<string, SyntaxTokenKind> Keywords { get; } =
            new Dictionary<string, SyntaxTokenKind>
            {
                // Normal keywords.
                ["and"] = SyntaxTokenKind.AndKeyword,
                ["as"] = SyntaxTokenKind.AsKeyword,
                ["assert"] = SyntaxTokenKind.AssertKeyword,
                ["break"] = SyntaxTokenKind.BreakKeyword,
                ["catch"] = SyntaxTokenKind.CatchKeyword,
                ["cond"] = SyntaxTokenKind.CondKeyword,
                ["const"] = SyntaxTokenKind.ConstKeyword,
                ["else"] = SyntaxTokenKind.ElseKeyword,
                ["exc"] = SyntaxTokenKind.ExcKeyword,
                ["extern"] = SyntaxTokenKind.ExternKeyword,
                ["fn"] = SyntaxTokenKind.FnKeyword,
                ["for"] = SyntaxTokenKind.ForKeyword,
                ["freeze"] = SyntaxTokenKind.FreezeKeyword,
                ["if"] = SyntaxTokenKind.IfKeyword,
                ["in"] = SyntaxTokenKind.InKeyword,
                ["let"] = SyntaxTokenKind.LetKeyword,
                ["loop"] = SyntaxTokenKind.LoopKeyword,
                ["match"] = SyntaxTokenKind.MatchKeyword,
                ["mod"] = SyntaxTokenKind.ModKeyword,
                ["mut"] = SyntaxTokenKind.MutKeyword,
                ["not"] = SyntaxTokenKind.NotKeyword,
                ["or"] = SyntaxTokenKind.OrKeyword,
                ["priv"] = SyntaxTokenKind.PrivKeyword,
                ["pub"] = SyntaxTokenKind.PubKeyword,
                ["raise"] = SyntaxTokenKind.RaiseKeyword,
                ["rec"] = SyntaxTokenKind.RecKeyword,
                ["recv"] = SyntaxTokenKind.RecvKeyword,
                ["return"] = SyntaxTokenKind.ReturnKeyword,
                ["test"] = SyntaxTokenKind.TestKeyword,
                ["use"] = SyntaxTokenKind.UseKeyword,
                ["while"] = SyntaxTokenKind.WhileKeyword,

                // Reserved keywords.
                ["asm"] = SyntaxTokenKind.AsmKeyword,
                ["async"] = SyntaxTokenKind.AsyncKeyword,
                ["await"] = SyntaxTokenKind.AwaitKeyword,
                ["do"] = SyntaxTokenKind.DoKeyword,
                ["goto"] = SyntaxTokenKind.GotoKeyword,
                ["macro"] = SyntaxTokenKind.MacroKeyword,
                ["pragma"] = SyntaxTokenKind.PragmaKeyword,
                ["quote"] = SyntaxTokenKind.QuoteKeyword,
                ["super"] = SyntaxTokenKind.SuperKeyword,
                ["try"] = SyntaxTokenKind.TryKeyword,
                ["unquote"] = SyntaxTokenKind.UnquoteKeyword,
                ["yield"] = SyntaxTokenKind.YieldKeyword,

                // Not strictly keywords in the language reference, but functionally so.
                ["false"] = SyntaxTokenKind.BooleanLiteral,
                ["nil"] = SyntaxTokenKind.NilLiteral,
                ["true"] = SyntaxTokenKind.BooleanLiteral,
            };

        public static LexResult Lex(SourceText source)
        {
            using var lexer = new Lexer(source);

            return lexer.Lex();
        }
    }
}
