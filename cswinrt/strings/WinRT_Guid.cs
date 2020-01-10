using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Linq.Expressions;

#pragma warning disable 0169 // The field 'xxx' is never used
#pragma warning disable 0649 // Field 'xxx' is never assigned to, and will always have its default value

namespace WinRT
{
    public static class GuidGenerator
    {
        private static Type GetGuidType(Type type)
        {
            if (type.IsDelegate())
            {
                var type_name = type.FullName;
                if (type.IsGenericType)
                {
                    var backtick = type_name.IndexOf('`');
                    type_name = type_name.Substring(0, backtick) + "Helper`" + type_name.Substring(backtick + 1);
                }
                else
                {
                    type_name += "Helper";
                }
                return Type.GetType(type_name);
            }
            return type;
        }

        public static Guid GetGUID(Type type)
        {
            return GetGuidType(type).GUID;
        }

        public static Guid GetIID(Type type)
        {
            type = GetGuidType(type);
            if (!type.IsGenericType)
            {
                return type.GUID;
            }
            return (Guid)type.GetField("PIID").GetValue(null);
        }

        public static string GetSignature(Type type)
        {
            if (type == typeof(IInspectable))
            {
                return "cinterface(IInspectable)";
            }

            if (type.IsGenericType)
            {
                var args = type.GetGenericArguments().Select(t => GetSignature(t));
                return "pinterface({" + GetGUID(type) + "};" + String.Join(";", args) + ")";
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
                                var isFlags = type.CustomAttributes.Any(cad => cad.AttributeType == typeof(FlagsAttribute));
                                return "enum(" + type.FullName + ";" + (isFlags ? "u4" : "i4") + ")";
                            }
                            if (!type.IsPrimitive)
                            {
                                var args = type.GetFields().Select(fi => GetSignature(fi.FieldType));
                                return "struct(" + type.FullName + ";" + String.Join(";", args) + ")";
                            }
                            throw new InvalidOperationException("unsupported value type");
                        }
                }
            }

            if (type == typeof(String))
            {
                return "string";
            }

            var _default = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance).FirstOrDefault((FieldInfo fi) => fi.Name == "_default");
            if (_default != null)
            {
                return "rc(" + type.FullName + ";" + GetSignature(_default.FieldType) + ")";
            }

            if (type.IsDelegate())
            {
                return "delegate({" + GetGUID(type) + "})";
            }

            return "{" + type.GUID.ToString() + "}";
        }

        private static Guid encode_guid(byte[] data)
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
            return new Guid(data.Take(16).ToArray());
        }

        private static Guid wrt_pinterface_namespace = new Guid("d57af411-737b-c042-abae-878b1e16adee");

        public static Guid CreateIID(Type type)
        {
            var sig = GetSignature(type);
            if (!type.IsGenericType)
            {
                return new Guid(sig);
            }
            var data = wrt_pinterface_namespace.ToByteArray().Concat(UTF8Encoding.UTF8.GetBytes(sig)).ToArray();
            using (SHA1 sha = new SHA1CryptoServiceProvider())
            {
                var hash = sha.ComputeHash(data);
                return encode_guid(hash);
            }
        }
    }
}
