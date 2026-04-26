// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.IO;
using AsmResolver.PE.DotNet.Cil;
namespace WindowsRuntime.InteropGenerator.Resolvers;

/// <summary>
/// A resolver for IIDs of interface types from generated <c>ABI.InterfaceIIDs</c> types.
/// </summary>
internal static class InterfaceIIDResolver
{
    /// <summary>
    /// Tries to get the IID of a given type from the <c>ABI.InterfaceIIDs</c> type in a specified module.
    /// </summary>
    /// <param name="module">The module containing the <c>ABI.InterfaceIIDs</c> type.</param>
    /// <param name="typeFullName">The full name of the type to get the IID for (e.g. <c>AuthoringTest.IDouble</c>).</param>
    /// <param name="iid">The resulting IID, if found.</param>
    /// <returns>Whether the IID was successfully resolved.</returns>
    public static bool TryGetIID(ModuleDefinition module, string typeFullName, out Guid iid)
    {
        TypeDefinition? interfaceIIDsType = null;

        foreach (TypeDefinition type in module.TopLevelTypes)
        {
            if (type.Namespace is Utf8String ns && ns.AsSpan().SequenceEqual("ABI"u8) &&
                type.Name is Utf8String name && name.AsSpan().SequenceEqual("InterfaceIIDs"u8))
            {
                interfaceIIDsType = type;
                break;
            }
        }

        if (interfaceIIDsType is null)
        {
            iid = Guid.Empty;
            return false;
        }

        DefaultInterpolatedStringHandler handler = new(0, 0, null, stackalloc char[256]);

        handler.AppendLiteral("get_IID_");
        handler.AppendLiteral(typeFullName);

        // Replace all '.' characters with '_' to match the generated naming convention
        handler.Text.AsSpanUnsafe().Replace('.', '_');

        int maxByteCount = Encoding.UTF8.GetMaxByteCount(handler.Text.Length);

        byte[]? arrayFromPool = null;
        Span<byte> utf8Bytes = maxByteCount <= 256
            ? stackalloc byte[256]
            : (arrayFromPool = ArrayPool<byte>.Shared.Rent(maxByteCount));

        int writtenBytes = Encoding.UTF8.GetBytes(handler.Text, utf8Bytes);

        handler.Clear();

        // Try to find the method
        MethodDefinition? get_IIDMethod = null;

        foreach (MethodDefinition method in interfaceIIDsType.Methods)
        {
            if (method.Name is Utf8String methodName && methodName.AsSpan().SequenceEqual(utf8Bytes[..writtenBytes]))
            {
                get_IIDMethod = method;
                break;
            }
        }

        if (arrayFromPool is not null)
        {
            ArrayPool<byte>.Shared.Return(arrayFromPool);
        }

        if (get_IIDMethod?.CilMethodBody is null)
        {
            iid = Guid.Empty;
            return false;
        }

        foreach (CilInstruction instruction in get_IIDMethod.CilMethodBody.Instructions)
        {
            if (instruction.OpCode != CilOpCodes.Ldsflda)
            {
                continue;
            }

            FieldDefinition rvaField = (FieldDefinition)instruction.Operand!;

            if (!rvaField.HasFieldRva)
            {
                break;
            }

            Span<byte> iidBytes = stackalloc byte[16];

            if (rvaField.FieldRva is not IReadableSegment readableSegment)
            {
                break;
            }

            BinaryStreamReader reader = readableSegment.CreateReader(readableSegment.Offset, 16);
            _ = reader.ReadBytes(iidBytes);

            iid = new(iidBytes);
            return true;
        }

        iid = Guid.Empty;
        return false;
    }
}
