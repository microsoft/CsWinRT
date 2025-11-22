// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System;

using System.Runtime.Versioning;
using global::System.Diagnostics;
using global::System.Runtime.CompilerServices;
using global::System.Runtime.InteropServices;
using global::System.Threading;
using global::System.Threading.Tasks;
using global::Windows.Foundation;
using WindowsRuntime.InteropServices;

#if NET
[global::System.Runtime.Versioning.SupportedOSPlatform("windows10.0.10240.0")]
#endif
#if EMBED
internal
#else 
public
#endif
static class WindowsRuntimeSystemExtensions
{
    public static Task AsTask(this IAsyncAction source)
    {
        return AsTask(source, CancellationToken.None);
    }

    public static Task AsTask(this IAsyncAction source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source is AsyncInfoAdapter { Task: Task task })
        {
            return cancellationToken.CanBeCanceled ?
                task.WaitAsync(cancellationToken) :
                task;
        }

        switch (source.Status)
        {
            case AsyncStatus.Completed:
                return Task.CompletedTask;
            case AsyncStatus.Error:
                return Task.FromException(source.ErrorCode!);
            case AsyncStatus.Canceled:
                return Task.FromCanceled(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
        }

        AsyncInfoToTaskBridge<VoidValueTypeParameter> bridge = new(source, cancellationToken);

        source.Completed = bridge.Complete;

        return bridge.Task;
    }

    public static TaskAwaiter GetAwaiter(this IAsyncAction source)
    {
        return AsTask(source).GetAwaiter();
    }

    public static void Wait(this IAsyncAction source)
    {
        AsTask(source).Wait();
    }

    public static Task<TResult> AsTask<TResult>(this IAsyncOperation<TResult> source)
    {
        return AsTask(source, CancellationToken.None);
    }

    public static Task<TResult> AsTask<TResult>(this IAsyncOperation<TResult> source, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (source is AsyncInfoAdapter { Task: Task<TResult> task })
        {
            return cancellationToken.CanBeCanceled ?
                task.WaitAsync(cancellationToken) :
                task;
        }

        switch (source.Status)
        {
            case AsyncStatus.Completed:
                return Task.FromResult(source.GetResults());
            case AsyncStatus.Error:
                return Task.FromException<TResult>(source.ErrorCode!);
            case AsyncStatus.Canceled:
                return Task.FromCanceled<TResult>(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
        }

        AsyncInfoToTaskBridge<TResult, VoidValueTypeParameter> bridge = new(source, cancellationToken);

        source.Completed = bridge.Complete;

        return bridge.Task;
    }

    public static TaskAwaiter<TResult> GetAwaiter<TResult>(this IAsyncOperation<TResult> source)
    {
        return AsTask(source).GetAwaiter();
    }

    public static void Wait<TResult>(this IAsyncOperation<TResult> source)
    {
        AsTask(source).Wait();
    }

    public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source, CancellationToken cancellationToken, IProgress<TProgress> progress)
    {
        ArgumentNullException.ThrowIfNull(source);

        // fast path is underlying asyncInfo is Task and no IProgress provided
        if (source is AsyncInfoAdapter { Task: Task task } && progress is null)
        {
            return cancellationToken.CanBeCanceled ?
                task.WaitAsync(cancellationToken) :
                task;
        }

        switch (source.Status)
        {
            case AsyncStatus.Completed:
                return Task.CompletedTask;

            case AsyncStatus.Error:
                return Task.FromException(source.ErrorCode);

            case AsyncStatus.Canceled:
                return Task.FromCanceled(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
        }

        if (progress != null)
        {
            SetProgress(source, progress);
        }

        var bridge = new AsyncInfoToTaskBridge<TProgress>(source, cancellationToken);
        source.Completed = bridge.Complete;
        return bridge.Task;
    }

    private static void SetProgress<TProgress>(IAsyncActionWithProgress<TProgress> source, IProgress<TProgress> sink)
    {
        // This is separated out into a separate method so that we only pay the costs of compiler-generated closure if progress is non-null.
        source.Progress = new AsyncActionProgressHandler<TProgress>((_, info) => sink.Report(info));
    }

    public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source)
    {
        return AsTask(source, CancellationToken.None, null);
    }

    public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source, CancellationToken cancellationToken)
    {
        return AsTask(source, cancellationToken, null);
    }

    public static Task AsTask<TProgress>(this IAsyncActionWithProgress<TProgress> source, IProgress<TProgress> progress)
    {
        return AsTask(source, CancellationToken.None, progress);
    }

    public static TaskAwaiter GetAwaiter<TProgress>(this IAsyncActionWithProgress<TProgress> source)
    {
        return AsTask(source).GetAwaiter();
    }

    public static void Wait<TProgress>(this IAsyncActionWithProgress<TProgress> source)
    {
        AsTask(source).Wait();
    }

    public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source, CancellationToken cancellationToken, IProgress<TProgress> progress)
    {
        ArgumentNullException.ThrowIfNull(source);

        // fast path is underlying asyncInfo is Task and no IProgress provided
        if (source is AsyncInfoAdapter { Task: Task<TResult> task } && progress is null)
        {
            return cancellationToken.CanBeCanceled ?
                task.WaitAsync(cancellationToken) :
                task;
        }

        switch (source.Status)
        {
            case AsyncStatus.Completed:
                return Task.FromResult(source.GetResults());

            case AsyncStatus.Error:
                return Task.FromException<TResult>(source.ErrorCode);

            case AsyncStatus.Canceled:
                return Task.FromCanceled<TResult>(cancellationToken.IsCancellationRequested ? cancellationToken : new CancellationToken(true));
        }

        if (progress != null)
        {
            SetProgress(source, progress);
        }

        var bridge = new AsyncInfoToTaskBridge<TResult, TProgress>(source, cancellationToken);
        source.Completed = bridge.Complete;
        return bridge.Task;
    }

    private static void SetProgress<TResult, TProgress>(IAsyncOperationWithProgress<TResult, TProgress> source, IProgress<TProgress> sink)
    {
        // This is separated out into a separate method so that we only pay the costs of compiler-generated closure if progress is non-null.
        source.Progress = new AsyncOperationProgressHandler<TResult, TProgress>((_, info) => sink.Report(info));
    }

    public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source)
    {
        return AsTask(source, CancellationToken.None, null);
    }

    public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source, CancellationToken cancellationToken)
    {
        return AsTask(source, cancellationToken, null);
    }

    public static Task<TResult> AsTask<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source, IProgress<TProgress> progress)
    {
        return AsTask(source, CancellationToken.None, progress);
    }

    public static TaskAwaiter<TResult> GetAwaiter<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source)
    {
        return AsTask(source).GetAwaiter();
    }

    public static void Wait<TResult, TProgress>(this IAsyncOperationWithProgress<TResult, TProgress> source)
    {
        AsTask(source).Wait();
    }

    public static IAsyncAction AsAsyncAction(this Task source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new AsyncActionAdapter(source, cancellationTokenSource: null);
    }

    public static IAsyncOperation<TResult> AsAsyncOperation<TResult>(this Task<TResult> source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new AsyncOperationAdapter<TResult>(source, cancellationTokenSource: null);
    }
}

// Marker type since generic parameters cannot be 'void'
struct VoidValueTypeParameter { }

/// <summary>This can be used instead of <code>VoidValueTypeParameter</code> when a reference type is required.
/// In case of an actual instantiation (e.g. through <code>default(T)</code>),
/// using <code>VoidValueTypeParameter</code> offers better performance.</summary>
internal class VoidReferenceTypeParameter { }