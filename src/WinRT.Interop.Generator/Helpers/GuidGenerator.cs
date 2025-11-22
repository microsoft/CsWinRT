// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.References;

#pragma warning disable IDE0010

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
        string signature = GetSignature(type, interopReferences, useWindowsUIXamlProjections);
        Guid guid = CreateGuidFromSignature(signature);

        return guid;
    }

    /// <summary>
    /// Generates the Windows Runtime signature for the specified type.
    /// </summary>
    /// <param name="type">The <see cref="TypeSignature"/> to generate the signature for.</param>
    /// <param name="interopReferences"> The <see cref="InteropReferences"/> instance to use. </param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    /// <returns>The resulting signature for <paramref name="type"/>.</returns>
    private static string GetSignature(
        TypeSignature type,
        InteropReferences interopReferences,
        bool useWindowsUIXamlProjections)
    {
        if (TypeMapping.TryFindMappedTypeSignature(type.FullName, useWindowsUIXamlProjections, out string? mappedSignature))
        {
            return mappedSignature;
        }

        TypeDefinition? typeDefinition = type.Resolve()! ?? throw new ArgumentException("TypeDefinition could not be resolved for type signature: " + type.FullName);
        string typeFullName = TypeMapping.TryFindMappedTypeName(typeDefinition.FullName, useWindowsUIXamlProjections, out string? typeFullNameMapped) ? typeFullNameMapped : typeDefinition.FullName;

        switch (type.ElementType)
        {
            // value types
            case ElementType.I1:
                return "i1";
            case ElementType.U1:
                return "u1";
            case ElementType.I2:
                return "i2";
            case ElementType.U2:
                return "u2";
            case ElementType.I4:
                return "i4";
            case ElementType.U4:
                return "u4";
            case ElementType.I8:
                return "i8";
            case ElementType.U8:
                return "u8";
            case ElementType.R4:
                return "f4";
            case ElementType.R8:
                return "f8";
            case ElementType.Boolean:
                return "b1";
            case ElementType.Char:
                return "c2";
            case ElementType.Object:
                return "cinterface(IInspectable)";
            case ElementType.String:
                return "string";
            case ElementType.Type:
                return "struct(Windows.UI.Xaml.Interop.TypeName;string;enum(Windows.UI.Xaml.Interop.TypeKind;i4))";

            case ElementType.GenericInst:
                GenericInstanceTypeSignature genericTypeSignature = (GenericInstanceTypeSignature)type;
                TypeDefinition? genericTypeDefinition = genericTypeSignature.GenericType.Resolve();

                if (genericTypeDefinition is not null)
                {
                    IList<TypeSignature> typeArugmentList = genericTypeSignature.TypeArguments;
                    String[] typeArgumentSignatures = new String[typeArugmentList.Count];

                    for (int i = 0; i < typeArugmentList.Count; i++)
                    {
                        typeArgumentSignatures[i] = GetSignature(typeArugmentList[i], interopReferences, useWindowsUIXamlProjections);
                    }

                    return "pinterface({" + GetGuidFromWellKnownInterfaceIIDsOrAttribute(genericTypeSignature.GenericType, interopReferences) + "};" + string.Join(";", typeArgumentSignatures) + ")";
                }
                throw new ArgumentException("Invalid ElementType.GenericInst");

            case ElementType.ValueType when typeDefinition.IsClass && typeDefinition.IsEnum:
                bool isFlags = typeDefinition.HasCustomAttribute("System", "FlagsAttribute");
                return "enum(" + typeFullName + ";" + (isFlags ? "u4" : "i4") + ")";

            case ElementType.ValueType when typeDefinition.IsClass && type.IsGuidType(interopReferences):
                return "g16";

            case ElementType.ValueType when typeDefinition.IsClass: // Struct case
                IList<FieldDefinition> fieldDefinition = typeDefinition.Fields;
                List<string> enumFieldSignatures = [];

                for (int i = 0; i < fieldDefinition.Count; i++)
                {
                    if (!fieldDefinition[i].IsStatic)
                    {
                        FieldSignature? fieldSignature = fieldDefinition[i].Signature ?? throw new ArgumentException("FieldSignature is missing");
                        enumFieldSignatures.Add(GetSignature(fieldSignature.FieldType, interopReferences, useWindowsUIXamlProjections));
                    }
                }

                return "struct(" + typeFullName + ";" + string.Join(";", enumFieldSignatures) + ")";

            case ElementType.Class when typeDefinition.IsClass && typeDefinition.IsDelegate: // delegate case
                return "delegate({" + GetGuidFromWellKnownInterfaceIIDsOrAttribute(typeDefinition, interopReferences) + "})";

            case ElementType.Class when typeDefinition.IsClass: // class case
                return TryGetDefaultInterfaceSignatureFromAttribute(typeDefinition, interopReferences, out TypeSignature? defaultInterfaceSig) ?
                    "rc(" + typeFullName + ";" + GetSignature(defaultInterfaceSig, interopReferences, useWindowsUIXamlProjections) + ")" : // Class case with default interface
                    "{" + GetGuidFromWellKnownInterfaceIIDsOrAttribute(typeDefinition, interopReferences) + "}"; // Class case without default interface

            case ElementType.Class when typeDefinition.IsInterface: // interface case without generic parameters
                return "{" + GetGuidFromWellKnownInterfaceIIDsOrAttribute(typeDefinition, interopReferences) + "}";

            case ElementType.SzArray:
                SzArrayTypeSignature arrayTypeSignature = (SzArrayTypeSignature)type;

                if (arrayTypeSignature != null)
                {
                    return "pinterface({61c17707-2d65-11e0-9ae8-d48564015472};" + GetSignature(arrayTypeSignature.BaseType, interopReferences, useWindowsUIXamlProjections) + ")";
                }

                throw new ArgumentException("Invalid ElementType.SzArray");

            default:
                // TODO: when all the type signatures are coming in are all properly filter out, for example: System.Void* or System.Reflection.* types,
                // we can uncomment the below line to throw exception for unsupported types.
                // throw new ArgumentException("Unsupported ElementType: " + typeSignature.ElementType + " : " + typeSignature.FullName);
                return type.FullName;
        }
    }

    /// <summary>
    /// Resolves a GUID for the specified type signature by checking well-known WinRT interfaces
    /// and, if necessary, the type's <c>System.Runtime.InteropServices.GuidAttribute</c>.
    /// </summary>
    /// <param name="type">
    /// The <see cref="ITypeDefOrRef"/> to resolve to a GUID.
    /// </param>
    /// <param name="interopReferences">
    /// The <see cref="InteropReferences"/> instance to use.
    /// </param>
    /// <returns>
    /// The resolved <see cref="Guid"/> if found.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when the type has no GUID.</exception>
    private static Guid GetGuidFromWellKnownInterfaceIIDsOrAttribute(ITypeDescriptor type, InteropReferences interopReferences)
    {
        // First try to get the IID from the custom-mapped types mapping
        if (WellKnownInterfaceIIDs.TryGetGUID(type, interopReferences, out Guid result))
        {
            return result;
        }

        // If the type was a normal projected type, then try to resolve the IID from the '[Guid]' attribute
        if (type.Resolve() is TypeDefinition typeDefinition && TryGetGuidFromAttribute(typeDefinition, interopReferences, out result))
        {
            return result;
        }

        return Guid.Empty; // TODO: don'turn empty guid but throw instead like below. Currently, not all the types we need to be filtered are not filtered out such as System.Reflection.* types.
        //throw new ArgumentException("Type does not have a Guid attribute");
    }

    /// <summary>
    /// Attempts to retrieve the IID from the <see cref="System.Runtime.InteropServices.GuidAttribute"/> applied to the specified type.
    /// </summary>
    /// <param name="type">The type definition to inspect for <see cref="System.Runtime.InteropServices.GuidAttribute"/>.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="guid">The resulting <see cref="Guid"/> value, if found.</param>
    /// <returns>Whether <paramref name="guid"/> was succesfully retrieved.</returns>
    private static bool TryGetGuidFromAttribute(TypeDefinition type, InteropReferences interopReferences, out Guid guid)
    {
        if (type.TryGetCustomAttribute(interopReferences.GuidAttribute, out CustomAttribute? customAttribute))
        {
            if (customAttribute.Signature is { FixedArguments: [{ Element: Utf8String guidString }, ..] })
            {
                return Guid.TryParse(guidString.Value, out guid);
            }
        }

        guid = Guid.Empty;

        return false;
    }

    /// <summary>
    /// Attempts to retrieve the default interface signature from the <c>[WindowsRuntimeDefaultInterface]</c>
    /// attribute applied to the specified type, which is assumed to be some projected Windows Runtime class.
    /// </summary>
    /// <param name="type">The type definition to inspect for the default interface attribute.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="defaultInterface">The <see cref="TypeSignature"/> instance for the default interface for <paramref name="type"/>, if found.</param>
    /// <returns>Whether <paramref name="defaultInterface"/> was successfully retrieved.</returns>
    private static bool TryGetDefaultInterfaceSignatureFromAttribute(
        TypeDefinition type,
        InteropReferences interopReferences,
        [NotNullWhen(true)] out TypeSignature? defaultInterface)
    {
        if (type.TryGetCustomAttribute(interopReferences.WindowsRuntimeDefaultInterfaceAttribute, out CustomAttribute? customAttribute))
        {
            if (customAttribute.Signature is { FixedArguments: [{ Element: TypeSignature signature }, ..] })
            {
                defaultInterface = signature;

                return true;
            }
        }

        defaultInterface = null;

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
