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
/// Runs a dependency graph of coarse operations with bounded, non-buffering workers.
/// </summary>
/// <typeparam name="TContext">The shared context passed to each operation.</typeparam>
internal sealed class InteropGenerationWorkQueue<TContext>
{
    /// <summary>The initial operations, in declaration order.</summary>
    private readonly Node[] _roots;

    /// <summary>The immutable prerequisite counts, copied for each run.</summary>
    private readonly int[] _prerequisiteCounts;

    /// <summary>
    /// Captures a completed graph without retaining its builder.
    /// </summary>
    /// <param name="nodes">The operations in declaration order.</param>
    private InteropGenerationWorkQueue(List<Node> nodes)
    {
        _roots = [.. nodes.Where(static node => node.PrerequisiteCount == 0)];
        _prerequisiteCounts = [.. nodes.Select(static node => node.PrerequisiteCount)];
    }

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

        if (_prerequisiteCounts.Length == 0)
        {
            return;
        }

        int workerCount = Math.Min(
            maxDegreesOfParallelism == -1 ? Environment.ProcessorCount : maxDegreesOfParallelism,
            _prerequisiteCounts.Length);

        // Shared graph nodes stay immutable; concurrent runs own separate counters and ready queues
        int[] pending = (int[])_prerequisiteCounts.Clone();
        using BlockingCollection<Node> ready = new(new ConcurrentQueue<Node>(_roots));

        // A single worker needs neither the parallel loop nor linked cancellation
        if (workerCount == 1)
        {
            RunSequential(context, pending, ready, token);

            return;
        }

        int remaining = pending.Length;

        _ = Parallel.ForEachUnbufferedUntilFirstFailure(
            source: ready.GetConsumingEnumerable,
            token: token,
            maxDegreesOfParallelism: workerCount,
            body: (node, workerToken) => RunNode(node, context, pending, ready, workerToken, ref remaining));
    }

    /// <summary>
    /// Runs one parallel operation, releases its dependents, and closes the queue when all work is done.
    /// </summary>
    /// <param name="node">The operation to run.</param>
    /// <param name="context">The context to pass to the operation.</param>
    /// <param name="pending">This run's remaining prerequisite counts.</param>
    /// <param name="ready">The shared queue of operations ready to run.</param>
    /// <param name="token">The token observing invocation cancellation and sibling failures.</param>
    /// <param name="remaining">The shared count of unfinished operations, decremented atomically.</param>
    private static void RunNode(
        Node node,
        TContext context,
        int[] pending,
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
            if (Interlocked.Decrement(ref pending[dependent.Index]) == 0)
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
    /// Runs ready operations serially, releasing their dependents as prerequisites finish.
    /// </summary>
    /// <param name="context">The context to pass to each operation.</param>
    /// <param name="pending">This run's remaining prerequisite counts.</param>
    /// <param name="ready">The queue of operations ready to run.</param>
    /// <param name="token">The invocation's cancellation token.</param>
    private static void RunSequential(
        TContext context,
        int[] pending,
        BlockingCollection<Node> ready,
        CancellationToken token)
    {
        while (ready.TryTake(out Node? node))
        {
            token.ThrowIfCancellationRequested();
            node.Body(context);

            foreach (Node dependent in node.Dependents)
            {
                if (--pending[dependent.Index] == 0)
                {
                    ready.Add(dependent);
                }
            }
        }

        token.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// Builds a graph directly from references to previously declared operations.
    /// </summary>
    public sealed class Builder
    {
        /// <summary>The operations, whose positions provide stable counter indices.</summary>
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
        /// Freezes the topology and creates a queue that can be shared across runs.
        /// </summary>
        /// <returns>The reusable work queue.</returns>
        public InteropGenerationWorkQueue<TContext> Build()
        {
            _built = true;

            return new(_nodes);
        }
    }

    /// <summary>
    /// An operation in the graph, with no mutable per-run state.
    /// </summary>
    public sealed class Node
    {
        /// <summary>
        /// Creates an operation registered by a graph builder.
        /// </summary>
        /// <param name="index">The operation's counter index.</param>
        /// <param name="body">The operation to run.</param>
        /// <param name="prerequisiteCount">The number of prerequisite operations.</param>
        internal Node(int index, Action<TContext> body, int prerequisiteCount)
        {
            Index = index;
            Body = body;
            PrerequisiteCount = prerequisiteCount;
        }

        /// <summary>Gets the operation's counter index.</summary>
        internal int Index { get; }

        /// <summary>Gets the operation to run.</summary>
        internal Action<TContext> Body { get; }

        /// <summary>Gets the operations released by this node.</summary>
        internal List<Node> Dependents { get; } = [];

        /// <summary>Gets the number of prerequisite operations.</summary>
        internal int PrerequisiteCount { get; }
    }
}
