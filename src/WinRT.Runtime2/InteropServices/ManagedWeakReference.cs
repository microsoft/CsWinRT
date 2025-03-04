// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The built-in default implementation of <see cref="IWeakReference"/>, wrapping managed objects.
/// </summary>
internal sealed class ManagedWeakReference : IWeakReference
{
    /// <summary>
    /// The weak reference for the target object.
    /// </summary>
    private readonly WeakReference<object> _weakReference;

    /// <summary>
    /// Creates a new <see cref="ManagedWeakReference"/> instance with the specified parameters.
    /// </summary>
    /// <param name="targetObject">The target object to create a weak reference for.</param>
    public ManagedWeakReference(object targetObject)
    {
        _weakReference = new WeakReference<object>(targetObject);
    }

    /// <inheritdoc/>
    public WindowsRuntimeObjectReference? Resolve(in Guid interfaceId)
    {
        if (_weakReference.TryGetTarget(out object? targetObject))
        {
            // TODO
        }

        return null;
    }
}
