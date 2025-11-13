// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Helpers;

internal static class GuidGenerator
{
    internal static string GetSignature(
            TypeSignature typeSignature,
            InteropReferences interopReferences)
    {
        AsmResolver.DotNet.TypeDefinition? typeDefinition = typeSignature.Resolve()!;
#pragma warning disable IDE0010 // Maybe temporary until all cases are handled
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
                return "Boolean";
            case ElementType.Char:
                return "Char";

            //case ElementType.GUID:
            //    return "u8";

            case ElementType.Object:
                return "cinterface(IInspectable)";
            case ElementType.String:
                return "string";
            case ElementType.Type:
                return "struct(Windows.UI.Xaml.Interop.222TypeName;string;enum(Windows.UI.Xaml.Interop.TypeKind;i4))";
            case ElementType.ValueType:
                if (typeDefinition is not null)
                {
                    if (typeDefinition.IsEnum)
                    {
                        bool isFlags = typeDefinition.HasCustomAttribute("System", "FlagsAttribute");
                        return "enum(" + typeDefinition.FullName + ";" + (isFlags ? "u4" : "i4") + ")";
                    }
                    if (typeDefinition.IsClass) // struct case
                    {
                        IList<FieldDefinition> fieldDefinition = typeDefinition.Fields; // IList<TypeSignature>
                        String[] typeArgumentSignatures = new String[fieldDefinition.Count];
                        for (int i = 0; i < fieldDefinition.Count; i++)
                        {
                            FieldSignature? fieldSignature = fieldDefinition[i].Signature;
                            typeArgumentSignatures[i] = fieldSignature != null
                                ? GetSignature(fieldSignature.FieldType, interopReferences)
                                : throw new ArgumentException("FieldSignature is missing");
                        }
                        return "struct(" + typeDefinition.FullName + ";" + string.Join(";", typeArgumentSignatures) + ")";
                    }
                }
                throw new ArgumentException("TypeSignature with ValueType is neither Enum or Struct");
            case ElementType.GenericInst:
                GenericInstanceTypeSignature genericTypeSignature = (GenericInstanceTypeSignature)typeSignature;
                AsmResolver.DotNet.TypeDefinition? genericTypeDefinition = genericTypeSignature.GenericType.Resolve();
                if (genericTypeDefinition is not null)
                {
                    if (genericTypeDefinition.IsDelegate)
                    {
                        return "delegate({" + GetGuid(typeSignature, interopReferences) + "})";
                    }
                    if (genericTypeDefinition.IsClass)
                    {
                        if (genericTypeDefinition.TryGetCustomAttribute("WindowsRuntime", "WindowsRuntimeDefaultInterfaceAttribute", out CustomAttribute? customAttribute))
                        {
                            CustomAttributeSignature? customAttributeSignature = customAttribute.Signature;
                            if (customAttributeSignature is not null && customAttributeSignature.FixedArguments.Count != 0)
                            {
                                object? element = customAttributeSignature.FixedArguments[0].Element;
                                if (element is TypeSignature defaultInterfaceSig)
                                {
                                    return "rc(" + typeSignature.FullName + ";" + GetSignature(defaultInterfaceSig, interopReferences) + ")";
                                }
                            }
                        }
                        return "{" + GetGuid(typeSignature, interopReferences) + "}";
                    }
                    if (genericTypeDefinition.IsInterface)
                    {
                        IList<TypeSignature> typeArugmentList = genericTypeSignature.TypeArguments; // IList<TypeSignature>
                        String[] typeArgumentSignatures = new String[typeArugmentList.Count];
                        for (int i = 0; i < typeArugmentList.Count; i++)
                        {
                            typeArgumentSignatures[i] = GetSignature(typeArugmentList[i], interopReferences);
                        }
                        return "pinterface({" + GetGuid(typeSignature, interopReferences) + "};" + string.Join(";", typeArgumentSignatures) + ")";
                    }
                }
                throw new ArgumentException("TypeSignature with GenericInst does not resolve to genericTypeSignature");
            case ElementType.Class:
                // TODO: Get default interface and add it to the signature
                if (typeDefinition is not null)
                {
                    if (typeDefinition.IsDelegate)
                    {
                        return "delegate({" + GetGuid(typeSignature, interopReferences) + "})";
                    }
                    if (typeDefinition.IsClass)
                    {
                        if (typeDefinition.TryGetCustomAttribute("WindowsRuntime", "WindowsRuntimeDefaultInterfaceAttribute", out CustomAttribute? customAttribute))
                        {
                            CustomAttributeSignature? customAttributeSignature = customAttribute.Signature;
                            if (customAttributeSignature is not null && customAttributeSignature.FixedArguments.Count != 0)
                            {
                                object? element = customAttributeSignature.FixedArguments[0].Element;
                                if (element is TypeSignature defaultInterfaceSig)
                                {
                                    return "rc(" + typeSignature.FullName + ";" + GetSignature(defaultInterfaceSig, interopReferences) + ")";
                                }
                            }
                        }
                        return "{" + GetGuid(typeSignature, interopReferences) + "}";
                    }
                    if (typeDefinition.IsInterface)
                    {
                        return "{" + GetGuid(typeSignature, interopReferences) + "}";
                    }
                }
                throw new ArgumentException("TypeSignature with GenericInst does not resolve to genericTypeSignature");
        }
#pragma warning restore IDE0010

        //if (typeDefinition is not null)
        //{
        //    if (typeDefinition.IsDelegate)
        //    {
        //        return "delegate({" + GetGuid(typeSignature, interopReferences) + "})";
        //    }
        //    else if (typeDefinition.IsEnum)
        //    {
        //        bool isFlags = typeDefinition.HasCustomAttribute("System", "FlagsAttribute");
        //        return "enum(" + typeDefinition.FullName + ";" + (isFlags ? "u4" : "i4") + ")";
        //    }
        //}
        return "";
    }

    internal static Guid GetGuid(TypeSignature typeSig, InteropReferences interopReferences)
    {
        AsmResolver.DotNet.TypeDefinition? typeDef = typeSig.Resolve();
        Guid result = WellKnownInterfaceIIDs.get_GUID(typeSig, true, interopReferences);
        if (result != Guid.Empty)
        {
            return result;
        }

        if (typeDef is not null)
        {
            if (typeDef.TryGetCustomAttribute("System.Runtime.InteropServices", "GuidAttribute", out CustomAttribute? customAttribute))
            {
                CustomAttributeSignature? customAttributeSignature = customAttribute.Signature;
                if (customAttributeSignature is not null && customAttributeSignature.FixedArguments.Count != 0)
                {
                    object? first = customAttributeSignature.FixedArguments[0].Element;
                    // AsmResolver represents strings as System.String here.
                    if (first is AsmResolver.Utf8String s)
                    {
                        _ = Guid.TryParse(s.Value, out result);
                        return result;
                    }
                }
            }

        }
        return Guid.Empty;
    }


    private static Guid EncodeGuid(Span<byte> data)
    {
        if (BitConverter.IsLittleEndian)
        {
            // swap bytes of int a
            (data[3], data[0]) = (data[0], data[3]);
            (data[2], data[1]) = (data[1], data[2]);

            // swap bytes of short b
            (data[5], data[4]) = (data[4], data[5]);

            // swap bytes of short c and encode rfc time/version field
            (data[7], data[6]) = ((byte)((data[6] & 0x0f) | (5 << 4)), data[7]);

            // encode rfc clock/reserved field
            data[8] = (byte)((data[8] & 0x3f) | 0x80);
        }
        return new Guid(data[0..16]);
    }

    private static readonly Guid wrt_pinterface_namespace = new(0xd57af411, 0x737b, 0xc042, 0xab, 0xae, 0x87, 0x8b, 0x1e, 0x16, 0xad, 0xee);

    public static Guid CreateIID(TypeSignature type, InteropReferences interopReferences)
    {
        return CreateIIDForGenericType(GetSignature(type, interopReferences));
    }

    internal static Guid CreateIIDForGenericType(string signature)
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
