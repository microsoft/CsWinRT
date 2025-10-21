// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using ABI.Windows.Foundation;
using Windows.Foundation;
using WindowsRuntime.InteropServices;

namespace WindowsRuntime.AsyncInfo;

/// <summary>
/// The implementation of a native object for <see cref="IAsyncAction"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/uwp/api/windows.foundation.iasyncaction"/>
internal sealed class WindowsRuntimeAsyncAction : WindowsRuntimeObject, IAsyncAction, IWindowsRuntimeInterface<IAsyncAction>
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
        get => IAsyncActionMethods.Completed(NativeObjectReference);
        set => IAsyncActionMethods.Completed(NativeObjectReference, value);
    }

    /// <inheritdoc/>
    public uint Id => IAsyncInfoMethods.Id(IAsyncInfoObjectReference);

    /// <inheritdoc/>
    public AsyncStatus Status => IAsyncInfoMethods.Status(IAsyncInfoObjectReference);

    /// <inheritdoc/>
    public Exception? ErrorCode => IAsyncInfoMethods.ErrorCode(IAsyncInfoObjectReference);

    /// <inheritdoc/>
    public void GetResults()
    {
        IAsyncActionMethods.GetResults(NativeObjectReference);
    }

    /// <inheritdoc/>
    public void Cancel()
    {
        IAsyncInfoMethods.Cancel(IAsyncInfoObjectReference);
    }

    /// <inheritdoc/>
    public void Close()
    {
        IAsyncInfoMethods.Close(IAsyncInfoObjectReference);
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IAsyncAction>.GetInterface()
    {
        return NativeObjectReference.AsValue();
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
