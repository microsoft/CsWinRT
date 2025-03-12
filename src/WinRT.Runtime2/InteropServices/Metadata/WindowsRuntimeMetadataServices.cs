// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Threading;

namespace WindowsRuntime.InteropServices.Metadata;

/// <summary>
/// Provides helpers to register additional metadata related to Windows Runtime types.
/// </summary>
/// <remarks>This API is for use by the output of the CsWinRT source generators, and it should not be called directly by user code.</remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class WindowsRuntimeMetadataServices
{
    /// <summary>
    /// The registered type hierarchy lookup to use to create managed instances of derived Windows Runtime types.
    /// </summary>
    private static WindowsRuntimeTypeHierarchyLookup? typeHierarchyLookup;

    /// <summary>
    /// Set the type hierarchy lookup to use to create managed instances of derived Windows Runtime types.
    /// </summary>
    /// <param name="typeHierarchyLookup">The type hierarchy lookup to set.</param>
    /// <remarks>
    /// This handler can only be set once. Trying to register a second handler fails with <see cref="InvalidOperationException"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="typeHierarchyLookup"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a resolver has already been set.</exception>
    public static void SetTypeHierarchyLookup(WindowsRuntimeTypeHierarchyLookup typeHierarchyLookup)
    {
        ArgumentNullException.ThrowIfNull(typeHierarchyLookup);

        // Set the new lookup, if this is the first time the method is called
        WindowsRuntimeTypeHierarchyLookup? previousTypeHierarchyLookup = Interlocked.CompareExchange(
            location1: ref WindowsRuntimeMetadataServices.typeHierarchyLookup,
            value: typeHierarchyLookup,
            comparand: null);

        // We only allow setting this once
        if (previousTypeHierarchyLookup is not null)
        {
            throw new InvalidOperationException("The type hierarchy lookup has already been set (it can only be set once).");
        }
    }

    /// <summary>
    /// Tries to get the runtime class name for the base type of a given Windows Runtime type.
    /// </summary>
    /// <param name="runtimeClassName"><inheritdoc cref="WindowsRuntimeTypeHierarchyLookup" path="/param[@name='runtimeClassName']/node()"/></param>
    /// <param name="baseRuntimeClassName"><inheritdoc cref="WindowsRuntimeTypeHierarchyLookup" path="/param[@name='baseRuntimeClassName']/node()"/></param>
    /// <returns><inheritdoc cref="WindowsRuntimeTypeHierarchyLookup" path="/returns/node()"/></returns>
    internal static bool TryGetBaseTypeRuntimeClassName(scoped ReadOnlySpan<char> runtimeClassName, out ReadOnlySpan<char> baseRuntimeClassName)
    {
        if (typeHierarchyLookup is null)
        {
            baseRuntimeClassName = [];

            return false;
        }

        return typeHierarchyLookup(runtimeClassName, out baseRuntimeClassName);
    }
}
