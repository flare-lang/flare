using System.Collections.Generic;
using System.Collections.Immutable;
using Flare.Runtime;

namespace Flare.Tree
{
    sealed class TreeContext
    {
        public Declaration Declaration { get; }

        public ImmutableArray<TreeParameter> Parameters { get; }

        public TreeVariadicParameter? VariadicParameter { get; }

        public TreeReference? Body { get; set; }

        public IReadOnlyList<TreeLocal> Locals => _locals;

        readonly List<TreeLocal> _locals = new List<TreeLocal>();

        readonly Dictionary<int, TreeNode> _nodes = new Dictionary<int, TreeNode>();

        int _nextId;

        TreeContext(Declaration declaration, ImmutableArray<TreeParameter> parameters,
            TreeVariadicParameter? variadicParameter)
        {
            Declaration = declaration;
            Parameters = parameters;
            VariadicParameter = variadicParameter;
        }

        public static TreeContext CreateConstant(Constant constant)
        {
            return new TreeContext(constant, ImmutableArray<TreeParameter>.Empty, null);
        }

        public static TreeContext CreateFunction(Function function)
        {
            var parms = ImmutableArray<TreeParameter>.Empty;
            var variadic = (TreeVariadicParameter?)null;

            foreach (var param in function.Parameters)
            {
                if (param.IsVariadic)
                    variadic = new TreeVariadicParameter(param);
                else
                    parms = parms.Add(new TreeParameter(param));
            }

            return new TreeContext(function, parms, variadic);
        }

        public static TreeContext CreateMacro(Macro macro)
        {
            var parms = ImmutableArray<TreeParameter>.Empty;

            foreach (var param in macro.Parameters)
                parms = parms.Add(new TreeParameter(param));

            return new TreeContext(macro, parms, null);
        }

        public static TreeContext CreateTest(Test test)
        {
            return new TreeContext(test, ImmutableArray<TreeParameter>.Empty, null);
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
