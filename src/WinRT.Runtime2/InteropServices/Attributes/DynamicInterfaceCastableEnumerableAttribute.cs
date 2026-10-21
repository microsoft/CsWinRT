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
    /// Checks and caches the constructed generic enumerable interface on an object.
    /// </summary>
    /// <param name="thisObject">The object to query.</param>
    /// <returns>Whether the constructed generic enumerable is supported.</returns>
    public abstract bool IsInterfaceImplemented(WindowsRuntimeObject thisObject);

    /// <summary>
    /// Enumerates an object using the associated constructed generic enumerable.
    /// </summary>
    /// <param name="thisObject">The object to enumerate.</param>
    /// <returns>The typed enumerator, exposed through its non-generic interface.</returns>
    protected abstract IEnumerator GetEnumeratorCore(WindowsRuntimeObject thisObject);

    /// <summary>
    /// Enumerates an object and unwraps any managed iterator adapters returned across the ABI.
    /// </summary>
    /// <param name="thisObject">The object to enumerate.</param>
    /// <returns>The original managed iterator, or the typed native iterator wrapper.</returns>
    internal IEnumerator GetEnumerator(WindowsRuntimeObject thisObject)
    {
        return IEnumeratorAdapter.TryUnwrap(GetEnumeratorCore(thisObject));
    }
}
