﻿// Copyright (c) Microsoft Corporation.
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
            var properties = context.AnalyzerConfigOptionsProvider.Select(static (provider, _) => (provider.IsCsWinRTAotOptimizerEnabled(), provider.IsCsWinRTComponent()));

            var vtableAttributesToAdd = context.SyntaxProvider.CreateSyntaxProvider(
                    static (n, _) => NeedVtableAttribute(n),
                    static (n, _) => GetVtableAttributeToAdd(n)
                ).Where(vtableAttribute => vtableAttribute != null);

            context.RegisterImplementationSourceOutput(vtableAttributesToAdd.Collect().Combine(properties), GenerateVtableAttributes);

            var vtablesToAddOnLookupTable = context.SyntaxProvider.CreateSyntaxProvider(
                    static (n, _) => NeedVtableOnLookupTable(n),
                    static (n, _) => GetVtableAttributesToAddOnLookupTable(n)
                ).Where(vtableAttribute => vtableAttribute != null);

            var genericInterfacesFromVtableAttribute = vtableAttributesToAdd.SelectMany(static (vtableAttribute, _) => vtableAttribute.GenericInterfaces).Collect();
            var genericInterfacesFromVtableLookupTable = vtablesToAddOnLookupTable.SelectMany(static (vtable, _) => vtable.SelectMany(v => v.GenericInterfaces)).Collect();

            context.RegisterImplementationSourceOutput(
                genericInterfacesFromVtableAttribute.Combine(genericInterfacesFromVtableLookupTable).Combine(properties),
                GenerateCCWForGenericInstantiation);

            context.RegisterImplementationSourceOutput(vtablesToAddOnLookupTable.SelectMany(static (vtable, _) => vtable).Collect().Combine(properties), GenerateVtableLookupTable);
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
            return GetVtableAttributeToAdd(symbol, GeneratorHelper.IsWinRTType, context.SemanticModel.Compilation.Assembly, false);
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

        internal static VtableAttribute GetVtableAttributeToAdd(ITypeSymbol symbol, Func<ISymbol, bool> isWinRTType, IAssemblySymbol assemblySymbol, bool isAuthoring, string authoringDefaultInterface = "")
        {
            if (GeneratorHelper.HasNonInstantiatedGeneric(symbol) || GeneratorHelper.HasPrivateclass(symbol))
            {
                return default;
            }

            HashSet<string> interfacesToAddToVtable = new();
            HashSet<GenericInterface> genericInterfacesToAddToVtable = new();

            if (!string.IsNullOrEmpty(authoringDefaultInterface))
            {
                interfacesToAddToVtable.Add(authoringDefaultInterface);
            }

            foreach (var iface in symbol.AllInterfaces)
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
                        // For covariant interfaces which are exclusive interfaces that the projection implemented
                        // such as overrides / protected interfaces in composable types, we don't include them in
                        // the vtable as they are today marked internal and we can't reference them due to that.
                        // If this scenarios matters where native callers do indeed QI for these exclusive
                        // covariant interfaces, we can in the future project them as public, but for now
                        // leaving as is.
                        if (GeneratorHelper.IsInternalInterfaceFromReferences(compatibleIface, assemblySymbol))
                        {
                            continue;
                        }

                        interfacesToAddToVtable.Add(ToFullyQualifiedString(compatibleIface));
                        AddGenericInterfaceInstantiation(compatibleIface);
                    }
                }
            }

            bool isDelegate = false;
            if (symbol.TypeKind == TypeKind.Delegate)
            {
                isDelegate = true;
                interfacesToAddToVtable.Add(ToFullyQualifiedString(symbol));
                AddGenericInterfaceInstantiation(symbol as INamedTypeSymbol);
            }

            if (!interfacesToAddToVtable.Any())
            {
                return default;
            }

            var typeName = ToFullyQualifiedString(symbol);
            bool isGlobalNamespace = symbol.ContainingNamespace == null || symbol.ContainingNamespace.IsGlobalNamespace;
            var @namespace = symbol.ContainingNamespace?.ToDisplayString();
            if (!isGlobalNamespace)
            {
                typeName = typeName[(@namespace.Length + 1)..];
            }

            return new VtableAttribute(
                isAuthoring ? "ABI.Impl." + @namespace : @namespace,
                isGlobalNamespace,
                typeName,
                interfacesToAddToVtable.ToImmutableArray(),
                genericInterfacesToAddToVtable.ToImmutableArray(),
                symbol is IArrayTypeSymbol,
                isDelegate);

            void AddGenericInterfaceInstantiation(INamedTypeSymbol iface)
            {
                if (iface.IsGenericType)
                {
                    List<GenericParameter> genericParameters = new();
                    foreach (var genericParameter in iface.TypeArguments)
                    {
                        // Handle initialization of nested generics as they may not be
                        // initialized already.
                        if (genericParameter is INamedTypeSymbol genericParameterIface && 
                            genericParameterIface.IsGenericType)
                        {
                            AddGenericInterfaceInstantiation(genericParameterIface);
                        }

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

            bool IsExternalInternalInterface(INamedTypeSymbol iface)
            {
                return (iface.DeclaredAccessibility == Accessibility.Internal && !SymbolEqualityComparer.Default.Equals(iface.ContainingAssembly, assemblySymbol)) || 
                    (iface.IsGenericType && iface.TypeArguments.Any(typeArgument => IsExternalInternalInterface(typeArgument as INamedTypeSymbol)));
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

        private static void GenerateVtableAttributes(SourceProductionContext sourceProductionContext, (ImmutableArray<VtableAttribute> vtableAttributes, (bool isCsWinRTAotOptimizerEnabled, bool isCsWinRTComponent) properties) value)
        {
            if (value.properties.isCsWinRTComponent || !value.properties.isCsWinRTAotOptimizerEnabled)
            {
                // Handled by the WinRT component source generator.
                return;
            }

            GenerateVtableAttributes(sourceProductionContext.AddSource, value.vtableAttributes);
        }

        internal static string GenerateVtableEntry(VtableAttribute vtableAttribute)
        {
            StringBuilder source = new();

            foreach (var genericInterface in vtableAttribute.GenericInterfaces)
            {
                source.AppendLine(GenericVtableInitializerStrings.GetInstantiationInitFunction(
                    genericInterface.GenericDefinition,
                    genericInterface.GenericParameters));
            }

            if (vtableAttribute.IsDelegate)
            {
                var @interface = vtableAttribute.Interfaces.First();
                source.AppendLine();
                source.AppendLine($$"""
                                var delegateInterface = new global::System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry
                                {
                                    IID = global::WinRT.GuidGenerator.GetIID(typeof(global::{{@interface}}).GetHelperType()),
                                    Vtable = global::ABI.{{@interface}}.AbiToProjectionVftablePtr
                                };

                                return global::WinRT.DelegateTypeDetails<{{@interface}}>.GetExposedInterfaces(delegateInterface);
                        """);
            }
            else if (vtableAttribute.Interfaces.Any())
            {
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

                    source.AppendLine($$"""
                                                new global::System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry
                                                {
                                                    IID = global::ABI.{{interfaceStaticsMethod}}.IID,
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

            return source.ToString();
        }

        internal static void GenerateVtableAttributes(Action<string, string> addSource, ImmutableArray<VtableAttribute> vtableAttributes)
        {
            // Using ToImmutableHashSet to avoid duplicate entries from the use of partial classes by the developer
            // to split out their implementation.  When they do that, we will get multiple entries here for that
            // and try to generate the same attribute and file with the same data as we use the semantic model
            // to get all the symbol data rather than the data at an instance of a partial class definition.
            foreach (var vtableAttribute in vtableAttributes.ToImmutableHashSet())
            {
                if (vtableAttribute.Interfaces.Any())
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

                            source.AppendLine($$"""
                                                new global::System.Runtime.InteropServices.ComWrappers.ComInterfaceEntry
                                                {
                                                    IID = global::ABI.{{interfaceStaticsMethod}}.IID,
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

        private static void GenerateCCWForGenericInstantiation(SourceProductionContext sourceProductionContext, 
            ((ImmutableArray<GenericInterface> list1, ImmutableArray<GenericInterface> list2) genericInterfaces, (bool isCsWinRTAotOptimizerEnabled, bool isCsWinRTComponent) properties) value)
        {
            if (value.properties.isCsWinRTComponent || !value.properties.isCsWinRTAotOptimizerEnabled)
            {
                // Handled by the WinRT component source generator.
                return;
            }

            GenerateCCWForGenericInstantiation(sourceProductionContext.AddSource, value.genericInterfaces.list1.AddRange(value.genericInterfaces.list2));
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

        private static bool NeedVtableOnLookupTable(SyntaxNode node)
        {
            return (node is InvocationExpressionSyntax invocation && invocation.ArgumentList.Arguments.Count != 0) ||
                    node is AssignmentExpressionSyntax ||
                    node is AwaitExpressionSyntax;
        }

        private static List<VtableAttribute> GetVtableAttributesToAddOnLookupTable(GeneratorSyntaxContext context)
        {
            HashSet<VtableAttribute> vtableAttributes = new();

            if (context.Node is InvocationExpressionSyntax invocation)
            {
                var invocationSymbol = context.SemanticModel.GetSymbolInfo(invocation.Expression).Symbol;
                // Check if function is within a CsWinRT projected class or interface.
                if (invocationSymbol is IMethodSymbol methodSymbol && GeneratorHelper.IsWinRTType(methodSymbol.ContainingSymbol))
                {
                    // Get the concrete types directly from the argument rather than
                    // using what the method accepts, which might just be an interface, so
                    // that we can try to include any other WinRT interfaces implemented by
                    // that type on the CCW when it is marshaled.
                    for (int idx = 0, paramsIdx = 0; idx < invocation.ArgumentList.Arguments.Count; idx++)
                    {
                        if (methodSymbol.Parameters[paramsIdx].RefKind != RefKind.Out)
                        {
                            var argumentType = context.SemanticModel.GetTypeInfo(invocation.ArgumentList.Arguments[idx].Expression);

                            // This handles the case where there is an WinRT array passed
                            // as an object. In this case, the IList interfaces of the array
                            // type need to be put on the CCW.
                            if (argumentType.Type is IArrayTypeSymbol arrayType)
                            {
                                if (methodSymbol.Parameters[paramsIdx].Type is not IArrayTypeSymbol)
                                {
                                    var vtableAtribute = GetVtableAttributeToAdd(arrayType, GeneratorHelper.IsWinRTType, context.SemanticModel.Compilation.Assembly, false);
                                    if (vtableAtribute != default)
                                    {
                                        vtableAttributes.Add(vtableAtribute);
                                    }

                                    // Also add the enumerator type to the lookup table as the native caller may call it.
                                    AddEnumeratorAdapterForType(arrayType.ElementType, context.SemanticModel, vtableAttributes);
                                }
                            }
                            else if (argumentType.Type is not null)
                            {
                                var argumentClassTypeSymbol = argumentType.Type;

                                // This handles the case where a generic delegate is passed.
                                if (argumentClassTypeSymbol.TypeKind == TypeKind.Delegate &&
                                    argumentClassTypeSymbol.MetadataName.Contains("`") &&
                                    GeneratorHelper.IsWinRTType(argumentClassTypeSymbol))
                                {
                                    var argumentClassNamedTypeSymbol = argumentClassTypeSymbol as INamedTypeSymbol;
                                    vtableAttributes.Add(GetVtableAttributeToAdd(argumentClassTypeSymbol, GeneratorHelper.IsWinRTType, context.SemanticModel.Compilation.Assembly, false));
                                }

                                // This handles the case where the source generator wasn't able to run
                                // and put the WinRTExposedType attribute on the class. This can be in the
                                // scenario where the caller defined their own generic class and
                                // pass it as a parameter.  With generic classes, the interface itself
                                // might be generic too and due to that we handle it here.
                                // This also handles the case where the type being passed is from a different
                                // library which happened to not run the AOT optimizer.  So as a best effort,
                                // we handle it here.
                                if (argumentClassTypeSymbol.TypeKind == TypeKind.Class &&
                                    (argumentClassTypeSymbol.MetadataName.Contains("`") ||
                                    (!GeneratorHelper.IsWinRTType(argumentClassTypeSymbol) &&
                                        !GeneratorHelper.HasWinRTExposedTypeAttribute(argumentClassTypeSymbol) &&
                                        // If the type is defined in the same assembly as what the source generator is running on,
                                        // we let the WinRTExposedType attribute generator handle it.
                                        !SymbolEqualityComparer.Default.Equals(argumentClassTypeSymbol.ContainingAssembly, context.SemanticModel.Compilation.Assembly))))
                                {
                                    var vtableAtribute = GetVtableAttributeToAdd(argumentClassTypeSymbol, GeneratorHelper.IsWinRTType, context.SemanticModel.Compilation.Assembly, false);
                                    if (vtableAtribute != default)
                                    {
                                        vtableAttributes.Add(vtableAtribute);
                                    }

                                    AddEnumeratorAdapterForEnumerableInterface(argumentClassTypeSymbol, context.SemanticModel, vtableAttributes);
                                }
                            }
                        }

                        // The method parameter can be declared as params which means
                        // an array of arguments can be passed for it and it is the
                        // last argument.
                        if (!methodSymbol.Parameters[paramsIdx].IsParams)
                        {
                            paramsIdx++;
                        }
                    }
                }
            }
            else if (context.Node is AssignmentExpressionSyntax assignment)
            {
                var leftSymbol = context.SemanticModel.GetSymbolInfo(assignment.Left).Symbol;

                // Check if property being assigned to is within a CsWinRT projected class or interface.
                if (leftSymbol is IPropertySymbol propertySymbol &&
                    GeneratorHelper.IsWinRTType(propertySymbol.ContainingSymbol))
                {
                    var argumentType = context.SemanticModel.GetTypeInfo(assignment.Right);

                    // This handles the case where there is an WinRT array being assigned
                    // to an object type. In this case, the IList interfaces of the array
                    // type need to be put on the CCW.
                    if (argumentType.Type is IArrayTypeSymbol arrayType)
                    {
                        if (propertySymbol.Type is not IArrayTypeSymbol)
                        {
                            var vtableAtribute = GetVtableAttributeToAdd(arrayType, GeneratorHelper.IsWinRTType, context.SemanticModel.Compilation.Assembly, false);
                            if (vtableAtribute != default)
                            {
                                vtableAttributes.Add(vtableAtribute);
                            }

                            // Also add the enumerator type to the lookup table as the native caller can call it.
                            AddEnumeratorAdapterForType(arrayType.ElementType, context.SemanticModel, vtableAttributes);
                        }
                    }
                    else if (argumentType.Type is not null || argumentType.ConvertedType is not null)
                    {
                        // Type might be null such as for lambdas, so check converted type in that case.
                        var argumentClassTypeSymbol = argumentType.Type ?? argumentType.ConvertedType;

                        // Check if a generic class or delegate or it isn't a
                        // WinRT type meaning we will need to probably create a CCW for.
                        if (argumentClassTypeSymbol.TypeKind == TypeKind.Delegate &&
                            argumentClassTypeSymbol.MetadataName.Contains("`") &&
                            GeneratorHelper.IsWinRTType(argumentClassTypeSymbol))
                        {
                            var argumentClassNamedTypeSymbol = argumentClassTypeSymbol as INamedTypeSymbol;
                            vtableAttributes.Add(GetVtableAttributeToAdd(argumentClassTypeSymbol, GeneratorHelper.IsWinRTType, context.SemanticModel.Compilation.Assembly, false));
                        }

                        if (argumentClassTypeSymbol.TypeKind == TypeKind.Class &&
                            (argumentClassTypeSymbol.MetadataName.Contains("`") ||
                            (!GeneratorHelper.IsWinRTType(argumentClassTypeSymbol) &&
                                !GeneratorHelper.HasWinRTExposedTypeAttribute(argumentClassTypeSymbol) &&
                                // If the type is defined in the same assembly as what the source generator is running on,
                                // we let the WinRTExposedType attribute generator handle it.
                                !SymbolEqualityComparer.Default.Equals(argumentClassTypeSymbol.ContainingAssembly, context.SemanticModel.Compilation.Assembly))))
                        {
                            var vtableAtribute = GetVtableAttributeToAdd(argumentClassTypeSymbol, GeneratorHelper.IsWinRTType, context.SemanticModel.Compilation.Assembly, false);
                            if (vtableAtribute != default)
                            {
                                vtableAttributes.Add(vtableAtribute);
                            }

                            AddEnumeratorAdapterForEnumerableInterface(argumentClassTypeSymbol, context.SemanticModel, vtableAttributes);
                        }
                    }
                }
                // This handles the class.event += event scenario where the event
                // can be generic and not have the have the WinRTExposedType attribute.
                else if (leftSymbol is IEventSymbol eventSymbol &&
                    GeneratorHelper.IsWinRTType(eventSymbol.ContainingSymbol) &&
                    eventSymbol.Type.MetadataName.Contains("`") &&
                    GeneratorHelper.IsWinRTType(eventSymbol.Type))
                {
                    var vtableAtribute = GetVtableAttributeToAdd(eventSymbol.Type, GeneratorHelper.IsWinRTType, context.SemanticModel.Compilation.Assembly, false);
                    if (vtableAtribute != default)
                    {
                        vtableAttributes.Add(vtableAtribute);
                    }
                }
            }
            // This handles the await asyncFunction() scenario where
            // the awaiter will register a completed handler which can be
            // a generic.
            else if (context.Node is AwaitExpressionSyntax awaitExpression)
            {
                var methodSymbol = context.SemanticModel.GetSymbolInfo(awaitExpression.Expression).Symbol as IMethodSymbol;
                // Check if await is being called on a WinRT function.
                if (methodSymbol != null && GeneratorHelper.IsWinRTType(methodSymbol.ContainingSymbol))
                {
                    var completedProperty = methodSymbol.ReturnType.GetMembers("Completed")[0] as IPropertySymbol;
                    if (completedProperty.Type.MetadataName.Contains("`"))
                    {
                        vtableAttributes.Add(GetVtableAttributeToAdd(completedProperty.Type, GeneratorHelper.IsWinRTType, context.SemanticModel.Compilation.Assembly, false));
                    }
                }
            }

            return vtableAttributes.ToList();
        }

        // Any of the IEnumerable interfaces on the vtable can be used to get the enumerator.  Given IEnumerable is
        // a covariant interface, it means that we can end up getting an instance of the enumerable adapter for any one
        // of those covariant interfaces and thereby need vtable lookup entries for all of them.
        private static void AddEnumeratorAdapterForType(ITypeSymbol type, SemanticModel semanticModel, HashSet<VtableAttribute> vtableAttributes)
        {
            var enumerableType = semanticModel.Compilation.GetTypeByMetadataName("System.Collections.Generic.IEnumerable`1").
                Construct(type);
            if (TryGetCompatibleWindowsRuntimeTypesForVariantType(enumerableType, null, GeneratorHelper.IsWinRTType, out var compatibleIfaces))
            {
                foreach (var compatibleIface in compatibleIfaces)
                {
                    if (compatibleIface.MetadataName == "IEnumerable`1" && 
                        !GeneratorHelper.IsInternalInterfaceFromReferences(compatibleIface, semanticModel.Compilation.Assembly))
                    {
                        var enumeratorAdapterType = semanticModel.Compilation.GetTypeByMetadataName("ABI.System.Collections.Generic.ToAbiEnumeratorAdapter`1").
                            Construct(compatibleIface.TypeArguments[0]);
                        vtableAttributes.Add(GetVtableAttributeToAdd(enumeratorAdapterType, GeneratorHelper.IsWinRTType, semanticModel.Compilation.Assembly, false));
                    }
                }
            }
        }

        private static void AddEnumeratorAdapterForEnumerableInterface(ITypeSymbol classType, SemanticModel semanticModel, HashSet<VtableAttribute> vtableAttributes)
        {
            // Type may implement multiple unique IEnumerable interfaces.
            foreach (var iface in classType.AllInterfaces)
            {
                if (iface.MetadataName == "IEnumerable`1")
                {
                    AddEnumeratorAdapterForType(iface.TypeArguments[0], semanticModel, vtableAttributes);
                }
            }
        }

        private static void GenerateVtableLookupTable(SourceProductionContext sourceProductionContext, (ImmutableArray<VtableAttribute> vtableAttributes, (bool isCsWinRTAotOptimizerEnabled, bool isCsWinRTComponent) properties) value)
        {
            if (!value.properties.isCsWinRTAotOptimizerEnabled)
            {
                return;
            }

            StringBuilder source = new();

            if (value.vtableAttributes.Any())
            {
                source.AppendLine($$"""
                                    using System; 
                                    using System.Runtime.InteropServices;
                                    using System.Runtime.CompilerServices;

                                    namespace WinRT.GenericHelpers
                                    {

                                        internal static class GlobalVtableLookup
                                        {

                                            [System.Runtime.CompilerServices.ModuleInitializer]
                                            internal static void InitializeGlobalVtableLookup()
                                            {
                                                ComWrappersSupport.RegisterTypeComInterfaceEntriesLookup(LookupVtableEntries);
                                            }

                                            private static ComWrappers.ComInterfaceEntry[] LookupVtableEntries(Type type)
                                            {
                                    """);
            }

            foreach (var vtableAttribute in value.vtableAttributes.ToImmutableHashSet())
            {
                string className = vtableAttribute.IsGlobalNamespace ? 
                    vtableAttribute.ClassName : $"global::{vtableAttribute.Namespace}.{vtableAttribute.ClassName}";
                source.AppendLine($$"""
                                if (type == typeof({{className}}))
                                {
                                    {{GenerateVtableEntry(vtableAttribute)}}
                                }
                    """);
            }

            if (value.vtableAttributes.Any())
            {
                source.AppendLine($$"""
                                                return default;
                                            }
                                        }
                                    }
                                    """);
                sourceProductionContext.AddSource($"WinRTGlobalVtableLookup.g.cs", source.ToString());
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
        EquatableArray<GenericInterface> GenericInterfaces,
        bool IsArray,
        bool IsDelegate);
}