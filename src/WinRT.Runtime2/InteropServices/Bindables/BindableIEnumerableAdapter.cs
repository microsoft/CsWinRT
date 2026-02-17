// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using WindowsRuntime.InteropServices.Marshalling;

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
}