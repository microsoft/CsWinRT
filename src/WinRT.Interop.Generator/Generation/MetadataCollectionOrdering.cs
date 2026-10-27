// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <summary>
/// Computes a sorted metadata snapshot without changing ownership until all readers have finished.
/// </summary>
internal abstract class MetadataCollectionOrdering
{
    /// <summary>
    /// Creates an ordering operation for an owned metadata collection.
    /// </summary>
    /// <typeparam name="T">The metadata element type.</typeparam>
    /// <param name="collection">The collection to reorder.</param>
    /// <param name="order">The ordering to compute while metadata ownership remains unchanged.</param>
    /// <returns>The deferred ordering operation.</returns>
    public static MetadataCollectionOrdering Create<T>(IList<T> collection, Func<IEnumerable<T>, IEnumerable<T>> order)
    {
        return new Collection<T>(collection, order);
    }

    /// <summary>
    /// Computes the sorted snapshot, without detaching any metadata from its owner.
    /// </summary>
    public abstract void Compute();

    /// <summary>
    /// Applies the sorted snapshot. All concurrent metadata readers must have finished first.
    /// </summary>
    public abstract void Apply();

    /// <summary>
    /// Holds the sorted snapshot of one metadata collection.
    /// </summary>
    /// <typeparam name="T">The metadata element type.</typeparam>
    /// <param name="collection">The collection to reorder.</param>
    /// <param name="order">The ordering to compute.</param>
    private sealed class Collection<T>(IList<T> collection, Func<IEnumerable<T>, IEnumerable<T>> order) : MetadataCollectionOrdering
    {
        /// <summary>The sorted snapshot, computed before metadata ownership changes.</summary>
        private T[]? _ordered;

        /// <inheritdoc/>
        public override void Compute()
        {
            _ordered = [.. order(collection)];
        }

        /// <inheritdoc/>
        public override void Apply()
        {
            T[] ordered = _ordered ?? throw new InvalidOperationException("The metadata order has not been computed.");

            collection.Clear();

            foreach (T item in ordered)
            {
                collection.Add(item);
            }

            _ordered = null;
        }
    }
}
