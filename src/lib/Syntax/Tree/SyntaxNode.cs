using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Flare.Syntax.Tree
{
    public abstract class SyntaxNode
    {
        public SyntaxNode? Parent { get; internal set; }

        public bool HasSkippedTokens => SkippedTokens.Count != 0;

        public IReadOnlyList<SyntaxToken> SkippedTokens { get; }

        public abstract bool HasTokens { get; }

        public abstract bool HasChildren { get; }

        public bool HasDiagnostics => Diagnostics.Count != 0;

        public IReadOnlyList<SyntaxDiagnostic> Diagnostics { get; }

        public bool HasAnnotations => _annotations.Count != 0;

        protected ImmutableDictionary<string, object> Annotations => _annotations;

        ImmutableDictionary<string, object> _annotations;

        private protected SyntaxNode(IReadOnlyList<SyntaxToken> skipped, IReadOnlyList<SyntaxDiagnostic> diagnostics,
            ImmutableDictionary<string, object> annotations)
        {
            SkippedTokens = skipped;
            Diagnostics = diagnostics;
            _annotations = annotations;
        }

        internal abstract SyntaxNode InternalDeepClone();

        public abstract IEnumerable<SyntaxToken> Tokens();

        public abstract IEnumerable<SyntaxNode> Children();

        public IEnumerable<SyntaxNode> Ancestors(bool self = false)
        {
            if (self)
                yield return this;

            SyntaxNode? current = this;

            while ((current = current.Parent) != null)
                yield return current;
        }

        public IEnumerable<SyntaxNode> Siblings(bool self = false)
        {
            var parent = Parent;

            if (parent == null)
                yield break;

            foreach (var child in parent.Children())
                if (self || child != this)
                    yield return child;
        }

        public IEnumerable<SyntaxNode> Descendants(bool self = false)
        {
            if (self)
                yield return this;

            var work = new Queue<SyntaxNode>();

            work.Enqueue(this);

            while (work.Count != 0)
            {
                var current = work.Dequeue();

                foreach (var child in current.Children())
                {
                    yield return child;

                    work.Enqueue(child);
                }
            }
        }

        internal abstract void Accept(SyntaxVisitor visitor);

        internal abstract T Accept<T>(SyntaxVisitor<T> visitor, T state);

        public T Get<T>(string name)
        {
            return _annotations.TryGetValue(name, out var obj) ? (T)obj :
                throw new ArgumentException("Annotation with the specified name does not exist.");
        }

        public bool TryGet<T>(string name, [NotNullWhen(true)] out T value)
        {
            var result = _annotations.TryGetValue(name, out var obj);

            value = result ? (T)obj : default!;

            return result;
        }

        public void Set<T>(string name, T value)
        {
            _ = ImmutableInterlocked.AddOrUpdate(ref _annotations, name, k => value!, (k, v) => value!);
        }

        public void Remove(string name)
        {
            _ = ImmutableInterlocked.TryRemove(ref _annotations, name, out _);
        }
    }
}
