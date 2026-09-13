// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if ROSLYN_4_12_0_OR_GREATER

using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;
using WinRT.SourceGenerator;

#nullable enable

namespace Generator;

/// <summary>
/// A diagnostic analyzer to warn for collection expressions that are not AOT compatible in WinRT scenarios.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CollectionExpressionAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [WinRTRules.NonEmptyCollectionExpressionTargetingNonBuilderInterfaceType];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(static context =>
        {
            // We only need to emit warnings if CsWinRT is in 'auto' mode
            if (!GeneratorExecutionContextHelper.IsCsWinRTAotOptimizerInAutoMode(context.Options.AnalyzerConfigOptionsProvider, context.Compilation))
            {
                return;
            }

            // Avoid registering the flow analysis for the common case where the compilation has no
            // collection expressions at all.
            if (!context.Compilation.SyntaxTrees.Any(
                tree => tree.GetRoot(context.CancellationToken).DescendantNodes().Any(static node => node is CollectionExpressionSyntax)))
            {
                return;
            }

            // Get the symbols for '[CollectionBuilder]', we need them for lookups. Note that we cannot just
            // use 'GetTypeByMetadataName' here, as it's possible for the attribute to exist across multiple
            // assemblies. This is the case if any referenced assemblies is using polyfills due to targeting
            // an older TFM that does not have the attribute. We still want to work correctly in those cases.
            // We can just use an array here, since in the vast majority of cases we only expect 1-2 items.
            ImmutableArray<INamedTypeSymbol> collectionBuilderSymbols = context.Compilation.GetTypesByMetadataName("System.Runtime.CompilerServices.CollectionBuilderAttribute");
            TypeMapper typeMapper = new(context.Options.AnalyzerConfigOptionsProvider.GetCsWinRTUseWindowsUIXamlProjections());
            bool isCsWinRTComponent = context.Options.AnalyzerConfigOptionsProvider.IsCsWinRTComponent();
            Func<ISymbol, TypeMapper, bool> isWinRTType = GeneratorHelper.IsWinRTType(
                context.Compilation,
                isCsWinRTComponent);
            Func<ISymbol, bool, bool> isWinRTClassOrInterface = GeneratorHelper.IsWinRTClassOrInterface(
                context.Compilation,
                isWinRTType,
                typeMapper);

            ConcurrentDictionary<CollectionExpressionKey, CollectionExpressionCandidate> candidates = new();

            // Edges point from a value's origin to its destination. Compilation-end analysis walks them
            // backwards from WinRT sinks so values that stay in managed code do not produce diagnostics.
            ConcurrentBag<FlowEdge> edges = [];
            ConcurrentDictionary<FlowNode, byte> sinks = new(FlowNodeComparer.Instance);
            ConcurrentDictionary<ISymbol, bool> winRTBoundaryTypes = new(SymbolEqualityComparer.Default);
            ConcurrentDictionary<ISymbol, bool> winRTBoundaryMethods = new(SymbolEqualityComparer.Default);
            ConcurrentDictionary<ISymbol, byte> methodsWithDispatchFlows = new(SymbolEqualityComparer.Default);

            CollectionExpressionCandidate? GetCandidate(ICollectionExpressionOperation operation)
            {
                // We only possibly warn if the target type is a generic interface type.
                if (operation.Type is not INamedTypeSymbol { TypeKind: TypeKind.Interface, IsGenericType: true } typeSymbol)
                {
                    return null;
                }

                // Empty collection expressions have a known concrete type.
                if (operation.Elements.IsEmpty)
                {
                    return null;
                }

                // 'ICollection<T>' and 'IList<T>' are guaranteed to use 'List<T>'.
                if (typeSymbol.ConstructedFrom.SpecialType is
                    SpecialType.System_Collections_Generic_ICollection_T or
                    SpecialType.System_Collections_Generic_IList_T)
                {
                    return null;
                }

                // A collection builder also gives the expression a statically knowable concrete type.
                if (GeneratorHelper.HasAttributeWithAnyType(typeSymbol, collectionBuilderSymbols))
                {
                    return null;
                }

                CollectionExpressionKey key = new(operation.Syntax.SyntaxTree, operation.Syntax.Span);

                return candidates.GetOrAdd(
                    key,
                    _ => new CollectionExpressionCandidate(key, operation.Syntax.GetLocation(), typeSymbol));
            }

            FlowNode GetSymbolNode(ISymbol symbol)
            {
                if (symbol is IMethodSymbol method)
                {
                    AddMethodDispatchFlows(method);
                }
                else if (symbol is IParameterSymbol { ContainingSymbol: IMethodSymbol containingMethod })
                {
                    AddMethodDispatchFlows(containingMethod);
                }
                else if (symbol is IPropertySymbol property)
                {
                    if (property.GetMethod is not null)
                    {
                        AddMethodDispatchFlows(property.GetMethod);
                    }

                    if (property.SetMethod is not null)
                    {
                        AddMethodDispatchFlows(property.SetMethod);
                    }
                }

                return FlowNode.ForSymbol(NormalizeFlowSymbol(symbol));
            }

            void AddEdge(FlowNode source, FlowNode target)
            {
                if (!FlowNodeComparer.Instance.Equals(source, target))
                {
                    edges.Add(new(source, target));
                }
            }

            void AddValueFlows(IOperation value, FlowNode target)
            {
                switch (value)
                {
                    case ICollectionExpressionOperation collectionExpression:
                        if (GetCandidate(collectionExpression) is { } candidate)
                        {
                            AddEdge(FlowNode.ForCollectionExpression(candidate.Key), target);
                        }
                        break;
                    case IConversionOperation conversion:
                        AddValueFlows(conversion.Operand, target);
                        break;
                    case IParenthesizedOperation parenthesized:
                        AddValueFlows(parenthesized.Operand, target);
                        break;
                    case ILocalReferenceOperation local:
                        AddEdge(GetSymbolNode(local.Local), target);
                        break;
                    case IParameterReferenceOperation parameter:
                        AddEdge(GetSymbolNode(parameter.Parameter), target);
                        break;
                    case IFieldReferenceOperation field:
                        AddEdge(GetSymbolNode(field.Field), target);
                        break;
                    case IPropertyReferenceOperation property:
                        AddEdge(GetSymbolNode(property.Property), target);
                        break;
                    case IInvocationOperation invocation:
                        if (invocation.TargetMethod.MethodKind == MethodKind.DelegateInvoke && invocation.Instance is { } delegateInstance)
                        {
                            AddValueFlows(delegateInstance, target);
                        }
                        else
                        {
                            AddEdge(GetSymbolNode(invocation.TargetMethod), target);
                        }
                        break;
                    case IAwaitOperation awaitOperation:
                        AddValueFlows(awaitOperation.Operation, target);
                        break;
                    case IAnonymousFunctionOperation anonymousFunction:
                        AddDelegateParameterFlows(anonymousFunction.Symbol, target);
                        AddAnonymousFunctionReturnFlows(anonymousFunction.Body, target);
                        break;
                    case IDelegateCreationOperation delegateCreation:
                        AddValueFlows(delegateCreation.Target, target);
                        break;
                    case IMethodReferenceOperation methodReference:
                        AddDelegateParameterFlows(methodReference.Method, target);
                        AddEdge(GetSymbolNode(methodReference.Method), target);

                        if (IsWinRTBoundaryMethod(methodReference.Method))
                        {
                            foreach (IParameterSymbol parameter in methodReference.Method.Parameters)
                            {
                                if (parameter.RefKind != RefKind.Out)
                                {
                                    sinks.TryAdd(GetSymbolNode(parameter), 0);
                                }
                            }
                        }
                        break;
                    case IConditionalOperation conditional:
                        AddValueFlows(conditional.WhenTrue, target);
                        if (conditional.WhenFalse is { } whenFalse)
                        {
                            AddValueFlows(whenFalse, target);
                        }
                        break;
                    case ICoalesceOperation coalesce:
                        AddValueFlows(coalesce.Value, target);
                        if (coalesce.WhenNull is { } whenNull)
                        {
                            AddValueFlows(whenNull, target);
                        }
                        break;
                    case ISwitchExpressionOperation switchExpression:
                        foreach (ISwitchExpressionArmOperation arm in switchExpression.Arms)
                        {
                            AddValueFlows(arm.Value, target);
                        }
                        break;
                    case IArrayCreationOperation { Initializer: { } initializer }:
                        foreach (IOperation element in initializer.ElementValues)
                        {
                            AddValueFlows(element, target);
                        }
                        break;
                    case IArrayInitializerOperation initializer:
                        foreach (IOperation element in initializer.ElementValues)
                        {
                            AddValueFlows(element, target);
                        }
                        break;
                    case IArrayElementReferenceOperation arrayElement:
                        AddValueFlows(arrayElement.ArrayReference, target);
                        break;
                }
            }

            void AddAnonymousFunctionReturnFlows(IOperation operation, FlowNode target)
            {
                if (operation is IAnonymousFunctionOperation or ILocalFunctionOperation)
                {
                    return;
                }

                if (operation is IReturnOperation { ReturnedValue: { } returnedValue })
                {
                    AddValueFlows(returnedValue, target);
                    return;
                }

                foreach (IOperation child in operation.ChildOperations)
                {
                    AddAnonymousFunctionReturnFlows(child, target);
                }
            }

            void AddDelegateParameterFlows(IMethodSymbol targetMethod, FlowNode delegateNode)
            {
                if (targetMethod.Parameters.IsEmpty || delegateNode.Symbol is not { } delegateSymbol)
                {
                    return;
                }

                foreach (IParameterSymbol parameter in targetMethod.Parameters)
                {
                    // Scope parameter slots to the stored delegate, not just its Action<> or Func<> type.
                    AddEdge(
                        FlowNode.ForDelegateParameter(delegateSymbol, parameter.Ordinal),
                        GetSymbolNode(parameter));
                }
            }

            void AddMethodDispatchFlows(IMethodSymbol method)
            {
                method = NormalizeMethod(method);

                if (!methodsWithDispatchFlows.TryAdd(method, 0))
                {
                    return;
                }

                foreach (IMethodSymbol contractMethod in GetContractMethods(method))
                {
                    IMethodSymbol normalizedContractMethod = NormalizeMethod(contractMethod);

                    // Returns flow implementation-to-contract; arguments flow contract-to-implementation.
                    AddEdge(FlowNode.ForSymbol(method), FlowNode.ForSymbol(normalizedContractMethod));

                    int parameterCount = Math.Min(method.Parameters.Length, normalizedContractMethod.Parameters.Length);

                    for (int i = 0; i < parameterCount; i++)
                    {
                        AddEdge(
                            FlowNode.ForSymbol(normalizedContractMethod.Parameters[i]),
                            FlowNode.ForSymbol(method.Parameters[i]));
                    }
                }

                if (method.AssociatedSymbol is IPropertySymbol property)
                {
                    foreach (IPropertySymbol contractProperty in GetContractProperties(property))
                    {
                        AddEdge(
                            FlowNode.ForSymbol(NormalizeFlowSymbol(property)),
                            FlowNode.ForSymbol(NormalizeFlowSymbol(contractProperty)));
                    }
                }
            }

            IEnumerable<IMethodSymbol> GetContractMethods(IMethodSymbol method)
            {
                for (IMethodSymbol? overriddenMethod = method.OverriddenMethod; overriddenMethod is not null; overriddenMethod = overriddenMethod.OverriddenMethod)
                {
                    yield return overriddenMethod;
                }

                foreach (IMethodSymbol implementedMethod in method.ExplicitInterfaceImplementations)
                {
                    yield return implementedMethod;
                }

                if (method.ContainingType is null)
                {
                    yield break;
                }

                foreach (INamedTypeSymbol interfaceType in method.ContainingType.AllInterfaces)
                {
                    foreach (ISymbol interfaceMember in interfaceType.GetMembers(method.Name))
                    {
                        if (interfaceMember is IMethodSymbol interfaceMethod &&
                            SymbolEqualityComparer.Default.Equals(
                                method.ContainingType.FindImplementationForInterfaceMember(interfaceMember)?.OriginalDefinition,
                                method.OriginalDefinition))
                        {
                            yield return interfaceMethod;
                        }
                    }
                }
            }

            IEnumerable<IPropertySymbol> GetContractProperties(IPropertySymbol property)
            {
                for (IPropertySymbol? overriddenProperty = property.OverriddenProperty; overriddenProperty is not null; overriddenProperty = overriddenProperty.OverriddenProperty)
                {
                    yield return overriddenProperty;
                }

                foreach (IPropertySymbol implementedProperty in property.ExplicitInterfaceImplementations)
                {
                    yield return implementedProperty;
                }

                if (property.ContainingType is null)
                {
                    yield break;
                }

                foreach (INamedTypeSymbol interfaceType in property.ContainingType.AllInterfaces)
                {
                    foreach (IPropertySymbol interfaceProperty in interfaceType.GetMembers(property.Name).OfType<IPropertySymbol>())
                    {
                        if (SymbolEqualityComparer.Default.Equals(
                            property.ContainingType.FindImplementationForInterfaceMember(interfaceProperty)?.OriginalDefinition,
                            property.OriginalDefinition))
                        {
                            yield return interfaceProperty;
                        }
                    }
                }
            }

            void AddAssignmentFlows(IOperation target, IOperation value)
            {
                if (!CanCarryCollectionExpression(value.Type))
                {
                    return;
                }

                switch (target)
                {
                    case ILocalReferenceOperation local:
                        FlowNode localNode = GetSymbolNode(local.Local);
                        AddValueFlows(value, localNode);

                        if (local.Local.Type is IArrayTypeSymbol)
                        {
                            AddStorageAliasFlow(localNode, value);
                        }
                        else if (local.Local.Type.TypeKind == TypeKind.Delegate)
                        {
                            AddDelegateAliasFlows(localNode, local.Local.Type, value);
                        }
                        break;
                    case IParameterReferenceOperation parameter:
                        FlowNode parameterNode = GetSymbolNode(parameter.Parameter);
                        AddValueFlows(value, parameterNode);

                        if (parameter.Parameter.Type is IArrayTypeSymbol)
                        {
                            AddStorageAliasFlow(parameterNode, value);
                        }
                        else if (parameter.Parameter.Type.TypeKind == TypeKind.Delegate)
                        {
                            AddDelegateAliasFlows(parameterNode, parameter.Parameter.Type, value);
                        }
                        break;
                    case IFieldReferenceOperation field:
                        FlowNode fieldNode = GetSymbolNode(field.Field);
                        AddValueFlows(value, fieldNode);

                        if (field.Field.Type is IArrayTypeSymbol)
                        {
                            AddStorageAliasFlow(fieldNode, value);
                        }
                        else if (field.Field.Type.TypeKind == TypeKind.Delegate)
                        {
                            AddDelegateAliasFlows(fieldNode, field.Field.Type, value);
                        }
                        break;
                    case IPropertyReferenceOperation property:
                        FlowNode propertyNode = GetSymbolNode(property.Property);
                        AddValueFlows(value, propertyNode);

                        if (property.Property.SetMethod is { Parameters: [.., { } valueParameter] })
                        {
                            AddValueFlows(value, GetSymbolNode(valueParameter));
                        }

                        if (property.Property.Type is IArrayTypeSymbol)
                        {
                            AddStorageAliasFlow(propertyNode, value);
                        }
                        else if (property.Property.Type.TypeKind == TypeKind.Delegate)
                        {
                            AddDelegateAliasFlows(propertyNode, property.Property.Type, value);
                        }

                        if (IsWinRTBoundaryProperty(property.Property))
                        {
                            sinks.TryAdd(propertyNode, 0);
                        }
                        break;
                    case IArrayElementReferenceOperation arrayElement:
                        AddFlowsToReferencedStorage(value, arrayElement.ArrayReference);
                        break;
                }
            }

            void AddStorageAliasFlow(FlowNode target, IOperation source)
            {
                switch (source)
                {
                    case ILocalReferenceOperation local:
                        AddEdge(target, GetSymbolNode(local.Local));
                        break;
                    case IParameterReferenceOperation parameter:
                        AddEdge(target, GetSymbolNode(parameter.Parameter));
                        break;
                    case IFieldReferenceOperation field:
                        AddEdge(target, GetSymbolNode(field.Field));
                        break;
                    case IPropertyReferenceOperation property:
                        AddEdge(target, GetSymbolNode(property.Property));
                        break;
                    case IConversionOperation conversion:
                        AddStorageAliasFlow(target, conversion.Operand);
                        break;
                    case IParenthesizedOperation parenthesized:
                        AddStorageAliasFlow(target, parenthesized.Operand);
                        break;
                }
            }

            void AddDelegateAliasFlows(FlowNode target, ITypeSymbol delegateType, IOperation source)
            {
                if (target.Symbol is not { } targetSymbol ||
                    delegateType is not INamedTypeSymbol namedDelegateType ||
                    namedDelegateType.DelegateInvokeMethod is not { } invokeMethod)
                {
                    return;
                }

                AddDelegateAliasFlowsFromOperation(targetSymbol, invokeMethod.Parameters.Length, source);
            }

            void AddDelegateAliasFlowsFromOperation(ISymbol targetSymbol, int parameterCount, IOperation source)
            {
                switch (source)
                {
                    case ILocalReferenceOperation local:
                        AddDelegateAliasFlowsBetweenSymbols(targetSymbol, NormalizeFlowSymbol(local.Local), parameterCount);
                        break;
                    case IParameterReferenceOperation parameter:
                        AddDelegateAliasFlowsBetweenSymbols(targetSymbol, NormalizeFlowSymbol(parameter.Parameter), parameterCount);
                        break;
                    case IFieldReferenceOperation field:
                        AddDelegateAliasFlowsBetweenSymbols(targetSymbol, NormalizeFlowSymbol(field.Field), parameterCount);
                        break;
                    case IPropertyReferenceOperation property:
                        AddDelegateAliasFlowsBetweenSymbols(targetSymbol, NormalizeFlowSymbol(property.Property), parameterCount);
                        break;
                    case IInvocationOperation invocation:
                        AddDelegateAliasFlowsBetweenSymbols(targetSymbol, NormalizeFlowSymbol(invocation.TargetMethod), parameterCount);
                        break;
                    case IConversionOperation conversion:
                        AddDelegateAliasFlowsFromOperation(targetSymbol, parameterCount, conversion.Operand);
                        break;
                    case IParenthesizedOperation parenthesized:
                        AddDelegateAliasFlowsFromOperation(targetSymbol, parameterCount, parenthesized.Operand);
                        break;
                }
            }

            void AddDelegateAliasFlowsBetweenSymbols(ISymbol targetSymbol, ISymbol sourceSymbol, int parameterCount)
            {
                for (int i = 0; i < parameterCount; i++)
                {
                    AddEdge(
                        FlowNode.ForDelegateParameter(targetSymbol, i),
                        FlowNode.ForDelegateParameter(sourceSymbol, i));
                }
            }

            void AddFlowsToReferencedStorage(IOperation value, IOperation storage)
            {
                switch (storage)
                {
                    case ILocalReferenceOperation local:
                        AddValueFlows(value, GetSymbolNode(local.Local));
                        break;
                    case IParameterReferenceOperation parameter:
                        AddValueFlows(value, GetSymbolNode(parameter.Parameter));
                        break;
                    case IFieldReferenceOperation field:
                        AddValueFlows(value, GetSymbolNode(field.Field));
                        break;
                    case IPropertyReferenceOperation property:
                        AddValueFlows(value, GetSymbolNode(property.Property));
                        break;
                    case IConversionOperation conversion:
                        AddFlowsToReferencedStorage(value, conversion.Operand);
                        break;
                    case IParenthesizedOperation parenthesized:
                        AddFlowsToReferencedStorage(value, parenthesized.Operand);
                        break;
                }
            }

            void AddFlowToReferencedStorage(FlowNode source, IOperation storage)
            {
                switch (storage)
                {
                    case ILocalReferenceOperation local:
                        AddEdge(source, GetSymbolNode(local.Local));
                        break;
                    case IParameterReferenceOperation parameter:
                        AddEdge(source, GetSymbolNode(parameter.Parameter));
                        break;
                    case IFieldReferenceOperation field:
                        AddEdge(source, GetSymbolNode(field.Field));
                        break;
                    case IPropertyReferenceOperation property:
                        AddEdge(source, GetSymbolNode(property.Property));
                        break;
                    case IDeclarationExpressionOperation declaration:
                        AddFlowToReferencedStorage(source, declaration.Expression);
                        break;
                    case IConversionOperation conversion:
                        AddFlowToReferencedStorage(source, conversion.Operand);
                        break;
                    case IParenthesizedOperation parenthesized:
                        AddFlowToReferencedStorage(source, parenthesized.Operand);
                        break;
                }
            }

            void AddDelegateArgumentFlows(IOperation value, IOperation delegateInstance, int parameterOrdinal)
            {
                switch (delegateInstance)
                {
                    case ILocalReferenceOperation local:
                        AddValueFlows(value, FlowNode.ForDelegateParameter(NormalizeFlowSymbol(local.Local), parameterOrdinal));
                        break;
                    case IParameterReferenceOperation parameter:
                        AddValueFlows(value, FlowNode.ForDelegateParameter(NormalizeFlowSymbol(parameter.Parameter), parameterOrdinal));
                        break;
                    case IFieldReferenceOperation field:
                        AddValueFlows(value, FlowNode.ForDelegateParameter(NormalizeFlowSymbol(field.Field), parameterOrdinal));
                        break;
                    case IPropertyReferenceOperation property:
                        AddValueFlows(value, FlowNode.ForDelegateParameter(NormalizeFlowSymbol(property.Property), parameterOrdinal));
                        break;
                    case IInvocationOperation invocation:
                        AddValueFlows(value, FlowNode.ForDelegateParameter(NormalizeFlowSymbol(invocation.TargetMethod), parameterOrdinal));
                        break;
                    case IConversionOperation conversion:
                        AddDelegateArgumentFlows(value, conversion.Operand, parameterOrdinal);
                        break;
                    case IParenthesizedOperation parenthesized:
                        AddDelegateArgumentFlows(value, parenthesized.Operand, parameterOrdinal);
                        break;
                }
            }

            bool IsWinRTBoundaryType(INamedTypeSymbol? type)
            {
                return type is not null &&
                    winRTBoundaryTypes.GetOrAdd(
                        type,
                        symbol =>
                            isWinRTClassOrInterface(symbol, true) ||
                            (isCsWinRTComponent &&
                             SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, context.Compilation.Assembly) &&
                             isWinRTType(symbol, typeMapper)));
            }

            bool IsWinRTBoundaryMethod(IMethodSymbol method)
            {
                method = (IMethodSymbol)NormalizeFlowSymbol(method);

                return winRTBoundaryMethods.GetOrAdd(method, IsWinRTBoundaryMethodCore);
            }

            bool IsWinRTBoundaryMethodCore(ISymbol symbol)
            {
                IMethodSymbol method = (IMethodSymbol)symbol;

                if (IsWinRTBoundaryType(method.ContainingType) &&
                    (!SymbolEqualityComparer.Default.Equals(method.ContainingAssembly, context.Compilation.Assembly) ||
                     IsExternallyVisible(method.DeclaredAccessibility)))
                {
                    return true;
                }

                foreach (IMethodSymbol implementedMethod in method.ExplicitInterfaceImplementations)
                {
                    if (IsWinRTBoundaryType(implementedMethod.ContainingType))
                    {
                        return true;
                    }
                }

                for (IMethodSymbol? overriddenMethod = method.OverriddenMethod; overriddenMethod is not null; overriddenMethod = overriddenMethod.OverriddenMethod)
                {
                    if (IsWinRTBoundaryType(overriddenMethod.ContainingType))
                    {
                        return true;
                    }
                }

                if (method.ContainingType is not null)
                {
                    foreach (INamedTypeSymbol interfaceType in method.ContainingType.AllInterfaces)
                    {
                        if (!IsWinRTBoundaryType(interfaceType))
                        {
                            continue;
                        }

                        foreach (ISymbol interfaceMember in interfaceType.GetMembers(method.Name))
                        {
                            if (SymbolEqualityComparer.Default.Equals(
                                method.ContainingType.FindImplementationForInterfaceMember(interfaceMember)?.OriginalDefinition,
                                method.OriginalDefinition))
                            {
                                return true;
                            }
                        }
                    }
                }

                return false;
            }

            bool IsWinRTBoundaryProperty(IPropertySymbol property)
            {
                return GeneratorHelper.IsGeneratedBindableCustomPropertyClass(context.Compilation, property.ContainingType) ||
                    (property.GetMethod is not null && IsWinRTBoundaryMethod(property.GetMethod)) ||
                    (property.SetMethod is not null && IsWinRTBoundaryMethod(property.SetMethod));
            }

            static bool IsExternallyVisible(Accessibility accessibility)
            {
                return accessibility is
                    Accessibility.Public or
                    Accessibility.Protected or
                    Accessibility.ProtectedOrInternal;
            }

            context.RegisterOperationAction(context =>
            {
                _ = GetCandidate((ICollectionExpressionOperation)context.Operation);
            }, OperationKind.CollectionExpression);

            context.RegisterOperationAction(context =>
            {
                IVariableDeclaratorOperation declarator = (IVariableDeclaratorOperation)context.Operation;

                if (declarator.Initializer is { Value: { } value })
                {
                    if (!CanCarryCollectionExpression(value.Type))
                    {
                        return;
                    }

                    FlowNode localNode = GetSymbolNode(declarator.Symbol);
                    AddValueFlows(value, localNode);

                    if (declarator.Symbol.Type is IArrayTypeSymbol)
                    {
                        AddStorageAliasFlow(localNode, value);
                    }
                    else if (declarator.Symbol.Type.TypeKind == TypeKind.Delegate)
                    {
                        AddDelegateAliasFlows(localNode, declarator.Symbol.Type, value);
                    }
                }
            }, OperationKind.VariableDeclarator);

            context.RegisterOperationAction(context =>
            {
                ISimpleAssignmentOperation assignment = (ISimpleAssignmentOperation)context.Operation;
                AddAssignmentFlows(assignment.Target, assignment.Value);
            }, OperationKind.SimpleAssignment);

            context.RegisterOperationAction(context =>
            {
                ICoalesceAssignmentOperation assignment = (ICoalesceAssignmentOperation)context.Operation;
                AddAssignmentFlows(assignment.Target, assignment.Value);
            }, OperationKind.CoalesceAssignment);

            context.RegisterOperationAction(context =>
            {
                ICompoundAssignmentOperation assignment = (ICompoundAssignmentOperation)context.Operation;

                if (assignment.Target.Type?.TypeKind == TypeKind.Delegate)
                {
                    AddAssignmentFlows(assignment.Target, assignment.Value);
                }
            }, OperationKind.CompoundAssignment);

            context.RegisterOperationAction(context =>
            {
                IArgumentOperation argument = (IArgumentOperation)context.Operation;

                if (argument.Parameter is not { } parameter)
                {
                    FlowNode unknownCallSink = FlowNode.ForUnknownCall();
                    AddValueFlows(argument.Value, unknownCallSink);
                    sinks.TryAdd(unknownCallSink, 0);
                    return;
                }

                if (!CanCarryCollectionExpression(argument.Value.Type))
                {
                    return;
                }

                FlowNode parameterNode = GetSymbolNode(parameter);

                if (parameter.RefKind != RefKind.Out)
                {
                    AddValueFlows(argument.Value, parameterNode);

                    if (parameter.Type.TypeKind == TypeKind.Delegate)
                    {
                        AddDelegateAliasFlows(parameterNode, parameter.Type, argument.Value);
                    }

                    if (parameter.ContainingSymbol is IMethodSymbol method && IsWinRTBoundaryMethod(method))
                    {
                        sinks.TryAdd(parameterNode, 0);
                    }
                }

                if (parameter.RefKind is RefKind.Ref or RefKind.Out || parameter.Type is IArrayTypeSymbol)
                {
                    AddFlowToReferencedStorage(parameterNode, argument.Value);
                }

                if (argument.Parent is IInvocationOperation
                    {
                        TargetMethod.MethodKind: MethodKind.DelegateInvoke,
                        Instance: { } delegateInstance
                    })
                {
                    AddDelegateArgumentFlows(argument.Value, delegateInstance, parameter.Ordinal);
                }
            }, OperationKind.Argument);

            context.RegisterOperationAction(context =>
            {
                IDynamicInvocationOperation invocation = (IDynamicInvocationOperation)context.Operation;
                FlowNode unknownCallSink = FlowNode.ForUnknownCall();

                foreach (IOperation argument in invocation.Arguments)
                {
                    if (CanCarryCollectionExpression(argument.Type))
                    {
                        AddValueFlows(argument, unknownCallSink);
                    }
                }

                sinks.TryAdd(unknownCallSink, 0);
            }, OperationKind.DynamicInvocation);

            context.RegisterOperationAction(context =>
            {
                IReturnOperation returnOperation = (IReturnOperation)context.Operation;

                if (returnOperation.ReturnedValue is null)
                {
                    return;
                }

                if (!CanCarryCollectionExpression(returnOperation.ReturnedValue.Type))
                {
                    return;
                }

                IMethodSymbol? method = null;

                for (IOperation? parent = returnOperation.Parent; parent is not null; parent = parent.Parent)
                {
                    if (parent is IAnonymousFunctionOperation)
                    {
                        return;
                    }

                    if (parent is ILocalFunctionOperation localFunction)
                    {
                        method = localFunction.Symbol;
                        break;
                    }

                    if (parent is IMethodBodyBaseOperation)
                    {
                        break;
                    }
                }

                method ??= context.ContainingSymbol as IMethodSymbol;

                if (method is null)
                {
                    return;
                }

                ISymbol returnSymbol = method.AssociatedSymbol ?? method;
                FlowNode returnNode = GetSymbolNode(returnSymbol);
                AddValueFlows(returnOperation.ReturnedValue, returnNode);

                if (returnOperation.ReturnedValue.Type?.TypeKind == TypeKind.Delegate)
                {
                    AddDelegateAliasFlows(returnNode, returnOperation.ReturnedValue.Type, returnOperation.ReturnedValue);
                }

                if (returnSymbol is IPropertySymbol property
                    ? IsWinRTBoundaryProperty(property)
                    : IsWinRTBoundaryMethod(method))
                {
                    sinks.TryAdd(returnNode, 0);
                }
            }, OperationKind.Return);

            context.RegisterOperationAction(context =>
            {
                IFieldInitializerOperation initializer = (IFieldInitializerOperation)context.Operation;

                if (!CanCarryCollectionExpression(initializer.Value.Type))
                {
                    return;
                }

                foreach (IFieldSymbol field in initializer.InitializedFields)
                {
                    FlowNode fieldNode = GetSymbolNode(field);
                    AddValueFlows(initializer.Value, fieldNode);

                    if (field.Type is IArrayTypeSymbol)
                    {
                        AddStorageAliasFlow(fieldNode, initializer.Value);
                    }
                    else if (field.Type.TypeKind == TypeKind.Delegate)
                    {
                        AddDelegateAliasFlows(fieldNode, field.Type, initializer.Value);
                    }
                }
            }, OperationKind.FieldInitializer);

            context.RegisterOperationAction(context =>
            {
                IPropertyInitializerOperation initializer = (IPropertyInitializerOperation)context.Operation;

                if (!CanCarryCollectionExpression(initializer.Value.Type))
                {
                    return;
                }

                foreach (IPropertySymbol property in initializer.InitializedProperties)
                {
                    FlowNode propertyNode = GetSymbolNode(property);
                    AddValueFlows(initializer.Value, propertyNode);

                    if (property.Type is IArrayTypeSymbol)
                    {
                        AddStorageAliasFlow(propertyNode, initializer.Value);
                    }
                    else if (property.Type.TypeKind == TypeKind.Delegate)
                    {
                        AddDelegateAliasFlows(propertyNode, property.Type, initializer.Value);
                    }

                    if (IsWinRTBoundaryProperty(property))
                    {
                        sinks.TryAdd(propertyNode, 0);
                    }
                }
            }, OperationKind.PropertyInitializer);

            context.RegisterCompilationEndAction(context =>
            {
                if (candidates.IsEmpty || sinks.IsEmpty)
                {
                    return;
                }

                Dictionary<FlowNode, List<FlowNode>> sourcesByTarget = new(FlowNodeComparer.Instance);

                foreach (FlowEdge edge in edges)
                {
                    if (!sourcesByTarget.TryGetValue(edge.Target, out List<FlowNode>? sources))
                    {
                        sources = [];
                        sourcesByTarget.Add(edge.Target, sources);
                    }

                    sources.Add(edge.Source);
                }

                HashSet<FlowNode> reachesWinRT = new(FlowNodeComparer.Instance);
                Queue<FlowNode> pending = new();

                foreach (FlowNode sink in sinks.Keys)
                {
                    if (reachesWinRT.Add(sink))
                    {
                        pending.Enqueue(sink);
                    }
                }

                while (pending.Count > 0)
                {
                    context.CancellationToken.ThrowIfCancellationRequested();

                    FlowNode target = pending.Dequeue();

                    if (!sourcesByTarget.TryGetValue(target, out List<FlowNode>? sources))
                    {
                        continue;
                    }

                    foreach (FlowNode source in sources)
                    {
                        if (reachesWinRT.Add(source))
                        {
                            pending.Enqueue(source);
                        }
                    }
                }

                foreach (CollectionExpressionCandidate candidate in candidates.Values)
                {
                    if (reachesWinRT.Contains(FlowNode.ForCollectionExpression(candidate.Key)))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            WinRTRules.NonEmptyCollectionExpressionTargetingNonBuilderInterfaceType,
                            candidate.Location,
                            candidate.Type));
                    }
                }
            });
        });
    }

    private static ISymbol NormalizeFlowSymbol(ISymbol symbol)
    {
        return symbol switch
        {
            IParameterSymbol { ContainingSymbol: IMethodSymbol method } parameter =>
                NormalizeMethod(method).Parameters[parameter.Ordinal],
            IMethodSymbol method => NormalizeMethod(method),
            IPropertySymbol property => property.OriginalDefinition,
            IFieldSymbol field => field.OriginalDefinition,
            _ => symbol
        };
    }

    private static IMethodSymbol NormalizeMethod(IMethodSymbol method)
    {
        method = method.OriginalDefinition;

        return method.PartialDefinitionPart ?? method;
    }

    private static bool CanCarryCollectionExpression(ITypeSymbol? type)
    {
        return type is null or IArrayTypeSymbol ||
            type.SpecialType == SpecialType.System_Object ||
            type.TypeKind is TypeKind.Interface or TypeKind.TypeParameter or TypeKind.Dynamic or TypeKind.Delegate;
    }

    private readonly record struct CollectionExpressionKey(SyntaxTree SyntaxTree, TextSpan Span);

    private sealed record CollectionExpressionCandidate(CollectionExpressionKey Key, Location Location, INamedTypeSymbol Type);

    private readonly record struct FlowEdge(FlowNode Source, FlowNode Target);

    private readonly struct FlowNode
    {
        private FlowNode(CollectionExpressionKey collectionExpression, ISymbol? symbol, FlowNodeKind kind, int parameterOrdinal)
        {
            CollectionExpression = collectionExpression;
            Symbol = symbol;
            Kind = kind;
            ParameterOrdinal = parameterOrdinal;
        }

        public CollectionExpressionKey CollectionExpression { get; }

        public ISymbol? Symbol { get; }

        public FlowNodeKind Kind { get; }

        public int ParameterOrdinal { get; }

        public static FlowNode ForCollectionExpression(CollectionExpressionKey collectionExpression)
        {
            return new(collectionExpression, null, FlowNodeKind.CollectionExpression, 0);
        }

        public static FlowNode ForSymbol(ISymbol symbol)
        {
            return new(default, symbol, FlowNodeKind.Symbol, 0);
        }

        public static FlowNode ForDelegateParameter(ISymbol delegateSymbol, int parameterOrdinal)
        {
            return new(default, delegateSymbol, FlowNodeKind.DelegateParameter, parameterOrdinal);
        }

        public static FlowNode ForUnknownCall()
        {
            return new(default, null, FlowNodeKind.UnknownCall, 0);
        }
    }

    private enum FlowNodeKind
    {
        Symbol,
        CollectionExpression,
        DelegateParameter,
        UnknownCall
    }

    private sealed class FlowNodeComparer : IEqualityComparer<FlowNode>
    {
        public static FlowNodeComparer Instance { get; } = new();

        public bool Equals(FlowNode x, FlowNode y)
        {
            if (x.Kind != y.Kind)
            {
                return false;
            }

            return x.Kind switch
            {
                FlowNodeKind.CollectionExpression => x.CollectionExpression.Equals(y.CollectionExpression),
                FlowNodeKind.DelegateParameter =>
                    x.ParameterOrdinal == y.ParameterOrdinal &&
                    SymbolEqualityComparer.Default.Equals(x.Symbol, y.Symbol),
                FlowNodeKind.UnknownCall => true,
                _ => SymbolEqualityComparer.Default.Equals(x.Symbol, y.Symbol)
            };
        }

        public int GetHashCode(FlowNode obj)
        {
            return obj.Kind switch
            {
                FlowNodeKind.CollectionExpression => obj.CollectionExpression.GetHashCode(),
                FlowNodeKind.DelegateParameter => unchecked(
                    (SymbolEqualityComparer.Default.GetHashCode(obj.Symbol!) * 397) ^
                    obj.ParameterOrdinal),
                FlowNodeKind.UnknownCall => (int)FlowNodeKind.UnknownCall,
                _ => SymbolEqualityComparer.Default.GetHashCode(obj.Symbol!)
            };
        }
    }
}

#endif
