// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using WinRT.SourceGenerator;

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

            HashSet<string> interfacesToAddToVtable = new();
            foreach (var iface in symbol.Interfaces)
            {
                AddInterfaceAndCompatibleInterfacesToVtable(iface);

                foreach (var baseIface in iface.AllInterfaces)
                {
                    AddInterfaceAndCompatibleInterfacesToVtable(baseIface);
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
                interfacesToAddToVtable.ToImmutableArray(),
                hasWinRTExposedBaseType);

            void AddInterfaceAndCompatibleInterfacesToVtable(INamedTypeSymbol iface)
            {
                if (GeneratorHelper.IsWinRTType(iface))
                {
                    interfacesToAddToVtable.Add(iface.ToDisplayString());
                }

                if (iface.IsGenericType && TryGetCompatibleWindowsRuntimeTypesForVariantType(iface, null, out var compatibleIfaces))
                {
                    foreach (var compatibleIface in compatibleIfaces)
                    {
                        interfacesToAddToVtable.Add(compatibleIface.ToDisplayString());
                    }
                }
            }
        }

        private static bool TryGetCompatibleWindowsRuntimeTypesForVariantType(INamedTypeSymbol type, Stack<INamedTypeSymbol> typeStack, out IList<INamedTypeSymbol> compatibleTypes)
        {
            compatibleTypes = null;

            // Out of all the C# interfaces which are valid WinRT interfaces and
            // support covariance, they all only have one generic parameter,
            // so scoping to only handle that.
            if (type is not { IsGenericType: true, TypeParameters: [{ Variance: VarianceKind.Out, IsValueType: false }] })
            {
                return false;
            }

            var definition = type.OriginalDefinition;
            if (!GeneratorHelper.IsWinRTType(definition))
            {
                return false;
            }

            if (typeStack == null)
            {
                typeStack = new Stack<INamedTypeSymbol>();
            }
            else
            {
                if (typeStack.Contains(type))
                {
                    return false;
                }
            }
            typeStack.Push(type);

            HashSet<ITypeSymbol> compatibleTypesForGeneric = new(SymbolEqualityComparer.Default);

            if (GeneratorHelper.IsWinRTType(type.TypeArguments[0]))
            {
                compatibleTypesForGeneric.Add(type.TypeArguments[0]);
            }

            foreach (var iface in type.TypeArguments[0].AllInterfaces)
            {
                if (GeneratorHelper.IsWinRTType(iface))
                {
                    compatibleTypesForGeneric.Add(iface);
                }

                if (iface.IsGenericType
                    && TryGetCompatibleWindowsRuntimeTypesForVariantType(iface, typeStack, out var compatibleIfaces))
                {
                    compatibleTypesForGeneric.UnionWith(compatibleIfaces);
                }
            }

            var baseType = type.TypeArguments[0].BaseType;
            while (baseType != null)
            {
                if (GeneratorHelper.IsWinRTType(baseType))
                {
                    compatibleTypesForGeneric.Add(baseType);
                }
                baseType = baseType.BaseType;
            }

            typeStack.Pop();

            compatibleTypes = new List<INamedTypeSymbol>(compatibleTypesForGeneric.Count);
            foreach (var compatibleType in compatibleTypesForGeneric)
            {
                compatibleTypes.Add(definition.Construct(compatibleType));
            }

            return true;
        }

        private static void GenerateVtableAttributes(SourceProductionContext sourceProductionContext, VtableAttribute vtableAttribute)
        {
            // Even if no interfaces are on the vtable, as long as the base type is a WinRT type,
            // we want to put the attribute as the attribute is the opt-in for the source generated
            // vtable generation.
            if (vtableAttribute.Interfaces.Any() || vtableAttribute.HasWinRTExposedBaseType)
            {
                var vtableEntries = string.Join(", ", vtableAttribute.Interfaces.Select((@interface) => $@"typeof({@interface})"));
                
                StringBuilder source = new();
                if (!vtableAttribute.IsGlobalNamespace)
                {
                    source.Append($$"""
                        namespace {{vtableAttribute.Namespace}}
                        {
                        """);
                }

                source.Append($$"""
                    [global::WinRT.WinRTExposedType({{vtableEntries}})]
                    partial class {{vtableAttribute.ClassName}}
                    {
                    }
                    """);

                if (!vtableAttribute.IsGlobalNamespace)
                {
                    source.Append($@"}}");
                }

                string prefix = vtableAttribute.IsGlobalNamespace ? "" : $"{vtableAttribute.Namespace}.";
                sourceProductionContext.AddSource($"{prefix}{vtableAttribute.ClassName}.WinRTVtable.g.cs", source.ToString());
            }
        }
    }

    internal sealed record VtableAttribute(
        string Namespace,
        bool IsGlobalNamespace,
        string ClassName,
        EquatableArray<string> Interfaces,
        bool HasWinRTExposedBaseType);
}
