// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Cil;
using WindowsRuntime.InteropGenerator.Errors;

namespace WindowsRuntime.InteropGenerator.Resolvers;

/// <summary>
/// A resolver for IIDs of interface types.
/// </summary>
internal static class InterfaceIIDResolver
{
    /// <summary>
    /// Gets the IID of a given type from the generated <c>ABI.InterfaceIIDs</c> type in its declaring module.
    /// </summary>
    /// <param name="type">The input type to get the IID for.</param>
    /// <returns>The IID for <paramref name="type"/>.</returns>
    public static Guid GetIID(TypeDefinition type)
    {
        // The IID of projected types that have a fixed one (e.g. interfaces, delegates, etc.)
        // is always in an 'IID_<INTERFACE_NAME>' property in the 'ABI.InterfaceIIDs' type.
        TypeDefinition interfaceIIDsType = type.DeclaringModule!.GetType("ABI"u8, "InterfaceIIDs"u8);

        DefaultInterpolatedStringHandler handler = new(0, 0, null, stackalloc char[256]);

        // Prepare the name of the property accessor method that we need to analyze
        handler.AppendLiteral("get_IID_");
        handler.AppendLiteral(type.FullName);

        // Replace all '.' characters with '_' to match the generated naming convention
        handler.Text.AsSpanUnsafe().Replace('.', '_');

        int maxByteCount = Encoding.UTF8.GetMaxByteCount(handler.Text.Length);

        // Allocate a buffer for the UTF8-encoded method name
        byte[]? arrayFromPool = null;
        Span<byte> utf8Bytes = maxByteCount <= 256
            ? stackalloc byte[256]
            : (arrayFromPool = ArrayPool<byte>.Shared.Rent(maxByteCount));

        // Transcode the get property accessor method name to UTF8.
        // We do it here so we can avoid the 'Utf8String' allocations.
        int writtenBytes = Encoding.UTF8.GetBytes(handler.Text, utf8Bytes);

        // We won't need the interpolated handler anymore, so we can clear it
        handler.Clear();

        // This method should always exist, just like the 'InterfaceIID' type itself we got earlier
        MethodDefinition get_IIDMethod = interfaceIIDsType.GetMethod(utf8Bytes[..writtenBytes]);

        if (arrayFromPool is not null)
        {
            ArrayPool<byte>.Shared.Return(arrayFromPool);
        }

        foreach (CilInstruction instruction in get_IIDMethod.CilMethodBody!.Instructions)
        {
            // We only care about the first (and only) 'ldsflda' instruction.
            // This is the one that's reading the RVA data with the IID bytes.
            if (instruction.OpCode != CilOpCodes.Ldsflda)
            {
                continue;
            }

            // The 'ldsflda' instruction always has a 'FieldDefinition' operand
            FieldDefinition rvaField = (FieldDefinition)instruction.Operand!;

            // Validate that the target field does in fact have RVA data
            if (!rvaField.HasFieldRva)
            {
                break;
            }

            Span<byte> iidBytes = stackalloc byte[16];

            // Read the IID data from the RVA field (we expect to always be able to do this)
            if (!rvaField.FieldRva!.TryWriteExactly(iidBytes))
            {
                throw WellKnownInteropExceptions.TypeIIDInvalidDataError(type);
            }

            // Construct the actual IID. Endianness doesn't matter here, because the data
            // from RVA fields will always be written for little-endian architectures, and
            // we will never be running on big-endian architectures anyway.
            return new(iidBytes);
        }

        // We couldn't resolve the IID (this should never happen)
        throw WellKnownInteropExceptions.TypeIIDResolutionError(type);
    }
}
