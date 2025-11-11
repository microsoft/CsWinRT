// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Interop;

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// Represents a reference to a <see cref="Type"/> value, for fast marshalling to native.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public unsafe ref struct TypeReference
{
    /// <inheritdoc cref="ABI.System.Type.Name"/>
    internal string? Name;

    /// <inheritdoc cref = "ABI.System.Type.Kind" />
    internal TypeKind Kind;

    /// <summary>
    /// The <see cref="HStringReference"/> to use for marshalling.
    /// </summary>
    private HStringReference NameRef;

    /// <summary>
    /// Converts the current <see cref="TypeReference"/> value into a <see cref="ABI.System.Type"/> value for marshalling.
    /// </summary>
    /// <returns>The resulting <see cref="ABI.System.Type"/> value for marshalling.</returns>
    /// <remarks>
    /// Unlike <see cref="ConvertToUnmanagedUnsafe"/>, this method will not use a fast-pass <c>HSTRING</c>, so it doesn't need pinning.
    /// </remarks>
    public ABI.System.Type ConvertToUnmanaged()
    {
        return new() { Name = HStringMarshaller.ConvertToUnmanaged(Name), Kind = Kind };
    }

    /// <summary>
    /// Converts the current <see cref="TypeReference"/> value into a <see cref="ABI.System.Type"/> value for marshalling.
    /// </summary>
    /// <returns>The resulting <see cref="ABI.System.Type"/> value for marshalling.</returns>
    /// <remarks>
    /// This method can only be used within a <see langword="fixed"/> block on <see cref="GetPinnableReference"/>.
    /// Calling this method while the <see cref="TypeReference"/> value is not fixed this way is undefined behavior.
    /// </remarks>
    public ABI.System.Type ConvertToUnmanagedUnsafe()
    {
        HStringMarshaller.ConvertToUnmanagedUnsafe(
            (char*)Unsafe.AsPointer(in Name!.GetPinnableReference()),
            Name?.Length,
            out NameRef);

        return new() { Name = NameRef.HString, Kind = Kind };
    }

    /// <summary>
    /// Returns a pinnable reference for the current <see cref="TypeReference"/> value.
    /// </summary>
    /// <returns>A pinnable reference for the current <see cref="TypeReference"/> value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref byte GetPinnableReference()
    {
        return ref Unsafe.As<char, byte>(ref Unsafe.AsRef(in Name!.GetPinnableReference()));
    }
}
