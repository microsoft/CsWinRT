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
/// The implementation of the projected Windows Runtime <see cref="IMemoryBufferReference"/> type.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.imemorybufferreference"/>
[WindowsRuntimeManagedOnlyType]
internal sealed class WindowsRuntimeMemoryBufferReference : WindowsRuntimeObject,
    IMemoryBufferReference,
    IWindowsRuntimeInterface<IMemoryBufferReference>,
    IWindowsRuntimeInterface<IDisposable>
{
    /// <inheritdoc/>
    public event EventHandler<IMemoryBufferReference, object> Closed
    {
        add => ABI.Windows.Foundation.IMemoryBufferReferenceMethods.Closed(this, NativeObjectReference).Subscribe(value);
        remove => ABI.Windows.Foundation.IMemoryBufferReferenceMethods.Closed(this, NativeObjectReference).Unsubscribe(value);
    }

    /// <summary>
    /// Creates a <see cref="WindowsRuntimeMemoryBufferReference"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    public WindowsRuntimeMemoryBufferReference(WindowsRuntimeObjectReference nativeObjectReference)
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
    public uint Capacity => ABI.Windows.Foundation.IMemoryBufferReferenceMethods.Capacity(NativeObjectReference);

    /// <inheritdoc/>
    public void Dispose()
    {
        ABI.System.IDisposableMethods.Dispose(IClosableObjectReference);
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IMemoryBufferReference>.GetInterface()
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
