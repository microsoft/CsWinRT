// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;

namespace WindowsRuntime.ProjectionWriter.Writers;

/// <summary>
/// Pool of <see cref="IndentedTextWriter"/> instances reused across the projection emission
/// pipeline. The writer is the workhorse of the codebase (every projected class, interface,
/// struct, IID expression, and per-method scratch buffer instantiates one or more), so
/// recycling them across work items eliminates the per-emission <see cref="System.Text.StringBuilder"/>
/// allocation and the four-entry <c>_availableIndentations</c> array allocation that would
/// otherwise happen on every call.
/// </summary>
/// <remarks>
/// <para>
/// Mirrors the <c>TypeSignatureBuilderPool</c> / <c>IidHashSetPool</c> pattern in
/// <c>WinRT.Interop.Generator</c> (see <c>InteropTypeDiscovery.cs</c>): a private static
/// <see cref="ConcurrentBag{T}"/> backs the pool so the parallel projection-emission pipeline
/// can lease and return writers from any thread without explicit locking.
/// </para>
/// <para>
/// Writers are <see cref="IndentedTextWriter.Clear"/>-ed on lease (in <see cref="GetOrCreate"/>)
/// and returned via the disposable <see cref="IndentedTextWriterOwner"/> token. The lease
/// shape is always a <c>using</c> block so the writer is returned to the pool on every exit
/// path (including exceptions). Pool growth is unbounded by design: under steady-state
/// parallel load the pool naturally caps at the worker-thread high-water mark.
/// </para>
/// </remarks>
internal static class IndentedTextWriterPool
{
    /// <summary>
    /// The backing pool. <see cref="ConcurrentBag{T}"/> is the right primitive for thread-local
    /// take/add patterns: each thread maintains a thread-local list and only walks other
    /// threads' lists on starvation, which fits the projection-writer lease pattern (lots of
    /// short leases per worker, with occasional cross-thread steals).
    /// </summary>
    private static readonly ConcurrentBag<IndentedTextWriter> Pool = [];

    /// <summary>
    /// Leases an <see cref="IndentedTextWriter"/> from the pool, wrapped in a disposable
    /// <see cref="IndentedTextWriterOwner"/> token. The token returns the writer to the pool
    /// when disposed, so the caller pattern is always
    /// <c>using IndentedTextWriterOwner owner = IndentedTextWriterPool.GetOrCreate();</c>
    /// followed by <c>owner.Writer.Write(...)</c>.
    /// </summary>
    /// <returns>The owning disposable token. Access the underlying writer via <see cref="IndentedTextWriterOwner.Writer"/>.</returns>
    public static IndentedTextWriterOwner GetOrCreate()
    {
        if (Pool.TryTake(out IndentedTextWriter? writer))
        {
            writer.Clear();
            return new IndentedTextWriterOwner(writer);
        }
        return new IndentedTextWriterOwner(new IndentedTextWriter());
    }

    /// <summary>
    /// Returns <paramref name="writer"/> to the pool for reuse. Called by
    /// <see cref="IndentedTextWriterOwner.Dispose"/>; not for direct use.
    /// </summary>
    /// <param name="writer">The writer to return.</param>
    internal static void Return(IndentedTextWriter writer)
    {
        Pool.Add(writer);
    }
}

/// <summary>
/// A disposable lease token for an <see cref="IndentedTextWriter"/> obtained from
/// <see cref="IndentedTextWriterPool.GetOrCreate"/>. The token is a <see langword="ref struct"/>
/// so it can only live on the stack: it's never accidentally captured by an async lambda or
/// stashed in a heap field, which would defeat the lifetime model.
/// </summary>
/// <remarks>
/// <para>
/// Disposal is idempotent and self-protecting: <see cref="Dispose"/> sets the inner reference
/// to <see langword="null"/> after returning the writer to the pool, so a double-dispose (e.g.
/// from a buggy refactor) cannot return the same instance twice. After disposal the
/// <see cref="Writer"/> property throws <see cref="ObjectDisposedException"/>.
/// </para>
/// </remarks>
internal ref struct IndentedTextWriterOwner : IDisposable
{
    private IndentedTextWriter? _writer;

    /// <summary>
    /// Initializes a new owner around <paramref name="writer"/>. Internal: callers obtain
    /// instances via <see cref="IndentedTextWriterPool.GetOrCreate"/>.
    /// </summary>
    /// <param name="writer">The leased writer.</param>
    internal IndentedTextWriterOwner(IndentedTextWriter writer)
    {
        _writer = writer;
    }

    /// <summary>
    /// Gets the leased <see cref="IndentedTextWriter"/>. Throws <see cref="ObjectDisposedException"/>
    /// if the owner has already been disposed (i.e. <see cref="Dispose"/> was called).
    /// </summary>
    public readonly IndentedTextWriter Writer
    {
        get
        {
            ObjectDisposedException.ThrowIf(_writer is null, typeof(IndentedTextWriterOwner));
            return _writer;
        }
    }

    /// <summary>
    /// Returns the leased writer to the pool and clears the inner reference so subsequent
    /// <see cref="Writer"/> accesses (or duplicate <see cref="Dispose"/> calls) cannot
    /// re-pool the same instance.
    /// </summary>
    public void Dispose()
    {
        if (_writer is { } writer)
        {
            _writer = null;
            IndentedTextWriterPool.Return(writer);
        }
    }
}
