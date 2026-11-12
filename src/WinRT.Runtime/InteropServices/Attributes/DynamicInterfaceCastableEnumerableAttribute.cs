// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#define WINDOWS_RUNTIME_IMPLEMENTATION_ONLY_FILE

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides typed enumeration for a generic collection interface implementation.
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
[WindowsRuntimeImplementationOnlyMember]
public abstract class DynamicInterfaceCastableEnumerableAttribute : Attribute
{
    /// <summary>
    /// Creates a <see cref="DynamicInterfaceCastableEnumerableAttribute"/> instance.
    /// </summary>
    protected DynamicInterfaceCastableEnumerableAttribute()
    {
    }

    /// <summary>
    /// Tries to enumerate an object using the associated constructed generic enumerable.
    /// </summary>
    /// <param name="thisObject">The object to enumerate.</param>
    /// <param name="enumerator">The typed enumerator, if the generic enumerable is supported.</param>
    /// <returns>Whether the generic enumerable is supported.</returns>
    public abstract bool TryGetEnumerator(WindowsRuntimeObject thisObject, [NotNullWhen(true)] out IEnumerator? enumerator);
}
