using System;
using System.Collections.Generic;
using System.Text;
using WinRT;

namespace ABI.System
{
    internal enum TypeKind
    {
        Primitive,
        Metadata,
        Custom
    }

    public struct Type
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

        public static Marshaler CreateMarshaler(global::System.Type value)
        {
            TypeKind kind = TypeKind.Custom;

            if (value is object)
            {
                // TODO: Handle non-blittable structures that don't have a helper type.
                if (value.IsPrimitive)
                {
                    kind = TypeKind.Primitive;
                }
                else if (value.FindHelperType() != null)
                {
                    kind = TypeKind.Metadata;
                } 
            }

            return new Marshaler
            {
                Name = MarshalString.CreateMarshaler(TypeNameSupport.GetNameForType(value, TypeNameGenerationFlags.None)),
                Kind = kind
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
            if (value.Kind != TypeKind.Custom)
            {
                return TypeNameSupport.FindTypeByName(name.AsSpan()).type;
            }
            return global::System.Type.GetType(name, throwOnError: true);
        }

        public static unsafe void CopyAbi(Marshaler arg, IntPtr dest) =>
            *(Type*)dest.ToPointer() = GetAbi(arg);

        public static Type FromManaged(global::System.Type value)
        {
            return GetAbi(CreateMarshaler(value));
        }

        public static unsafe void CopyManaged(global::System.Type arg, IntPtr dest) =>
            *(Type*)dest.ToPointer() = FromManaged(arg);

        public static void DisposeMarshaler(Marshaler m) { m.Dispose(); }
        public static void DisposeAbi(Type abi) { }

        public static string GetGuidSignature()
        {
            return "struct(Windows.UI.Xaml.Interop.TypeName;string;enum(Windows.UI.Xaml.Interop.TypeKind;i4))";
        }
    }
}
