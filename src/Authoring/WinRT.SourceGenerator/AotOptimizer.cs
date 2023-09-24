// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
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
            var isCsWinRTComponent = context.AnalyzerConfigOptionsProvider.Select(static (provider, _) => provider.IsCsWinRTComponent());

            var vtableAttributesToAdd = context.SyntaxProvider.CreateSyntaxProvider(
                static (n, _) => NeedVtableAttribute(n),
                static (n, _) => GetVtableAttributeToAdd(n));

            context.RegisterImplementationSourceOutput(vtableAttributesToAdd.Collect().Combine(isCsWinRTComponent), GenerateVtableAttributes);

            var genericInstantionsToGenerate = vtableAttributesToAdd.SelectMany(static (vtableAttribute, _) => vtableAttribute.GenericInterfaces).Collect();
            context.RegisterImplementationSourceOutput(genericInstantionsToGenerate.Combine(isCsWinRTComponent), GenerateCCWForGenericInstantiation);
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
            return GetVtableAttributeToAdd(symbol, GeneratorHelper.IsWinRTType, false);
        }

        private static string ToFullyQualifiedString(ISymbol symbol)
        {
            // Used to ensure class names within generics are fully qualified to avoid
            // having issues when put in ABI namespaces.
            var symbolDisplayString = new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Included,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

            var qualifiedString = symbol.ToDisplayString(symbolDisplayString);
            return qualifiedString.StartsWith("global::") ? qualifiedString[8..] : qualifiedString;
        }

        internal static VtableAttribute GetVtableAttributeToAdd(INamedTypeSymbol symbol, Func<ISymbol, bool> isWinRTType, bool isAuthoring, string authoringDefaultInterface = "")
        {
            bool IsNamedTypeWinRTType(INamedTypeSymbol namedType) => isWinRTType(namedType);

            HashSet<string> interfacesToAddToVtable = new();
            HashSet<GenericInterface> genericInterfacesToAddToVtable = new();

            if (!string.IsNullOrEmpty(authoringDefaultInterface))
            {
                interfacesToAddToVtable.Add(authoringDefaultInterface);
            }

            foreach (var iface in symbol.AllInterfaces)
            {
                AddInterfaceAndCompatibleInterfacesToVtable(iface);
            }

            bool hasWinRTExposedBaseType = false;
            var baseType = symbol.BaseType;
            while (baseType != null && baseType.SpecialType != SpecialType.System_Object)
            {
                if (isWinRTType(baseType) ||
                    baseType.Interfaces.Any(IsNamedTypeWinRTType))
                {
                    hasWinRTExposedBaseType = true;
                    break;
                }

                baseType = baseType.BaseType;
            }

            return new VtableAttribute(
                isAuthoring ? "ABI.Impl." + symbol.ContainingNamespace.ToDisplayString() : symbol.ContainingNamespace.ToDisplayString(),
                symbol.ContainingNamespace.IsGlobalNamespace,
                symbol.Name,
                interfacesToAddToVtable.ToImmutableArray(),
                hasWinRTExposedBaseType,
                genericInterfacesToAddToVtable.ToImmutableArray());

            void AddInterfaceAndCompatibleInterfacesToVtable(INamedTypeSymbol iface)
            {
                if (isWinRTType(iface))
                {
                    interfacesToAddToVtable.Add(ToFullyQualifiedString(iface));
                    AddGenericInterfaceInstantiation(iface);
                }

                if (iface.IsGenericType && TryGetCompatibleWindowsRuntimeTypesForVariantType(iface, null, isWinRTType, out var compatibleIfaces))
                {
                    foreach (var compatibleIface in compatibleIfaces)
                    {
                        interfacesToAddToVtable.Add(ToFullyQualifiedString(compatibleIface));
                        AddGenericInterfaceInstantiation(compatibleIface);
                    }
                }
            }

            void AddGenericInterfaceInstantiation(INamedTypeSymbol iface)
            {
                if (iface.IsGenericType)
                {
                    List<GenericParameter> genericParameters = new();
                    foreach(var genericParameter in iface.TypeArguments)
                    {
                        genericParameters.Add(new GenericParameter(
                            ToFullyQualifiedString(genericParameter),
                            GeneratorHelper.GetAbiType(genericParameter),
                            genericParameter.TypeKind));
                    }

                    genericInterfacesToAddToVtable.Add(new GenericInterface(
                        ToFullyQualifiedString(iface),
                        $$"""{{iface.ContainingNamespace}}.{{iface.MetadataName}}""",
                        genericParameters.ToImmutableArray()));
                }
            }
        }

        private static bool TryGetCompatibleWindowsRuntimeTypesForVariantType(INamedTypeSymbol type, Stack<INamedTypeSymbol> typeStack, Func<ISymbol, bool> isWinRTType, out IList<INamedTypeSymbol> compatibleTypes)
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
            if (!isWinRTType(definition))
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

            if (isWinRTType(type.TypeArguments[0]))
            {
                compatibleTypesForGeneric.Add(type.TypeArguments[0]);
            }

            foreach (var iface in type.TypeArguments[0].AllInterfaces)
            {
                if (isWinRTType(iface))
                {
                    compatibleTypesForGeneric.Add(iface);
                }

                if (iface.IsGenericType
                    && TryGetCompatibleWindowsRuntimeTypesForVariantType(iface, typeStack, isWinRTType, out var compatibleIfaces))
                {
                    compatibleTypesForGeneric.UnionWith(compatibleIfaces);
                }
            }

            var baseType = type.TypeArguments[0].BaseType;
            while (baseType != null)
            {
                if (isWinRTType(baseType))
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

        private static void GenerateVtableAttributes(SourceProductionContext sourceProductionContext, (ImmutableArray<VtableAttribute> vtableAttributes, bool isCsWinRTComponent) value)
        {
            if (value.isCsWinRTComponent)
            {
                // Handled by the WinRT component source generator.
                return;
            }

            GenerateVtableAttributes(sourceProductionContext.AddSource, value.vtableAttributes);
        }

        internal static void GenerateVtableAttributes(Action<string, string> addSource, ImmutableArray<VtableAttribute> vtableAttributes)
        {
            // Using ToImmutableHashSet to avoid duplicate entries from the use of partial classes by the developer
            // to split out their implementation.  When they do that, we will get multiple entries here for that
            // and try to generate the same attribute and file with the same data as we use the semantic model
            // to get all the symbol data rather than the data at an instance of a partial class definition.
            foreach (var vtableAttribute in vtableAttributes.ToImmutableHashSet())
            {
                // Even if no interfaces are on the vtable, as long as the base type is a WinRT type,
                // we want to put the attribute as the attribute is the opt-in for the source generated
                // vtable generation.
                if (vtableAttribute.Interfaces.Any() || vtableAttribute.HasWinRTExposedBaseType)
                {
                    StringBuilder source = new();
                    source.AppendLine("using static WinRT.TypeExtensions;\n");
                    if (!vtableAttribute.IsGlobalNamespace)
                    {
                        source.AppendLine($$"""
                        namespace {{vtableAttribute.Namespace}}
                        {
                        """);
                    }

                    source.AppendLine($$"""
                    [global::WinRT.WinRTExposedType(typeof({{vtableAttribute.ClassName}}WinRTTypeDetails))]
                    partial class {{vtableAttribute.ClassName}}
                    {
                    }

                    internal sealed class {{vtableAttribute.ClassName}}WinRTTypeDetails : global::WinRT.IWinRTExposedTypeDetails
                    {
                        public global::System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry[] GetExposedInterfaces()
                        {
                    """);

                    if (vtableAttribute.Interfaces.Any())
                    {
                        foreach (var genericInterface in vtableAttribute.GenericInterfaces)
                        {
                            source.AppendLine(GenericVtableInitializerStrings.GetInstantiationInitFunction(
                                genericInterface.GenericDefinition,
                                genericInterface.GenericParameters));
                        }

                        source.AppendLine();
                        source.AppendLine($$"""
                                return new global::System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry[]
                                {
                        """);

                        foreach (var @interface in vtableAttribute.Interfaces)
                        {
                            var genericStartIdx = @interface.IndexOf('<');
                            var interfaceStaticsMethod = @interface[..(genericStartIdx == -1 ? @interface.Length : genericStartIdx)] + "Methods";
                            if (genericStartIdx != -1)
                            {
                                interfaceStaticsMethod += @interface[genericStartIdx..@interface.Length];
                            }

                            // TODO: replace once IID propagated to manual projections
                            // For now, special casing the Overrides interfaces which are marked internal
                            // and the authored default interface which are under the Impl namespace.
                            var iid = @interface.Contains("Overrides") || @interface.EndsWith("Class") ? $"global::ABI.{interfaceStaticsMethod}.IID" :
                                $"global::WinRT.GuidGenerator.GetIID(typeof(global::{@interface}).GetHelperType())";

                            source.AppendLine($$"""
                                                new global::System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry
                                                {
                                                    IID = {{iid}},
                                                    Vtable = global::ABI.{{interfaceStaticsMethod}}.AbiToProjectionVftablePtr
                                                },
                                    """);
                        }
                        source.AppendLine($$"""
                                };
                                """);
                    }
                    else
                    {
                        source.AppendLine($$"""
                                        return global::System.Array.Empty<global::System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry>();
                                """);
                    }

                    source.AppendLine($$"""
                                }
                            }
                        """);

                    if (!vtableAttribute.IsGlobalNamespace)
                    {
                        source.AppendLine($@"}}");
                    }

                    string prefix = vtableAttribute.IsGlobalNamespace ? "" : $"{vtableAttribute.Namespace}.";
                    addSource($"{prefix}{vtableAttribute.ClassName}.WinRTVtable.g.cs", source.ToString());
                }
            }
        }

        private static void GenerateCCWForGenericInstantiation(SourceProductionContext sourceProductionContext, (ImmutableArray<GenericInterface> genericInterfaces, bool isCsWinRTComponent) value)
        {
            if (value.isCsWinRTComponent)
            {
                // Handled by the WinRT component source generator.
                return;
            }

            GenerateCCWForGenericInstantiation(sourceProductionContext.AddSource, value.genericInterfaces);
        }

        internal static void GenerateCCWForGenericInstantiation(Action<string, string> addSource, ImmutableArray<GenericInterface> genericInterfaces)
        {
            StringBuilder source = new();

            if (genericInterfaces.Any())
            {
                source.AppendLine($$"""
                                    using System;
                                    using System.Runtime.InteropServices;
                                    using System.Runtime.CompilerServices;

                                    namespace WinRT.GenericHelpers
                                    {
                                    """);
            }

            foreach (var genericInterface in genericInterfaces.ToImmutableHashSet())
            {
                source.AppendLine();
                source.AppendLine("// " + genericInterface.Interface);
                source.AppendLine(GenericVtableInitializerStrings.GetInstantiation(
                    genericInterface.GenericDefinition,
                    genericInterface.GenericParameters));
            }

            if (genericInterfaces.Any())
            {
                source.AppendLine("}");
                addSource($"WinRTGenericInstantiation.g.cs", source.ToString());
            }
        }
    }

    internal readonly record struct GenericParameter(
        string ProjectedType,
        string AbiType,
        TypeKind TypeKind);

    internal readonly record struct GenericInterface(
        string Interface,
        string GenericDefinition,
        EquatableArray<GenericParameter> GenericParameters);

    internal sealed record VtableAttribute(
        string Namespace,
        bool IsGlobalNamespace,
        string ClassName,
        EquatableArray<string> Interfaces,
        bool HasWinRTExposedBaseType,
        EquatableArray<GenericInterface> GenericInterfaces);
}