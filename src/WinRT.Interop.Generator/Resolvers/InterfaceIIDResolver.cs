// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Unicode;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using WindowsRuntime.InteropGenerator.Errors;

namespace WindowsRuntime.InteropGenerator.Resolvers;

/// <summary>
/// A resolver for IIDs of interface types.
/// </summary>
internal static class InterfaceIIDResolver
{
    public static Guid GetIID(TypeDefinition type)
    {
        TypeDefinition interfaceIIDsType = type.DeclaringModule!.GetType("ABI"u8, "InterfaceIIDs"u8);

        DefaultInterpolatedStringHandler handler = new(0, 0, null, stackalloc char[256]);

        handler.AppendLiteral("get_IID_");
        handler.AppendLiteral(type.FullName);

        handler.Text.AsSpanUnsafe().Replace('.', '_');

        int maxByteCount = Encoding.UTF8.GetMaxByteCount(handler.Text.Length);

        byte[]? arrayFromPool = null;
        Span<byte> utf8Bytes = maxByteCount <= 256
            ? stackalloc byte[256]
            : (arrayFromPool = ArrayPool<byte>.Shared.Rent(maxByteCount));

        int writtenBytes = Encoding.UTF8.GetBytes(handler.Text, utf8Bytes);

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

            byte[] iidBytes = rvaField.FieldRva!.WriteIntoArray();

            return new(iidBytes);
        }

        // We couldn't resolve the IID (this should never happen)
        throw WellKnownInteropExceptions.TypeIIDResolutionError(type);
    }
}
