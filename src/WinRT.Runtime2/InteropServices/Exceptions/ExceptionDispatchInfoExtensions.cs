// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;

#pragma warning disable IDE0055

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Extensions for <see cref="ExceptionDispatchInfo"/>.
/// </summary>
internal static class ExceptionDispatchInfoExtensions
{
    extension(ExceptionDispatchInfo)
    {
        /// <summary>
        /// Throws the source exception as an async exception, maintaining the original Watson
        /// information and augmenting rather than replacing the original stack trace.
        /// </summary>
        /// <param name="exception">The exception whose state is captured, then rethrown.</param>
        /// <param name="synchronizationContext">The target <see cref="SynchronizationContext"/>, if available.</param>
        /// <remarks>
        /// This method will throw <paramref name="exception"/> on <paramref name="synchronizationContext"/>, or on a thread pool thread.
        /// </remarks>
        public static void ThrowAsync(Exception exception, SynchronizationContext? synchronizationContext)
        {
            // If this is an error indicating the RPC called failed, it is most likely due
            // to the other process is gone. In this case, we just ignore it, as it just
            // means we weren't able to report the completion, and not actually an error
            // in the other process.
            if (exception is COMException { HResult:
                WellKnownErrorCodes.RPC_E_DISCONNECTED or
                WellKnownErrorCodes.RPC_S_SERVER_UNAVAILABLE or
                WellKnownErrorCodes.JSCRIPT_E_CANTEXECUTE })
            {
                return;
            }

            // Capture the exception dispatch info, so we preserve the stacktrace we originally had.
            // Otherwise, we'd reset that from the place where the exception is actually thrown from.
            ExceptionDispatchInfo exceptionDispatchInfo = ExceptionDispatchInfo.Capture(exception);

            // If we have a synchronization context, try posting to it first
            if (synchronizationContext is not null)
            {
                try
                {
                    synchronizationContext.Post(static e => Unsafe.As<ExceptionDispatchInfo>(e!).Throw(), exceptionDispatchInfo);
                }
                catch
                {
                    // Something went wrong in the 'Post' call, let's try using the thread pool instead
                }
            }

            bool scheduled = true;

            // Throw the exception from a thread pool thread. This call should never fail, but just in case.
            try
            {
                _ = ThreadPool.UnsafeQueueUserWorkItem(static e => Unsafe.As<ExceptionDispatchInfo>(e!).Throw(), exceptionDispatchInfo);
            }
            catch
            {
                // Something went wrong when scheduling the callback
                scheduled = false;
            }

            // As a last resort, just throw the exception from here
            if (!scheduled)
            {
                exceptionDispatchInfo.Throw();
            }
        }
    }
}