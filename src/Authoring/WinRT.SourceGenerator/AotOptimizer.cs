using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Generator
{
    [Generator]
    public class WinRTAotSourceGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var vtableAttributesToAdd = context.SyntaxProvider.CreateSyntaxProvider(
                static (n, _) => NeedVtableAttribute(n),
                static (n, _) => GetVtableAttributeToAdd(n));

            context.RegisterImplementationSourceOutput(vtableAttributesToAdd, GenerateVtableAttributes);
        }

        // Restrict to non-projected classes which can be instantiated
        // and are partial allowing to add attributes.
        private static bool NeedVtableAttribute(SyntaxNode node)
        {
            return node is ClassDeclarationSyntax declaration &&
                !declaration.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword) || m.IsKind(SyntaxKind.AbstractKeyword)) &&
                declaration.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)) &&
                !GeneratorHelper.IsWinRTType(declaration); // Making sure it isn't an RCW we are projecting.
        }

        private static VtableAttribute GetVtableAttributeToAdd(GeneratorSyntaxContext context)
        {
            var symbol = context.SemanticModel.GetDeclaredSymbol(context.Node as ClassDeclarationSyntax);

            // Filter to the WinRT interfaces.
            List<string> interfacesToAddToVtable = new();
            foreach (var @interface in symbol.Interfaces)
            {
                if (GeneratorHelper.IsWinRTType(@interface))
                {
                    interfacesToAddToVtable.Add(@interface.ToDisplayString());
                }
            }

            bool hasWinRTExposedBaseType = false;
            var baseType = symbol.BaseType;
            while (baseType != null)
            {
                if (GeneratorHelper.IsWinRTType(baseType) ||
                    baseType.Interfaces.Any(GeneratorHelper.IsWinRTType))
                {
                    hasWinRTExposedBaseType = true;
                    break;
                }

                baseType = baseType.BaseType;
            }

            return new VtableAttribute(
                symbol.ContainingNamespace.ToDisplayString(),
                symbol.ContainingNamespace.IsGlobalNamespace,
                symbol.Name, 
                interfacesToAddToVtable.ToArray(),
                hasWinRTExposedBaseType);
        }

        private static void GenerateVtableAttributes(SourceProductionContext sourceProductionContext, VtableAttribute vtableAttribute)
        {
            if (vtableAttribute.Interfaces.Any() || vtableAttribute.HasWinRTExposedBaseType)
            {
                var vtableEntries = string.Join(", ", vtableAttribute.Interfaces.Select((@interface) => $@"typeof({@interface})"));
                
                StringBuilder source = new();
                if (!vtableAttribute.IsGlobalNamespace)
                {
                    source.Append($@"namespace {vtableAttribute.Namespace}{{");
                }

                source.Append($@"
    [global::WinRT.WinRTExposedType({vtableEntries})]
    partial class {vtableAttribute.ClassName}
    {{
    }}");

                if (!vtableAttribute.IsGlobalNamespace)
                {
                    source.Append($@"}}");
                }

                string prefix = vtableAttribute.IsGlobalNamespace ? "" : $"{vtableAttribute.Namespace}.";
                sourceProductionContext.AddSource($"{prefix}{vtableAttribute.ClassName}.WinRTVtable.g.cs", source.ToString());
            }
        }
    }

    internal readonly struct VtableAttribute
    {
        public string Namespace { get; }
        public bool IsGlobalNamespace { get; }
        public string ClassName { get; }
        public string[] Interfaces { get; }
        public bool HasWinRTExposedBaseType { get; }

        public VtableAttribute(string @namespace, bool isGlobalNamespace, string className, string[] interfaces, bool hasWinRTExposedBaseType)
        {
            Namespace = @namespace;
            IsGlobalNamespace = isGlobalNamespace;
            ClassName = className;
            Interfaces = interfaces;
            HasWinRTExposedBaseType = hasWinRTExposedBaseType;
        }
    }
}
