// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

#pragma warning disable CS1573

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides fast lookup for base runtime class names for derived Windows Runtime classes.
/// </summary>
internal static class WindowsRuntimeTypeHierarchyBuilder
{
    /// <summary>
    /// Tries to find the runtime class name of the base class of a given Windows Runtime class.
    /// </summary>
    /// <param name="runtimeClassName">The runtime class name of the input Windows Runtime type.</param>
    /// <param name="baseRuntimeClassName">The resulting runtime class name of the base type for <paramref name="baseRuntimeClassName"/>.</param>
    /// <param name="nextBaseRuntimeClassNameIndex">The index of the next base class, if applicable (use with <see cref="TryGetNextBaseRuntimeClassName"/>).</param>
    /// <returns>Whether <paramref name="baseRuntimeClassName"/> was successfully retrieved.</returns>
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
    public static extern bool TryGetBaseRuntimeClassName(
        /* [UnsafeAccessorType("WindowsRuntime.Interop.WindowsRuntimeTypeHierarchyBuilder, WinRT.Interop.dll"))] */ object? _,
        scoped ReadOnlySpan<char> runtimeClassName,
        out ReadOnlySpan<char> baseRuntimeClassName,
        out int nextBaseRuntimeClassNameIndex);

    /// <summary>
    /// Tries to find the next runtime class name following a previous lookup.
    /// </summary>
    /// <param name="baseRuntimeClassNameIndex">The index of the base class name to retrieve, if available.</param>
    /// <param name="baseRuntimeClassName">The resulting runtime class name of the base type for <paramref name="baseRuntimeClassName"/>.</param>
    /// <param name="nextBaseRuntimeClassNameIndex">The index of the next base class, if applicable (use with <see cref="TryGetNextBaseRuntimeClassName"/>).</param>
    /// <returns>Whether <paramref name="baseRuntimeClassName"/> was successfully retrieved.</returns>
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
    public static extern bool TryGetNextBaseRuntimeClassName(
        /* [UnsafeAccessorType("WindowsRuntime.Interop.WindowsRuntimeTypeHierarchyBuilder, WinRT.Interop.dll"))] */ object? _,
        int baseRuntimeClassNameIndex,
        out ReadOnlySpan<char> baseRuntimeClassName,
        out int nextBaseRuntimeClassNameIndex);
}
