// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace WindowsRuntime.InteropServices;

/// <summary>
/// An interface providing access to the cached Windows Runtime object reference for a given interface.
/// </summary>
/// <typeparam name="T">The interface providing access to.</typeparam>
/// <remarks>
/// <para>
/// This interface is only meant to be implemented by projected runtime classes.
/// </para>
/// <para>
/// The <typeparamref name="T"/> type must refer to a projected Windows Runtime interface.
/// </para>
/// </remarks>
public interface IWindowsRuntimeInterface<T>
    where T : class
{
    /// <summary>
    /// Gets the cached <see cref="WindowsRuntimeObjectReferenceValue"/> instance for the interface <typeparamref name="T"/>.
    /// </summary>
    /// <returns>The cached <see cref="WindowsRuntimeObjectReferenceValue"/> instance for the interface <typeparamref name="T"/>.</returns>
    WindowsRuntimeObjectReferenceValue GetInterface();
}
