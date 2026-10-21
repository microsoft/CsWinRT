// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface for <see cref="System.Collections.Generic.IEnumerator{T}"/> adapter types.
/// </summary>
internal interface IEnumeratorAdapter
{
    /// <summary>
    /// Tries to unwrap managed iterator adapters returned across the ABI for non-generic enumeration.
    /// </summary>
    /// <param name="enumerator">The iterator returned by the native iterable.</param>
    /// <returns>The original managed iterator, or <paramref name="enumerator"/> if it is not an adapter.</returns>
    static IEnumerator TryUnwrap(IEnumerator enumerator)
    {
        // It's possible to get back an 'IEnumeratorAdapter<object>' instance here, and we want to unwrap the
        // inner-most instance here. There are basically two scenarios here. If we have originally marshalled
        // some 'IEnumerator<object>' object that had no marshalling information, then we'd end up with an
        // adapter for it, which we want to return here. If we have originally marshalled an 'IEnumerator'
        // instance that had no marshalling information, then we'd end up with an outer 'IEnumeratorAdapter<object>'
        // object that wraps a 'BindableIEnumeratorAdapter' instance. So we want to unwrap that inner object.
        if (enumerator is IEnumeratorAdapter<object> bindableOuterAdapter)
        {
            IEnumerator<object> objectEnumerator = bindableOuterAdapter.Enumerator;

            return objectEnumerator is BindableIEnumeratorAdapter bindableInnerAdapter
                ? bindableInnerAdapter.Enumerator
                : objectEnumerator;
        }

        // This handles the case where we have some 'T' adapter for an original 'IEnumerator<T>' instance
        return enumerator is IEnumeratorAdapter adapter ? adapter.Enumerator : enumerator;
    }

    /// <summary>
    /// Gets the wrapped <see cref="IEnumerator"/> instance.
    /// </summary>
    IEnumerator Enumerator { get; }
}