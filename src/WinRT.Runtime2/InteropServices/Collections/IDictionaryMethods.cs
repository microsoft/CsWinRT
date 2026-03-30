// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for implementations of <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> types.
/// </summary>
public static class IDictionaryMethods
{
    /// <inheritdoc cref="System.Collections.Generic.ICollection{T}.Count"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static int Count(WindowsRuntimeObjectReference thisReference)
    {
        return IMapMethods.Count(thisReference);
    }

    /// <inheritdoc cref="System.Collections.Generic.ICollection{T}.Clear"/>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static void Clear(WindowsRuntimeObjectReference thisReference)
    {
        IMapMethods.Clear(thisReference);
    }
}
#endif
