// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using Windows.Foundation;
using Windows.Storage.Streams;
using WindowsRuntime.InteropServices;

namespace WindowsRuntime;

/// <summary>
/// The implementation of the projected Windows Runtime <see cref="IInputStream"/> type.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.storage.streams.iinputstream"/>
[WindowsRuntimeManagedOnlyType]
internal sealed class WindowsRuntimeInputStream : WindowsRuntimeObject,
    IInputStream,
    IWindowsRuntimeInterface<IInputStream>,
    IWindowsRuntimeInterface<IDisposable>
{
    /// <summary>
    /// Creates a <see cref="WindowsRuntimeInputStream"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    public WindowsRuntimeInputStream(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference)
    {
    }

    /// <summary>
    /// Gets the lazy-loaded, cached object reference for <see cref="IDisposable"/> for the current object.
    /// </summary>
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
    public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
    {
        return ABI.Windows.Storage.Streams.IInputStreamMethods.ReadAsync(NativeObjectReference, buffer, count, options);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        ABI.System.IDisposableMethods.Dispose(IClosableObjectReference);
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IInputStream>.GetInterface()
    {
        return NativeObjectReference.AsValue();
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
#endif