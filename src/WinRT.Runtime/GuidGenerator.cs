// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using WinRT.Interop;

namespace WinRT
{
#if EMBED
    internal
#else 
    public
#endif
    static class GuidGenerator
    {
#if NET
        [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "This method only accesses 'Type.GUID', no fields are ever needed.")]
#endif
        public static Guid GetGUID(Type type)
        {
            type = type.GetGuidType();

            // Only check the WUX/MUX types if the feature switch is set, to avoid introducing
            // performance regressions in the standard case where MUX is targeted (default).
            if (FeatureSwitches.UseWindowsUIXamlProjections)
            {
                if (TryGetWindowsUIXamlIID(type, out Guid iid))
                {
                    return iid;
                }
            }

            return type.GUID;
        }

        public static Guid GetIID(
#if NET
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
            Type type)
        {
            type = type.GetGuidType();

            // Same optional check as above
            if (FeatureSwitches.UseWindowsUIXamlProjections)
            {
                if (TryGetWindowsUIXamlIID(type, out Guid iid))
                {
                    return iid;
                }
            }

            if (!type.IsGenericType)
            {
                return type.GUID;
            }
            return (Guid)type.GetField("PIID").GetValue(null);
        }

        internal static bool TryGetWindowsUIXamlIID(Type type, out Guid iid)
        {
            if (type == typeof(global::ABI.System.Collections.Specialized.INotifyCollectionChanged))
            {
                iid = IID.IID_WUX_INotifyCollectionChanged;

                return true;
            }

            if (type == typeof(global::ABI.System.ComponentModel.INotifyPropertyChanged))
            {
                iid = IID.IID_WUX_INotifyPropertyChanged;

                return true;
            }

            if (type == typeof(global::ABI.System.Collections.Specialized.NotifyCollectionChangedEventArgs))
            {
                iid = IID.IID_WUX_INotifyCollectionChangedEventArgs;

                return true;
            }

            if (type == typeof(global::ABI.System.Collections.Specialized.NotifyCollectionChangedEventHandler))
            {
                iid = IID.IID_WUX_NotifyCollectionChangedEventHandler;

                return true;
            }

            if (type == typeof(global::ABI.System.ComponentModel.PropertyChangedEventHandler))
            {
                iid = IID.IID_WUX_PropertyChangedEventHandler;

                return true;
            }

            iid = default;

            return false;
        }

        public static string GetSignature(
#if NET
            // This '[DynamicallyAccessedMembers]' annotation is here just for backwards-compatibility with old projections. Those are
            // not trim-safe, but we didn't want to break existing consumers that had code that happened to still work in this case.
            // The only case where 'GetSignature' actually uses reflection is in that scenario, but when using updated projections, the
            // generated attributes are always used instead, which are trim-safe. Therefore, it's safe to call this method suppressing
            // the trim warning for the 'type' parameter if legacy projections are not a concern, or for a "best effort" support there.
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

            if (type == typeof(Type))
            {
                return ABI.System.Type.GetGuidSignature();
            }

            if (type.IsGenericType)
            {
#if NET
                [UnconditionalSuppressMessage("Trimming", "IL2062", Justification = "Fallback path for old projections, not trim-safe by design.")]
#endif
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
                                
                                if (winrtTypeAttribute == null && 
                                    (winrtTypeAttribute = type.GetAuthoringMetadataType()?.GetCustomAttribute<WindowsRuntimeTypeAttribute>()) != null && 
                                    !string.IsNullOrEmpty(winrtTypeAttribute.GuidSignature))
                                {
                                    return winrtTypeAttribute.GuidSignature;
                                }

#if NET
                                if (!RuntimeFeature.IsDynamicCodeCompiled)
                                {
                                    throw new InvalidOperationException(
                                        $"Cannot compute signature for type '{type}', as doing so requires a fallback path that is not trim/AOT " +
                                        $"compatible. Using AOT requires all referenced projections to be up to date. Make sure to only reference " +
                                        $"WinRT projections that have been generated by a version of CsWinRT supported for AOT.");
                                }

                                [UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Fallback path for old projections, not trim-safe by design.")]
#endif
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
                                return "struct(" + type.FullName + ";" + string.Join(";", args) + ")";
                            }

                            throw new InvalidOperationException("Unsupported value type.");
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

#if NET
            [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "'GetSignature' will only actually use reflection when using old projections.")]
#endif
            static bool TryGetSignatureFromDefaultInterfaceTypeForRuntimeClassType(Type type, out string signature)
            {
                if (type.IsClass && Projections.TryGetDefaultInterfaceTypeForRuntimeClassType(type, out Type iface))
                {
                    signature = "rc(" + type.FullName + ";" + GetSignature(iface) + ")";

                    return true;
                }

                signature = null;

                return false;
            }

            if (TryGetSignatureFromDefaultInterfaceTypeForRuntimeClassType(type, out string signature))
            {
                return signature;
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

        private static readonly Guid wrt_pinterface_namespace = new(0xd57af411, 0x737b, 0xc042, 0xab, 0xae, 0x87, 0x8b, 0x1e, 0x16, 0xad, 0xee);

        public static Guid CreateIID(
#if NET
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
#endif
            Type type)
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

        /// <summary>
        /// Gets the IID of a given type, just like <see cref="CreateIID(Type)"/>, but without rooting reflection metadata
        /// for all public fields of that type. It can be used internally where we know that extra info is not actually needed.
        /// </summary>
        /// <param name="type">The type to get the IID for.</param>
        /// <returns>The IID for <paramref name="type"/>.</returns>
#if NET
        [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "This method is only used for types (eg. generics) where fields aren't needed.")]
#endif
        internal static Guid CreateIIDUnsafe(Type type)
        {
            return CreateIID(type);
        }

#if NET8_0_OR_GREATER
        [SkipLocalsInit]
#endif
        internal static Guid CreateIIDForGenericType(string signature)
        {
#if !NET
            var data = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Concat(wrt_pinterface_namespace.ToByteArray(), Encoding.UTF8.GetBytes(signature)));

            using (SHA1 sha = new SHA1CryptoServiceProvider())
            {
                return encode_guid(sha.ComputeHash(data));
            }
#else
#nullable enable
            // Get the maximum UTF8 byte size and allocate a buffer for the encoding.
            // If the minimum buffer is small enough, we can stack-allocate it.
            int maxUtf8ByteCount = Encoding.UTF8.GetMaxByteCount(signature.Length);
            int minimumPooledLength = 16 /* Number of bytes in a GUID */ + maxUtf8ByteCount;
            byte[]? utf8BytesFromPool = null;
            Span<byte> utf8Bytes = minimumPooledLength <= 128
                ? stackalloc byte[128]
                : (utf8BytesFromPool = ArrayPool<byte>.Shared.Rent(minimumPooledLength));

            wrt_pinterface_namespace.TryWriteBytes(utf8Bytes);

            int encodedUtf8BytesWritten = Encoding.UTF8.GetBytes(signature, utf8Bytes[16..]);
            Span<byte> sha1Bytes = stackalloc byte[SHA1.HashSizeInBytes];

            // Hash the encoded signature (the bytes written are guaranteed to always match the span)
            _ = SHA1.HashData(utf8Bytes[..(16 + encodedUtf8BytesWritten)], sha1Bytes);

            Guid iid = encode_guid(sha1Bytes);

            // Before exiting, make sure to return the array from the pool, if we rented one
            if (utf8BytesFromPool is not null)
            {
                ArrayPool<byte>.Shared.Return(utf8BytesFromPool);
            }

            return iid;
#endif
        }
    }
}
