namespace Flare.Syntax.Tree
{
    public abstract partial class SyntaxVisitor
    {
        public void Visit(SyntaxNode node)
        {
            node.Accept(this);
        }

        protected virtual void DefaultVisit(SyntaxNode node)
        {
        }
    }

    public abstract partial class SyntaxVisitor<T>
    {
        public T Visit(SyntaxNode node, T state)
        {
            return node.Accept(this, state);
        }

        protected virtual T DefaultVisit(SyntaxNode node, T state)
        {
            return state;
        }
    }
}
