// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#define WINDOWS_RUNTIME_IMPLEMENTATION_ONLY_FILE

using System;
using System.Collections;

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
    /// <returns>The typed enumerator, or <see langword="null"/> if the generic enumerable is not supported.</returns>
    protected abstract IEnumerator? TryGetEnumeratorCore(WindowsRuntimeObject thisObject);

    /// <summary>
    /// Tries to enumerate an object and unwraps any managed iterator adapters returned across the ABI.
    /// </summary>
    /// <param name="thisObject">The object to enumerate.</param>
    /// <returns>The unwrapped enumerator, or <see langword="null"/> if the provider did not return one.</returns>
    internal IEnumerator? TryGetEnumerator(WindowsRuntimeObject thisObject)
    {
        IEnumerator? enumerator = TryGetEnumeratorCore(thisObject);

        return enumerator is not null ? IEnumeratorAdapter.TryUnwrap(enumerator) : null;
    }
}
