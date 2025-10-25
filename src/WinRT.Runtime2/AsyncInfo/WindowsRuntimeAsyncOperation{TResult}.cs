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
/// The implementation of a native object for <see cref="IAsyncOperation{TResult}"/>.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
/// <typeparam name="TIAsyncOperationMethods">The <see cref="IAsyncOperationMethodsImpl{TResult}"/> implementation type.</typeparam>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.iasyncoperation-1"/>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class WindowsRuntimeAsyncOperation<TResult, TIAsyncOperationMethods> : WindowsRuntimeObject,
    IAsyncOperation<TResult>,
    IWindowsRuntimeInterface<IAsyncOperation<TResult>>,
    IWindowsRuntimeInterface<IAsyncInfo>
    where TIAsyncOperationMethods : IAsyncOperationMethodsImpl<TResult>
{
    /// <summary>
    /// Creates a <see cref="WindowsRuntimeAsyncAction"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    protected WindowsRuntimeAsyncOperation(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference)
    {
    }

    /// <inheritdoc cref="WindowsRuntimeAsyncAction.IAsyncInfoObjectReference"/>
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

    /// <inheritdoc/>
    public AsyncOperationCompletedHandler<TResult>? Completed
    {
        get => TIAsyncOperationMethods.Completed(NativeObjectReference);
        set => TIAsyncOperationMethods.Completed(NativeObjectReference, value);
    }

    /// <inheritdoc/>
    public uint Id => IAsyncInfoMethods.Id(IAsyncInfoObjectReference);

    /// <inheritdoc/>
    public AsyncStatus Status => IAsyncInfoMethods.Status(IAsyncInfoObjectReference);

    /// <inheritdoc/>
    public Exception? ErrorCode => IAsyncInfoMethods.ErrorCode(IAsyncInfoObjectReference);

    /// <inheritdoc/>
    public TResult GetResults()
    {
        return TIAsyncOperationMethods.GetResults(NativeObjectReference);
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
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IAsyncOperation<TResult>>.GetInterface()
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
