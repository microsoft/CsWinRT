// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using Windows.Storage.Streams;
using WindowsRuntime.InteropServices;

namespace WindowsRuntime;

/// <summary>
/// The implementation of the projected Windows Runtime <see cref="IBuffer"/> type.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.storage.streams.ibuffer"/>
[WindowsRuntimeManagedOnlyType]
internal sealed class WindowsRuntimeBuffer : WindowsRuntimeObject, IBuffer, IWindowsRuntimeInterface<IBuffer>
{
    /// <summary>
    /// Creates a <see cref="WindowsRuntimeBuffer"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    public WindowsRuntimeBuffer(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference)
    {
    }

    /// <inheritdoc/>
    protected internal override bool HasUnwrappableNativeObjectReference => true;

    /// <inheritdoc/>
    public uint Capacity => ABI.Windows.Storage.Streams.IBufferMethods.Capacity(NativeObjectReference);

    /// <inheritdoc/>
    public uint Length
    {
        get => ABI.Windows.Storage.Streams.IBufferMethods.Length(NativeObjectReference);
        set => ABI.Windows.Storage.Streams.IBufferMethods.Length(NativeObjectReference, value);
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IBuffer>.GetInterface()
    {
        return NativeObjectReference.AsValue();
    }

    /// <inheritdoc/>
    protected override bool IsOverridableInterface(in Guid iid)
    {
        return false;
    }
}
#endif
