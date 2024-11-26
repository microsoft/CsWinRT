// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if ROSLYN_4_12_0_OR_GREATER

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using WinRT.SourceGenerator;

#nullable enable

namespace Generator;

/// <summary>
/// <para>
/// A diagnostic suppressor to suppress collection expression warnings where needed for AOT compatibility in WinRT scenarios.
/// </para>
/// <para>
/// This analyzer suppress diagnostics for cases like these:
/// <code lang="csharp">
/// var builder = ImmutableArray.CreateBuilder<int>();
/// builder.Add(1);
/// builder.AddRange(new int[] { 5, 6, 7 });
/// ImmutableArray<int> i = builder.ToImmutable();
/// </code>
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CollectionExpressionIDE0304Suppressor : DiagnosticSuppressor
{
    /// <inheritdoc/>
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = [WinRTSuppressions.CollectionExpressionIDE0304];

    /// <inheritdoc/>
    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        // Skip the logic if CsWinRT is not in 'auto' mode
        if (!GeneratorExecutionContextHelper.IsCsWinRTAotOptimizerInAutoMode(context.Options.AnalyzerConfigOptionsProvider, context.Compilation))
        {
            return;
        }

        foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
        {
            // The 'IDE0304' analyzer will add the location of the invocation expression in the additional locations set
            if (diagnostic.AdditionalLocations is not [{ } invocationLocation, ..])
            {
                continue;
            }

            // Check the target invocation. The only thing we care about for this warning is whether the final invocation
            // was being assigned to a concrete type (which is supported), or to a readonly interface type (which isn't).
            SyntaxNode? syntaxNode = invocationLocation.SourceTree?.GetRoot(context.CancellationToken).FindNode(invocationLocation.SourceSpan);

            if (CollectionExpressionIDE0305Suppressor.IsInvocationAssignedToUnsupportedInterfaceType(context, syntaxNode))
            {
                context.ReportSuppression(Suppression.Create(WinRTSuppressions.CollectionExpressionIDE0304, diagnostic));
            }
        }
    }
}

#endif
