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
/// The implementation of a native object for <see cref="IAsyncActionWithProgress{TProgress}"/>.
/// </summary>
/// <typeparam name="TProgress">The type of progress information.</typeparam>
/// <typeparam name="TIAsyncActionWithProgressMethods">The <see cref="IAsyncActionWithProgress{TProgress}"/> implementation type.</typeparam>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.iasyncactionwithprogress-1"/>
public abstract class WindowsRuntimeAsyncActionWithProgress<TProgress, TIAsyncActionWithProgressMethods> : WindowsRuntimeObject,
    IAsyncActionWithProgress<TProgress>,
    IWindowsRuntimeInterface<IAsyncActionWithProgress<TProgress>>
    where TIAsyncActionWithProgressMethods : IAsyncActionWithProgressMethodsImpl<TProgress>
{
    /// <summary>
    /// Creates a <see cref="WindowsRuntimeAsyncAction"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    protected WindowsRuntimeAsyncActionWithProgress(WindowsRuntimeObjectReference nativeObjectReference)
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
    public AsyncActionProgressHandler<TProgress>? Progress
    {
        get => TIAsyncActionWithProgressMethods.Progress(NativeObjectReference);
        set => TIAsyncActionWithProgressMethods.Progress(NativeObjectReference, value);
    }

    /// <inheritdoc/>
    public AsyncActionWithProgressCompletedHandler<TProgress>? Completed
    {
        get => TIAsyncActionWithProgressMethods.Completed(NativeObjectReference);
        set => TIAsyncActionWithProgressMethods.Completed(NativeObjectReference, value);
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
        IAsyncActionWithProgressMethods.GetResults(NativeObjectReference);
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
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IAsyncActionWithProgress<TProgress>>.GetInterface()
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
