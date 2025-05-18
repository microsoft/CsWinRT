// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface for specialized generic implementations of projected <see cref="System.Collections.Generic.IEnumerator{T}"/> methods.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
[Obsolete("This type is an implementation detail, and it's only meant to be consumed by 'cswinrtgen'")]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IEnumeratorMethods<T>
{
    /// <inheritdoc cref="System.Collections.Generic.IEnumerator{T}.Current"/>
    /// <param name="thisObject">The <see cref="WindowsRuntimeObject"/> instance owning <paramref name="thisReference"/>.</param>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static T Current<TMethods>(WindowsRuntimeObject thisObject, WindowsRuntimeObjectReference thisReference)
        where TMethods : IIteratorMethods<T>
    {
        return TMethods.Current(thisReference);
    }

    /// <inheritdoc cref="System.Collections.IEnumerator.MoveNext"/>
    /// <param name="thisObject">The <see cref="WindowsRuntimeObject"/> instance owning <paramref name="thisReference"/>.</param>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static bool MoveNext<TMethods>(WindowsRuntimeObject thisObject, WindowsRuntimeObjectReference thisReference)
        where TMethods : IIteratorMethods<T>
    {
        return TMethods.MoveNext(thisReference);
    }
}
