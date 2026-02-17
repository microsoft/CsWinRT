// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using Windows.Foundation;
using Windows.Storage.Streams;
using WindowsRuntime.InteropServices;

namespace WindowsRuntime;

/// <summary>
/// The implementation of the projected Windows Runtime <see cref="IRandomAccessStream"/> type.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.storage.streams.irandomaccessstream"/>
[WindowsRuntimeManagedOnlyType]
internal sealed class WindowsRuntimeRandomAccessStream : WindowsRuntimeObject,
    IRandomAccessStream,
    IWindowsRuntimeInterface<IRandomAccessStream>,
    IWindowsRuntimeInterface<IInputStream>,
    IWindowsRuntimeInterface<IOutputStream>,
    IWindowsRuntimeInterface<IDisposable>
{
    /// <summary>
    /// Creates a <see cref="WindowsRuntimeRandomAccessStream"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="WindowsRuntimeRandomAccessStream">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    public WindowsRuntimeRandomAccessStream(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference)
    {
    }

    /// <summary>
    /// Gets the lazy-loaded, cached object reference for <see cref="IInputStream"/> for the current object.
    /// </summary>
    private WindowsRuntimeObjectReference IInputStreamObjectReference
    {
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            WindowsRuntimeObjectReference InitializeIInputStreamObjectReference()
            {
                _ = Interlocked.CompareExchange(
                    location1: ref field,
                    value: NativeObjectReference.As(in WellKnownWindowsInterfaceIIDs.IID_IInputStream),
                    comparand: null);

                return field;
            }

            return field ?? InitializeIInputStreamObjectReference();
        }
    }

    /// <summary>
    /// Gets the lazy-loaded, cached object reference for <see cref="IOutputStream"/> for the current object.
    /// </summary>
    private WindowsRuntimeObjectReference IOutputStreamObjectReference
    {
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            WindowsRuntimeObjectReference InitializeIOutputStreamObjectReference()
            {
                _ = Interlocked.CompareExchange(
                    location1: ref field,
                    value: NativeObjectReference.As(in WellKnownWindowsInterfaceIIDs.IID_IOutputStream),
                    comparand: null);

                return field;
            }

            return field ?? InitializeIOutputStreamObjectReference();
        }
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
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
        DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
        UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected internal override bool HasUnwrappableNativeObjectReference => true;

    /// <inheritdoc/>
    public bool CanRead => ABI.Windows.Storage.Streams.IRandomAccessStreamMethods.CanRead(NativeObjectReference);

    /// <inheritdoc/>
    public bool CanWrite => ABI.Windows.Storage.Streams.IRandomAccessStreamMethods.CanWrite(NativeObjectReference);

    /// <inheritdoc/>
    public ulong Position => ABI.Windows.Storage.Streams.IRandomAccessStreamMethods.Position(NativeObjectReference);

    /// <inheritdoc/>
    public ulong Size
    {
        get => ABI.Windows.Storage.Streams.IRandomAccessStreamMethods.Size(NativeObjectReference);
        set => ABI.Windows.Storage.Streams.IRandomAccessStreamMethods.Size(NativeObjectReference, value);
    }

    /// <inheritdoc/>
    public IInputStream GetInputStreamAt(ulong position)
    {
        return ABI.Windows.Storage.Streams.IRandomAccessStreamMethods.GetInputStreamAt(NativeObjectReference, position);
    }

    /// <inheritdoc/>
    public IOutputStream GetOutputStreamAt(ulong position)
    {
        return ABI.Windows.Storage.Streams.IRandomAccessStreamMethods.GetOutputStreamAt(NativeObjectReference, position);
    }

    /// <inheritdoc/>
    public void Seek(ulong position)
    {
        ABI.Windows.Storage.Streams.IRandomAccessStreamMethods.Seek(NativeObjectReference, position);
    }

    /// <inheritdoc/>
    public IRandomAccessStream CloneStream()
    {
        return ABI.Windows.Storage.Streams.IRandomAccessStreamMethods.CloneStream(NativeObjectReference);
    }

    /// <inheritdoc/>
    public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
    {
        return ABI.Windows.Storage.Streams.IInputStreamMethods.ReadAsync(NativeObjectReference, buffer, count, options);
    }

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
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IRandomAccessStream>.GetInterface()
    {
        return NativeObjectReference.AsValue();
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IInputStream>.GetInterface()
    {
        return IInputStreamObjectReference.AsValue();
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IOutputStream>.GetInterface()
    {
        return IOutputStreamObjectReference.AsValue();
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IDisposable>.GetInterface()
    {
        return IClosableObjectReference.AsValue();
    }

    /// <inheritdoc/>
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
        DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
        UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected override bool IsOverridableInterface(in Guid iid)
    {
        return false;
    }
}
