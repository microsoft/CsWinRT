// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A callback helper for <see cref="IWindowsRuntimeUnsealedObjectComWrappersCallback"/>.
/// </summary>
internal abstract unsafe class WindowsRuntimeUnsealedObjectComWrappersCallback
{
    /// <summary>
    /// Resolves a <see cref="WindowsRuntimeUnsealedObjectComWrappersCallback"/> instance for a given <see cref="IWindowsRuntimeUnsealedObjectComWrappersCallback"/> implementation.
    /// </summary>
    /// <typeparam name="TCallback">The callback type.</typeparam>
    /// <returns>The <see cref="WindowsRuntimeUnsealedObjectComWrappersCallback"/> instance for <typeparamref name="TCallback"/>.</returns>
    public static WindowsRuntimeUnsealedObjectComWrappersCallback GetInstance<TCallback>()
        where TCallback : IWindowsRuntimeUnsealedObjectComWrappersCallback, allows ref struct
    {
        return WindowsRuntimeUnsealedObjectComWrappersCallbackHost<TCallback>.Instance;
    }

    /// <inheritdoc cref="IWindowsRuntimeUnsealedObjectComWrappersCallback.TryCreateObject"/>
    public abstract bool TryCreateObject(
        void* value,
        ReadOnlySpan<char> runtimeClassName,
        [NotNullWhen(true)] out object? wrapperObject,
        out CreatedWrapperFlags wrapperFlags);
}

/// <summary>
/// A callback host for <see cref="IWindowsRuntimeUnsealedObjectComWrappersCallback"/>.
/// </summary>
/// <typeparam name="TCallback">The callback type.</typeparam>
file sealed class WindowsRuntimeUnsealedObjectComWrappersCallbackHost<TCallback> : WindowsRuntimeUnsealedObjectComWrappersCallback
    where TCallback : IWindowsRuntimeUnsealedObjectComWrappersCallback, allows ref struct
{
    /// <summary>
    /// The singleton instance wrapping <typeparamref name="TCallback"/>.
    /// </summary>
    public static readonly WindowsRuntimeUnsealedObjectComWrappersCallbackHost<TCallback> Instance = new();

    /// <inheritdoc/>
    public override unsafe bool TryCreateObject(
        void* value,
        ReadOnlySpan<char> runtimeClassName,
        [NotNullWhen(true)] out object? wrapperObject,
        out CreatedWrapperFlags wrapperFlags)
    {
        return TCallback.TryCreateObject(value, runtimeClassName, out wrapperObject, out wrapperFlags);
    }
}
