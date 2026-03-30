// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Windows.Foundation;
using Windows.Storage.Streams;
using WindowsRuntime.InteropServices;

namespace WindowsRuntime;

/// <summary>
/// The implementation of the projected Windows Runtime <see cref="IOutputStream"/> type.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.storage.streams.ioutputstream"/>
[WindowsRuntimeManagedOnlyType]
internal sealed class WindowsRuntimeOutputStream : WindowsRuntimeObject,
    IOutputStream,
    IWindowsRuntimeInterface<IOutputStream>,
    IWindowsRuntimeInterface<IDisposable>
{
    /// <summary>
    /// Creates a <see cref="WindowsRuntimeOutputStream"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    public WindowsRuntimeOutputStream(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference)
    {
    }

    /// <inheritdoc cref="WindowsRuntimeInputStream.IClosableObjectReference"/>
    private WindowsRuntimeObjectReference IClosableObjectReference
    {
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            WindowsRuntimeObjectReference InitializeIClosableObjectReference()
            {
                _ = Interlocked.CompareExchange(
                    location1: ref field,
                    value: NativeObjectReference.As(in WellKnownWindowsInterfaceIIDs.IID_IClosable),
                    comparand: null);

                return field;
            }

            return field ?? InitializeIClosableObjectReference();
        }
    }

    /// <inheritdoc/>
    protected internal override bool HasUnwrappableNativeObjectReference => true;

    /// <inheritdoc/>
    public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
    {
        return ABI.Windows.Storage.Streams.IOutputStreamMethods.WriteAsync(NativeObjectReference, buffer);
    }

    /// <inheritdoc/>
    public IAsyncOperation<bool> FlushAsync()
    {
        return ABI.Windows.Storage.Streams.IOutputStreamMethods.FlushAsync(NativeObjectReference);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        ABI.System.IDisposableMethods.Dispose(IClosableObjectReference);
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IOutputStream>.GetInterface()
    {
        return NativeObjectReference.AsValue();
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IDisposable>.GetInterface()
    {
        return IClosableObjectReference.AsValue();
    }

    /// <inheritdoc/>
    protected override bool IsOverridableInterface(in Guid iid)
    {
        return false;
    }
}
#endif
