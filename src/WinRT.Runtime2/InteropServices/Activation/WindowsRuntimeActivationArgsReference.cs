// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A reference to additional parameters provided during Windows Runtime activation.
/// </summary>
/// <remarks>
/// This type works around the lack of support for <see cref="TypedReference"/> for byref-like types.
/// </remarks>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly ref struct WindowsRuntimeActivationArgsReference
{
    /// <summary>
    /// The reference to the additional parameters.
    /// </summary>
    private readonly ref readonly byte _reference;

    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeActivationArgsReference"/> value with the specified parameters.
    /// </summary>
    /// <param name="reference">The reference to the additional parameters.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private WindowsRuntimeActivationArgsReference([UnscopedRef] ref readonly byte reference)
    {
        _reference = ref reference;
    }

    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeActivationArgsReference"/> value pointing to the input byref-like type containinng the activation arguments.
    /// </summary>
    /// <typeparam name="T">The type of arguments being used.</typeparam>
    /// <param name="args">The byref-like type containing the activation arguments.</param>
    /// <returns>A <see cref="WindowsRuntimeActivationArgsReference"/> value wrapping a reference to <paramref name="args"/>.</returns>
    /// <remarks>
    /// The <paramref name="args"/> parameter must not outlive the returned <see cref="WindowsRuntimeActivationArgsReference"/> value.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WindowsRuntimeActivationArgsReference CreateUnsafe<T>([UnscopedRef] in T args)
        where T : allows ref struct
    {
        return new(ref Unsafe.As<T, byte>(ref Unsafe.AsRef(in args)));
    }

    /// <summary>
    /// Gets the original value from the internal reference for the current <see cref="WindowsRuntimeActivationArgsReference"/> instance.
    /// </summary>
    /// <typeparam name="T">The type of arguments being used.</typeparam>
    /// <returns>The resulting <typeparamref name="T"/> arguments.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T GetValueUnsafe<T>()
        where T : allows ref struct
    {
        return Unsafe.As<byte, T>(ref Unsafe.AsRef(in _reference));
    }
}
