// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
/// ImmutableArray<int> i = ImmutableArray.Create(1, 2, 3);
/// IEnumerable<int> j = ImmutableArray.Create(1, 2, 3);
/// </code>
/// </para>
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class CollectionExpressionIDE0303Suppressor : DiagnosticSuppressor
{
    /// <inheritdoc/>
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = [WinRTSuppressions.CollectionExpressionIDE0303];

    /// <inheritdoc/>
    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (Diagnostic diagnostic in context.ReportedDiagnostics)
        {
            if (CollectionExpressionIDE0305Suppressor.IsInvocationAssignedToUnsupportedInterfaceType(context, diagnostic))
            {
                context.ReportSuppression(Suppression.Create(WinRTSuppressions.CollectionExpressionIDE0303, diagnostic));
            }
        }
    }
}
