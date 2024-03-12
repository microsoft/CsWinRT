﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace WinRT
{
#if EMBED
    internal
#else 
    public
#endif
    static class GuidGenerator
    {
        public static Guid GetGUID(Type type)
        {
            return type.GetGuidType().GUID;
        }

        public static Guid GetIID(
#if NET
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
            Type type)
        {
            type = type.GetGuidType();
            if (!type.IsGenericType)
            {
                return type.GUID;
            }
            return (Guid)type.GetField("PIID").GetValue(null);
        }

        public static string GetSignature(
#if NET
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
            Type type)
        {
            if (type == typeof(object))
            {
                return "cinterface(IInspectable)";
            }

            if (type == typeof(string))
            {
                return "string";
            }

            if (type.IsGenericType)
            {
                static string[] SelectSignaturesForTypes(Type[] types)
                {
                    string[] signatures = new string[types.Length];

                    for (int i = 0; i < types.Length; i++)
                    {
                        signatures[i] = GetSignature(types[i]);
                    }

                    return signatures;
                }

                var args = SelectSignaturesForTypes(type.GetGenericArguments());
                var genericHelperType = type.GetGenericTypeDefinition().FindHelperType() ?? type;
                return "pinterface({" + genericHelperType.GUID + "};" + string.Join(";", args) + ")";
            }

            var helperType = type.FindHelperType();
            if (helperType != null)
            {
                var sigMethod = helperType.GetMethod("GetGuidSignature", BindingFlags.Static | BindingFlags.Public);
                if (sigMethod != null)
                {
                    return (string)sigMethod.Invoke(null, null);
                }
            }

            if (type.IsValueType)
            {
                switch (type.Name)
                {
                    case "SByte": return "i1";
                    case "Byte": return "u1";
                    case "Int16": return "i2";
                    case "UInt16": return "u2";
                    case "Int32": return "i4";
                    case "UInt32": return "u4";
                    case "Int64": return "i8";
                    case "UInt64": return "u8";
                    case "Single": return "f4";
                    case "Double": return "f8";
                    case "Boolean": return "b1";
                    case "Char": return "c2";
                    case "Guid": return "g16";
                    default:
                        {
                            if (type.IsEnum)
                            {
                                var isFlags = type.IsDefined(typeof(FlagsAttribute));
                                return "enum(" + type.FullName + ";" + (isFlags ? "u4" : "i4") + ")";
                            }
                            if (!type.IsPrimitive)
                            {
                                var winrtTypeAttribute = type.GetCustomAttribute<WindowsRuntimeTypeAttribute>();
                                if (winrtTypeAttribute != null && !string.IsNullOrEmpty(winrtTypeAttribute.GuidSignature))
                                {
                                    return winrtTypeAttribute.GuidSignature;
                                }
                                else if (winrtTypeAttribute == null && 
                                    (winrtTypeAttribute = type.GetAuthoringMetadataType()?.GetCustomAttribute<WindowsRuntimeTypeAttribute>()) != null && 
                                    !string.IsNullOrEmpty(winrtTypeAttribute.GuidSignature))
                                {
                                    return winrtTypeAttribute.GuidSignature;
                                }
                                else
                                {
                                    static string[] SelectSignaturesForFields(FieldInfo[] fields)
                                    {
                                        string[] signatures = new string[fields.Length];

                                        for (int i = 0; i < fields.Length; i++)
                                        {
                                            signatures[i] = GetSignature(fields[i].FieldType);
                                        }

                                        return signatures;
                                    }

                                    var args = SelectSignaturesForFields(type.GetFields(BindingFlags.Instance | BindingFlags.Public));
                                    return "struct(" + type.FullName + ";" + String.Join(";", args) + ")";
                                }
                            }
                            throw new InvalidOperationException("unsupported value type");
                        }
                }
            }

            // For authoring interfaces, we can use the metadata type or the helper type to get the guid.
            // For built-in system interfaces that are custom type mapped, we use the helper type to get the guid.
            // For others, either the type itself or the helper type has the same guid and can be used.
            type = type.IsInterface ? (helperType ?? type) : type;

            if (type.IsDelegate())
            {
                return "delegate({" + GetGUID(type) + "})";
            }

            if (type.IsClass && Projections.TryGetDefaultInterfaceTypeForRuntimeClassType(type, out Type iface))
            {
                return "rc(" + type.FullName + ";" + GetSignature(iface) + ")";
            }

            return "{" + type.GUID.ToString() + "}";
        }

        private static Guid encode_guid(Span<byte> data)
        {
            if (BitConverter.IsLittleEndian)
            {
                // swap bytes of int a
                byte t = data[0];
                data[0] = data[3];
                data[3] = t;
                t = data[1];
                data[1] = data[2];
                data[2] = t;
                // swap bytes of short b
                t = data[4];
                data[4] = data[5];
                data[5] = t;
                // swap bytes of short c and encode rfc time/version field
                t = data[6];
                data[6] = data[7];
                data[7] = (byte)((t & 0x0f) | (5 << 4));
                // encode rfc clock/reserved field
                data[8] = (byte)((data[8] & 0x3f) | 0x80);
            }
#if !NET
            return new Guid(data.Slice(0, 16).ToArray());
#else
            return new Guid(data[0..16]);
#endif
        }

        private readonly static Guid wrt_pinterface_namespace = new(0xd57af411, 0x737b, 0xc042, 0xab, 0xae, 0x87, 0x8b, 0x1e, 0x16, 0xad, 0xee);

        public static Guid CreateIID(Type type)
        {
            var sig = GetSignature(type);
            if (!type.IsGenericType)
            {
                return new Guid(sig);
            }
            else
            {
                return CreateIIDForGenericType(sig);
            }
        }

        internal static Guid CreateIIDForGenericType(string signature)
        {
#if !NET
            var data = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Concat(wrt_pinterface_namespace.ToByteArray(), Encoding.UTF8.GetBytes(signature)));

            using (SHA1 sha = new SHA1CryptoServiceProvider())
            {
                return encode_guid(sha.ComputeHash(data));
            }
#else
            var maxBytes = UTF8Encoding.UTF8.GetMaxByteCount(signature.Length);

            var data = new byte[16 /* Number of bytes in a GUID */ + maxBytes];
            Span<byte> dataSpan = data;
            wrt_pinterface_namespace.TryWriteBytes(dataSpan);
            var numBytes = UTF8Encoding.UTF8.GetBytes(signature, dataSpan[16..]);
            data = data[..(16 + numBytes)];

            return encode_guid(SHA1.HashData(data));
#endif
        }
    }
}
