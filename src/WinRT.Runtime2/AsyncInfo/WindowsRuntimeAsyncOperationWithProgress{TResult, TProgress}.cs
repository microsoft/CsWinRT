// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using ABI.Windows.Foundation;
using Windows.Foundation;
using WindowsRuntime.InteropServices;

namespace WindowsRuntime.AsyncInfo;

/// <summary>
/// The implementation of a native object for <see cref="IAsyncOperationWithProgress{TResult, TProgress}"/>.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
/// <typeparam name="TProgress">The type of progress information.</typeparam>
/// <typeparam name="TIAsyncOperationWithProgressMethods">The <see cref="IAsyncOperationWithProgressMethodsImpl{TResult, TProgress}"/> implementation type.</typeparam>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.iasyncactionwithprogress-1"/>
public abstract class WindowsRuntimeAsyncOperationWithProgress<TResult, TProgress, TIAsyncOperationWithProgressMethods> : WindowsRuntimeObject,
    IAsyncOperationWithProgress<TResult, TProgress>,
    IWindowsRuntimeInterface<IAsyncOperationWithProgress<TResult, TProgress>>
    where TIAsyncOperationWithProgressMethods : IAsyncOperationWithProgressMethodsImpl<TResult, TProgress>
{
    /// <summary>
    /// Creates a <see cref="WindowsRuntimeAsyncAction"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    protected WindowsRuntimeAsyncOperationWithProgress(WindowsRuntimeObjectReference nativeObjectReference)
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
                    value: NativeObjectReference.As(in WellKnownInterfaceIds.IID_IAsyncInfo),
                    comparand: null);

                return field;
            }

            return field ?? InitializeIAsyncInfoObjectReference();
        }
    }

    /// <inheritdoc/>
    protected internal override bool HasUnwrappableNativeObjectReference => true;

    /// <inheritdoc/>
    public AsyncOperationProgressHandler<TResult, TProgress>? Progress
    {
        get => TIAsyncOperationWithProgressMethods.Progress(NativeObjectReference);
        set => TIAsyncOperationWithProgressMethods.Progress(NativeObjectReference, value);
    }

    /// <inheritdoc/>
    public AsyncOperationWithProgressCompletedHandler<TResult, TProgress>? Completed
    {
        get => TIAsyncOperationWithProgressMethods.Completed(NativeObjectReference);
        set => TIAsyncOperationWithProgressMethods.Completed(NativeObjectReference, value);
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
        return TIAsyncOperationWithProgressMethods.GetResults(NativeObjectReference);
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
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IAsyncOperationWithProgress<TResult, TProgress>>.GetInterface()
    {
        return NativeObjectReference.AsValue();
    }

    /// <inheritdoc/>
    protected override bool IsOverridableInterface(in Guid iid)
    {
        return false;
    }
}
