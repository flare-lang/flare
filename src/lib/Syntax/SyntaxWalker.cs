using System.Collections.Generic;

namespace Flare.Syntax
{
    public abstract partial class SyntaxWalker : SyntaxVisitor
    {
        readonly SyntaxWalkerDepth _depth;

        protected SyntaxWalker(SyntaxWalkerDepth depth = SyntaxWalkerDepth.Nodes)
        {
            _depth = depth.Check(nameof(depth));
        }

        public virtual void Visit(SyntaxToken token)
        {
        }

        public virtual void VisitSkipped(SyntaxToken token)
        {
        }

        public virtual void VisitLeading(SyntaxTrivia trivia)
        {
        }

        public virtual void VisitTrailing(SyntaxTrivia trivia)
        {
        }

        void VisitTokens(IEnumerable<SyntaxToken> tokens, bool skipped)
        {
            if (_depth >= SyntaxWalkerDepth.Tokens)
            {
                foreach (var token in tokens)
                {
                    if (skipped)
                        VisitSkipped(token);
                    else
                        Visit(token);

                    if (_depth >= SyntaxWalkerDepth.Trivia)
                    {
                        if (token.HasLeadingTrivia)
                            foreach (var trivia in token.LeadingTrivia)
                                VisitLeading(trivia);

                        if (token.HasTrailingTrivia)
                            foreach (var trivia in token.TrailingTrivia)
                                VisitTrailing(trivia);
                    }
                }
            }
        }

        protected override void DefaultVisit(SyntaxNode node)
        {
            if (node.HasChildren)
                foreach (var child in node.Children())
                    Visit(child);

            if (node.HasSkippedTokens)
                VisitTokens(node.SkippedTokens, true);

            if (node.HasTokens)
                VisitTokens(node.Tokens(), false);
        }
    }

    public abstract partial class SyntaxWalker<T> : SyntaxVisitor<T>
    {
        readonly SyntaxWalkerDepth _depth;

        protected SyntaxWalker(SyntaxWalkerDepth depth = SyntaxWalkerDepth.Nodes)
        {
            _depth = depth.Check(nameof(depth));
        }

        public virtual T Visit(SyntaxToken token, T state)
        {
            return state;
        }

        public virtual T VisitSkipped(SyntaxToken token, T state)
        {
            return state;
        }

        public virtual T VisitLeading(SyntaxTrivia trivia, T state)
        {
            return state;
        }

        public virtual T VisitTrailing(SyntaxTrivia trivia, T state)
        {
            return state;
        }

        T VisitTokens(IEnumerable<SyntaxToken> tokens, bool skipped, T state)
        {
            if (_depth >= SyntaxWalkerDepth.Tokens)
            {
                foreach (var token in tokens)
                {
                    state = skipped ? VisitSkipped(token, state) : Visit(token, state);

                    if (_depth >= SyntaxWalkerDepth.Trivia)
                    {
                        if (token.HasLeadingTrivia)
                            foreach (var trivia in token.LeadingTrivia)
                                state = VisitLeading(trivia, state);

                        if (token.HasTrailingTrivia)
                            foreach (var trivia in token.TrailingTrivia)
                                state = VisitTrailing(trivia, state);
                    }
                }
            }

            return state;
        }

        protected override T DefaultVisit(SyntaxNode node, T state)
        {
            if (node.HasChildren)
                foreach (var child in node.Children())
                    state = Visit(child, state);

            if (node.HasSkippedTokens)
                state = VisitTokens(node.SkippedTokens, true, state);

            if (node.HasTokens)
                state = VisitTokens(node.Tokens(), false, state);

            return state;
        }
    }
}
