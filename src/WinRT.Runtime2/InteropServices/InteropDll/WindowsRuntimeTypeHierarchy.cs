// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

#pragma warning disable CS1573

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides fast lookup for base runtime class names for derived Windows Runtime classes.
/// </summary>
internal static class WindowsRuntimeTypeHierarchy
{
    /// <summary>
    /// Tries to find the runtime class name of the base class of a given Windows Runtime class.
    /// </summary>
    /// <param name="runtimeClassName">The runtime class name of the input Windows Runtime type.</param>
    /// <param name="baseRuntimeClassName">The resulting runtime class name of the base type for <paramref name="baseRuntimeClassName"/>.</param>
    /// <param name="nextBaseRuntimeClassNameIndex">The index of the next base class, if applicable (use with <see cref="TryGetNextBaseRuntimeClassName"/>).</param>
    /// <returns>Whether <paramref name="baseRuntimeClassName"/> was successfully retrieved.</returns>
    public static bool TryGetBaseRuntimeClassName(
        scoped ReadOnlySpan<char> runtimeClassName,
        out ReadOnlySpan<char> baseRuntimeClassName,
        out int nextBaseRuntimeClassNameIndex)
    {
        throw null!;
    }

    /// <summary>
    /// Tries to find the next runtime class name following a previous lookup.
    /// </summary>
    /// <param name="baseRuntimeClassNameIndex">The index of the base class name to retrieve, if available.</param>
    /// <param name="baseRuntimeClassName">The resulting runtime class name of the base type for <paramref name="baseRuntimeClassName"/>.</param>
    /// <param name="nextBaseRuntimeClassNameIndex">The index of the next base class, if applicable (use with <see cref="TryGetNextBaseRuntimeClassName"/>).</param>
    /// <returns>Whether <paramref name="baseRuntimeClassName"/> was successfully retrieved.</returns>
    public static bool TryGetNextBaseRuntimeClassName(
        int baseRuntimeClassNameIndex,
        out ReadOnlySpan<char> baseRuntimeClassName,
        out int nextBaseRuntimeClassNameIndex)
    {
        throw null!;
    }
}

/// <summary>
/// Forwarded stubs from the <c>WindowsRuntimeTypeHierarchyData</c> type in <c>WinRT.Interop.dll</c>.
/// </summary>
file static class WindowsRuntimeTypeHierarchyData
{
    /// <summary>
    /// Gets the <see cref="ReadOnlySpan{T}"/> for the runtime class name lookup buckets.
    /// </summary>
    /// <returns>The buckets <see cref="ReadOnlySpan{T}"/>.</returns>
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
    public static extern ReadOnlySpan<int> get_Buckets(
        /* [UnsafeAccessorType("WindowsRuntime.Interop.WindowsRuntimeTypeHierarchyData, WinRT.Interop.dll"))] */ object? _);

    /// <summary>
    /// Gets the <see cref="ReadOnlySpan{T}"/> for the runtime class name lookup keys.
    /// </summary>
    /// <returns>The keys <see cref="ReadOnlySpan{T}"/>.</returns>
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
    public static extern ReadOnlySpan<byte> get_Keys(
        /* [UnsafeAccessorType("WindowsRuntime.Interop.WindowsRuntimeTypeHierarchyData, WinRT.Interop.dll"))] */ object? _);

    /// <summary>
    /// Gets the <see cref="ReadOnlySpan{T}"/> for the runtime class name lookup values.
    /// </summary>
    /// <returns>The values <see cref="ReadOnlySpan{T}"/>.</returns>
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
    public static extern ReadOnlySpan<byte> get_Values(
        /* [UnsafeAccessorType("WindowsRuntime.Interop.WindowsRuntimeTypeHierarchyData, WinRT.Interop.dll"))] */ object? _);

    /// <summary>
    /// Gets the <see cref="ReadOnlySpan{T}"/> for the runtime class name lookup buckets.
    /// </summary>
    /// <returns>The buckets <see cref="ReadOnlySpan{T}"/>.</returns>
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
    public static extern bool IsLengthInRange(
        /* [UnsafeAccessorType("WindowsRuntime.Interop.WindowsRuntimeTypeHierarchyData, WinRT.Interop.dll"))] */ object? _,
        int length);
}
