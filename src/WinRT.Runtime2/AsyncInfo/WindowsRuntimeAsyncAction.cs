// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using Windows.Foundation;
using WindowsRuntime.InteropServices;

namespace WindowsRuntime;

/// <summary>
/// The implementation of a native object for <see cref="IAsyncAction"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/uwp/api/windows.foundation.iasyncaction"/>
internal sealed class WindowsRuntimeAsyncAction : WindowsRuntimeObject,
    IAsyncAction,
    IWindowsRuntimeInterface<IAsyncAction>,
    IWindowsRuntimeInterface<IAsyncInfo>
{
    /// <summary>
    /// Creates a <see cref="WindowsRuntimeAsyncAction"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    public WindowsRuntimeAsyncAction(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference)
    {
    }

    /// <summary>
    /// Gets the lazy-loaded, cached object reference for <see cref="IAsyncInfo"/> for the current object.
    /// </summary>
    private WindowsRuntimeObjectReference IAsyncInfoObjectReference
    {
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            WindowsRuntimeObjectReference InitializeIAsyncInfoObjectReference()
            {
                _ = Interlocked.CompareExchange(
                    location1: ref field,
                    value: NativeObjectReference.As(in WellKnownWindowsInterfaceIIDs.IID_IAsyncInfo),
                    comparand: null);

                return field;
            }

            return field ?? InitializeIAsyncInfoObjectReference();
        }
    }

    /// <inheritdoc/>
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
        DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
        UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected internal override bool HasUnwrappableNativeObjectReference => true;

    public AsyncActionCompletedHandler? Completed
    {
        get => ABI.Windows.Foundation.IAsyncActionMethods.Completed(NativeObjectReference);
        set => ABI.Windows.Foundation.IAsyncActionMethods.Completed(NativeObjectReference, value);
    }

    /// <inheritdoc/>
    public uint Id => ABI.Windows.Foundation.IAsyncInfoMethods.Id(IAsyncInfoObjectReference);

    /// <inheritdoc/>
    public AsyncStatus Status => ABI.Windows.Foundation.IAsyncInfoMethods.Status(IAsyncInfoObjectReference);

    /// <inheritdoc/>
    public Exception? ErrorCode => ABI.Windows.Foundation.IAsyncInfoMethods.ErrorCode(IAsyncInfoObjectReference);

    /// <inheritdoc/>
    public void GetResults()
    {
        ABI.Windows.Foundation.IAsyncActionMethods.GetResults(NativeObjectReference);
    }

    /// <inheritdoc/>
    public void Cancel()
    {
        ABI.Windows.Foundation.IAsyncInfoMethods.Cancel(IAsyncInfoObjectReference);
    }

    /// <inheritdoc/>
    public void Close()
    {
        ABI.Windows.Foundation.IAsyncInfoMethods.Close(IAsyncInfoObjectReference);
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IAsyncAction>.GetInterface()
    {
        return NativeObjectReference.AsValue();
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IAsyncInfo>.GetInterface()
    {
        return IAsyncInfoObjectReference.AsValue();
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