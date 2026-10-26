// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for bounded, non-buffering <see cref="Parallel"/> loops.
/// </summary>
internal static class ParallelExtensions
{
    extension(Parallel)
    {
        /// <summary>
        /// Processes one source item at a time per worker, with standard parallel exception and cancellation behavior.
        /// </summary>
        /// <typeparam name="T">The source item type.</typeparam>
        /// <param name="source">The items to process.</param>
        /// <param name="token">The invocation's cancellation token.</param>
        /// <param name="maxDegreesOfParallelism">The worker limit, or -1 for the default parallel scheduler limit.</param>
        /// <param name="body">The operation to run for each item.</param>
        /// <returns>The loop's completion status, after all workers have finished.</returns>
        /// <remarks>Body failures remain aggregated; cancellation uses the supplied token.</remarks>
        public static ParallelLoopResult ForEachUnbuffered<T>(
            IEnumerable<T> source,
            CancellationToken token,
            int maxDegreesOfParallelism,
            Action<T> body)
        {
            return Parallel.ForEach(
                source: Partitioner.Create(source, EnumerablePartitionerOptions.NoBuffering),
                parallelOptions: new ParallelOptions { CancellationToken = token, MaxDegreeOfParallelism = maxDegreesOfParallelism },
                body: body);
        }

        /// <summary>
        /// Processes a potentially blocking source, cancelling consumers on a body failure and rethrowing its first exception.
        /// </summary>
        /// <typeparam name="T">The source item type.</typeparam>
        /// <param name="source">Creates the source with the token used to unblock consumers after a failure.</param>
        /// <param name="token">The invocation's cancellation token.</param>
        /// <param name="maxDegreesOfParallelism">The worker limit, which must be bounded for a blocking source.</param>
        /// <param name="body">The operation to run for each item, with the token observing sibling failures.</param>
        /// <returns>The loop's completion status, after all workers have finished.</returns>
        /// <remarks>
        /// Source waits must observe the supplied linked token. All workers finish before the method returns or throws.
        /// Invocation cancellation takes precedence over a body failure and preserves the invocation's original token.
        /// Failures outside a body retain the underlying parallel loop's exception behavior.
        /// </remarks>
        public static ParallelLoopResult ForEachUnbufferedUntilFirstFailure<T>(
            Func<CancellationToken, IEnumerable<T>> source,
            CancellationToken token,
            int maxDegreesOfParallelism,
            Action<T, CancellationToken> body)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(maxDegreesOfParallelism, 1);

            using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);

            // Preserve the worker's original stack trace when rethrowing on the caller thread, rather than resetting it with 'throw exception'
            ExceptionDispatchInfo? failure = null;
            ParallelLoopResult result;

            try
            {
                result = Parallel.ForEachUnbuffered(
                    source: source(cancellation.Token),
                    token: cancellation.Token,
                    maxDegreesOfParallelism: maxDegreesOfParallelism,
                    body: item =>
                    {
                        try
                        {
                            body(item, cancellation.Token);
                        }
                        catch (Exception exception)
                        {
                            // Preserve the initiating failure before cancellation wakes blocked source consumers
                            _ = Interlocked.CompareExchange(ref failure, ExceptionDispatchInfo.Capture(exception), null);

                            // Rethrowing alone cannot wake workers blocked on the source; cancel so they can exit and be joined
                            cancellation.Cancel();

                            throw;
                        }
                    });
            }
            catch (AggregateException)
            {
                token.ThrowIfCancellationRequested();
                failure?.Throw();

                throw;
            }
            catch (OperationCanceledException)
            {
                token.ThrowIfCancellationRequested();
                failure?.Throw();

                throw;
            }

            token.ThrowIfCancellationRequested();

            return result;
        }
    }
}
