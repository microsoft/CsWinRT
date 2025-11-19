// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Helpers;

internal static class GuidGenerator
{
    private static readonly Guid wrt_pinterface_namespace = new(0xd57af411, 0x737b, 0xc042, 0xab, 0xae, 0x87, 0x8b, 0x1e, 0x16, 0xad, 0xee);

    // TODO: Debug code; Will remove later
#pragma warning disable IDE0044 // Add readonly modifier
    private static readonly string printPath = @"C:\Users\kythant\Documents\staging\GUIDsFromCSWinRTGen.txt";
    private static HashSet<TypeSignature> generatedIIDs = [];
    private static StreamWriter writer = new(printPath, append: false);
#pragma warning restore IDE0044 // Add readonly modifier

    public static Guid CreateIID(TypeSignature type, InteropReferences interopReferences, bool useWindowsUIXamlProjections)
    {
        string signature = GetSignature(type, interopReferences, useWindowsUIXamlProjections);
        Guid guid = CreateGuidFromSignature(signature);
        //TODO: Debug code; Will remove later
        if (!generatedIIDs.Contains(type))
        {
            writer.WriteLine(type.FullName + "\n    " + signature + "\n    " + guid);
            _ = generatedIIDs.Add(type);
        }
        return guid;
    }

    /// <summary>
    /// Generates the WinRT signature string for the specified type.
    /// </summary>
    /// <param name="typeSignature">
    /// The CLR/AsmResolver type signature to translate.
    /// </param>
    /// <param name="interopReferences">
    /// Interop metadata used to resolve GUIDs for interfaces, delegates, and generics.
    /// </param>
    /// <param name="useWindowsUIXamlProjections">True to apply Windows.UI.Xaml projection mappings if available.</param>
    /// <returns>
    /// A WinRT signature string representing the given type.
    /// </returns>
    private static string GetSignature(
            TypeSignature typeSignature,
            InteropReferences interopReferences,
            bool useWindowsUIXamlProjections)
    {
        string? mappedSignature = TypeMapping.FindGuidSignatureForMappedType(typeSignature.Namespace, typeSignature.Name, useWindowsUIXamlProjections);

        if (mappedSignature is not null)
        {
            return mappedSignature;
        }

        TypeDefinition? typeDefinition = typeSignature.Resolve()! ?? throw new ArgumentException("TypeDefinition could not be resolved for type signature: " + typeSignature.FullName);
        string typeFullNameMapped = (typeDefinition.Namespace is null || typeDefinition.Name is null) ? typeDefinition.FullName : TypeMapping.FindMappedWinRTFullName(typeDefinition.Namespace, typeDefinition.Name, useWindowsUIXamlProjections);

#pragma warning disable IDE0010 // Add missing cases
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
                return "struct(Windows.UI.Xaml.Interop.222TypeName;string;enum(Windows.UI.Xaml.Interop.TypeKind;i4))";
            case ElementType.ValueType:
                if (typeDefinition.IsClass)
                {
                    // Enum case
                    if (typeDefinition.IsEnum)
                    {
                        bool isFlags = typeDefinition.HasCustomAttribute("System", "FlagsAttribute");
                        return "enum(" + typeFullNameMapped + ";" + (isFlags ? "u4" : "i4") + ")";
                    }

                    // Guid Case
                    if (typeSignature.IsGuidType(interopReferences))
                    {
                        return "g16";
                    }

                    // Struct case
                    IList<FieldDefinition> fieldDefinition = typeDefinition.Fields;
                    List<string> typeArgumentSignatures = [];

                    for (int i = 0; i < fieldDefinition.Count; i++)
                    {
                        if (!fieldDefinition[i].IsStatic)
                        {
                            FieldSignature? fieldSignature = fieldDefinition[i].Signature ?? throw new ArgumentException("FieldSignature is missing");
                            typeArgumentSignatures.Add(GetSignature(fieldSignature.FieldType, interopReferences, useWindowsUIXamlProjections));
                        }
                    }
                    return "struct(" + typeFullNameMapped + ";" + string.Join(";", typeArgumentSignatures) + ")";
                }

                throw new ArgumentException("Invalid ElementType.ValueType");

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

                    return "pinterface({" + GetGuid(typeSignature, interopReferences) + "};" + string.Join(";", typeArgumentSignatures) + ")";
                }

                throw new ArgumentException("Invalid ElementType.GenericInst");

            case ElementType.Class:
                if (typeDefinition.IsClass)
                {
                    return typeDefinition.IsDelegate
                        ? "delegate({" + GetGuid(typeSignature, interopReferences) + "})" // Delegate case
                        : GetDefaultInterfaceSignatureFromAttribute(typeDefinition, interopReferences, out TypeSignature defaultInterfaceSig)
                            ? "rc(" + typeFullNameMapped + ";" + GetSignature(defaultInterfaceSig, interopReferences, useWindowsUIXamlProjections) + ")" // Class case with default interface
                            : "{" + GetGuid(typeSignature, interopReferences) + "}"; // Class case without default interface
                }
                if (typeDefinition.IsInterface) // interface case
                {
                    return "{" + GetGuid(typeSignature, interopReferences) + "}";
                }

                throw new ArgumentException("Invalid ElementType.Class");

            case ElementType.SzArray:
                SzArrayTypeSignature arrayTypeSignature = (SzArrayTypeSignature)typeSignature;

                if (arrayTypeSignature != null)
                {
                    return "pinterface({61c17707-2d65-11e0-9ae8-d48564015472};" + GetSignature(arrayTypeSignature.BaseType, interopReferences, useWindowsUIXamlProjections) + ")";
                }

                throw new ArgumentException("Invalid ElementType.SzArray");

            default:
                // TODO: throw new ArgumentException("Unsupported ElementType: " + typeSignature.ElementType + " : " + typeSignature.FullName);
                return typeSignature.FullName;
        }
#pragma warning restore IDE0010 // Add missing cases
    }

    /// <summary>
    /// Resolves a GUID for the specified type signature by checking well-known WinRT interfaces
    /// and, if necessary, the type's <c>System.Runtime.InteropServices.GuidAttribute</c>.
    /// </summary>
    /// <param name="typeSig">
    /// The <see cref="AsmResolver.DotNet.Signatures.TypeSignature"/> to resolve to a GUID.
    /// </param>
    /// <param name="interopReferences">
    /// Context used to resolve well-known WinRT interface IIDs and related interop metadata.
    /// </param>
    /// <returns>
    /// The resolved <see cref="Guid"/> if found.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when the type has no GUID.</exception>
    private static Guid GetGuid(TypeSignature typeSig, InteropReferences interopReferences)
    {
        Guid result = WellKnownInterfaceIIDs.get_GUID(typeSig, true, interopReferences);

        if (result != Guid.Empty)
        {
            return result;
        }

        TypeDefinition? typeDef = typeSig.Resolve();
        if (typeDef is not null)
        {
            if (GetGuidFromAttribute(typeDef, interopReferences, out result))
            {
                return result;
            }
        }

        return Guid.Empty; // TODO: don'turn empty guid but throw instead like below. Currently, not all the types we need to be filtered are not filtered out such as System.Reflection.* types.
        //throw new ArgumentException("Type does not have a Guid attribute");
    }

    private static bool GetGuidFromAttribute(TypeDefinition typeDef, InteropReferences interopReferences, out Guid guid)
    {
        guid = Guid.Empty;
        if (typeDef.TryGetCustomAttribute(interopReferences.GuidAttribute, out CustomAttribute? customAttribute))
        {
            if (customAttribute.Signature is { FixedArguments: [{ Element: AsmResolver.Utf8String guidString }, ..] })
            {
                return Guid.TryParse(guidString.Value, out guid);
            }
        }
        return false;
    }

    private static bool GetDefaultInterfaceSignatureFromAttribute(TypeDefinition typeDef, InteropReferences interopReferences, [NotNullWhen(true)] out TypeSignature defaultInterfaceSig)
    {
        defaultInterfaceSig = null!;
        if (typeDef.TryGetCustomAttribute(interopReferences.WindowsRuntimeDefaultInterfaceAttribute, out CustomAttribute? customAttribute))
        {
            if (customAttribute.Signature is { FixedArguments: [{ Element: TypeSignature signature }, ..] })
            {
                defaultInterfaceSig = signature;
                return true;
            }
        }
        return false;
    }

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
            // swap bytes of int a
            (buffer[3], buffer[0]) = (buffer[0], buffer[3]);
            (buffer[2], buffer[1]) = (buffer[1], buffer[2]);

            // swap bytes of short b
            (buffer[5], buffer[4]) = (buffer[4], buffer[5]);

            // swap bytes of short c and encode RFC time/version field
            (buffer[7], buffer[6]) = ((byte)((buffer[6] & 0x0F) | (5 << 4)), buffer[7]);

            // encode RFC clock/reserved field
            buffer[8] = (byte)((buffer[8] & 0x3F) | 0x80);
        }

        return new Guid(buffer);
    }

    internal static Guid CreateGuidFromSignature(string signature)
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

        _ = wrt_pinterface_namespace.TryWriteBytes(utf8Bytes);

        int encodedUtf8BytesWritten = Encoding.UTF8.GetBytes(signature, utf8Bytes[16..]);
        Span<byte> sha1Bytes = stackalloc byte[20]; // 'SHA1.HashSizeInBytes' does not exist on .NET 6, update this when that's dropped

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
