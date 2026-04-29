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

        // Look for the 'ABI.InterfaceIIDs' type, which is where IIDs for interface types are generated.
        // This type is guaranteed to exist in the generated implementation .dll-s, by naming convention.
        foreach (TypeDefinition type in module.TopLevelTypes)
        {
            if (type.Namespace is Utf8String ns && ns.AsSpan().SequenceEqual("ABI"u8) &&
                type.Name is Utf8String name && name.AsSpan().SequenceEqual("InterfaceIIDs"u8))
            {
                interfaceIIDsType = type;

                break;
            }
        }

        // If we didn't find the type, the assembly is invalid (callers will throw as needed)
        if (interfaceIIDsType is null)
        {
            iid = Guid.Empty;

            return false;
        }

        DefaultInterpolatedStringHandler handler = new(0, 0, null, stackalloc char[256]);

        // All IID properties use the same prefix for the 'get' accessor names
        handler.AppendLiteral("get_IID_");
        handler.AppendLiteral(typeFullName);

        // Replace all '.' characters with '_' to match the generated naming convention
        handler.Text.AsSpanUnsafe().Replace('.', '_');

        int maxByteCount = Encoding.UTF8.GetMaxByteCount(handler.Text.Length);

        byte[]? arrayFromPool = null;
        Span<byte> utf8Bytes = maxByteCount <= 256
            ? stackalloc byte[256]
            : (arrayFromPool = ArrayPool<byte>.Shared.Rent(maxByteCount));

        // The metadata method names is encoded in UTF8, so transcode the formatted name first
        int writtenBytes = Encoding.UTF8.GetBytes(handler.Text, utf8Bytes);

        handler.Clear();

        // Try to find the method
        _ = interfaceIIDsType.TryGetMethod(utf8Bytes[..writtenBytes], out MethodDefinition? get_IIDMethod);

        // At this point we're done with the UTF8 buffer, so return it to the pool if we rented one
        if (arrayFromPool is not null)
        {
            ArrayPool<byte>.Shared.Return(arrayFromPool);
        }

        // If we failed to find a matching method with the name, we can't do anything else
        if (get_IIDMethod?.CilMethodBody is null)
        {
            iid = Guid.Empty;

            return false;
        }

        // Each method is returning a 'Guid' value from the .rdata section, and the instructions
        // will contain an 'ldsflda' opcode loading from a constant data field. So we need to
        // look for that, access the target RVA field, and extract the underlying IID data.
        foreach (CilInstruction instruction in get_IIDMethod.CilMethodBody.Instructions)
        {
            // The current instruction isn't 'ldsflda', so ignore it
            if (instruction.OpCode != CilOpCodes.Ldsflda)
            {
                continue;
            }

            FieldDefinition rvaField = (FieldDefinition)instruction.Operand!;

            // If the target field isn't an RVA field, the accessor is (somehow) invalid
            if (!rvaField.HasFieldRva)
            {
                break;
            }

            // Make sure the target RVA field is a segment we can extract data from
            if (rvaField.FieldRva is not IReadableSegment readableSegment)
            {
                break;
            }

            BinaryStreamReader reader = readableSegment.CreateReader(readableSegment.Offset, 16);

            Span<byte> iidBytes = stackalloc byte[16];

            // Read the RVA data into our span, so we can create a 'Guid' from it
            _ = reader.ReadBytes(iidBytes);

            // Note: we don't need to fixup the endianness, the IID data is already correct
            iid = new(iidBytes);

            return true;
        }

        iid = Guid.Empty;

        return false;
    }
}
