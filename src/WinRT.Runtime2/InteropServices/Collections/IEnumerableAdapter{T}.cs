// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using WindowsRuntime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A stateless adapter for <see cref="IEnumerable{T}"/>, to be exposed as <c>Windows.Foundation.Collections.IIterable&lt;T&gt;</c>.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterable-1"/>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IEnumerableAdapter<T>
{
    /// <summary>
    /// Returns an iterator for the items in the collection.
    /// </summary>
    /// <param name="enumerable">The wrapped <see cref="IEnumerable{T}"/> instance.</param>
    /// <param name="iid">The IID for the interface being marshalled.</param>
    /// <param name="enumerator">The enumerator.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterable-1.first"/>
    public static unsafe void First(IEnumerable<T> enumerable, in Guid iid, void** enumerator)
    {
        IEnumerator<T> managedEnumerator = enumerable.GetEnumerator();

        // Try to directly marshal the managed enumerator to a Windows Runtime interface pointer first. This
        // will succeed only if we have marshalling info available for the type (because the 'Exact' method
        // never marshals objects as opaque 'IInspectable' pointers as a fallback). In general, we would
        // very often expect to have marshalling info for types being marshalled here, but only if we can
        // see them at compile time. We do have the ability to inspect internal types, however that is not
        // the case for types coming from the BCL, because we only get reference assemblies for those. So
        // in practice we can't always assume this information will be available here.
        //
        // Note: technically speaking, the same can happen with any interface type. However, we only have
        // this special handling for 'IEnumerator<T>' types, because they're particular critical and used
        // everywhere, and also because it's much more likely for them to be internal and harder to see.
        // This also makes arrays work, which have a special internal enumerator type we can't ever track.
        if (WindowsRuntimeInterfaceMarshaller<IEnumerator<T>>.TryConvertToUnmanagedExact(
            value: managedEnumerator,
            iid: in iid,
            result: out WindowsRuntimeObjectReferenceValue enumeratorValue))
        {
            *enumerator = enumeratorValue.DetachThisPtrUnsafe();
        }
        else
        {
            // As a fallback, wrap the enumerator in an adapter that implements 'IEnumerator<T>' and just
            // forwards all calls. We will use this adapter to marshal the native enumerator interface.
            IEnumeratorAdapter<T> enumeratorAdapter = new(managedEnumerator);

            // Marshal the adapter, with the assumption that we'll always have marshalling info for it. This time
            // we can actually rely on this, because we will manually track those adapter types for each generic
            // instantiation that 'IEnumerable<T>' that is discovered. Meaning if we get here, we'll have that info.
            *enumerator = (void*)WindowsRuntimeComWrappers.GetOrCreateComInterfaceForObjectExact(enumeratorAdapter, in iid);
        }
    }
}