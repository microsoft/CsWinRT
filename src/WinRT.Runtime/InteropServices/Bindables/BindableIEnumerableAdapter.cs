// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using WindowsRuntime.InteropServices.Marshalling;

#pragma warning disable IDE0046

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A stateless adapter for <see cref="IEnumerable"/>, to be exposed as <c>Windows.UI.Xaml.Interop.IBindableIterable</c>.
/// </summary>
internal static class BindableIEnumerableAdapter
{
    /// <summary>
    /// Returns an iterator for the items in the collection.
    /// </summary>
    /// <param name="enumerable">The wrapped <see cref="IEnumerable"/> instance.</param>
    /// <param name="enumerator">The enumerator.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindableiterable.first"/>
    public static unsafe void First(IEnumerable enumerable, void** enumerator)
    {
        IEnumerator managedEnumerator = enumerable.GetEnumerator();

        // First try to directly marshal the managed enumerators (see details in 'IEnumerableAdapter<T>.First')
        if (WindowsRuntimeInterfaceMarshaller<IEnumerator>.TryConvertToUnmanagedExact(
            value: managedEnumerator,
            iid: in WellKnownWindowsInterfaceIIDs.IID_IBindableIterator,
            result: out WindowsRuntimeObjectReferenceValue enumeratorValue))
        {
            *enumerator = enumeratorValue.DetachThisPtrUnsafe();
        }
        else
        {
            // If the previous marshalling failed, try to check whether the managed enumerator that we got
            // implements 'IEnumerator<object>'. If it does, we can directly wrap that in the adapter that
            // we'll marshal to native. Otherwise, we need an intermediate adapter to adapt the non-generic
            // managed enumerator that we got. This also means we'll effectively need two nested adapters.
            IEnumerator<object>? managedObjectEnumerator = managedEnumerator as IEnumerator<object> ?? new BindableIEnumeratorAdapter(managedEnumerator);

            // This is the same adapter we use for 'IEnumerator<T>'. Code paths dealing with it should also
            // be aware that they might potentially have another nested wrapper. We don't have special logic
            // in 'cswinrtinteropgen' to track a constructed 'IEnumerator<object>' when we see 'IEnumerable' being
            // used, however that is not needed given that this interface will always be discovered anyway.
            // With that, trimming will then automatically keep code for it if this code path is reachable.
            IEnumeratorAdapter<object> enumeratorAdapter = new(managedObjectEnumerator);

            // Marshal the adapter, we'll always have marshalling info for it (same as in 'IEnumerableAdapter<T>.First')
            *enumerator = (void*)WindowsRuntimeComWrappers.GetOrCreateComInterfaceForObjectExact(enumeratorAdapter, in WellKnownWindowsInterfaceIIDs.IID_IBindableIterator);
        }
    }

    /// <summary>
    /// Gets an <see cref="IEnumerable{T}"/> of <see cref="object"/> view of a given <see cref="IEnumerable"/> instance.
    /// </summary>
    /// <param name="enumerable">The input <see cref="IEnumerable"/> instance.</param>
    /// <returns>
    /// <paramref name="enumerable"/> itself if it already implements <see cref="IEnumerable{T}"/> of <see cref="object"/>,
    /// otherwise an <see cref="IEnumerable{T}"/> of <see cref="object"/> instance enumerating the items in <paramref name="enumerable"/>.
    /// </returns>
    /// <remarks>
    /// This method is used to marshal <see cref="IEnumerable"/> values that need to be exposed as
    /// <c>Windows.Foundation.Collections.IIterable&lt;IInspectable&gt;</c> (rather than as
    /// <c>Windows.UI.Xaml.Interop.IBindableIterable</c>) when the input instance doesn't
    /// also implement the generic interface.
    /// </remarks>
    [return: NotNullIfNotNull(nameof(enumerable))]
    public static IEnumerable<object>? AsObjectEnumerable(IEnumerable? enumerable)
    {
        // Keep the iterator in this assembly, so that the interop generator can discover it and generate its CCW
        static IEnumerable<object> Enumerate(IEnumerable enumerable)
        {
            foreach (object value in enumerable)
            {
                yield return value;
            }
        }

        if (enumerable is null)
        {
            return null;
        }

        return enumerable as IEnumerable<object> ?? Enumerate(enumerable);
    }
}