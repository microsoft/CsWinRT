// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
using Microsoft.CodeAnalysis.Text;

namespace WindowsRuntime.Migration;

/// <summary>
/// Makes legacy geometry narrowing explicit without changing double expression evaluation.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp), Shared]
public sealed class GeometryCodeFixProvider : CodeFixProvider
{
    /// <summary>The action preserving the legacy double-to-float boundary.</summary>
    private const string EquivalenceKey = "CsWinRT.MigrateGeometryNarrowing";

    /// <inheritdoc/>
    public override ImmutableArray<string> FixableDiagnosticIds { get; } = ["CS1503", "CS0266", "CS0664"];

    /// <inheritdoc/>
    public override FixAllProvider GetFixAllProvider()
    {
        return new GeometryFixAllProvider();
    }

    /// <inheritdoc/>
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        Document changed = await TryConvertAsync(
            context.Document, context.Diagnostics[0], context.CancellationToken).ConfigureAwait(false);

        if (changed != context.Document)
        {
            context.RegisterCodeFix(CodeAction.Create(
                "Preserve CsWinRT 2.x geometry narrowing",
                token => TryConvertAsync(context.Document, context.Diagnostics[0], token),
                EquivalenceKey), context.Diagnostics);
        }
    }

    /// <summary>Validates one constructor or coordinate assignment before making its narrowing explicit.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="diagnostic">The compiler conversion error.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>The edited document, or the original when equivalence is not established.</returns>
    private static async Task<Document> TryConvertAsync(Document document, Diagnostic diagnostic, CancellationToken token)
    {
        SyntaxNode? root = await document.GetSyntaxRootAsync(token).ConfigureAwait(false);
        SemanticModel? model = await document.GetSemanticModelAsync(token).ConfigureAwait(false);

        if (root is null || model is null || !LegacyAttributeMigration.IsVersion3(root.SyntaxTree) ||
            LegacyAttributeMigration.IsGenerated(root.SyntaxTree, document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider, token) ||
            !root.FullSpan.Contains(diagnostic.Location.SourceSpan))
        {
            return document;
        }

        SyntaxNode site = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
        ExpressionSyntax? expression = null;
        IMethodSymbol? constructor = null;

        if (diagnostic.Id == "CS1503" &&
            site.FirstAncestorOrSelf<ArgumentSyntax>() is { Parent: ArgumentListSyntax { Parent: BaseObjectCreationExpressionSyntax targetCreation } })
        {
            expression = targetCreation;
            constructor = GetConstructor(targetCreation, model, token);
        }
        else if (diagnostic.Id is "CS0266" or "CS0664" &&
            site.FirstAncestorOrSelf<AssignmentExpressionSyntax>() is { RawKind: (int)SyntaxKind.SimpleAssignmentExpression } assignment &&
            assignment.Right.Span.Contains(diagnostic.Location.SourceSpan) &&
            (assignment.Parent is ExpressionStatementSyntax ||
                assignment.Parent is InitializerExpressionSyntax { RawKind: (int)SyntaxKind.ObjectInitializerExpression }) &&
            model.GetSymbolInfo(assignment.Left, token).Symbol is IFieldSymbol
            { Name: "X" or "Y", Type.SpecialType: SpecialType.System_Single } field &&
            IsGeometryType(field.ContainingType, model.Compilation))
        {
            expression = assignment.Right;
        }

        if (expression is null || LegacyAttributeMigration.HasInteriorDirectives(expression) ||
            (constructor is null && (expression is BaseObjectCreationExpressionSyntax ||
                model.GetTypeInfo(expression, token).Type?.SpecialType != SpecialType.System_Double)) ||
            !model.GetDiagnostics(diagnostic.Location.SourceSpan, token).Any(item =>
                item.Id == diagnostic.Id && item.Location.SourceSpan == diagnostic.Location.SourceSpan))
        {
            return document;
        }

        if (constructor is not null)
        {
            foreach (BaseObjectCreationExpressionSyntax enclosing in expression.Ancestors().OfType<BaseObjectCreationExpressionSyntax>())
            {
                if (GetConstructor(enclosing, model, token) is { } enclosingConstructor &&
                    !LegacyAttributeMigration.HasInteriorDirectives(enclosing))
                {
                    expression = enclosing;
                    constructor = enclosingConstructor;
                }
            }
        }

        Dictionary<BaseObjectCreationExpressionSyntax, IMethodSymbol> constructors = [];
        foreach (BaseObjectCreationExpressionSyntax creation in expression.DescendantNodesAndSelf().OfType<BaseObjectCreationExpressionSyntax>())
        {
            if (GetConstructor(creation, model, token) is { } target)
            {
                constructors.Add(creation, target);
            }
        }

        ExpressionSyntax[] calls = [.. root.DescendantNodes().OfType<ExpressionSyntax>()
            .Where(static node => node is InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax)];
        ExpressionSyntax[] values = [.. constructors.Keys.SelectMany(static creation =>
            creation.ArgumentList!.Arguments.Select(static argument => argument.Expression))];
        SyntaxNode tracked = root.TrackNodes(calls.Concat(values).Append(expression).Distinct());
        ExpressionSyntax original = tracked.GetCurrentNode(expression)!;
        ExpressionSyntax replacement = original.ReplaceNodes(
            constructors.Keys.Select(creation => tracked.GetCurrentNode(creation)!),
            (previous, current) =>
            {
                BaseObjectCreationExpressionSyntax source = constructors.Keys.Single(creation => creation.Span == previous.Span);
                return current.WithArgumentList(current.ArgumentList!.WithArguments(
                    SyntaxFactory.SeparatedList(current.ArgumentList.Arguments.Select((argument, index) =>
                        argument.WithExpression(Narrow(argument.Expression,
                            model.GetTypeInfo(source.ArgumentList!.Arguments[index].Expression, token).Type?.SpecialType != SpecialType.System_Double))),
                        current.ArgumentList.Arguments.GetSeparators())));
            });

        if (constructor is null)
        {
            replacement = Narrow(replacement, widenFirst: false);
        }

        SyntaxNode nextRoot = tracked.ReplaceNode(original, replacement);
        Document changed = document.WithSyntaxRoot(nextRoot);
        SemanticModel? next = await changed.GetSemanticModelAsync(token).ConfigureAwait(false);
        nextRoot = (await changed.GetSyntaxRootAsync(token).ConfigureAwait(false))!;
        ExpressionSyntax? converted = nextRoot.GetCurrentNode(expression);

        if (next is null || converted is null ||
            !MigrationCompilationValidation.HasNoNewErrors(
                model.Compilation, next.Compilation, root.SyntaxTree, next.SyntaxTree,
                new TextChange(expression.Span, replacement.ToString()), token) ||
            (constructor is not null && !SameMethod(constructor, next.GetSymbolInfo(converted, token).Symbol as IMethodSymbol)))
        {
            return document;
        }

        foreach (ExpressionSyntax value in values)
        {
            ExpressionSyntax? candidate = nextRoot.GetCurrentNode(value);
            Conversion conversion = candidate is null ? default :
                next.ClassifyConversion(candidate, next.Compilation.GetSpecialType(SpecialType.System_Double));

            if (!conversion.IsImplicit || (!conversion.IsNumeric && !conversion.IsIdentity))
            {
                return document;
            }
        }

        foreach (ExpressionSyntax call in calls)
        {
            ExpressionSyntax? candidate = nextRoot.GetCurrentNode(call);
            IMethodSymbol? expected = call is BaseObjectCreationExpressionSyntax creation &&
                constructors.TryGetValue(creation, out IMethodSymbol? target)
                    ? target : model.GetSymbolInfo(call, token).Symbol as IMethodSymbol;

            if (candidate is null || !SameMethod(expected,
                next.GetSymbolInfo(candidate, token).Symbol as IMethodSymbol))
            {
                return document;
            }
        }

        return changed;
    }

    /// <summary>Identifies a float constructor corresponding to the legacy double overload.</summary>
    /// <param name="creation">The broken constructor call.</param>
    /// <param name="model">The original semantic model.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>The exact constructor, or null for unsupported arguments or types.</returns>
    private static IMethodSymbol? GetConstructor(
        BaseObjectCreationExpressionSyntax creation, SemanticModel model, CancellationToken token)
    {
        if (creation.ArgumentList is not { } list ||
            model.GetTypeInfo(creation, token).Type is not INamedTypeSymbol type ||
            !IsGeometryType(type, model.Compilation))
        {
            return null;
        }

        int count = type.Name == "Rect" ? 4 : 2;
        IMethodSymbol? constructor = type.InstanceConstructors.SingleOrDefault(method =>
            method.Parameters.Length == count &&
            method.Parameters.All(static parameter => parameter.Type.SpecialType == SpecialType.System_Single));
        SymbolInfo info = model.GetSymbolInfo(creation, token);

        if (constructor is null || list.Arguments.Count != count ||
            (!SymbolEqualityComparer.Default.Equals(info.Symbol, constructor) &&
                !info.CandidateSymbols.Contains(constructor, SymbolEqualityComparer.Default)) ||
            !list.Arguments.Any(argument => model.GetTypeInfo(argument.Expression, token).Type?.SpecialType == SpecialType.System_Double))
        {
            return null;
        }

        HashSet<int> ordinals = [];
        ITypeSymbol doubleType = model.Compilation.GetSpecialType(SpecialType.System_Double);

        for (int index = 0; index < list.Arguments.Count; index++)
        {
            ArgumentSyntax argument = list.Arguments[index];
            int ordinal = argument.NameColon is null ? index :
                constructor.Parameters.FirstOrDefault(parameter => parameter.Name == argument.NameColon.Name.Identifier.ValueText)?.Ordinal ?? -1;
            Conversion conversion = model.ClassifyConversion(argument.Expression, doubleType);
            bool recoverable = model.GetTypeInfo(argument.Expression, token).Type?.TypeKind == TypeKind.Error &&
                argument.Expression.DescendantNodes().OfType<BaseObjectCreationExpressionSyntax>()
                    .Any(nested => GetConstructor(nested, model, token) is not null);

            if (argument.RefKindKeyword.RawKind != 0 || ordinal < 0 || !ordinals.Add(ordinal) ||
                (!recoverable && (!conversion.IsImplicit || (!conversion.IsNumeric && !conversion.IsIdentity))))
            {
                return null;
            }
        }

        return constructor;
    }

    /// <summary>Recognizes only metadata-defined CsWinRT geometry structs on the selected runtime.</summary>
    /// <param name="type">The candidate struct.</param>
    /// <param name="compilation">The source compilation.</param>
    /// <returns>Whether the type is the runtime's exact geometry definition.</returns>
    private static bool IsGeometryType(INamedTypeSymbol type, Compilation compilation)
    {
        return type.TypeKind == TypeKind.Struct && type.DeclaringSyntaxReferences.IsEmpty &&
               type.ContainingAssembly.Identity.Name == "WinRT.Runtime" &&
               type.ToDisplayString() is "Windows.Foundation.Point" or "Windows.Foundation.Size" or "Windows.Foundation.Rect" &&
               SymbolEqualityComparer.Default.Equals(type, compilation.GetTypeByMetadataName(type.ToDisplayString()));
    }

    /// <summary>Preserves evaluation of the full expression before the legacy float boundary.</summary>
    /// <param name="expression">The original value expression.</param>
    /// <param name="widenFirst">Whether the old double overload first widened a non-double argument.</param>
    /// <returns>The explicit narrowing expression with the original exterior trivia.</returns>
    private static ExpressionSyntax Narrow(ExpressionSyntax expression, bool widenFirst)
    {
        ExpressionSyntax value = expression.WithoutTrivia();

        if (widenFirst)
        {
            value = SyntaxFactory.CastExpression(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.DoubleKeyword)),
                SyntaxFactory.ParenthesizedExpression(value));
        }

        return SyntaxFactory.CastExpression(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.FloatKeyword)),
            SyntaxFactory.ParenthesizedExpression(value)).WithTriviaFrom(expression);
    }

    /// <summary>Compares selected overloads and inferred type arguments across source compilations.</summary>
    /// <param name="before">The original method binding.</param>
    /// <param name="after">The candidate method binding.</param>
    /// <returns>Whether the selected method's assembly and constructed signature are unchanged.</returns>
    private static bool SameMethod(IMethodSymbol? before, IMethodSymbol? after)
    {
        return before is null ? after is null :
            after is not null && before.ContainingAssembly.Identity.Equals(after.ContainingAssembly.Identity) &&
            before.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ==
                after.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    /// <summary>Reevaluates compiler diagnostics after each edit to handle overlapping constructor arguments.</summary>
    private sealed class GeometryFixAllProvider : FixAllProvider
    {
        /// <inheritdoc/>
        public override Task<CodeAction?> GetFixAsync(FixAllContext context)
        {
            return Task.FromResult<CodeAction?>(CodeAction.Create("Preserve CsWinRT 2.x geometry narrowing",
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
                        ImmutableArray<Diagnostic> permitted = await context.GetDocumentDiagnosticsAsync(original).ConfigureAwait(false);
                        HashSet<string> diagnosticIds = [.. permitted.Select(static diagnostic => diagnostic.Id)
                            .Where(context.DiagnosticIds.Contains)];

                        if (diagnosticIds.Count == 0)
                        {
                            continue;
                        }

                        Document current = solution.GetDocument(original.Id)!;
                        bool converted;

                        do
                        {
                            converted = false;
                            SyntaxTree? tree = await current.GetSyntaxTreeAsync(token).ConfigureAwait(false);
                            Compilation? compilation = await current.Project.GetCompilationAsync(token).ConfigureAwait(false);

                            foreach (Diagnostic diagnostic in compilation!.GetDiagnostics(token).Where(item =>
                                item.Location.SourceTree == tree && diagnosticIds.Contains(item.Id))
                                .OrderByDescending(static item => item.Location.SourceSpan.Start))
                            {
                                Document next = await TryConvertAsync(current, diagnostic, token).ConfigureAwait(false);

                                if (next != current)
                                {
                                    current = next;
                                    converted = true;
                                    break;
                                }
                            }
                        }
                        while (converted);

                        solution = current.Project.Solution;
                    }

                    return solution;
                }, EquivalenceKey));
        }
    }
}
