// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
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
    partial struct Type
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

        public ref struct Pinnable
        {
            internal MarshalString.Pinnable Name;
            internal TypeKind Kind;

            public Pinnable(global::System.Type type)
            {
                var abi = ToAbi(type);
                Name = new MarshalString.Pinnable(abi.Name);
                Kind = abi.Kind;
            }

            public ref readonly char GetPinnableReference()
            {
                return ref Name.GetPinnableReference();
            }
        }

        private static (String Name, TypeKind Kind) ToAbi(global::System.Type value)
        {
#if NET
            if (value is FakeMetadataType fakeMetadataType)
            {
                return (fakeMetadataType.FullName, TypeKind.Metadata);
            }
#endif

            TypeKind kind = TypeKind.Custom;
            if (value is not null)
            {
                if (value.IsPrimitive)
                {
                    kind = TypeKind.Primitive;
                }
                else if (value == typeof(object) || value == typeof(string) || value == typeof(Guid) || value == typeof(global::System.Type))
                {
                    kind = TypeKind.Metadata;
                }
                else if (Projections.IsTypeWindowsRuntimeType(value))
                {
                    kind = TypeKind.Metadata;
                }
            }

            return (GetNameForTypeCached(value, kind == TypeKind.Custom), kind);
        }

        private static readonly ConcurrentDictionary<global::System.Type, string> typeNameCache = new();
        private static string GetNameForTypeCached(global::System.Type value, bool customKind)
        {
            if (customKind)
            {
                return typeNameCache.GetOrAdd(value, (type) => type.AssemblyQualifiedName);
            }
            else
            {
                return typeNameCache.GetOrAdd(value, (type) => TypeNameSupport.GetNameForType(type, TypeNameGenerationFlags.None));
            }
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

        public static Type GetAbi(ref Pinnable p)
        {
            return new Type
            {
                Name = MarshalString.GetAbi(ref p.Name),
                Kind = p.Kind
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

#if NET
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2057", 
            Justification = "Any types which are trimmed are not used by managed user code and there is fallback logic to handle that.")]
#endif
        public static global::System.Type FromAbi(Type value)
        {
            string name = MarshalString.FromAbi(value.Name);
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            if (value.Kind == TypeKind.Custom)
            {
                return global::System.Type.GetType(name);
            }

            var type = TypeNameSupport.FindTypeByNameCached(name);

#if NET
            // The type might have been trimmed, represent it with a fake type if requested
            // by the the Xaml metadata provider. Given there are no C# references to it, it shouldn't
            // be used, but if it is, an exception will be thrown.
            if (type == null && value.Kind == TypeKind.Metadata)
            {
                type = FakeMetadataType.GetFakeMetadataType(name);
            }
#endif

            return type;
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

    // Restricting to NET5 or greater as TypeInfo doesn't expose a constructor on .NET Standard 2.0 and we only need this
    // for WinUI scenarios.
#if NET
    internal sealed class FakeMetadataType : global::System.Reflection.TypeInfo
    {
        private static readonly ConcurrentDictionary<string, FakeMetadataType> fakeMetadataTypeCache = new(StringComparer.Ordinal);

        private readonly string fullName;

        private FakeMetadataType(string fullName)
        {
            this.fullName = fullName;
        }

        internal static FakeMetadataType GetFakeMetadataType(string name)
        {
            return fakeMetadataTypeCache.GetOrAdd(name, (name) => new FakeMetadataType(name));
        }

        public override Assembly Assembly => throw new InvalidOperationException();

        public override string AssemblyQualifiedName => throw new InvalidOperationException();

        public override global::System.Type BaseType => throw new InvalidOperationException();

        public override string FullName => fullName;

        public override Guid GUID => throw new InvalidOperationException();

        public override Module Module => throw new InvalidOperationException();

        public override string Namespace => throw new InvalidOperationException();

        public override global::System.Type UnderlyingSystemType => throw new InvalidOperationException();

        public override string Name => throw new InvalidOperationException();

        [SuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
        public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
        {
            throw new InvalidOperationException();
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            throw new InvalidOperationException();
        }

        public override object[] GetCustomAttributes(global::System.Type attributeType, bool inherit)
        {
            throw new InvalidOperationException();
        }

        public override global::System.Type GetElementType()
        {
            throw new InvalidOperationException();
        }

        [SuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
        public override EventInfo GetEvent(string name, BindingFlags bindingAttr)
        {
            throw new InvalidOperationException();
        }

        [SuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
        public override EventInfo[] GetEvents(BindingFlags bindingAttr)
        {
            throw new InvalidOperationException();
        }

        [SuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
        public override FieldInfo GetField(string name, BindingFlags bindingAttr)
        {
            throw new InvalidOperationException();
        }

        [SuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
        public override FieldInfo[] GetFields(BindingFlags bindingAttr)
        {
            throw new InvalidOperationException();
        }

        [SuppressMessage("Trimming", "IL2093", Justification = "This method will always throw.")]
        [SuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
        public override global::System.Type GetInterface(string name, bool ignoreCase)
        {
            throw new InvalidOperationException();
        }

        [SuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
        public override global::System.Type[] GetInterfaces()
        {
            throw new InvalidOperationException();
        }

        [SuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
        public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
        {
            throw new InvalidOperationException();
        }

        [SuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
        public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
        {
            throw new InvalidOperationException();
        }

        [SuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
        public override global::System.Type GetNestedType(string name, BindingFlags bindingAttr)
        {
            throw new InvalidOperationException();
        }

        [SuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
        public override global::System.Type[] GetNestedTypes(BindingFlags bindingAttr)
        {
            throw new InvalidOperationException();
        }

        [SuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
        public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
        {
            throw new InvalidOperationException();
        }

        [SuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
        public override object InvokeMember(string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
        {
            throw new InvalidOperationException();
        }

        public override bool IsDefined(global::System.Type attributeType, bool inherit)
        {
            throw new InvalidOperationException();
        }

        protected override TypeAttributes GetAttributeFlagsImpl()
        {
            throw new InvalidOperationException();
        }

        [SuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
        protected override ConstructorInfo GetConstructorImpl(BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, global::System.Type[] types, ParameterModifier[] modifiers)
        {
            throw new InvalidOperationException();
        }

        [SuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
        protected override MethodInfo GetMethodImpl(string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, global::System.Type[] types, ParameterModifier[] modifiers)
        {
            throw new InvalidOperationException();
        }

        [SuppressMessage("Trimming", "IL2094", Justification = "This method will always throw.")]
        protected override PropertyInfo GetPropertyImpl(string name, BindingFlags bindingAttr, Binder binder, global::System.Type returnType, global::System.Type[] types, ParameterModifier[] modifiers)
        {
            throw new InvalidOperationException();
        }

        protected override bool HasElementTypeImpl()
        {
            throw new InvalidOperationException();
        }

        protected override bool IsArrayImpl()
        {
            throw new InvalidOperationException();
        }

        protected override bool IsByRefImpl()
        {
            throw new InvalidOperationException();
        }

        protected override bool IsCOMObjectImpl()
        {
            throw new InvalidOperationException();
        }

        protected override bool IsPointerImpl()
        {
            throw new InvalidOperationException();
        }

        protected override bool IsPrimitiveImpl()
        {
            throw new InvalidOperationException();
        }

        public override string ToString()
        {
            return fullName;
        }

        public override bool Equals(object o)
        {
            return ReferenceEquals(this, o);
        }

        public override bool Equals(global::System.Type o)
        {
            return ReferenceEquals(this, o);
        }

        public override int GetHashCode()
        {
            return fullName.GetHashCode();
        }
    }
#endif
}
