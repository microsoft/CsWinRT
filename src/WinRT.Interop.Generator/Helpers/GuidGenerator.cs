// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Helpers;

#pragma warning disable IDE0010 // Add missing cases

internal static class GuidGenerator
{
    private static readonly Guid WindowsRuntimePIIDNamespace = new(0xD57AF411, 0x737B, 0xC042, 0xAB, 0xAE, 0x87, 0x8B, 0x1E, 0x16, 0xAD, 0xEE);

    // TODO: Debug code; Will remove later
    //#pragma warning disable IDE0044 // Add readonly modifier
    //    private static readonly string printPath = @"C:\Users\kythant\Documents\staging\GUIDsFromCSWinRTGen.txt";
    //    private static HashSet<TypeSignature> generatedIIDs = [];
    //    private static StreamWriter writer = new(printPath, append: false);
    //#pragma warning restore IDE0044 // Add readonly modifier

    /// <summary>
    /// Generates the IID for the specified type by computing its WinRT signature and deriving a GUID from that signature.
    /// </summary>
    /// <param name="type">The <see cref="AsmResolver.DotNet.Signatures.TypeSignature"/> to generate an IID for.</param>
    /// <param name="interopReferences">Interop metadata used to resolve type mappings and GUIDs during signature generation.</param>
    /// <param name="useWindowsUIXamlProjections">When <c>true</c>, applies Windows.UI.Xaml projection mappings while producing the signature.</param>
    /// <returns>The <see cref="Guid"/> IID derived from the computed WinRT signature.</returns>
    public static Guid CreateIID(TypeSignature type, InteropReferences interopReferences, bool useWindowsUIXamlProjections)
    {
        string signature = GetSignature(type, interopReferences, useWindowsUIXamlProjections);
        Guid guid = CreateGuidFromSignature(signature);
        //TODO: Debug code; Will remove later
        //if (!generatedIIDs.Contains(type))
        //{
        //    writer.WriteLine(type.FullName + "\n    " + signature + "\n    " + guid);
        //    _ = generatedIIDs.Add(type);
        //}
        return guid;
    }

    /// <summary>
    /// Generates the WinRT signature string for the specified type.
    /// </summary>
    /// <param name="typeSignature"> The CLR/AsmResolver type signature to translate. </param>
    /// <param name="interopReferences"> The <see cref="InteropReferences"/> instance to use. </param>
    /// <param name="useWindowsUIXamlProjections">True to apply Windows.UI.Xaml projection mappings if available.</param>
    /// <returns>A WinRT signature string representing the given type.</returns>
    private static string GetSignature(
        TypeSignature typeSignature,
        InteropReferences interopReferences,
        bool useWindowsUIXamlProjections)
    {
        if (TypeMapping.TryFindGuidSignatureForMappedType(typeSignature.FullName, useWindowsUIXamlProjections, out string? mappedSignature))
        {
            return mappedSignature;
        }

        TypeDefinition? typeDefinition = typeSignature.Resolve()! ?? throw new ArgumentException("TypeDefinition could not be resolved for type signature: " + typeSignature.FullName);
        string typeFullName = TypeMapping.TryFindMappedWinRTFullName(typeDefinition.FullName, useWindowsUIXamlProjections, out string? typeFullNameMapped) ? typeFullNameMapped : typeDefinition.FullName;

        switch (typeSignature.ElementType)
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
                GenericInstanceTypeSignature genericTypeSignature = (GenericInstanceTypeSignature)typeSignature;
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

            case ElementType.ValueType when typeDefinition.IsClass && typeSignature.IsGuidType(interopReferences):
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
                return TryGetDefaultInterfaceSignatureFromAttribute(typeDefinition, interopReferences, out TypeSignature defaultInterfaceSig) ?
                    "rc(" + typeFullName + ";" + GetSignature(defaultInterfaceSig, interopReferences, useWindowsUIXamlProjections) + ")" : // Class case with default interface
                    "{" + GetGuidFromWellKnownInterfaceIIDsOrAttribute(typeDefinition, interopReferences) + "}"; // Class case without default interface

            case ElementType.Class when typeDefinition.IsInterface: // interface case without generic parameters
                return "{" + GetGuidFromWellKnownInterfaceIIDsOrAttribute(typeDefinition, interopReferences) + "}";

            case ElementType.SzArray:
                SzArrayTypeSignature arrayTypeSignature = (SzArrayTypeSignature)typeSignature;

                if (arrayTypeSignature != null)
                {
                    return "pinterface({61c17707-2d65-11e0-9ae8-d48564015472};" + GetSignature(arrayTypeSignature.BaseType, interopReferences, useWindowsUIXamlProjections) + ")";
                }

                throw new ArgumentException("Invalid ElementType.SzArray");

            default:
                // TODO: when all the type signatures are coming in are all properly filter out, for example: System.Void* or System.Reflection.* types,
                // we can uncomment the below line to throw exception for unsupported types.
                // throw new ArgumentException("Unsupported ElementType: " + typeSignature.ElementType + " : " + typeSignature.FullName);
                return typeSignature.FullName;
        }
    }

    /// <summary>
    /// Resolves a GUID for the specified type signature by checking well-known WinRT interfaces
    /// and, if necessary, the type's <c>System.Runtime.InteropServices.GuidAttribute</c>.
    /// </summary>
    /// <param name="typeDefOrRef">
    /// The <see cref="AsmResolver.DotNet.ITypeDefOrRef"/> to resolve to a GUID.
    /// </param>
    /// <param name="interopReferences">
    /// The <see cref="InteropReferences"/> instance to use.
    /// </param>
    /// <returns>
    /// The resolved <see cref="Guid"/> if found.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when the type has no GUID.</exception>
    private static Guid GetGuidFromWellKnownInterfaceIIDsOrAttribute(ITypeDefOrRef typeDefOrRef, InteropReferences interopReferences)
    {
        if (WellKnownInterfaceIIDs.TryGetGUID(typeDefOrRef, interopReferences, out Guid result))
        {
            return result;
        }

        TypeDefinition? typeDef = typeDefOrRef.Resolve();
        if (typeDef is not null)
        {
            if (TryGetGuidFromAttribute(typeDef, interopReferences, out result))
            {
                return result;
            }
        }

        return Guid.Empty; // TODO: don'turn empty guid but throw instead like below. Currently, not all the types we need to be filtered are not filtered out such as System.Reflection.* types.
        //throw new ArgumentException("Type does not have a Guid attribute");
    }

    /// <summary>
    /// Attempts to retrieve a GUID from the <c>GuidAttribute</c> applied to the specified type.
    /// </summary>
    /// <param name="typeDef">The type definition to inspect for the GUID attribute.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="guid">When this method returns <c>true</c>, contains the parsed GUID value.</param>
    /// <returns><c>true</c> if a valid GUID was found and parsed; otherwise, <c>false</c>.</returns>
    private static bool TryGetGuidFromAttribute(TypeDefinition typeDef, InteropReferences interopReferences, out Guid guid)
    {
        if (typeDef.TryGetCustomAttribute(interopReferences.GuidAttribute, out CustomAttribute? customAttribute))
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
    /// Attempts to retrieve the default interface signature from the <c>WindowsRuntimeDefaultInterfaceAttribute</c>
    /// applied to the specified type.
    /// </summary>
    /// <param name="typeDef">The type definition to inspect for the default interface attribute.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="defaultInterfaceSig">When this method returns <c>true</c>, contains the default interface type signature.</param>
    /// <returns><c>true</c> if the default interface signature was found; otherwise, <c>false</c>.</returns>
    private static bool TryGetDefaultInterfaceSignatureFromAttribute(TypeDefinition typeDef, InteropReferences interopReferences, [NotNullWhen(true)] out TypeSignature defaultInterfaceSig)
    {
        if (typeDef.TryGetCustomAttribute(interopReferences.WindowsRuntimeDefaultInterfaceAttribute, out CustomAttribute? customAttribute))
        {
            if (customAttribute.Signature is { FixedArguments: [{ Element: TypeSignature signature }, ..] })
            {
                defaultInterfaceSig = signature;
                return true;
            }
        }
        defaultInterfaceSig = null!;
        return false;
    }

    /// <summary>
    /// Encodes a GUID from a 16-byte sequence following RFC 4122 rules.
    /// Adjusts byte order for little-endian systems and sets version and variant bits.
    /// </summary>
    /// <param name="data">A read-only span of bytes representing the GUID data. Must be at least 16 bytes.</param>
    /// <returns>A <see cref="Guid"/> constructed from the encoded byte sequence.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="data"/> contains fewer than 16 bytes.</exception>
    private static Guid EncodeGuid(ReadOnlySpan<byte> data)
    {
        if (data.Length < 16)
        {
            throw new ArgumentException("Data must be at least 16 bytes.", nameof(data));
        }

        Span<byte> buffer = stackalloc byte[16];
        data[..16].CopyTo(buffer);

        if (BitConverter.IsLittleEndian)
        {
            // Swap bytes of int a
            (buffer[3], buffer[0]) = (buffer[0], buffer[3]);
            (buffer[2], buffer[1]) = (buffer[1], buffer[2]);

            // Swap bytes of short b
            (buffer[5], buffer[4]) = (buffer[4], buffer[5]);

            // Swap bytes of short c and encode RFC time/version field
            (buffer[7], buffer[6]) = ((byte)((buffer[6] & 0x0F) | (5 << 4)), buffer[7]);

            // Encode RFC clock/reserved field
            buffer[8] = (byte)((buffer[8] & 0x3F) | 0x80);
        }

        return new Guid(buffer);
    }

    /// <summary>Computes a deterministic GUID (IID) from a WinRT signature</summary>
    /// <param name="signature">WinRT signature string.</param>
    /// <returns>The derived <see cref="Guid"/>.</returns>
    private static Guid CreateGuidFromSignature(string signature)
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
