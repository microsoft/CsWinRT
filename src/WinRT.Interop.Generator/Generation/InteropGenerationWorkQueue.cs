// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// Runs a dependency graph of coarse operations with bounded, non-buffering workers.
/// </summary>
/// <typeparam name="TKey">The work item identifier type.</typeparam>
/// <typeparam name="TContext">The shared context passed to each operation.</typeparam>
/// <param name="items">The operations and their prerequisites, in preferred scheduling order.</param>
internal sealed class InteropGenerationWorkQueue<TKey, TContext>(params InteropGenerationWorkQueue<TKey, TContext>.WorkItem[] items)
    where TKey : notnull
{
    /// <summary>
    /// Runs all operations, returning only after every worker has finished.
    /// </summary>
    /// <param name="context">The context to pass to each operation in this run.</param>
    /// <param name="maxDegreesOfParallelism">The worker limit, or -1 for the processor count.</param>
    /// <param name="token">The invocation's cancellation token.</param>
    public void Run(TContext context, int maxDegreesOfParallelism, CancellationToken token)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDegreesOfParallelism, -1);
        ArgumentOutOfRangeException.ThrowIfZero(maxDegreesOfParallelism);
        token.ThrowIfCancellationRequested();

        if (items.Length == 0)
        {
            return;
        }

        Node[] nodes = CreateNodes();
        int workerCount = Math.Min(
            maxDegreesOfParallelism == -1 ? Environment.ProcessorCount : maxDegreesOfParallelism,
            nodes.Length);

        using BlockingCollection<Node> ready = new();

        foreach (Node node in nodes)
        {
            if (node.RemainingPrerequisites == 0)
            {
                ready.Add(node);
            }
        }

        // A single worker needs neither the parallel loop nor linked cancellation.
        if (workerCount == 1)
        {
            while (ready.TryTake(out Node? node))
            {
                token.ThrowIfCancellationRequested();
                node.Body(context);

                foreach (Node dependent in node.Dependents)
                {
                    if (--dependent.RemainingPrerequisites == 0)
                    {
                        ready.Add(dependent);
                    }
                }
            }

            token.ThrowIfCancellationRequested();

            return;
        }

        using CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(token);
        ExceptionDispatchInfo? failure = null;
        int remaining = nodes.Length;

        try
        {
            _ = Parallel.ForEach(
                source: Partitioner.Create(ready.GetConsumingEnumerable(cancellation.Token), EnumerablePartitionerOptions.NoBuffering),
                parallelOptions: new ParallelOptions { CancellationToken = cancellation.Token, MaxDegreeOfParallelism = workerCount },
                body: node =>
                {
                    try
                    {
                        cancellation.Token.ThrowIfCancellationRequested();
                        node.Body(context);

                        foreach (Node dependent in node.Dependents)
                        {
                            if (Interlocked.Decrement(ref dependent.RemainingPrerequisites) == 0)
                            {
                                ready.Add(dependent, cancellation.Token);
                            }
                        }

                        if (Interlocked.Decrement(ref remaining) == 0)
                        {
                            ready.CompleteAdding();
                        }
                    }
                    catch (Exception exception)
                    {
                        // Publish the initiating failure before cancellation wakes the blocked consumers.
                        _ = Interlocked.CompareExchange(ref failure, ExceptionDispatchInfo.Capture(exception), null);
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
    }

    /// <summary>
    /// Creates per-run dependency counters and rejects incomplete or cyclic graphs before starting workers.
    /// </summary>
    /// <returns>The validated execution nodes.</returns>
    private Node[] CreateNodes()
    {
        Dictionary<TKey, Node> lookup = new(items.Length);
        Node[] nodes = new Node[items.Length];

        for (int i = 0; i < items.Length; i++)
        {
            WorkItem item = items[i];
            Node node = new(item.Body, item.Prerequisites.Length);

            lookup.Add(item.Key, node);
            nodes[i] = node;
        }

        for (int i = 0; i < items.Length; i++)
        {
            foreach (TKey prerequisite in items[i].Prerequisites)
            {
                lookup[prerequisite].Dependents.Add(nodes[i]);
            }
        }

        Queue<Node> ready = new(nodes.Where(static node => node.RemainingPrerequisites == 0));
        int visited = 0;

        while (ready.TryDequeue(out Node? node))
        {
            visited++;

            foreach (Node dependent in node.Dependents)
            {
                if (--dependent.RemainingPrerequisites == 0)
                {
                    ready.Enqueue(dependent);
                }
            }
        }

        if (visited != nodes.Length)
        {
            throw new InvalidOperationException("The interop generation work graph contains a dependency cycle.");
        }

        for (int i = 0; i < items.Length; i++)
        {
            nodes[i].RemainingPrerequisites = items[i].Prerequisites.Length;
        }

        return nodes;
    }

    /// <summary>
    /// An operation and the named operations that must finish before it starts.
    /// </summary>
    /// <param name="Key">The operation's unique identifier.</param>
    /// <param name="Body">The operation to run.</param>
    /// <param name="Prerequisites">The prerequisite operation identifiers.</param>
    public sealed record WorkItem(TKey Key, Action<TContext> Body, params TKey[] Prerequisites);

    /// <summary>
    /// The dependency state owned by one invocation of <see cref="Run"/>.
    /// </summary>
    /// <param name="body">The operation to run.</param>
    /// <param name="remainingPrerequisites">The initial prerequisite count.</param>
    private sealed class Node(Action<TContext> body, int remainingPrerequisites)
    {
        /// <summary>Gets the operation to run.</summary>
        public Action<TContext> Body { get; } = body;

        /// <summary>Gets the operations released by this node.</summary>
        public List<Node> Dependents { get; } = [];

        /// <summary>The remaining prerequisite count, decremented atomically by parallel workers.</summary>
        public int RemainingPrerequisites = remainingPrerequisites;
    }
}
