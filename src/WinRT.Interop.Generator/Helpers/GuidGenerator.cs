// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Collections.Generic;
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

    /// <summary>
    /// Generates the WinRT signature string for the specified type.
    /// </summary>
    /// <param name="typeSignature">
    /// The CLR/AsmResolver type signature to translate.
    /// </param>
    /// <param name="interopReferences">
    /// Interop metadata used to resolve GUIDs for interfaces, delegates, and generics.
    /// </param>
    /// <returns>
    /// A WinRT signature string representing the given type.
    /// </returns>
    internal static string GetSignature(
            TypeSignature typeSignature,
            InteropReferences interopReferences)
    {
        string? mappedSignature = TypeMapping.FindGuidSignatureForMappedType(typeSignature.Namespace, typeSignature.Name);

        if (mappedSignature is not null)
        {
            return mappedSignature;
        }

        AsmResolver.DotNet.TypeDefinition? typeDefinition = typeSignature.Resolve()!;

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
                if (typeDefinition is not null)
                {
                    if (typeDefinition.IsClass)
                    {
                        // Enum case
                        if (typeDefinition.IsEnum)
                        {
                            bool isFlags = typeDefinition.HasCustomAttribute("System", "FlagsAttribute");
                            return "enum(" + typeDefinition.FullName + ";" + (isFlags ? "u4" : "i4") + ")";
                        }

                        // Guid Case
                        if (typeDefinition.IsGuid)
                        {
                            return "g16";
                        }

                        // Struct case
                        IList<AsmResolver.DotNet.FieldDefinition> fieldDefinition = typeDefinition.Fields;
                        List<string> typeArgumentSignatures = [];

                        for (int i = 0; i < fieldDefinition.Count; i++)
                        {
                            if (!fieldDefinition[i].IsStatic)
                            {
                                FieldSignature? fieldSignature = fieldDefinition[i].Signature ?? throw new ArgumentException("FieldSignature is missing");
                                typeArgumentSignatures.Add(GetSignature(fieldSignature.FieldType, interopReferences));
                            }
                        }

                        return typeDefinition.Namespace is null || typeDefinition.Name is null
                            ? "struct(" + typeDefinition.FullName + ";" + string.Join(";", typeArgumentSignatures) + ")"
                            : "struct(" + TypeMapping.FindMappedWinRTFullName(typeDefinition.Namespace, typeDefinition.Name) + ";" + string.Join(";", typeArgumentSignatures) + ")";
                    }
                }

                throw new ArgumentException("Invalid ElementType.ValueType");

            case ElementType.GenericInst:
                GenericInstanceTypeSignature genericTypeSignature = (GenericInstanceTypeSignature)typeSignature;
                AsmResolver.DotNet.TypeDefinition? genericTypeDefinition = genericTypeSignature.GenericType.Resolve();

                if (genericTypeDefinition is not null)
                {
                    IList<TypeSignature> typeArugmentList = genericTypeSignature.TypeArguments;
                    String[] typeArgumentSignatures = new String[typeArugmentList.Count];

                    for (int i = 0; i < typeArugmentList.Count; i++)
                    {
                        typeArgumentSignatures[i] = GetSignature(typeArugmentList[i], interopReferences);
                    }

                    return "pinterface({" + GetGuid(typeSignature, interopReferences) + "};" + string.Join(";", typeArgumentSignatures) + ")";
                }

                throw new ArgumentException("Invalid ElementType.GenericInst");

            case ElementType.Class:
                if (typeDefinition is not null)
                {
                    if (typeDefinition.IsClass)
                    {
                        return typeDefinition.IsDelegate
                            ? "delegate({" + GetGuid(typeSignature, interopReferences) + "})" // Delegate case
                            : GetDefaultInterfaceSignatureFromAttribute(typeDefinition, out TypeSignature defaultInterfaceSig)
                                ? "rc(" + typeDefinition.FullName + ";" + GetSignature(defaultInterfaceSig, interopReferences) + ")" // Class case with default interface
                                : "{" + GetGuid(typeSignature, interopReferences) + "}"; // Class case without default interface
                    }
                    if (typeDefinition.IsInterface) // interface case
                    {
                        return "{" + GetGuid(typeSignature, interopReferences) + "}";
                    }
                }

                throw new ArgumentException("Invalid ElementType.Class");

            case ElementType.SzArray:
                SzArrayTypeSignature arrayTypeSignature = (SzArrayTypeSignature)typeSignature;

                if (arrayTypeSignature != null)
                {
                    return "pinterface({61c17707-2d65-11e0-9ae8-d48564015472};" + GetSignature(arrayTypeSignature.BaseType, interopReferences) + ")";
                }

                throw new ArgumentException("Invalid ElementType.SzArray");

            case ElementType.None:
            case ElementType.Void:
            case ElementType.Ptr:
            case ElementType.ByRef:
            case ElementType.Var:
            case ElementType.Array:
            case ElementType.TypedByRef:
            case ElementType.I:
            case ElementType.U:
            case ElementType.FnPtr:
            case ElementType.MVar:
            case ElementType.CModReqD:
            case ElementType.CModOpt:
            case ElementType.Internal:
            case ElementType.Modifier:
            case ElementType.Sentinel:
            case ElementType.Pinned:
            case ElementType.Boxed:
            case ElementType.Enum:
            default:
                // TODO: throw new ArgumentException("Unsupported ElementType: " + typeSignature.ElementType + " : " + typeSignature.FullName);
                return typeSignature.FullName;
        }
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
    internal static Guid GetGuid(TypeSignature typeSig, InteropReferences interopReferences)
    {
        Guid result = WellKnownInterfaceIIDs.get_GUID(typeSig, true, interopReferences);

        if (result != Guid.Empty)
        {
            return result;
        }

        AsmResolver.DotNet.TypeDefinition? typeDef = typeSig.Resolve();
        if (typeDef is not null)
        {
            if (GetGuidFromAttribute(typeDef, out result))
            {
                return result;
            }
            //else
            //{
            //    //throw new ArgumentException("Type does not have a Guid attribute"); 
            //}
        }

        return Guid.Empty; // TODO: don'turn empty guid but throw instead like below. Currently, not all the types we need to be filtered are not filtered out such as System.Reflection.* types.
        //throw new ArgumentException("Type does not have a Guid attribute");
    }

    internal static bool GetGuidFromAttribute(TypeDefinition typeDef, out Guid guid)
    {
        guid = Guid.Empty;
        if (typeDef.TryGetCustomAttribute("System.Runtime.InteropServices", "GuidAttribute", out AsmResolver.DotNet.CustomAttribute? customAttribute))
        {
            CustomAttributeSignature? customAttributeSignature = customAttribute.Signature;
            if (customAttributeSignature is not null && customAttributeSignature.FixedArguments.Count != 0)
            {
                object? first = customAttributeSignature.FixedArguments[0].Element;
                // AsmResolver represents strings as System.String here.
                if (first is AsmResolver.Utf8String s)
                {
                    return Guid.TryParse(s.Value, out guid);
                }
            }
        }
        return false;
    }

    internal static bool GetDefaultInterfaceSignatureFromAttribute(TypeDefinition typeDef, out TypeSignature defaultInterfaceSig)
    {
        defaultInterfaceSig = null!;
        if (typeDef.TryGetCustomAttribute("WindowsRuntime", "WindowsRuntimeDefaultInterfaceAttribute", out AsmResolver.DotNet.CustomAttribute? customAttribute))
        {
            CustomAttributeSignature? customAttributeSignature = customAttribute.Signature;
            if (customAttributeSignature is not null && customAttributeSignature.FixedArguments.Count != 0)
            {
                object? element = customAttributeSignature.FixedArguments[0].Element;
                if (element is TypeSignature sig)
                {
                    defaultInterfaceSig = sig;
                    return true;
                }
            }
        }
        return false;
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


    // TODO: Debug code; Will remove later
//#pragma warning disable IDE0044 // Add readonly modifier
//    private static readonly string printPath = @"C:\Users\kythant\Documents\staging\GUIDsFromCSWinRTGen.txt";
//    private static HashSet<TypeSignature> generatedIIDs = [];
//    private static StreamWriter writer = new(printPath, append: false);
//#pragma warning restore IDE0044 // Add readonly modifier

    public static Guid CreateIID(TypeSignature type, InteropReferences interopReferences)
    {
        string signature = GetSignature(type, interopReferences);
        Guid guid = CreateGuidFromSignature(signature);
        // TODO: Debug code; Will remove later
        //if (!generatedIIDs.Contains(type))
        //{
        //    writer.WriteLine(type.FullName + "\n    " + signature + "\n    " + guid);
        //    _ = generatedIIDs.Add(type);
        //}
        return guid;
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
