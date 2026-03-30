// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
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

    /// <inheritdoc cref="IWindowsRuntimeUnsealedObjectComWrappersCallback.CreateObject"/>
    public abstract object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags);
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

    /// <inheritdoc/>
    public override unsafe object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        return TCallback.CreateObject(value, out wrapperFlags);
    }
}
#endif