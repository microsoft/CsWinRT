// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace WindowsRuntime.Migration;

/// <summary>
/// Validates source edits against the original compilation's exact error locations.
/// </summary>
internal static class MigrationCompilationValidation
{
    /// <summary>Matches existing errors through an edit's offset change without hiding new failures.</summary>
    /// <param name="before">The original compilation.</param>
    /// <param name="after">The candidate compilation.</param>
    /// <param name="oldTree">The original source tree.</param>
    /// <param name="newTree">The changed source tree.</param>
    /// <param name="change">The source edit.</param>
    /// <param name="token">The cancellation token.</param>
    /// <returns>Whether every candidate error already existed outside or encompassing the edited region.</returns>
    public static bool HasNoNewErrors(
        Compilation before, Compilation after, SyntaxTree oldTree, SyntaxTree newTree, TextChange change, CancellationToken token)
    {
        HashSet<(string Id, string Message, SyntaxTree? Tree, int Start, int Length)> baseline = [.. before.GetDiagnostics(token)
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .Select(static diagnostic => (diagnostic.Id, diagnostic.GetMessage(),
                diagnostic.Location.SourceTree, diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.Length))];
        int delta = change.NewText!.Length - change.Span.Length;
        TextSpan replaced = new(change.Span.Start, change.NewText.Length);

        foreach (Diagnostic diagnostic in after.GetDiagnostics(token).Where(static item => item.Severity == DiagnosticSeverity.Error))
        {
            TextSpan span = diagnostic.Location.SourceSpan;
            bool changedTree = diagnostic.Location.SourceTree == newTree;

            if (changedTree && replaced.IntersectsWith(span) && !span.Contains(replaced))
            {
                return false;
            }

            int start = changedTree && span.Start >= replaced.End ? span.Start - delta : span.Start;
            int length = changedTree && span.Contains(replaced) ? span.Length - delta : span.Length;

            if (!baseline.Contains((diagnostic.Id, diagnostic.GetMessage(),
                changedTree ? oldTree : diagnostic.Location.SourceTree, start, length)))
            {
                return false;
            }
        }

        return true;
    }
}
