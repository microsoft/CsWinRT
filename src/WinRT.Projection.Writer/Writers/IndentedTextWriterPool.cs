// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
/// <c>WinRT.Interop.Generator</c> (see
/// <c>InteropTypeDiscovery.cs</c>): a private static <see cref="ConcurrentBag{T}"/> backs
/// the pool so the parallel projection-emission pipeline can lease and return writers from
/// any thread without explicit locking.
/// </para>
/// <para>
/// Writers are <see cref="IndentedTextWriter.Clear"/>-ed on lease (in <see cref="GetOrCreate"/>)
/// and returned as-is by <see cref="Return"/>. Returning a writer with stale buffer content
/// or a non-zero indent level is therefore safe; the next consumer always observes a
/// freshly reset writer. Pool growth is unbounded by design: under steady-state parallel
/// load the pool naturally caps at the worker-thread high-water mark.
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
    /// Returns an <see cref="IndentedTextWriter"/> instance for the caller to use. If the pool
    /// has a recycled writer available it is reset (buffer cleared, indent level zeroed) and
    /// returned; otherwise a fresh instance is constructed.
    /// </summary>
    /// <returns>A clean <see cref="IndentedTextWriter"/> ready to be written to.</returns>
    public static IndentedTextWriter GetOrCreate()
    {
        if (Pool.TryTake(out IndentedTextWriter? writer))
        {
            writer.Clear();
            return writer;
        }
        return new IndentedTextWriter();
    }

    /// <summary>
    /// Returns <paramref name="writer"/> to the pool for reuse. The writer is NOT cleared on
    /// return; the next <see cref="GetOrCreate"/> caller is responsible for resetting it
    /// before use. This keeps the return path branch-free and lets the lease site stay
    /// straight-line ("flush, return, done") without extra bookkeeping.
    /// </summary>
    /// <param name="writer">The writer to return. Must not be used by the caller after this call.</param>
    public static void Return(IndentedTextWriter writer)
    {
        Pool.Add(writer);
    }
}
