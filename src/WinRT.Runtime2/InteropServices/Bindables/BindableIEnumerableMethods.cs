// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for implementations of the <see cref="IEnumerable"/> type.
/// </summary>
internal static class BindableIEnumerableMethods
{
    /// <inheritdoc cref="IEnumerable.GetEnumerator"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IEnumerator GetEnumerator(WindowsRuntimeObjectReference thisReference)
    {
        IEnumerator enumerator = IBindableIterableMethods.First(thisReference);

        // Look for a wrapped non-generic adapter (same logic as in 'IEnumerableMethods.GetEnumerator')
        if (enumerator is IEnumeratorAdapter<object> bindableOuterAdapter)
        {
            IEnumerator<object> objectEnumerator = bindableOuterAdapter.Enumerator;

            return objectEnumerator is BindableIEnumeratorAdapter bindableInnerAdapter
                ? bindableInnerAdapter.Enumerator
                : objectEnumerator;
        }

        // This handles the case where we have some 'T' adapter for an original 'IEnumerator<T>' instance
        if (enumerator is IEnumeratorAdapter adapter)
        {
            return adapter.Enumerator;
        }

        // Otherwise, just return the original enumerator directly, it shouldn't be wrapping anything
        return enumerator;
    }
}
#endif
