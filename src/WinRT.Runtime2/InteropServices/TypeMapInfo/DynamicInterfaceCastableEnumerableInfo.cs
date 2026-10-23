// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Lazily caches typed enumeration providers used by the non-generic enumerable fallback.
/// </summary>
internal static class DynamicInterfaceCastableEnumerableInfo
{
    /// <summary>
    /// The enumeration providers for implementation types, including an immutable absence placeholder.
    /// </summary>
    /// <remarks>The weak-key table avoids generating collection marshalling for this private metadata cache.</remarks>
    private static readonly ConditionalWeakTable<Type, DynamicInterfaceCastableEnumerableAttribute> TypeToEnumerableTable = [];

    /// <summary>
    /// The cached factory for retrieving an implementation type's enumeration provider.
    /// </summary>
    private static readonly Func<Type, DynamicInterfaceCastableEnumerableAttribute> CreateEnumerableCallback = new(CreateEnumerable);

    /// <summary>
    /// Tries to retrieve the shared enumeration provider for an implementation type.
    /// </summary>
    /// <param name="implementationType">The implementation type to inspect.</param>
    /// <param name="enumerable">The shared enumeration provider, if available.</param>
    /// <returns>Whether a provider was found.</returns>
    public static bool TryGetInfo(Type implementationType, [NotNullWhen(true)] out DynamicInterfaceCastableEnumerableAttribute? enumerable)
    {
        DynamicInterfaceCastableEnumerableAttribute value = TypeToEnumerableTable.GetOrAdd(implementationType, CreateEnumerableCallback);

        if (value is not DynamicInterfaceCastableEnumerableAttributePlaceholder)
        {
            enumerable = value;

            return true;
        }

        enumerable = null;

        return false;
    }

    /// <summary>
    /// Retrieves an implementation type's enumeration provider, or the absence placeholder.
    /// </summary>
    /// <param name="implementationType">The implementation type to inspect.</param>
    /// <returns>The enumeration provider or absence placeholder.</returns>
    private static DynamicInterfaceCastableEnumerableAttribute CreateEnumerable(Type implementationType)
    {
        return implementationType.GetCustomAttribute<DynamicInterfaceCastableEnumerableAttribute>(inherit: false)
            ?? DynamicInterfaceCastableEnumerableAttributePlaceholder.Instance;
    }
}
