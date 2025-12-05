// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Helpers;

/// <summary>
/// A generator for IIDs of Windows Runtime types.
/// </summary>
internal static class GuidGenerator
{
    /// <summary>
    /// The PIID for the Windows Runtime namespace, used for generating IIDs of generic instantiations.
    /// </summary>
    /// <see href="https://learn.microsoft.com/uwp/winrt-cref/winrt-type-system#guid-generation-for-parameterized-types"/>
    private static readonly Guid WindowsRuntimePIIDNamespace = new(0xD57AF411, 0x737B, 0xC042, 0xAB, 0xAE, 0x87, 0x8B, 0x1E, 0x16, 0xAD, 0xEE);

    /// <summary>
    /// Generates the IID for the specified type by computing its Windows Runtime signature and deriving an IID from that signature.
    /// </summary>
    /// <param name="type">The <see cref="TypeSignature"/> to generate the IID for.</param>
    /// <param name="interopReferences"> The <see cref="InteropReferences"/> instance to use. </param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    /// <returns>The resulting IID for <paramref name="type"/>.</returns>
    public static Guid CreateIID(TypeSignature type, InteropReferences interopReferences, bool useWindowsUIXamlProjections)
    {
        string signature = SignatureGenerator.GetSignature(type, interopReferences, useWindowsUIXamlProjections);
        Guid guid = CreateGuidFromSignature(signature);

        return guid;
    }

    /// <summary>
    /// Tries to resolve the IID for the specified type signature by checking well-known Windows Runtime
    /// interfaces and, if necessary, the type's <see cref="System.Runtime.InteropServices.GuidAttribute"/>.
    /// </summary>
    /// <param name="type">The type descriptor to try to get the IID for.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="iid">The resulting <see cref="Guid"/> value, if found.</param>
    /// <returns>Whether <paramref name="iid"/> was succesfully retrieved.</returns>
    public static bool TryGetIIDFromWellKnownInterfaceIIDsOrAttribute(ITypeDescriptor type, InteropReferences interopReferences, out Guid iid)
    {
        // First try to get the IID from the custom-mapped types mapping
        if (WellKnownInterfaceIIDs.TryGetGUID(type, interopReferences, out iid))
        {
            return true;
        }

        if (type.Resolve() is TypeDefinition typeDefinition)
        {
            // If the type was a normal projected type, then try to resolve the IID from the '[Guid]' attribute
            if (TryGetIIDFromAttribute(typeDefinition, interopReferences, out iid))
            {
                return true;
            }
        }

        iid = Guid.Empty;

        return false;
    }

    /// <summary>
    /// Attempts to retrieve the IID from the <see cref="System.Runtime.InteropServices.GuidAttribute"/> applied to the specified type.
    /// </summary>
    /// <param name="type">The type definition to inspect for <see cref="System.Runtime.InteropServices.GuidAttribute"/>.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="iid">The resulting <see cref="Guid"/> value, if found.</param>
    /// <returns>Whether <paramref name="iid"/> was succesfully retrieved.</returns>
    private static bool TryGetIIDFromAttribute(TypeDefinition type, InteropReferences interopReferences, out Guid iid)
    {
        if (type.TryGetCustomAttribute(interopReferences.GuidAttribute, out CustomAttribute? customAttribute))
        {
            if (customAttribute.Signature is { FixedArguments: [{ Element: Utf8String guidString }, ..] })
            {
                return Guid.TryParse(guidString.Value, out iid);
            }
        }

        iid = Guid.Empty;

        return false;
    }

    /// <summary>
    /// Encodes a <see cref="Guid"/> from a 16-byte sequence following RFC 4122 rules.
    /// Adjusts byte order for little-endian systems and sets version and variant bits.
    /// </summary>
    /// <param name="data">The input <see cref="ReadOnlySpan{T}"/> representing the <see cref="Guid"/> data.</param>
    /// <returns>The resulting <see cref="Guid"/> value constructed from <paramref name="data"/>.</returns>
    private static Guid EncodeGuid(ReadOnlySpan<byte> data)
    {
        // No need to actually perform a runtime check: this method is only ever
        // used from 'CreateGuidFromSignature' below, which hardcodes the length.
        Debug.Assert(data.Length >= 16);

        Span<byte> buffer = stackalloc byte[16];

        data[..16].CopyTo(buffer);

        if (BitConverter.IsLittleEndian)
        {
            // Swap bytes of 'int a'
            (buffer[3], buffer[0]) = (buffer[0], buffer[3]);
            (buffer[2], buffer[1]) = (buffer[1], buffer[2]);

            // Swap bytes of 'short b'
            (buffer[5], buffer[4]) = (buffer[4], buffer[5]);

            // Swap bytes of 'short c' and encode the RFC time/version field
            (buffer[7], buffer[6]) = ((byte)((buffer[6] & 0x0F) | (5 << 4)), buffer[7]);

            // Encode the RFC clock/reserved field
            buffer[8] = (byte)((buffer[8] & 0x3F) | 0x80);
        }

        return new(buffer);
    }

    /// <summary>
    /// Computes a deterministic IID from a Windows Runtime type signature.
    /// </summary>
    /// <param name="signature">The input Windows Runtime type signature.</param>
    /// <returns>The resulting IID for <paramref name="signature"/>.</returns>
    private static Guid CreateGuidFromSignature(ReadOnlySpan<char> signature)
    {
        // Get the maximum UTF8 byte size and allocate a buffer for the encoding.
        // If the minimum buffer is small enough, we can stack-allocate it.
        // The 512 threshold is the smallest one that will contain generally all
        // enum types names. Eg. we'd get signature strings like this:
        // 'pinterface({61c17706-2d65-11e0-9ae8-d48564015472};enum(Windows.UI.Xaml.Visibility;u4))'.
        // Which would result in a number of bytes ~280, including the GUID size.
        int maxUtf8ByteCount = Encoding.UTF8.GetMaxByteCount(signature.Length);
        int minimumPooledLength = 16 /* Number of bytes in a GUID */ + maxUtf8ByteCount;
        byte[]? utf8BytesFromPool = null;
        Span<byte> utf8Bytes = minimumPooledLength <= 512
            ? stackalloc byte[512]
            : (utf8BytesFromPool = ArrayPool<byte>.Shared.Rent(minimumPooledLength));

        _ = WindowsRuntimePIIDNamespace.TryWriteBytes(utf8Bytes);

        int encodedUtf8BytesWritten = Encoding.UTF8.GetBytes(signature, utf8Bytes[16..]);
        Span<byte> sha1Bytes = stackalloc byte[SHA1.HashSizeInBytes];

        // Hash the encoded signature (the bytes written are guaranteed to always match the span)
        _ = SHA1.HashData(utf8Bytes[..(16 + encodedUtf8BytesWritten)], sha1Bytes);

        Guid iid = EncodeGuid(sha1Bytes);

        // Before exiting, make sure to return the array from the pool, if we rented one
        if (utf8BytesFromPool is not null)
        {
            ArrayPool<byte>.Shared.Return(utf8BytesFromPool);
        }

        return iid;
    }
}
