// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface for callbacks for <see cref="ComWrappers.CreateObject"/>, for unsealed Windows Runtime objects.
/// </summary>
public unsafe interface IWindowsRuntimeUnsealedObjectComWrappersCallback
{
    /// <summary>
    /// Creates a managed Windows Runtime object for a given native object of an unsealed type.
    /// </summary>
    /// <param name="value">The input native object to marshal.</param>
    /// <param name="runtimeClassName">The runtime class name of the native object.</param>
    /// <param name="wrapperObject">The resulting wrapper object, if it could be created.</param>
    /// <param name="wrapperFlags">Flags used to describe the created wrapper object.</param>
    /// <returns>Whether <paramref name="wrapperObject"/> could be created successfully.</returns>
    /// <remarks>
    /// <para>
    /// Implementations of this method can use <paramref name="runtimeClassName"/> to determine whether the actual
    /// runtime class name of <paramref name="value"/> matches the one they are implementing the marshalling logic
    /// for. If so, they can optimize the marshalling operation by reusing the <paramref name="value"/> pointer.
    /// </para>
    /// <para>
    /// The same semantics around <paramref name="value"/> apply as in <see cref="IWindowsRuntimeObjectComWrappersCallback.CreateObject"/>.
    /// </para>
    /// <para>
    /// Like <see cref="IWindowsRuntimeObjectComWrappersCallback.CreateObject"/>, this method will be called from the <see cref="ComWrappers.CreateObject"/>
    /// method, so implementations must not call back into <see cref="ComWrappers.CreateObject"/> themselves, but rather they should just marshal the object
    /// directly, and return it. Calling back into <see cref="ComWrappers.CreateObject"/> is not supported, and it will likely lead to a stack overlow exception.
    /// </para>
    /// </remarks>
    /// <seealso cref="IWindowsRuntimeObjectComWrappersCallback"/>
    static abstract bool TryCreateObject(
        void* value,
        ReadOnlySpan<char> runtimeClassName,
        [NotNullWhen(true)] out object? wrapperObject,
        out CreatedWrapperFlags wrapperFlags);
}
