// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using WindowsRuntime.InteropServices;

namespace WindowsRuntime;

/// <summary>
/// The base class for all projected Windows Runtime delegates.
/// </summary>
/// <remarks>
/// This type should only be used as a base type by generated projected types. It is a minimal version of <see cref="WindowsRuntimeObject"/>,
/// which is used as target instance to produce managed objects for native Windows Runtime delegates. It should never be used directly by
/// application code. For this same reason, it also doesn't implement any additional interface, unlike <see cref="WindowsRuntimeObject"/>.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract unsafe class WindowsRuntimeDelegate
{
    /// <summary>
    /// Creates a <see cref="WindowsRuntimeDelegate"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    protected WindowsRuntimeDelegate(WindowsRuntimeObjectReference nativeObjectReference)
    {
        ArgumentNullException.ThrowIfNull(nativeObjectReference);

        NativeObjectReference = nativeObjectReference;
    }

    /// <summary>
    /// Gets the inner Windows Runtime object reference for the current instance.
    /// </summary>
    /// <remarks>
    /// This object reference should point to the delegate interface, for the wrapped Windows Runtime delegate object.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected internal WindowsRuntimeObjectReference NativeObjectReference { get; }
}
