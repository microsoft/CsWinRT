// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Naming conventions for the array-temporary locals used by inline-array-or-array-pool
/// marshalling stubs (used by <c>DoAbi</c> / <c>RcwCaller</c> / <c>ConstructorFactory</c>).
/// Centralises the <c>__{prefix}_inlineArray</c> / <c>__{prefix}_arrayFromPool</c> /
/// <c>__{prefix}_span</c> name conventions so future churn lands in one place.
/// </summary>
/// <param name="Prefix">The parameter/local identifier prefix (e.g. the raw or call name).</param>
internal readonly record struct ArrayTempNames(string Prefix)
{
    /// <summary>The local-variable name for the <c>InlineArray16&lt;T&gt;</c> inline buffer.</summary>
    public string InlineArray => "__" + Prefix + "_inlineArray";

    /// <summary>The local-variable name for the <c>T[]</c> array-pool rental.</summary>
    public string ArrayFromPool => "__" + Prefix + "_arrayFromPool";

    /// <summary>The local-variable name for the <c>Span&lt;T&gt;</c> view of either backing store.</summary>
    public string Span => "__" + Prefix + "_span";

    /// <summary>Auxiliary triple used when string-array marshalling needs a parallel <c>HStringHeader[]</c> pool.</summary>
    public string InlineHeaderArray => "__" + Prefix + "_inlineHeaderArray";

    /// <summary>The companion to <see cref="InlineHeaderArray"/>.</summary>
    public string HeaderArrayFromPool => "__" + Prefix + "_headerArrayFromPool";

    /// <summary>The companion to <see cref="InlineHeaderArray"/>.</summary>
    public string HeaderSpan => "__" + Prefix + "_headerSpan";

    /// <summary>Auxiliary triple used when string-array marshalling needs a parallel pinned-handle <c>nint[]</c> pool.</summary>
    public string InlinePinnedHandleArray => "__" + Prefix + "_inlinePinnedHandleArray";

    /// <summary>The companion to <see cref="InlinePinnedHandleArray"/>.</summary>
    public string PinnedHandleArrayFromPool => "__" + Prefix + "_pinnedHandleArrayFromPool";

    /// <summary>The companion to <see cref="InlinePinnedHandleArray"/>.</summary>
    public string PinnedHandleSpan => "__" + Prefix + "_pinnedHandleSpan";
}
