// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.References;

#pragma warning disable IDE0046

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
    /// Parameterized IIDs shared by marshalling and metadata emission for each invocation.
    /// </summary>
    private static readonly ConditionalWeakTable<RuntimeContext, ConcurrentDictionary<string, Guid>> IidCaches = [];

    /// <summary>
    /// Generates the IID for the specified type by computing its Windows Runtime signature and deriving an IID from that signature.
    /// </summary>
    /// <param name="type">The <see cref="TypeSignature"/> to generate the IID for.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences"> The <see cref="InteropReferences"/> instance to use. </param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    /// <returns>The resulting IID for <paramref name="type"/>.</returns>
    public static Guid CreateIID(
        TypeSignature type,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        bool useWindowsUIXamlProjections)
    {
        string signature = SignatureGenerator.GetSignature(type, interopDefinitions, interopReferences, useWindowsUIXamlProjections);

        return GetOrCreateIID(signature, interopReferences.RuntimeContext);
    }

    /// <summary>
    /// Tries to get the IID represented by a Windows Runtime type using the existing metadata and signature helpers.
    /// </summary>
    /// <param name="type">The Windows Runtime type.</param>
    /// <param name="interopDefinitions">The projection definitions.</param>
    /// <param name="interopReferences">The references for this invocation.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use UWP XAML mappings.</param>
    /// <param name="iid">The IID of the interface represented by the input type, or a runtime class's default interface.</param>
    /// <returns>Whether the type has a Windows Runtime IID.</returns>
    public static bool TryCreateIID(
        TypeSignature type,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        bool useWindowsUIXamlProjections,
        out Guid iid)
    {
        // Reject non-WinRT types and the non-generic 'EventHandler', which is a special custom mapping
        if (!type.IsWindowsRuntimeType(interopReferences) ||
            interopReferences.SignatureComparer.Equals(type, interopReferences.EventHandler))
        {
            iid = default;

            return false;
        }

        // 'object' represents 'IInspectable' rather than a specific runtime class
        if (interopReferences.SignatureComparer.Equals(type, interopReferences.Object))
        {
            iid = WellKnownInterfaceIIDs.IID_IInspectable;

            return true;
        }

        // Mapped classes carry their default IID in the mapping, while raw values have no interface identity
        if (type.IsFundamentalWindowsRuntimeType(interopReferences) ||
            type.IsCustomMappedWindowsRuntimeNonGenericStructOrClassType(interopReferences))
        {
            return TypeMapping.TryFindMappedTypeIID(type.FullName, useWindowsUIXamlProjections, out iid);
        }

        TypeDefinition definition = type.Resolve(interopReferences.RuntimeContext);

        // Static runtime classes have metadata names but no native interface identity
        if (definition.IsAbstract && definition.IsSealed)
        {
            iid = default;

            return false;
        }

        // Constructed interfaces, arrays, and boxed delegates reuse marshalling's parameterized IID generation
        if (type is GenericInstanceTypeSignature or SzArrayTypeSignature or BoxedTypeSignature)
        {
            iid = CreateIID(type, interopDefinitions, interopReferences, useWindowsUIXamlProjections);

            return true;
        }

        // Raw structs and enums need 'Nullable<T>' to represent 'IReference<T>', which uses the parameterized path
        if (definition.IsValueType)
        {
            iid = default;

            return false;
        }

        // Resolve remaining IIDs from well-known mappings, attributes, or component metadata,
        // treating missing required IID metadata as a generation error rather than an unsupported type.
        if (!SignatureGenerator.TryGetIIDFromWellKnownInterfaceIIDsOrAttribute(
            type: type,
            useWindowsUIXamlProjections: useWindowsUIXamlProjections,
            interopDefinitions: interopDefinitions,
            interopReferences: interopReferences,
            iid: out iid))
        {
            throw WellKnownInteropExceptions.TypeSignatureGenerationError(type);
        }

        return true;
    }

    /// <summary>
    /// Reuses signature-derived IIDs without retaining caches from completed invocations.
    /// </summary>
    /// <param name="signature">The Windows Runtime parameterized signature.</param>
    /// <param name="runtimeContext">The context for this invocation.</param>
    /// <returns>The IID associated with the signature.</returns>
    private static Guid GetOrCreateIID(string signature, RuntimeContext runtimeContext)
    {
        return IidCaches
            .GetOrAdd(runtimeContext, static _ => new ConcurrentDictionary<string, Guid>(StringComparer.Ordinal))
            .GetOrAdd(signature, static signature => CreateGuidFromSignature(signature));
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
