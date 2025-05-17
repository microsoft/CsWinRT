// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.ComponentModel;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface for specialized generic implementations of projected <see cref="System.Collections.Generic.IEnumerator{T}"/> methods.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
/// <remarks>
/// This type should only be used by generated code.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IEnumeratorMethods<T>
{
    /// <inheritdoc cref="System.Collections.Generic.IEnumerator{T}.Current"/>
    /// <param name="thisObject">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static T Current<TMethods>(WindowsRuntimeObjectReference thisObject)
        where TMethods : IIteratorMethods<T>
    {
        return TMethods.Current(thisObject);
    }

    /// <inheritdoc cref="System.Collections.IEnumerator.MoveNext"/>
    /// <param name="thisObject">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    public static bool MoveNext<TMethods>(WindowsRuntimeObjectReference thisObject)
        where TMethods : IIteratorMethods<T>
    {
        return TMethods.MoveNext(thisObject);
    }
}
