// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
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

        return IEnumeratorAdapter.TryUnwrap(enumerator);
    }
}