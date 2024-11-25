using Microsoft.CodeAnalysis;

namespace WinRT.SourceGenerator;

/// <summary>
/// A container for all <see cref="DiagnosticDescriptor"/> instances for errors reported by analyzers in this project.
/// </summary>
public static class WinRTSuppressions
{
    /// <summary>
    /// Gets a <see cref="SuppressionDescriptor"/> for 'IDE0300', ie. "Use collection expression for array".
    /// </summary>
    public static readonly SuppressionDescriptor CollectionExpressionIDE0300 = new(
        id: "CsWinRT2001",
        suppressedDiagnosticId: "IDE0300",
        justification:
            "Using collection expressions when targeting 'IEnumerable<T>', 'ICollection<T>', 'IList<T>', 'IReadOnlyCollection<T>', " +
            "or 'IReadOnlyList<T>', when the collection expression is not empty, is not AOT compatible in WinRT scenarios");

    /// <summary>
    /// Gets a <see cref="SuppressionDescriptor"/> for 'IDE0303', ie. "Use collection expression for Create()".
    /// </summary>
    public static readonly SuppressionDescriptor CollectionExpressionIDE0303 = new(
        id: "CsWinRT2002",
        suppressedDiagnosticId: "IDE0303",
        justification: "Using collection expressions when targeting an interface type without '[CollectionBuilder]' is not AOT compatible in WinRT scenarios");

    /// <summary>
    /// Gets a <see cref="SuppressionDescriptor"/> for 'IDE0304', ie. "Use collection expression for builder".
    /// </summary>
    public static readonly SuppressionDescriptor CollectionExpressionIDE0304 = new(
        id: "CsWinRT2003",
        suppressedDiagnosticId: "IDE0304",
        justification: "Using collection expressions when targeting an interface type without '[CollectionBuilder]' is not AOT compatible in WinRT scenarios");

    /// <summary>
    /// Gets a <see cref="SuppressionDescriptor"/> for 'IDE0305', ie. "Use collection expression for fluent".
    /// </summary>
    public static readonly SuppressionDescriptor CollectionExpressionIDE0305 = new(
        id: "CsWinRT2004",
        suppressedDiagnosticId: "IDE0305",
        justification: "Using collection expressions when targeting an interface type without '[CollectionBuilder]' is not AOT compatible in WinRT scenarios");
}
