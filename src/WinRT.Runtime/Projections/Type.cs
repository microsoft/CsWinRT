// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using WinRT;

namespace ABI.System
{
    [WindowsRuntimeType("Windows.Foundation.UniversalApiContract")]
    internal enum TypeKind : int
    {
        Primitive,
        Metadata,
        Custom
    }

#if EMBED
    internal
#else
    public
#endif
    struct Type
    {
        private IntPtr Name;
        private TypeKind Kind;

        public struct Marshaler
        {
            internal MarshalString Name;
            internal TypeKind Kind;

            internal void Dispose()
            {
                MarshalString.DisposeMarshaler(Name);
            }
        }

        private static (String Name, TypeKind Kind) ToAbi(global::System.Type value)
        {
            TypeKind kind = TypeKind.Custom;

            if (value is object)
            {
                if (value.IsPrimitive)
                {
                    kind = TypeKind.Primitive;
                }
                else if (value == typeof(object) || value == typeof(string) || value == typeof(Guid) || value == typeof(System.Type))
                {
                    kind = TypeKind.Metadata;
                }
                else if (Projections.IsTypeWindowsRuntimeType(value))
                {
                    kind = TypeKind.Metadata;
                }
            }

            return (kind == TypeKind.Custom ? value.AssemblyQualifiedName : TypeNameSupport.GetNameForType(value, TypeNameGenerationFlags.None), kind);
        }

        public static Marshaler CreateMarshaler(global::System.Type value)
        {
            var abi = ToAbi(value);
            return new Marshaler
            {
                Name = MarshalString.CreateMarshaler(abi.Name),
                Kind = abi.Kind
            };
        }

        public static Type GetAbi(Marshaler m)
        {
            return new Type
            {
                Name = MarshalString.GetAbi(m.Name),
                Kind = m.Kind
            };
        }

        public static global::System.Type FromAbi(Type value)
        {
            string name = MarshalString.FromAbi(value.Name);
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            if(value.Kind == TypeKind.Custom)
            {
                return global::System.Type.GetType(name);
            }

            return TypeNameSupport.FindTypeByName(name.AsSpan()).type;
        }

        public static unsafe void CopyAbi(Marshaler arg, IntPtr dest) =>
            *(Type*)dest.ToPointer() = GetAbi(arg);

        public static Type FromManaged(global::System.Type value)
        {
            var abi = ToAbi(value);
            return new Type
            {
                Name = MarshalString.FromManaged(abi.Name),
                Kind = abi.Kind
            };
        }

        public static unsafe void CopyManaged(global::System.Type arg, IntPtr dest) =>
            *(Type*)dest.ToPointer() = FromManaged(arg);

        public static void DisposeMarshaler(Marshaler m) { m.Dispose(); }
        public static void DisposeAbi(Type abi) { MarshalString.DisposeAbi(abi.Name); }

        public static string GetGuidSignature()
        {
            return "struct(Windows.UI.Xaml.Interop.TypeName;string;enum(Windows.UI.Xaml.Interop.TypeKind;i4))";
        }
    }
}
