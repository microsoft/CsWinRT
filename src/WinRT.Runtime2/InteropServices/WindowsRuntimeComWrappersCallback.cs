// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A callback helper for <see cref="IWindowsRuntimeComWrappersCallback"/>.
/// </summary>
internal abstract unsafe class WindowsRuntimeComWrappersCallback
{
    /// <summary>
    /// Resolves a <see cref="WindowsRuntimeComWrappersCallback"/> instance for a given <see cref="IWindowsRuntimeComWrappersCallback"/> implementation.
    /// </summary>
    /// <typeparam name="TCallback">The callback type.</typeparam>
    /// <returns>The <see cref="WindowsRuntimeComWrappersCallback"/> instance for <typeparamref name="TCallback"/>.</returns>
    public static WindowsRuntimeComWrappersCallback GetInstance<TCallback>()
        where TCallback : IWindowsRuntimeComWrappersCallback, allows ref struct
    {
        return WindowsRuntimeComWrappersCallbackHost<TCallback>.Instance;
    }

    /// <inheritdoc cref="IWindowsRuntimeComWrappersCallback.CreateObject"/>
    public abstract object CreateObject(void* value);
}

internal abstract unsafe class WindowsRuntimeUnsealedComWrappersCallback
{
    public abstract bool TryCreateObject(
        void* value,
        ReadOnlySpan<char> runtimeClassName,
        [NotNullWhen(true)] out object? result);
}

/// <summary>
/// A callback host for <see cref="IWindowsRuntimeComWrappersCallback"/>.
/// </summary>
/// <typeparam name="TCallback">The callback type.</typeparam>
file sealed class WindowsRuntimeComWrappersCallbackHost<TCallback> : WindowsRuntimeComWrappersCallback
    where TCallback : IWindowsRuntimeComWrappersCallback, allows ref struct
{
    /// <summary>
    /// The singleton instance wrapping <typeparamref name="TCallback"/>.
    /// </summary>
    public static readonly WindowsRuntimeComWrappersCallbackHost<TCallback> Instance = new();

    /// <inheritdoc/>
    public override unsafe object CreateObject(void* value)
    {
        return TCallback.CreateObject(value);
    }
}
