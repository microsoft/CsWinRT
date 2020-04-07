using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace WinRT
{
    public static class GuidGenerator
    {
        public static IEnumerable<string> GetAllPossibleSignatureCombinations(
            this IEnumerable<IEnumerable<string>> memberSignatures)
        {
            // Implementation adapted from https://stackoverflow.com/a/4424005
            var accum = new List<string>();
            var signaturesArray = memberSignatures.ToArray();
            if (signaturesArray.Length > 0)
            {
                GetAllPossibleSignatureCombinationsCore(
                    accum,
                    new Stack<string>(),
                    signaturesArray,
                    signaturesArray.Length - 1);
            }
            return accum;

            static void GetAllPossibleSignatureCombinationsCore(List<string> accum, Stack<string> stack,
                                            IEnumerable<string>[] signatures, int index)
            {
                foreach (string item in signatures[index])
                {
                    stack.Push(item);
                    if (index == 0)
                    {
                        // IEnumerable on a System.Collections.Generic.Stack
                        // enumerates in order of removal (last to first).
                        // As a result, we get the correct ordering here.
                        accum.Add(string.Join(";", stack));
                    }
                    else
                    {
                        GetAllPossibleSignatureCombinationsCore(accum, stack, signatures, index - 1);
                    }
                    stack.Pop();
                }
            }
        }


        private static Type GetGuidType(Type type)
        {
            if (type.IsDelegate())
            {
                return type.GetHelperType();
            }

            Type guidType = Projections.FindCustomHelperTypeMapping(type);
            if (guidType is object)
            {
                return guidType;
            }
            return type;
        }

        public static Guid GetGUID(Type type)
        {
            return type.GetGuidType().GUID;
        }

        public static Guid GetIID(Type type) => GetIIDs(type)[0];

        public static Guid[] GetIIDs(Type type)
        {
            type = type.GetGuidType();
            if (!type.IsGenericType)
            {
                return new[] { type.GUID };
            }
            return (Guid[])type.GetField("PIIDs")?.GetValue(null) ?? new[] { (Guid)type.GetField("PIID").GetValue(null) };
        }

        public static IEnumerable<string> GetSignatures(Type type)
        {
            var helperType = type.FindHelperType();
            if (helperType != null)
            {
                var signaturesMethod = helperType.GetMethod("GetGuidSignatures", BindingFlags.Static | BindingFlags.Public);
                if (signaturesMethod != null)
                {
                    return (IEnumerable<string>)signaturesMethod.Invoke(null, Type.EmptyTypes);
                }
                var signatureMethod = helperType.GetMethod("GetGuidSignature", BindingFlags.Static | BindingFlags.Public);
                if (signatureMethod != null)
                {
                    return new[] { (string)signatureMethod.Invoke(null, Type.EmptyTypes) };
                }
            }

            if (type == typeof(object))
            {
                return new[] { "cinterface(IInspectable)" };
            }

            if (type.IsGenericType)
            {
                var args = type.GetGenericArguments().Select(t => GetSignatures(t));
                return GetAllPossibleSignatureCombinations(args)
                    .Select(argsSignature => "pinterface({" + GetGUID(type) + "};" + argsSignature + ")");
            }

            if (type.IsValueType)
            {
                switch (type.Name)
                {
                    case "SByte": return new[] { "i1" };
                    case "Byte": return new[] { "u1" };
                    case "Int16": return new[] { "i2" };
                    case "UInt16": return new[] { "u2" };
                    case "Int32": return new[] { "i4" };
                    case "UInt32": return new[] { "u4" };
                    case "Int64": return new[] { "i8" };
                    case "UInt64": return new[] { "u8" };
                    case "Single": return new[] { "f4" };
                    case "Double": return new[] { "f8" };
                    case "Boolean": return new[] { "b1" };
                    case "Char": return new[] { "c2" };
                    case "Guid": return new[] { "g16" };
                    default:
                    {
                        if (type.IsEnum)
                        {
                            var isFlags = type.CustomAttributes.Any(cad => cad.AttributeType == typeof(FlagsAttribute));
                            return new[] { "enum(" + type.FullName + ";" + (isFlags ? "u4" : "i4") + ")" };
                        }
                        if (!type.IsPrimitive)
                        {
                            var args = type.GetFields(BindingFlags.Instance | BindingFlags.Public).Select(fi => GetSignatures(fi.FieldType));
                            return GetAllPossibleSignatureCombinations(args)
                                    .Select(argsSignature => "struct(" + type.FullName + ";" + argsSignature + ")");
                        }
                        throw new InvalidOperationException("unsupported value type");
                    }
                }
            }

            if (type == typeof(string))
            {
                return new[] { "string" };
            }

            if (Projections.TryGetDefaultInterfaceTypeForRuntimeClassType(type, out Type iface))
            {
                return GetSignatures(iface)
                    .Select(defaultSignature => "rc(" + type.FullName + ";" + defaultSignature + ")");
            }

            if (type.IsDelegate())
            {
                return new[] { "delegate({" + GetGUID(type) + "})" };
            }

            return new[] { "{" + type.GUID.ToString() + "}" };
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
#if !NETCOREAPP5_0
            return new Guid(data.Take(16).ToArray());
#else
            return new Guid(data.AsSpan()[0..16]);
#endif
        }

        private static Guid wrt_pinterface_namespace = new Guid("d57af411-737b-c042-abae-878b1e16adee");

        public static string GetSignature(Type type)
        {
            var sigs = GetSignatures(type).ToArray();

            if (sigs.Length != 1)
            {
                throw new ArgumentException($"The provided type: '{type.FullName}' has multiple possible signatures. Call GetSignatures instead.", nameof(type));
            }

            return sigs[0];
        }

        public static Guid CreateIID(Type type)
        {
            var iids = CreateIIDs(type);

            if (iids.Length != 1)
            {
                throw new ArgumentException($"The provided type: '{type.FullName}' has multiple possible IIDs. Call CreateIIDs instead.", nameof(type));
            }

            return iids[0];
        }

        public static Guid[] CreateIIDs(Type type)
        {
            var sigs = GetSignatures(type).ToArray();
            var guids = new Guid[sigs.Length];
            for (int i = 0; i < sigs.Length; i++)
            {
                if (!type.IsGenericType)
                {
                    guids[i] = new Guid(sigs[i]);
                }
                else
                {
                    var data = wrt_pinterface_namespace.ToByteArray().Concat(UTF8Encoding.UTF8.GetBytes(sigs[i])).ToArray();
                    using (SHA1 sha = new SHA1CryptoServiceProvider())
                    {
                        var hash = sha.ComputeHash(data);
                        guids[i] = encode_guid(hash);
                    } 
                }
            }

            return guids;
        }
    }
}
