namespace Flare.Syntax
{
    public enum SyntaxTokenKind
    {
        Missing,
        EndOfInput,
        Unrecognized,
        MultiplicativeOperator,
        AdditiveOperator,
        ShiftOperator,
        BitwiseOperator,
        Exclamation,
        ExclamationEquals,
        Hash,
        OpenParen,
        CloseParen,
        Comma,
        Dot,
        DotDot,
        MinusCloseAngle,
        Colon,
        ColonColon,
        Semicolon,
        OpenAngle,
        OpenAngleMinus,
        OpenAngleEquals,
        Equals,
        EqualsEquals,
        EqualsCloseAngle,
        CloseAngle,
        CloseAngleEquals,
        Question,
        At,
        OpenBracket,
        CloseBracket,
        OpenBrace,
        CloseBrace,
        AndKeyword,
        AsKeyword,
        AssertKeyword,
        BreakKeyword,
        CatchKeyword,
        CondKeyword,
        ConstKeyword,
        ElseKeyword,
        ExcKeyword,
        ExternKeyword,
        FnKeyword,
        ForKeyword,
        FreezeKeyword,
        IfKeyword,
        InKeyword,
        LetKeyword,
        LoopKeyword,
        MacroKeyword,
        MatchKeyword,
        ModKeyword,
        MutKeyword,
        NotKeyword,
        OrKeyword,
        PrivKeyword,
        PubKeyword,
        RaiseKeyword,
        RecKeyword,
        RecvKeyword,
        ReturnKeyword,
        UseKeyword,
        WhileKeyword,
        AsmKeyword,
        AsyncKeyword,
        AwaitKeyword,
        DoKeyword,
        GotoKeyword,
        PragmaKeyword,
        QuoteKeyword,
        SuperKeyword,
        TestKeyword,
        TryKeyword,
        UnquoteKeyword,
        YieldKeyword,
        ModuleIdentifier,
        ValueIdentifier,
        FragmentIdentifier,
        NilLiteral,
        BooleanLiteral,
        AtomLiteral,
        IntegerLiteral,
        RealLiteral,
        StringLiteral,
    }
}
