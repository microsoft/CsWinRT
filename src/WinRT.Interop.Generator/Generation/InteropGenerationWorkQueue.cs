// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// Runs a dependency graph of coarse operations once, with bounded, non-buffering workers.
/// </summary>
/// <typeparam name="TContext">The shared context passed to each operation.</typeparam>
internal sealed class InteropGenerationWorkQueue<TContext>
{
    /// <summary>The initial operations, in declaration order.</summary>
    private readonly Node[] _roots;

    /// <summary>The total number of operations, including those waiting for prerequisites.</summary>
    private readonly int _count;

    /// <summary>Whether execution has already been claimed by a caller.</summary>
    private int _started;

    /// <summary>
    /// Captures a completed graph without retaining its builder.
    /// </summary>
    /// <param name="nodes">The operations in declaration order.</param>
    private InteropGenerationWorkQueue(List<Node> nodes)
    {
        _roots = [.. nodes.Where(static node => node.RemainingPrerequisites == 0)];
        _count = nodes.Count;
    }

    /// <summary>
    /// Runs all operations once, returning only after every worker has finished.
    /// </summary>
    /// <param name="context">The context to pass to each operation.</param>
    /// <param name="maxDegreesOfParallelism">The positive worker limit, with the default already resolved.</param>
    /// <param name="token">The invocation's cancellation token.</param>
    /// <remarks>
    /// Once the arguments are validated, the queue is consumed even if empty, canceled, or faulted.
    /// Subsequent calls are rejected before checking cancellation or starting any operations.
    /// </remarks>
    public void Run(TContext context, int maxDegreesOfParallelism, CancellationToken token)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxDegreesOfParallelism, 1);

        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            throw new InvalidOperationException("The interop generation work queue can only run once.");
        }

        token.ThrowIfCancellationRequested();

        if (_count == 0)
        {
            return;
        }

        int workerCount = Math.Min(maxDegreesOfParallelism, _count);

        using BlockingCollection<Node> ready = new(new ConcurrentQueue<Node>(_roots));

        int remaining = _count;

        _ = Parallel.ForEachUnbufferedUntilFirstFailure(
            source: ready.GetConsumingEnumerable,
            token: token,
            maxDegreesOfParallelism: workerCount,
            body: (node, workerToken) => RunNode(node, context, ready, workerToken, ref remaining));
    }

    /// <summary>
    /// Runs one parallel operation, releases its dependents, and closes the queue when all work is done.
    /// </summary>
    /// <param name="node">The operation to run.</param>
    /// <param name="context">The context to pass to the operation.</param>
    /// <param name="ready">The shared queue of operations ready to run.</param>
    /// <param name="token">The token observing invocation cancellation and sibling failures.</param>
    /// <param name="remaining">The shared count of unfinished operations, decremented atomically.</param>
    private static void RunNode(
        Node node,
        TContext context,
        BlockingCollection<Node> ready,
        CancellationToken token,
        ref int remaining)
    {
        // Don't start another operation after cancellation or a sibling failure
        token.ThrowIfCancellationRequested();

        node.Body(context);

        // Only the last completed prerequisite releases each dependent
        foreach (Node dependent in node.Dependents)
        {
            if (Interlocked.Decrement(ref dependent.RemainingPrerequisites) == 0)
            {
                ready.Add(dependent, token);
            }
        }

        // The count includes not-yet-ready operations, so reaching zero means no producer can add more work
        if (Interlocked.Decrement(ref remaining) == 0)
        {
            // Wake blocked consumers once no work remains
            ready.CompleteAdding();
        }
    }

    /// <summary>
    /// Builds a graph directly from references to previously declared operations.
    /// </summary>
    public sealed class Builder
    {
        /// <summary>The registered operations, in declaration order.</summary>
        private readonly List<Node> _nodes = [];

        /// <summary>Whether the topology has been frozen for execution.</summary>
        private bool _built;

        /// <summary>
        /// Registers an operation and links it to already registered prerequisites.
        /// </summary>
        /// <param name="body">The operation to run.</param>
        /// <param name="prerequisites">The operations that must finish before this operation starts.</param>
        /// <returns>The node to reference from subsequent operations.</returns>
        public Node Add(Action<TContext> body, params Node[] prerequisites)
        {
            if (_built)
            {
                throw new InvalidOperationException("The interop generation work graph is already built.");
            }

            // References can only point backwards within this builder, so missing nodes and cycles are impossible
            foreach (Node prerequisite in prerequisites)
            {
                if (prerequisite.Index >= _nodes.Count || !ReferenceEquals(_nodes[prerequisite.Index], prerequisite))
                {
                    throw new ArgumentException("A prerequisite belongs to a different work graph.", nameof(prerequisites));
                }
            }

            Node node = new(_nodes.Count, body, prerequisites.Length);

            foreach (Node prerequisite in prerequisites)
            {
                prerequisite.Dependents.Add(node);
            }

            _nodes.Add(node);

            return node;
        }

        /// <summary>
        /// Freezes the topology and transfers its nodes to a one-shot queue.
        /// </summary>
        /// <returns>The one-shot work queue.</returns>
        public InteropGenerationWorkQueue<TContext> Build()
        {
            if (_built)
            {
                throw new InvalidOperationException("The interop generation work graph is already built.");
            }

            _built = true;

            return new(_nodes);
        }
    }

    /// <summary>
    /// An operation and its prerequisite counter, owned by a single queue execution.
    /// </summary>
    public sealed class Node
    {
        /// <summary>
        /// Creates an operation registered by a graph builder.
        /// </summary>
        /// <param name="index">The operation's registration index.</param>
        /// <param name="body">The operation to run.</param>
        /// <param name="prerequisiteCount">The number of prerequisite operations.</param>
        internal Node(int index, Action<TContext> body, int prerequisiteCount)
        {
            Index = index;
            Body = body;
            RemainingPrerequisites = prerequisiteCount;
        }

        /// <summary>Gets the operation's registration index for validating prerequisite ownership.</summary>
        internal int Index { get; }

        /// <summary>Gets the operation to run.</summary>
        internal Action<TContext> Body { get; }

        /// <summary>Gets the operations released by this node.</summary>
        internal List<Node> Dependents { get; } = [];

        /// <summary>The remaining prerequisite count, decremented atomically by parallel workers.</summary>
        internal int RemainingPrerequisites;
    }
}
