using System;
using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Flare.Generator
{
    [Generator]
    sealed class SyntaxTreeGenerator : ISourceGenerator
    {
        sealed class Settings
        {
            public string DefaultBaseTypeName { get; }

            public string TypeNameSuffix { get; }

            public Settings(XElement element)
            {
                DefaultBaseTypeName = (string)element.Element("base");
                TypeNameSuffix = (string)element.Element("suffix");
            }
        }

        sealed class Node
        {
            public string TypeName { get; }

            public string BaseTypeName { get; }

            public bool IsAbstract { get; }

            public ImmutableArray<Property> Properties { get; }

            public Node(XElement element, Settings settings)
            {
                TypeName = (string)element.Attribute("name") + settings.TypeNameSuffix;
                BaseTypeName = ((string)element.Attribute("base") ??
                    settings.DefaultBaseTypeName) + settings.TypeNameSuffix;
                IsAbstract = (bool?)element.Attribute("abstract") ?? false;
                Properties = element.Elements().Select(elem => new Property(elem, settings)).ToImmutableArray();
            }
        }

        sealed class Property
        {
            public string PropertyName { get; }

            public string ParameterName { get; }

            public string TypeName { get; }

            public bool IsToken { get; }

            public bool IsTokens { get; }

            public bool IsChild { get; }

            public bool IsChildren { get; }

            public bool IsSeparated { get; }

            public bool IsOptional { get; }

            public bool IsOverride { get; }

            public string ElementsAccess { get; }

            public Property(XElement element, Settings settings)
            {
                IsToken = element.Name == "token";
                IsTokens = element.Name == "tokens";
                IsChild = element.Name == "child";
                IsChildren = element.Name == "children";
                IsSeparated = (bool?)element.Attribute("separated") ?? false;
                IsOptional = (bool?)element.Attribute("optional") ?? false;

                var prop = (string)element.Attribute("name");

                if (IsToken)
                    prop += "Token";
                else if (IsTokens)
                    prop += "Tokens";

                var param = char.ToLowerInvariant(prop[0]) + prop.Substring(1);

                if (SyntaxFacts.GetKeywordKind(param) != SyntaxKind.None)
                    param = '@' + param;

                var type = IsToken || IsTokens ?
                    "SyntaxToken" : (string)element.Attribute("type") + settings.TypeNameSuffix;
                var elems = string.Empty;

                if (IsTokens)
                {
                    type = "SyntaxTokenList";
                    elems = ".Tokens";
                }
                else if (IsChildren)
                {
                    type = $"SyntaxNodeList<{type}>";
                    elems = ".Nodes";
                }

                if (IsSeparated)
                    type = "Separated" + type;
                else
                    elems = string.Empty;

                if (IsOptional)
                    type += '?';

                PropertyName = prop;
                ParameterName = param;
                TypeName = type;
                IsOverride = (bool?)element.Attribute("override") ?? false;
                ElementsAccess = elems;
            }
        }

        const string SyntaxTreeFileName = "SyntaxTree.xml";

        static readonly DiagnosticDescriptor _fl0001 = new DiagnosticDescriptor(
            "FL0001",
            "Syntax tree definition file is required",
            $"The syntax tree definition file '{SyntaxTreeFileName}' could not be loaded.",
            nameof(Flare),
            DiagnosticSeverity.Error,
            true);

        static readonly DiagnosticDescriptor _fl0002 = new DiagnosticDescriptor(
            "FL0002",
            "Syntax tree definition file must contain valid XML",
            $"The syntax tree definition file '{{0}}' contains invalid XML: {{1}}",
            nameof(Flare),
            DiagnosticSeverity.Error,
            true);

        public void Initialize(GeneratorInitializationContext context)
        {
        }

        [SuppressMessage("Microsoft.Design", "CA1031", Justification = "Reported in diagnostic.")]
        public void Execute(GeneratorExecutionContext context)
        {
            var file = context.AdditionalFiles.SingleOrDefault(x => Path.GetFileName(x.Path) == SyntaxTreeFileName);

            if (file?.GetText() is not SourceText text)
            {
                context.ReportDiagnostic(Diagnostic.Create(_fl0001, null));
                return;
            }

            try
            {
                var root = XDocument.Load(new StringReader(text.ToString())).Root;
                var settings = new Settings(root.Element("settings"));
                var nodes = root.Element("nodes").Elements().Select(e => new Node(e, settings)).ToImmutableArray();

                foreach (var node in nodes)
                    WriteFile(context, node.TypeName, WriteSyntaxNode, node);

                WriteFile(context, "SyntaxVisitor", WriteSyntaxVisitor, nodes);
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(_fl0002, null, file.Path, ex.Message));
                return;
            }
        }

        static void WriteFile<T>(GeneratorExecutionContext context, string name, Action<IndentedTextWriter, T> action,
            T argument)
        {
            var sb = new StringBuilder();
            using var writer = new IndentedTextWriter(new StringWriter(sb));

            writer.WriteLine("// <auto-generated />");
            writer.WriteLine();
            writer.WriteLine("#nullable enable");
            writer.WriteLine();
            writer.WriteLine("using System;");
            writer.WriteLine("using System.Collections.Generic;");
            writer.WriteLine("using System.Collections.Immutable;");
            writer.WriteLine("using System.Linq;");
            writer.WriteLine();
            writer.WriteLine("namespace Flare.Syntax");
            writer.WriteLine("{");

            writer.Indent++;

            action(writer, argument);

            writer.Indent--;

            writer.WriteLine("}");

            context.AddSource(name, SourceText.From(sb.ToString(), Encoding.UTF8));
        }

        static void WriteSyntaxNode(IndentedTextWriter writer, Node node)
        {
            var abs = node.IsAbstract;
            var type = node.TypeName;

            writer.WriteLine("public {0} class {1} : {2}", abs ? "abstract" : "sealed", type, node.BaseTypeName);
            writer.WriteLine("{");

            writer.Indent++;

            var props = node.Properties;

            foreach (var prop in props)
            {
                var attr = string.Empty;

                if (abs)
                    attr = " abstract";
                else if (prop.IsOverride)
                    attr = " override";

                writer.WriteLine("public{0} {1} {2} {{ get; }}", attr, prop.TypeName, prop.PropertyName);
                writer.WriteLine();
            }

            if (abs)
            {
                writer.WriteLine("private protected {0}(", type);

                writer.Indent++;

                writer.WriteLine("ImmutableArray<SyntaxToken> skipped,");
                writer.WriteLine("ImmutableArray<SyntaxDiagnostic> diagnostics,");
                writer.WriteLine("ImmutableDictionary<string, object> annotations)");

                writer.WriteLine(": base(skipped, diagnostics, annotations)");

                writer.Indent--;

                writer.WriteLine("{");
                writer.WriteLine("}");
            }
            else
            {
                var tokens = props.Where(
                    p => p.IsToken || p.IsTokens || (p.IsChildren && p.IsSeparated)).ToImmutableArray();

                writer.Write("public override bool HasTokens");

                if (tokens.IsEmpty)
                    writer.WriteLine("=> false;");
                else if (tokens.Any(p => p.IsToken && !p.IsOptional))
                    writer.WriteLine("=> true;");
                else
                {
                    writer.WriteLine("{");

                    writer.Indent++;

                    writer.WriteLine("get");
                    writer.WriteLine("{");

                    writer.Indent++;

                    for (var i = 0; i < tokens.Length; i++)
                    {
                        var prop = tokens[i];
                        var name = prop.PropertyName;

                        if (prop.IsTokens)
                            writer.WriteLine("if ({0}{1}.Count != 0)", name, prop.ElementsAccess);
                        else if (prop.IsToken)
                            writer.WriteLine("if ({0} != null)", name);

                        if (!prop.IsChildren)
                        {
                            writer.Indent++;

                            writer.WriteLine("return true;");

                            writer.Indent--;
                        }

                        if (prop.IsSeparated)
                        {
                            if (!prop.IsChildren)
                                writer.WriteLine();

                            writer.WriteLine("if ({0}.Separators.Count != 0)", name);

                            writer.Indent++;

                            writer.WriteLine("return true;");

                            writer.Indent--;
                        }

                        if (i != tokens.Length - 1)
                            writer.WriteLine();
                    }

                    writer.WriteLine();
                    writer.WriteLine("return false;");

                    writer.Indent--;

                    writer.WriteLine("}");

                    writer.Indent--;

                    writer.WriteLine("}");
                }

                writer.WriteLine();

                var children = props.Where(prop => prop.IsChild || prop.IsChildren).ToImmutableArray();

                writer.Write("public override bool HasChildren");

                if (children.IsEmpty)
                    writer.WriteLine("=> false;");
                else if (children.Any(prop => prop.IsChild && !prop.IsOptional))
                    writer.WriteLine("=> true;");
                else
                {
                    writer.WriteLine("{");

                    writer.Indent++;

                    writer.WriteLine("get");
                    writer.WriteLine("{");

                    writer.Indent++;

                    for (var i = 0; i < children.Length; i++)
                    {
                        var prop = children[i];
                        var name = prop.PropertyName;

                        if (prop.IsChildren)
                            writer.WriteLine("if ({0}{1}.Count != 0)", name, prop.ElementsAccess);
                        else
                            writer.WriteLine("if ({0} != null)", name);

                        writer.Indent++;

                        writer.WriteLine("return true;");

                        writer.Indent--;

                        if (i != children.Length - 1)
                            writer.WriteLine();
                    }

                    writer.WriteLine();
                    writer.WriteLine("return false;");

                    writer.Indent--;

                    writer.WriteLine("}");

                    writer.Indent--;

                    writer.WriteLine("}");
                }

                writer.WriteLine();
                writer.WriteLine("internal {0}(", type);

                writer.Indent++;

                writer.WriteLine("ImmutableArray<SyntaxToken> skipped,");
                writer.Write("ImmutableArray<SyntaxDiagnostic> diagnostics");

                if (!props.IsEmpty)
                {
                    writer.WriteLine(",");

                    for (var i = 0; i < props.Length; i++)
                    {
                        var prop = props[i];

                        writer.Write("{0} {1}", prop.TypeName, prop.ParameterName);

                        if (i == props.Length - 1)
                            writer.Write(")");
                        else
                            writer.Write(",");

                        writer.WriteLine();
                    }
                }
                else
                    writer.WriteLine(")");

                writer.WriteLine(": this(");

                writer.Indent++;

                writer.WriteLine("skipped,");
                writer.WriteLine("diagnostics,");
                writer.Write("ImmutableDictionary<string, object>.Empty");

                if (!props.IsEmpty)
                {
                    writer.WriteLine(",");

                    for (var i = 0; i < props.Length; i++)
                    {
                        writer.Write(props[i].ParameterName);

                        if (i == props.Length - 1)
                            writer.Write(")");
                        else
                            writer.Write(",");

                        writer.WriteLine();
                    }
                }
                else
                    writer.WriteLine(")");

                writer.Indent--;
                writer.Indent--;

                writer.WriteLine("{");
                writer.WriteLine("}");
                writer.WriteLine();
                writer.WriteLine("{0}(", type);

                writer.Indent++;

                writer.WriteLine("ImmutableArray<SyntaxToken> skipped,");
                writer.WriteLine("ImmutableArray<SyntaxDiagnostic> diagnostics,");
                writer.Write("ImmutableDictionary<string, object> annotations");

                if (!props.IsEmpty)
                {
                    writer.WriteLine(",");

                    for (var i = 0; i < props.Length; i++)
                    {
                        var prop = props[i];

                        writer.Write("{0} {1}", prop.TypeName, prop.ParameterName);

                        if (i == props.Length - 1)
                            writer.Write(")");
                        else
                            writer.Write(",");

                        writer.WriteLine();
                    }
                }
                else
                    writer.WriteLine(")");

                writer.WriteLine(": base(skipped, diagnostics, annotations)");

                writer.Indent--;

                writer.WriteLine("{");

                writer.Indent++;

                for (var i = 0; i < props.Length; i++)
                {
                    var prop = props[i];
                    var param = prop.ParameterName;

                    writer.WriteLine("{0} = {1};", prop.PropertyName, param);

                    if (prop.IsChildren || prop.IsTokens)
                    {
                        writer.WriteLine();
                        writer.WriteLine("foreach (var child in {0}{1})", param, prop.ElementsAccess);

                        writer.Indent++;

                        writer.WriteLine("child.Parent = this;");

                        writer.Indent--;

                        if (prop.IsSeparated)
                        {
                            writer.WriteLine();
                            writer.WriteLine("foreach (var child in {0}.Separators)", param);

                            writer.Indent++;

                            writer.WriteLine("child.Parent = this;");

                            writer.Indent--;
                        }
                    }
                    else
                    {
                        if (prop.IsOptional)
                        {
                            writer.WriteLine();
                            writer.WriteLine("if ({0} != null)", param);

                            writer.Indent++;
                        }

                        writer.WriteLine("{0}.Parent = this;", param);

                        if (prop.IsOptional)
                            writer.Indent--;
                    }

                    if (i != props.Length - 1)
                        writer.WriteLine();
                }

                writer.Indent--;

                writer.WriteLine("}");
                writer.WriteLine();
                writer.WriteLine("public {0} DeepClone()", type);
                writer.WriteLine("{");

                writer.Indent++;

                writer.WriteLine("return new {0}(", type);

                writer.Indent++;

                writer.WriteLine("SkippedTokens.Select(x => x.DeepClone()).ToImmutableArray(),");
                writer.WriteLine("Diagnostics,");
                writer.Write("Annotations");

                if (!props.IsEmpty)
                {
                    writer.WriteLine(",");

                    for (var i = 0; i < props.Length; i++)
                    {
                        var prop = props[i];
                        var name = prop.PropertyName;

                        if (prop.IsChild)
                        {
                            writer.Write("({0}){1}", prop.TypeName, name);

                            if (prop.IsOptional)
                                writer.Write("?");

                            writer.Write(".InternalDeepClone()");
                        }
                        else
                        {
                            writer.Write(name);

                            if (prop.IsOptional)
                                writer.Write("?");

                            writer.Write(".DeepClone()");
                        }

                        if (i == props.Length - 1)
                            writer.Write(");");
                        else
                            writer.Write(",");

                        writer.WriteLine();
                    }
                }
                else
                    writer.WriteLine(");");

                writer.Indent--;
                writer.Indent--;

                writer.WriteLine("}");
                writer.WriteLine();
                writer.WriteLine("internal override SyntaxNode InternalDeepClone()");
                writer.WriteLine("{");

                writer.Indent++;

                writer.WriteLine("return DeepClone();");

                writer.Indent--;

                writer.WriteLine("}");
                writer.WriteLine();
                writer.WriteLine("public override IEnumerable<SyntaxToken> Tokens()");
                writer.WriteLine("{");

                writer.Indent++;

                if (!tokens.IsEmpty)
                {
                    for (var i = 0; i < tokens.Length; i++)
                    {
                        var prop = tokens[i];
                        var name = prop.PropertyName;

                        if (prop.IsTokens)
                        {
                            writer.WriteLine("foreach (var token in {0}{1})", name, prop.ElementsAccess);

                            writer.Indent++;

                            writer.WriteLine("yield return token;");

                            writer.Indent--;
                        }
                        else if (prop.IsToken)
                        {
                            if (prop.IsOptional)
                            {
                                writer.WriteLine("if ({0} != null)", name);

                                writer.Indent++;
                            }

                            writer.WriteLine("yield return {0};", name);

                            if (prop.IsOptional)
                                writer.Indent--;
                        }

                        if (prop.IsSeparated)
                        {
                            if (prop.IsTokens)
                                writer.WriteLine();

                            writer.WriteLine("foreach (var token in {0}.Separators)", name);

                            writer.Indent++;

                            writer.WriteLine("yield return token;");

                            writer.Indent--;
                        }

                        if (i != tokens.Length - 1)
                            writer.WriteLine();
                    }
                }
                else
                    writer.WriteLine("return Array.Empty<SyntaxToken>();");

                writer.Indent--;

                writer.WriteLine("}");
                writer.WriteLine();
                writer.WriteLine("public override IEnumerable<SyntaxNode> Children()");
                writer.WriteLine("{");

                writer.Indent++;

                if (!children.IsEmpty)
                {
                    for (var i = 0; i < children.Length; i++)
                    {
                        var prop = children[i];
                        var name = prop.PropertyName;

                        if (prop.IsChildren)
                        {
                            writer.WriteLine("foreach (var child in {0}{1})", name, prop.ElementsAccess);

                            writer.Indent++;

                            writer.WriteLine("yield return child;");

                            writer.Indent--;
                        }
                        else
                        {
                            if (prop.IsOptional)
                            {
                                writer.WriteLine("if ({0} != null)", name);

                                writer.Indent++;
                            }

                            writer.WriteLine("yield return {0};", name);

                            if (prop.IsOptional)
                                writer.Indent--;
                        }

                        if (i != children.Length - 1)
                            writer.WriteLine();
                    }
                }
                else
                    writer.WriteLine("return Array.Empty<SyntaxNode>();");

                writer.Indent--;

                writer.WriteLine("}");
                writer.WriteLine();
                writer.WriteLine("internal override void Accept(SyntaxVisitor visitor)");
                writer.WriteLine("{");

                writer.Indent++;

                writer.WriteLine("visitor.Visit(this);");

                writer.Indent--;

                writer.WriteLine("}");
                writer.WriteLine();
                writer.WriteLine("internal override T Accept<T>(SyntaxVisitor<T> visitor, T state)");
                writer.WriteLine("{");

                writer.Indent++;

                writer.WriteLine("return visitor.Visit(this, state);");

                writer.Indent--;

                writer.WriteLine("}");
            }

            writer.Indent--;

            writer.WriteLine("}");
        }

        static void WriteSyntaxVisitor(IndentedTextWriter writer, ImmutableArray<Node> nodes)
        {
            void WriteVisitor(bool generic)
            {
                writer.Write("public partial class SyntaxVisitor");

                if (generic)
                    writer.Write("<T>");

                writer.WriteLine();
                writer.WriteLine("{");

                writer.Indent++;

                for (var i = 0; i < nodes.Length; i++)
                {
                    var node = nodes[i];

                    if (node.IsAbstract)
                        continue;

                    writer.Write("public virtual {0} Visit({1} node", generic ? "T" : "void", node.TypeName);

                    if (generic)
                        writer.Write(", T state");

                    writer.WriteLine(")");
                    writer.WriteLine("{");

                    writer.Indent++;

                    if (generic)
                        writer.Write("return ");

                    writer.Write("DefaultVisit(node");

                    if (generic)
                        writer.Write(", state");

                    writer.WriteLine(");");

                    writer.Indent--;

                    writer.WriteLine("}");

                    if (i != nodes.Length - 1)
                        writer.WriteLine();
                }

                writer.Indent--;

                writer.WriteLine("}");
            }

            WriteVisitor(false);
            writer.WriteLine();
            WriteVisitor(true);
        }
    }
}
