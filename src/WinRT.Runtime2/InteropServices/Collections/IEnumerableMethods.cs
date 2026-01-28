// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using WindowsRuntime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for <see cref="IEnumerable{T}"/> types to be used when no generic context is available.
/// </summary>
/// <remarks>
/// This type is only meant to be used to support <see cref="IEnumerable"/> marshalling.
/// </remarks>
internal static unsafe class IEnumerableMethods
{
    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerator GetEnumerator(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        void* result;

        // We have some 'IIterable<T>' instantiation, and we don't know the 'T'. However, we know that
        // the vtable slot for 'First()' will always be the same, and there's no ABI concerns since the
        // enumerator type will always be an object (some 'IIterator<T>' object). So we can just invoke
        // that method, ignoring the specific instantiation, and we'll get back some 'IInspectable'.
        RestrictedErrorInfo.ThrowExceptionForHR(((IIterableVftbl*)*(void***)thisPtr)->First(thisPtr, &result));

        try
        {
            // Because we don't know the 'T' type for the current enumerable, we will just marshal the
            // resulting enumerator without type information. This means that we'll either unwrap the
            // CCW for the managed object, or marshal the type as some RCW for a native object.
            object enumerator = WindowsRuntimeObjectMarshaller.ConvertToManaged(result)!;

            // It's possible to get back some 'IEnumeratorAdapter<T>' instance here, so we
            // can unwrap the original managed enumerator object from that in this case.
            // This matches the logic for the normal 'IEnumerable<T>' methods.
            if (enumerator is IEnumeratorAdapter adapter)
            {
                return adapter.Enumerator;
            }

            // Otherwise, just cast the resulting object. It should either be some generated RCW for a
            // generic 'IEnumerator<T>' instantiation, in which case it will also implement 'IEnumerator'
            // directly in metadata, or it will be some opaque object, which will trigger a dynamic cast.
            return (IEnumerator)enumerator;
        }
        finally
        {
            WindowsRuntimeUnknownMarshaller.Free(result);
        }
    }
}