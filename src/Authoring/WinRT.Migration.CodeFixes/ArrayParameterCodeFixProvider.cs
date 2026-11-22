// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Text;

namespace WindowsRuntime.Migration;

#pragma warning disable IDE0046

/// <summary>
/// Converts attributed array parameters when the body and loaded callers support the span contract.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class ArrayParameterCodeFixProvider : CodeFixProvider
{
    /// <summary>The action for a 3.0-only signature.</summary>
    private const string DirectKey = "CsWinRT.MigrateArrayParameter";

    /// <summary>The action preserving the legacy signature.</summary>
    private const string PreserveKey = "CsWinRT.MigrateArrayParameter.PreserveVersion2";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = [LegacyAttributeMigration.ArrayDiagnosticId];

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider()
    {
        return new ArrayFixAllProvider();
    }

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        bool? mode = LegacyWindowsRuntimeAttributeCodeFixProvider.GetPreservationMode(
            context.Document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GlobalOptions);

        bool[] strategies = mode is null ? [true, false] : [mode.Value];
        foreach (bool preserve in strategies)
        {
            Document changed = await TryConvertAsync(context.Document, context.Diagnostics[0], preserve,
                cleanImports: true, context.CancellationToken).ConfigureAwait(false);

            if (changed == context.Document)
            {
                continue;
            }

            context.RegisterCodeFix(CodeAction.Create(
                preserve ? "Convert array parameter to span and preserve CsWinRT 2.x" : "Convert array parameter to span for CsWinRT 3.0",
                token => TryConvertAsync(context.Document, context.Diagnostics[0], preserve, cleanImports: true, token),
                preserve ? PreserveKey : DirectKey), context.Diagnostics);
        }
    }

    /// <summary>Validates and applies one parameter conversion without changing other signatures.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="diagnostic">The mapping suggestion.</param>
    /// <param name="preserve">Whether the original parameter remains on 2.x.</param>
    /// <param name="cleanImports">Whether to migrate safely recognized dangling imports.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>The converted document, or the original when the contract is unsupported.</returns>
    private static async Task<Document> TryConvertAsync(
        Document document, Diagnostic diagnostic, bool preserve, bool cleanImports, CancellationToken token)
    {
        SyntaxNode? root = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
        SemanticModel? model = await document.GetSemanticModelAsync(token).ConfigureAwait(false);

        if (root is null || model is null || !LegacyAttributeMigration.IsVersion3(root.SyntaxTree) ||
            LegacyAttributeMigration.IsGenerated(root.SyntaxTree, document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider, token) ||
            (!preserve && LegacyWindowsRuntimeAttributeCodeFixProvider.GetPreservationMode(
                document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider.GlobalOptions) == true) ||
            root.FindNode(diagnostic.Location.SourceSpan).FirstAncestorOrSelf<AttributeSyntax>() is not { Parent: AttributeListSyntax { Parent: ParameterSyntax parameter } list } attribute ||
            !LegacyAttributeMigration.TryGetArraySpanName(attribute, model, token, out string? spanName) ||
            !CanConvert(parameter, attribute, model, spanName!, token, out IMethodSymbol? method))
        {
            return document;
        }

        IArrayTypeSymbol array = (IArrayTypeSymbol)model.GetDeclaredSymbol(parameter, token)!.Type;
        TypeSyntax element = parameter.Type is ArrayTypeSyntax arraySyntax
            ? arraySyntax.ElementType.WithoutTrivia()
            : SyntaxFactory.ParseTypeName(array.ElementType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        NameSyntax type = SyntaxFactory.QualifiedName(SyntaxFactory.ParseName("global::System"),
            SyntaxFactory.GenericName(SyntaxFactory.Identifier(spanName == "System.ReadOnlySpan`1" ? "ReadOnlySpan" : "Span"),
                SyntaxFactory.TypeArgumentList(SyntaxFactory.SingletonSeparatedList(element))));
        AttributeListSyntax remaining = list.RemoveNode(attribute, SyntaxRemoveOptions.KeepExteriorTrivia)!;
        ParameterSyntax modern = parameter.WithAttributeLists(remaining.Attributes.Count == 0
            ? parameter.AttributeLists.Remove(list)
            : parameter.AttributeLists.Replace(list, remaining)).WithType(type.WithTriviaFrom(parameter.Type!));
        SourceText text = await document.GetTextAsync(token).ConfigureAwait(false);
        string replacement = preserve
            ? LegacyWindowsRuntimeAttributeCodeFixProvider.ConditionalText(text, parameter.Span, parameter.ToString(), modern.ToString())
            : modern.ToString();
        TextChange change = new(parameter.Span, replacement);
        Document changed = document.WithText(text.WithChanges(change));

        if (!await PreservesCompilationAsync(document, changed, method!,
            model.GetDeclaredSymbol(parameter, token)!.Ordinal, change, token).ConfigureAwait(false))
        {
            return document;
        }

        return cleanImports
            ? await LegacyWindowsRuntimeAttributeCodeFixProvider.CleanLegacyImportsAsync(changed, preserve, token).ConfigureAwait(false)
            : changed;
    }

    /// <summary>Restricts automatic edits to synchronous, independent signatures and nonescaping array uses.</summary>
    /// <param name="parameter">The array parameter.</param>
    /// <param name="attribute">The direction annotation.</param>
    /// <param name="model">The source semantic model.</param>
    /// <param name="spanName">The target span metadata name.</param>
    /// <param name="token">The cancellation token.</param>
    /// <param name="method">The owning method.</param>
    /// <returns>Whether the local contract can be validated automatically.</returns>
    private static bool CanConvert(
        ParameterSyntax parameter, AttributeSyntax attribute, SemanticModel model, string spanName,
        CancellationToken token, out IMethodSymbol? method)
    {
        method = null;

        if (LegacyAttributeMigration.HasInteriorDirectives(parameter) ||
            model.GetDeclaredSymbol(parameter, token) is not IParameterSymbol { RefKind: RefKind.None, IsParams: false, HasExplicitDefaultValue: false, Type.NullableAnnotation: not NullableAnnotation.Annotated } symbol ||
            symbol.ContainingSymbol is not IMethodSymbol owner ||
            owner.MethodKind is not (MethodKind.Ordinary or MethodKind.Constructor) ||
            owner.IsAsync || owner.IsAbstract || owner.IsVirtual || owner.IsOverride || owner.IsExtern ||
            owner.PartialDefinitionPart is not null || owner.PartialImplementationPart is not null ||
            !owner.ExplicitInterfaceImplementations.IsEmpty ||
            owner.ContainingType.TypeKind == TypeKind.Interface ||
            parameter.Parent?.Parent is not BaseMethodDeclarationSyntax declaration ||
            declaration.Modifiers.Any(SyntaxKind.PartialKeyword) ||
            (declaration.Body is null && declaration.ExpressionBody is null) ||
            declaration.DescendantNodes().OfType<YieldStatementSyntax>().Any())
        {
            return false;
        }

        INamedTypeSymbol? span = model.Compilation.GetTypeByMetadataName(spanName);

        if (span is not { IsRefLikeType: true } ||
            !SymbolEqualityComparer.Default.Equals(span.ContainingAssembly,
                model.Compilation.GetSpecialType(SpecialType.System_Object).ContainingAssembly))
        {
            return false;
        }

        foreach (INamedTypeSymbol implementedInterface in owner.ContainingType.AllInterfaces)
        {
            if (implementedInterface.GetMembers().Any(member =>
                SymbolEqualityComparer.Default.Equals(owner.ContainingType.FindImplementationForInterfaceMember(member), owner)))
            {
                return false;
            }
        }

        foreach (AttributeSyntax other in parameter.AttributeLists.SelectMany(static item => item.Attributes))
        {
            if (other != attribute && LegacyAttributeMigration.TryGetArraySpanName(other, model, token, out _))
            {
                return false;
            }
        }

        bool readOnly = spanName == "System.ReadOnlySpan`1";

        foreach (IdentifierNameSyntax identifier in declaration.DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            if (!SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(identifier, token).Symbol, symbol))
            {
                continue;
            }

            if (identifier.Parent is MemberAccessExpressionSyntax { Expression: var receiver, Name.Identifier.ValueText: "Length" } &&
                receiver == identifier)
            {
                continue;
            }

            if (identifier.Parent is ElementAccessExpressionSyntax access && access.Expression == identifier &&
                ((readOnly && IsReadOnlyElementUse(access, model, token)) || (access.Parent is AssignmentExpressionSyntax assignment &&
                    assignment.IsKind(SyntaxKind.SimpleAssignmentExpression) && assignment.Left == access)))
            {
                continue;
            }

            if (readOnly && identifier.Parent is ForEachStatementSyntax loop && loop.Expression == identifier)
            {
                continue;
            }

            if (identifier.Ancestors().OfType<InvocationExpressionSyntax>().Any(invocation =>
                model.GetOperation(invocation, token) is INameOfOperation))
            {
                continue;
            }

            if (model.GetOperation(identifier, token)?.Parent is IConversionOperation
                { Type: INamedTypeSymbol convertedType } &&
                SymbolEqualityComparer.Default.Equals(convertedType.OriginalDefinition, span))
            {
                continue;
            }

            return false;
        }

        method = owner;
        return true;
    }

    /// <summary>Rejects receiver uses that would mutate a defensive copy after readonly indexing.</summary>
    /// <param name="access">The original array element access.</param>
    /// <param name="model">The original semantic model.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>Whether readonly indexing preserves the element's value or receiver semantics.</returns>
    private static bool IsReadOnlyElementUse(ElementAccessExpressionSyntax access, SemanticModel model, CancellationToken token)
    {
        IOperation? current = model.GetOperation(access, token);

        while (current?.Type is { IsReferenceType: false } and not INamedTypeSymbol { IsReadOnly: true })
        {
            switch (current.Parent)
            {
                case IFieldReferenceOperation field when field.Instance == current:
                    if (field.Field.IsReadOnly)
                    {
                        return true;
                    }

                    current = field;
                    continue;
                case IPropertyReferenceOperation property when property.Instance == current:
                    return property.Property.GetMethod is { IsReadOnly: true, ReturnsByRef: false, ReturnsByRefReadonly: false };
                case IInvocationOperation invocation when invocation.Instance == current:
                    return invocation.TargetMethod.IsReadOnly;
                case IConditionalAccessOperation conditional when conditional.Operation == current:
                case IDynamicMemberReferenceOperation:
                    return false;
                default:
                    return true;
            }
        }

        return current is not null;
    }

    /// <summary>Rejects introduced errors and changed overload binding in the project and loaded dependents.</summary>
    /// <param name="original">The original document.</param>
    /// <param name="changed">The candidate document.</param>
    /// <param name="method">The original owning method.</param>
    /// <param name="ordinal">The converted parameter ordinal.</param>
    /// <param name="change">The parameter text change.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>Whether the loaded compilations retain their existing contracts.</returns>
    private static async Task<bool> PreservesCompilationAsync(
        Document original, Document changed, IMethodSymbol method, int ordinal, TextChange change, CancellationToken token)
    {
        SyntaxTree oldTree = (await original.GetSyntaxTreeAsync(token).ConfigureAwait(false))!;
        SyntaxTree newTree = (await changed.GetSyntaxTreeAsync(token).ConfigureAwait(false))!;
        Compilation before = (await original.Project.GetCompilationAsync(token).ConfigureAwait(false))!;
        Compilation after = (await changed.Project.GetCompilationAsync(token).ConfigureAwait(false))!;
        BaseMethodDeclarationSyntax oldDeclaration = (BaseMethodDeclarationSyntax)method.DeclaringSyntaxReferences[0].GetSyntax(token);
        BaseMethodDeclarationSyntax? newDeclaration = newTree.GetRoot(token).DescendantNodes().OfType<BaseMethodDeclarationSyntax>()
            .FirstOrDefault(node => node.SpanStart == oldDeclaration.SpanStart);
        IMethodSymbol? newMethod = newDeclaration is null ? null : after.GetSemanticModel(newTree).GetDeclaredSymbol(newDeclaration, token);

        if (newMethod is null || !MigrationCompilationValidation.HasNoNewErrors(before, after, oldTree, newTree, change, token) ||
            !KeepsCallBindings(before, after, method, newMethod, ordinal, oldTree, newTree, change, token))
        {
            return false;
        }

        foreach (ProjectId id in original.Project.Solution.GetProjectDependencyGraph()
            .GetProjectsThatTransitivelyDependOnThisProject(original.Project.Id))
        {
            Project previousProject = original.Project.Solution.GetProject(id)!;
            Project nextProject = changed.Project.Solution.GetProject(id)!;
            Compilation? previous = await previousProject.GetCompilationAsync(token).ConfigureAwait(false);
            Compilation? next = await nextProject.GetCompilationAsync(token).ConfigureAwait(false);

            if (previous is null || next is null ||
                !MigrationCompilationValidation.HasNoNewErrors(previous, next, oldTree, newTree, change, token) ||
                !KeepsCallBindings(previous, next, method, newMethod, ordinal, oldTree, newTree, change, token))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Rejects calls or method groups that switch to or from the changed overload.</summary>
    /// <param name="before">The original compilation.</param>
    /// <param name="after">The candidate compilation.</param>
    /// <param name="method">The original method.</param>
    /// <param name="newMethod">The changed method.</param>
    /// <param name="ordinal">The converted parameter ordinal.</param>
    /// <param name="oldTree">The original source tree.</param>
    /// <param name="newTree">The changed source tree.</param>
    /// <param name="change">The parameter change.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>Whether the overload selected by every loaded reference stays the same.</returns>
    private static bool KeepsCallBindings(
        Compilation before, Compilation after, IMethodSymbol method, IMethodSymbol newMethod, int ordinal,
        SyntaxTree oldTree, SyntaxTree newTree, TextChange change, CancellationToken token)
    {
        int delta = change.NewText!.Length - change.Span.Length;

        foreach (SyntaxTree tree in before.SyntaxTrees)
        {
            SyntaxTree candidateTree = tree == oldTree ? newTree : tree;
            if (!after.SyntaxTrees.Contains(candidateTree))
            {
                return false;
            }

            SemanticModel previous = before.GetSemanticModel(tree);
            SemanticModel next = after.GetSemanticModel(candidateTree);
            IOperation[] previousOperations = [.. GetCallOperations(previous, token)];
            IOperation[] nextOperations = [.. GetCallOperations(next, token)];

            if (previousOperations.Any(operation => CouldDynamicallyCall(operation, method)) ||
                nextOperations.Any(operation => CouldDynamicallyCall(operation, newMethod)))
            {
                return false;
            }

            // Collection expressions do not expose their implicit 'Add' calls as invocations in the operation tree
            if (method.Name == "Add" && tree.GetRoot(token).DescendantNodes().OfType<CollectionExpressionSyntax>().Any(expression =>
                UsesCollectionAdd(expression, previous, method, token)))
            {
                return false;
            }

            TextSpan replaced = new(change.Span.Start, change.NewText!.Length);
            ILookup<(TextSpan Span, OperationKind Kind, bool IsImplicit), IOperation> originalCalls = previousOperations
                .Where(operation => tree != oldTree || !change.Span.IntersectsWith(operation.Syntax.Span))
                .ToLookup(static operation => (operation.Syntax.Span, operation.Kind, operation.IsImplicit));
            ILookup<(TextSpan Span, OperationKind Kind, bool IsImplicit), IOperation> candidateCalls = nextOperations
                .Where(operation => candidateTree != newTree || !replaced.IntersectsWith(operation.Syntax.Span))
                .ToLookup(operation =>
                {
                    TextSpan span = operation.Syntax.Span;
                    int start = span.Start - (candidateTree == newTree && span.Start >= replaced.End ? delta : 0);
                    return (new TextSpan(start, span.Length), operation.Kind, operation.IsImplicit);
                });

            foreach ((TextSpan Span, OperationKind Kind, bool IsImplicit) key in
                originalCalls.Select(static group => group.Key).Union(candidateCalls.Select(static group => group.Key)))
            {
                IOperation[] originalOperations = [.. originalCalls[key]];
                IOperation[] candidateOperations = [.. candidateCalls[key]];
                bool referenced = originalOperations.Any(operation => SymbolEqualityComparer.Default.Equals(
                    GetCallTarget(operation)?.OriginalDefinition, method.OriginalDefinition));
                bool references = candidateOperations.Any(operation => SymbolEqualityComparer.Default.Equals(
                    GetCallTarget(operation)?.OriginalDefinition, newMethod.OriginalDefinition));

                if (!referenced && !references)
                {
                    continue;
                }

                if (originalOperations.Length != candidateOperations.Length)
                {
                    return false;
                }

                for (int index = 0; index < originalOperations.Length; index++)
                {
                    IMethodSymbol? originalTarget = GetCallTarget(originalOperations[index]);
                    IMethodSymbol? candidateTarget = GetCallTarget(candidateOperations[index]);
                    bool wasTarget = SymbolEqualityComparer.Default.Equals(originalTarget?.OriginalDefinition, method.OriginalDefinition);
                    bool isTarget = SymbolEqualityComparer.Default.Equals(candidateTarget?.OriginalDefinition, newMethod.OriginalDefinition);

                    // Delegate calls cannot be proven safe from the referenced method's binding alone
                    if ((wasTarget || isTarget) &&
                        (originalOperations[index] is IMethodReferenceOperation || candidateOperations[index] is IMethodReferenceOperation))
                    {
                        return false;
                    }

                    bool changedTypeArguments = wasTarget && isTarget &&
                        (originalTarget!.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) !=
                            candidateTarget!.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ||
                         !originalTarget.TypeArguments.Select(static type => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
                             .SequenceEqual(candidateTarget.TypeArguments.Select(static type => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))));
                    ImmutableArray<IArgumentOperation> arguments = originalOperations[index] switch
                    {
                        IInvocationOperation invocation => invocation.Arguments,
                        IObjectCreationOperation creation => creation.Arguments,
                        _ => []
                    };
                    ITypeSymbol? requiredElement = wasTarget &&
                        newMethod.Parameters[ordinal].Type.OriginalDefinition.ToDisplayString() == "System.Span<T>"
                            ? ((IArrayTypeSymbol)originalTarget!.Parameters[ordinal].Type).ElementType : null;
                    bool unsupportedArray = wasTarget && arguments.Any(argument =>
                        argument.Parameter?.Ordinal == ordinal &&
                        !IsKnownNonNullArray(argument.Value.Syntax, previous, requiredElement, token));

                    if (wasTarget != isTarget || unsupportedArray || changedTypeArguments)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    /// <summary>Recognizes collection expressions whose implicit population calls are not available for validation.</summary>
    /// <param name="expression">The collection expression.</param>
    /// <param name="model">The caller semantic model.</param>
    /// <param name="method">The method being converted.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>Whether population can use the changed Add method.</returns>
    private static bool UsesCollectionAdd(
        CollectionExpressionSyntax expression, SemanticModel model, IMethodSymbol method, CancellationToken token)
    {
        if (method.IsStatic && !method.IsExtensionMethod)
        {
            return false;
        }

        if (method.IsExtensionMethod)
        {
            return true;
        }

        for (INamedTypeSymbol? type = model.GetTypeInfo(expression, token).ConvertedType as INamedTypeSymbol;
            type is not null; type = type.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, method.ContainingType.OriginalDefinition))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Enumerates explicit and implicit calls from executable operation roots, including constructor chains and collection initializers.</summary>
    /// <param name="model">The source semantic model.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>The invocations, constructions, and method references in the tree.</returns>
    private static IEnumerable<IOperation> GetCallOperations(SemanticModel model, CancellationToken token)
    {
        HashSet<IOperation> roots = [];

        foreach (SyntaxNode syntax in model.SyntaxTree.GetRoot(token).DescendantNodes())
        {
            if (syntax is not (BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or EqualsValueClauseSyntax or
                ArrowExpressionClauseSyntax or GlobalStatementSyntax or ConstructorInitializerSyntax or PrimaryConstructorBaseTypeSyntax) ||
                model.GetOperation(syntax, token) is not { } operation)
            {
                continue;
            }

            while (operation.Parent is { } parent)
            {
                operation = parent;
            }

            if (roots.Add(operation))
            {
                foreach (IOperation descendant in operation.DescendantsAndSelf())
                {
                    if (descendant is IInvocationOperation or IObjectCreationOperation or IMethodReferenceOperation or
                        IDynamicInvocationOperation or IDynamicObjectCreationOperation or IDynamicMemberReferenceOperation)
                    {
                        yield return descendant;
                    }
                }
            }
        }
    }

    /// <summary>Declines dynamic dispatch whose member or constructed type could select the changed signature.</summary>
    /// <param name="operation">The original or candidate call operation.</param>
    /// <param name="method">The signature being converted.</param>
    /// <returns>Whether runtime dispatch prevents proving the call contract.</returns>
    private static bool CouldDynamicallyCall(IOperation operation, IMethodSymbol method)
    {
        return operation switch
        {
            IDynamicMemberReferenceOperation member => member.MemberName == method.Name,
            IDynamicObjectCreationOperation creation => method.MethodKind == MethodKind.Constructor &&
                (creation.Type is not INamedTypeSymbol type ||
                 SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, method.ContainingType.OriginalDefinition)),
            IDynamicInvocationOperation invocation => CouldDynamicallyCall(invocation.Operation, method) ||
                (method.MethodKind == MethodKind.Constructor &&
                 invocation.Syntax.AncestorsAndSelf().Any(static syntax =>
                     syntax is ConstructorInitializerSyntax or PrimaryConstructorBaseTypeSyntax)),
            _ => false
        };
    }

    /// <summary>Gets the selected method independently of whether a call is explicit or compiler-generated.</summary>
    /// <param name="operation">The call or method reference.</param>
    /// <returns>The selected method, if available.</returns>
    private static IMethodSymbol? GetCallTarget(IOperation operation)
    {
        return operation switch
        {
            IInvocationOperation invocation => invocation.TargetMethod,
            IObjectCreationOperation creation => creation.Constructor,
            IMethodReferenceOperation reference => reference.Method,
            _ => null
        };
    }

    /// <summary>Requires array construction evidence for null behavior and writable element identity.</summary>
    /// <param name="value">The original caller argument.</param>
    /// <param name="model">The caller semantic model.</param>
    /// <param name="requiredElement">The exact constructed element type required by a writable span, or null for readonly spans.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>Whether the argument constructs an array or reads an unmodified local initialized with one.</returns>
    private static bool IsKnownNonNullArray(
        SyntaxNode value, SemanticModel model, ITypeSymbol? requiredElement, CancellationToken token)
    {
        while (value is ParenthesizedExpressionSyntax parentheses)
        {
            value = parentheses.Expression;
        }

        if (value is ArrayCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax or CollectionExpressionSyntax)
        {
            return HasRequiredArrayElement(value, model, requiredElement, token);
        }

        if (value is not IdentifierNameSyntax identifier ||
            model.GetSymbolInfo(identifier, token).Symbol is not ILocalSymbol { RefKind: RefKind.None } local ||
            local.DeclaringSyntaxReferences.Length != 1 ||
            local.DeclaringSyntaxReferences[0].GetSyntax(token) is not VariableDeclaratorSyntax { Initializer.Value: { } initializer } declaration ||
            initializer is not (ArrayCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax or
                CollectionExpressionSyntax or InitializerExpressionSyntax))
        {
            return false;
        }

        foreach (IdentifierNameSyntax reference in declaration.SyntaxTree.GetRoot(token).DescendantNodes().OfType<IdentifierNameSyntax>())
        {
            if (!SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(reference, token).Symbol, local))
            {
                continue;
            }

            ExpressionSyntax use = reference;
            while (use.Parent is ParenthesizedExpressionSyntax parent)
            {
                use = parent;
            }

            // Only reads of the local are accepted; reference escape, reassignment, and captures require review
            if (!SymbolEqualityComparer.Default.Equals(model.GetEnclosingSymbol(reference.SpanStart, token), local.ContainingSymbol) ||
                !(use.Parent is ArgumentSyntax { RefKindKeyword.RawKind: 0, Parent: ArgumentListSyntax } ||
                  (use.Parent is MemberAccessExpressionSyntax member && member.Expression == use) ||
                  (use.Parent is ElementAccessExpressionSyntax element && element.Expression == use)))
            {
                return false;
            }
        }

        return HasRequiredArrayElement(initializer, model, requiredElement, token);
    }

    /// <summary>Checks construction identity rather than a covariantly widened array local's declared type.</summary>
    /// <param name="construction">The expression that creates the actual array.</param>
    /// <param name="model">The caller semantic model.</param>
    /// <param name="requiredElement">The writable element type, or null when covariance is supported.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>Whether writable span construction cannot introduce an array-type mismatch.</returns>
    private static bool HasRequiredArrayElement(
        SyntaxNode construction, SemanticModel model, ITypeSymbol? requiredElement, CancellationToken token)
    {
        if (requiredElement is null)
        {
            return true;
        }

        TypeInfo info = model.GetTypeInfo(construction, token);
        IArrayTypeSymbol? array = (info.Type ?? info.ConvertedType) as IArrayTypeSymbol;
        return array is not null && SymbolEqualityComparer.Default.Equals(array.ElementType, requiredElement);
    }

    /// <summary>Processes independent parameters from the end of each document, then cleans imports once.</summary>
    private sealed class ArrayFixAllProvider : FixAllProvider
    {
        /// <inheritdoc/>
        public override Task<CodeAction?> GetFixAsync(FixAllContext context)
        {
            bool preserve = context.CodeActionEquivalenceKey == PreserveKey;
            CodeAction action = CodeAction.Create(
                preserve ? "Convert array parameters and preserve CsWinRT 2.x" : "Convert array parameters to spans",
                async token =>
                {
                    Solution solution = context.Solution;
                    IEnumerable<Document> documents = context.Scope switch
                    {
                        FixAllScope.Document => [context.Document!],
                        FixAllScope.Project => context.Project.Documents,
                        FixAllScope.Solution => solution.Projects.SelectMany(static project => project.Documents),
                        FixAllScope.Custom or FixAllScope.ContainingMember or FixAllScope.ContainingType => [],
                        _ => []
                    };

                    foreach (Document original in documents)
                    {
                        ImmutableArray<Diagnostic> diagnostics = await context.GetDocumentDiagnosticsAsync(original).ConfigureAwait(false);
                        Document changed = solution.GetDocument(original.Id)!;

                        foreach (Diagnostic diagnostic in diagnostics.OrderByDescending(static item => item.Location.SourceSpan.Start))
                        {
                            changed = await TryConvertAsync(changed, diagnostic, preserve, cleanImports: false, token).ConfigureAwait(false);
                        }

                        if (changed != solution.GetDocument(original.Id))
                        {
                            changed = await LegacyWindowsRuntimeAttributeCodeFixProvider.CleanLegacyImportsAsync(changed, preserve, token).ConfigureAwait(false);
                            solution = changed.Project.Solution;
                        }
                    }

                    return solution;
                },
                preserve ? PreserveKey : DirectKey);

            return Task.FromResult<CodeAction?>(action);
        }
    }
}
