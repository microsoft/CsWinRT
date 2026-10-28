// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// Computes a sorted metadata snapshot without changing ownership until all readers have finished.
/// </summary>
internal sealed class MetadataCollectionOrdering
{
    /// <summary>Computes the sorted snapshot without changing metadata ownership.</summary>
    private readonly Action _compute;

    /// <summary>Applies the sorted snapshot after all readers finish.</summary>
    private readonly Action _apply;

    /// <summary>
    /// Creates an ordering operation with a typed snapshot captured by its callbacks.
    /// </summary>
    /// <param name="compute">The operation that prepares the snapshot.</param>
    /// <param name="apply">The operation that applies the prepared snapshot.</param>
    private MetadataCollectionOrdering(Action compute, Action apply)
    {
        _compute = compute;
        _apply = apply;
    }

    /// <summary>
    /// Creates an ordering operation for an owned metadata collection.
    /// </summary>
    /// <typeparam name="T">The metadata element type.</typeparam>
    /// <param name="collection">The collection to reorder.</param>
    /// <param name="order">The ordering to compute while metadata ownership remains unchanged.</param>
    /// <returns>The deferred ordering operation.</returns>
    /// <remarks>The callbacks share the typed snapshot, releasing it after a successful <see cref="Apply"/>.</remarks>
    public static MetadataCollectionOrdering Create<T>(
        IList<T> collection,
        Func<IEnumerable<T>, IEnumerable<T>> order)
    {
        T[]? ordered = null;

        return new(
            compute: () => ordered = [.. order(collection)],
            apply: () =>
            {
                T[] snapshot = ordered ?? throw new InvalidOperationException("The metadata order has not been computed.");

                collection.Clear();

                foreach (T item in snapshot)
                {
                    collection.Add(item);
                }

                ordered = null;
            });
    }

    /// <summary>
    /// Computes the sorted snapshot, without detaching any metadata from its owner.
    /// </summary>
    public void Compute()
    {
        _compute();
    }

    /// <summary>
    /// Applies the sorted snapshot. All concurrent metadata readers must have finished first.
    /// </summary>
    public void Apply()
    {
        _apply();
    }
}
