// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;

namespace WindowsRuntime.InteropGenerator.Helpers;

internal static class GuidGenerator
{
    internal static string GetSignature(
            TypeSignature typeSignature)
    {

        switch (typeSignature.ElementType)
        {
            case ElementType.Object:
                return "cinterface(IInspectable)";

            case ElementType.String:
                return "string";

            case ElementType.Type:
                return "struct(Windows.UI.Xaml.Interop.TypeName;string;enum(Windows.UI.Xaml.Interop.TypeKind;i4))";

            case ElementType.GenericInst:
                GenericInstanceTypeSignature genericTypeSignature = (GenericInstanceTypeSignature)typeSignature;
                IList<TypeSignature> typeArugmentList = genericTypeSignature.TypeArguments; // IList<TypeSignature>
                String[] typeArgumentSignatures = new String[typeArugmentList.Count];
                for (int i = 0; i < typeArugmentList.Count; i++)
                {
                    typeArgumentSignatures[i] = GetSignature(typeArugmentList[i]);
                }
                return "pinterface({" + TryGetGuidFromGuidAttribute(typeSignature) + "};" + string.Join(";", typeArgumentSignatures) + ")";

            case ElementType.Class:
                return "cinterface(IInspectable)";

            case ElementType.Enum:
                AsmResolver.DotNet.TypeDefinition? type = typeSignature.Resolve() ?? throw new InvalidOperationException("Cannot resolve enum type.");
                bool isFlags = true;/*type.IsDefined(typeof(FlagsAttribute));*/
                return "enum(" + type.FullName + ";" + (isFlags ? "u4" : "i4") + ")";

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
            case ElementType.None:
            case ElementType.Void:
            case ElementType.Ptr:
            case ElementType.ByRef:
            case ElementType.ValueType:
            case ElementType.Var:
            case ElementType.Array:
            case ElementType.TypedByRef:
            case ElementType.I:
            case ElementType.U:
            case ElementType.FnPtr:
            case ElementType.SzArray:
            case ElementType.MVar:
            case ElementType.CModReqD:
            case ElementType.CModOpt:
            case ElementType.Internal:
            case ElementType.Modifier:
            case ElementType.Sentinel:
            case ElementType.Pinned:
            case ElementType.Boxed:
            default:
                throw new NotSupportedException($"Unsupported element type: {typeSignature.ElementType}");
        }


        //[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "'GetSignature' will only actually use reflection when using old projections.")]
        //static bool TryGetSignatureFromDefaultInterfaceTypeForRuntimeClassType(Type type, out string signature)
        //{
        //    if (type.IsClass && Projections.TryGetDefaultInterfaceTypeForRuntimeClassType(type, out Type iface))
        //    {
        //        signature = "rc(" + type.FullName + ";" + GetSignature(iface) + ")";

        //        return true;
        //    }

        //    signature = null;

        //    return false;
        //}

        //if (TryGetSignatureFromDefaultInterfaceTypeForRuntimeClassType(type, out string signature))
        //{
        //    return signature;
        //}


        //return "{" + type.GUID.ToString() + "}";
    }

    /// <summary>
    /// Tries to get the GUID value from the GuidAttribute applied to the type represented by the given TypeSignature.
    /// Returns true on success; false if the attribute is missing or malformed.
    /// </summary>
    internal static Guid TryGetGuidFromGuidAttribute(TypeSignature typeSig)
    {
        //guid = default;

        //// 1) Peel wrappers (array/byref/pointer/modopt/pinned/boxed).
        //var core = Peel(typeSig);

        //// 2) Reduce to a TypeDefOrRefSignature (unwrap GenericInstance to its generic type).
        //TypeDefOrRefSignature? typeRefSig = core switch
        //{
        //    GenericInstanceTypeSignature gi => gi.GenericType,
        //    TypeDefOrRefSignature tdr => tdr,
        //    _ => null
        //};

        //if (typeRefSig is null)
        //    return false;

        // 3) Resolve to a TypeDefinition (metadata owner of attributes).
        AsmResolver.DotNet.TypeDefinition? typeDef = typeSig.Resolve();
        if (typeDef is not null)
        {
            // 4) Find [System.Runtime.InteropServices.GuidAttribute(...)]
            foreach (AsmResolver.DotNet.CustomAttribute customAttribute in typeDef.CustomAttributes)
            {
                AsmResolver.DotNet.ITypeDefOrRef? ctorType = customAttribute.Constructor?.DeclaringType;
                if (ctorType is null)
                {
                    continue;
                }

                // Match on full name to be precise.
                if (!string.Equals(ctorType.Namespace, "System.Runtime.InteropServices", StringComparison.Ordinal) ||
                    !string.Equals(ctorType.Name, "GuidAttribute", StringComparison.Ordinal))
                {
                    continue;
                }

                // GuidAttribute normally has a single string fixed argument: the textual GUID.
                CustomAttributeSignature? customAttributeSignature = customAttribute.Signature;
                if (customAttributeSignature is not null && customAttributeSignature.FixedArguments.Count != 0)
                {
                    object? first = customAttributeSignature.FixedArguments[0].Element;
                    if (first is null)
                    {
                        continue;
                    }
                    // AsmResolver represents strings as System.String here.
                    if (first is string s && Guid.TryParse(s, out Guid guid))
                    {
                        return guid;
                    }
                }
            }
            return Guid.Empty;
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

    public static Guid CreateIID(TypeSignature type)
    {
        return Guid.Empty;
        //return CreateIIDForGenericType(GetSignature(type));
    }

    [SkipLocalsInit]
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
