using System;
using System.Collections.Generic;

namespace Flare.Tree
{
    sealed class TreeContext
    {
        // TODO: Link to the generic VM metadata structures.

        public IReadOnlyList<TreeParameter> Parameters { get; }

        public TreeVariadicParameter? VariadicParameter { get; }

        public TreeReference? Body { get; set; }

        public bool HasLocals => Locals.Count != 0;

        public IReadOnlyList<TreeLocal> Locals => _locals;

        readonly List<TreeLocal> _locals = new List<TreeLocal>();

        readonly Dictionary<int, TreeNode> _nodes = new Dictionary<int, TreeNode>();

        int _nextId;

        TreeContext(IReadOnlyList<TreeParameter> parameters, TreeVariadicParameter? variadicParameter)
        {
            Parameters = parameters;
            VariadicParameter = variadicParameter;
        }

        public static TreeContext CreateConstant()
        {
            return new TreeContext(Array.Empty<TreeParameter>(), null);
        }

        public static TreeContext CreateFunction(IReadOnlyList<TreeParameter> parameters,
            TreeVariadicParameter variadicParameter)
        {
            return new TreeContext(parameters, variadicParameter);
        }

        public static TreeContext CreateMacro(IReadOnlyList<TreeFragment> fragments)
        {
            return new TreeContext(fragments, null);
        }

        public TreeLocal CreateLocal(TreeType type, bool mutable, string? name = null)
        {
            var local = new TreeLocal(type, name, mutable);

            _locals.Add(local);

            return local;
        }

        public void RemoveLocal(TreeLocal local)
        {
            _ = _locals.Remove(local);
        }

        public TreeReference RegisterNode(TreeNode node)
        {
            var r = new TreeReference(this, _nextId++);

            _nodes.Add(r.Id, node);

            return r;
        }

        public TreeNode ResolveNode(int id)
        {
            return _nodes[id];
        }

        public void ReplaceNode(int id, TreeNode node)
        {
            _nodes[id] = node;
        }
    }
}
