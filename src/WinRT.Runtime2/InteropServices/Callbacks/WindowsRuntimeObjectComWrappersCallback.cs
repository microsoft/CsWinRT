// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A callback helper for <see cref="IWindowsRuntimeObjectComWrappersCallback"/>.
/// </summary>
internal abstract unsafe class WindowsRuntimeObjectComWrappersCallback
{
    /// <summary>
    /// Resolves a <see cref="WindowsRuntimeObjectComWrappersCallback"/> instance for a given <see cref="IWindowsRuntimeObjectComWrappersCallback"/> implementation.
    /// </summary>
    /// <typeparam name="TCallback">The callback type.</typeparam>
    /// <returns>The <see cref="WindowsRuntimeObjectComWrappersCallback"/> instance for <typeparamref name="TCallback"/>.</returns>
    public static WindowsRuntimeObjectComWrappersCallback GetInstance<TCallback>()
        where TCallback : IWindowsRuntimeObjectComWrappersCallback, allows ref struct
    {
        return WindowsRuntimeObjectComWrappersCallbackHost<TCallback>.Instance;
    }

    /// <inheritdoc cref="IWindowsRuntimeObjectComWrappersCallback.CreateObject"/>
    public abstract object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags);
}

/// <summary>
/// A callback host for <see cref="IWindowsRuntimeObjectComWrappersCallback"/>.
/// </summary>
/// <typeparam name="TCallback">The callback type.</typeparam>
file sealed class WindowsRuntimeObjectComWrappersCallbackHost<TCallback> : WindowsRuntimeObjectComWrappersCallback
    where TCallback : IWindowsRuntimeObjectComWrappersCallback, allows ref struct
{
    /// <summary>
    /// The singleton instance wrapping <typeparamref name="TCallback"/>.
    /// </summary>
    public static readonly WindowsRuntimeObjectComWrappersCallbackHost<TCallback> Instance = new();

    /// <inheritdoc/>
    public override unsafe object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        return TCallback.CreateObject(value, out wrapperFlags);
    }
}
