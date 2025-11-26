// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for Windows Runtime types.
/// </summary>
internal static class WindowsRuntimeExtensions
{
    extension(IHasCustomAttribute type)
    {
        /// <summary>
        /// Checks whether a <see cref="IHasCustomAttribute"/> represents a projected Windows Runtime type.
        /// </summary>
        /// <returns>Whether the type represents a projected Windows Runtime type.</returns>
        public bool IsProjectedWindowsRuntimeType => type.HasCustomAttribute(WellKnownMetadataNames.WindowsRuntime, WellKnownMetadataNames.WindowsRuntimeMetadataAttribute);
    }

    extension(ITypeDescriptor type)
    {
        /// <summary>
        /// Checks whether an <see cref="ITypeDescriptor"/> is some <see cref="System.Guid"/> type.
        /// </summary>
        /// <returns>Whether the type is some <see cref="System.Guid"/> type.</returns>
        public bool IsGuidType(InteropReferences interopReferences)
        {
            return SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Guid);
        }

        /// <summary>
        /// Checks whether an <see cref="ITypeDescriptor"/> represents a fundamental Windows Runtime type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>Whether the input type is a fundamental Windows Runtime type.</returns>
        public bool IsFundamentalWindowsRuntimeType(InteropReferences interopReferences)
        {
            // Check all fundamental primitive types
            if (SignatureComparer.IgnoreVersion.Equals(type, interopReferences.CorLibTypeFactory.Boolean) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.CorLibTypeFactory.String) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.CorLibTypeFactory.Single) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.CorLibTypeFactory.Double) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.CorLibTypeFactory.UInt16) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.CorLibTypeFactory.UInt32) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.CorLibTypeFactory.UInt64) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.CorLibTypeFactory.Int16) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.CorLibTypeFactory.Int32) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.CorLibTypeFactory.Int64) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.CorLibTypeFactory.Char) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.CorLibTypeFactory.Byte) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.CorLibTypeFactory.Object))
            {
                return true;
            }

            // 'Guid' is special and also counts as a fundamental type
            return SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Guid);
        }

        /// <summary>
        /// Checks whether an <see cref="ITypeDescriptor"/> represents a custom-mapped Windows Runtime generic interface type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>Whether the type represents a custom-mapped Windows Runtime generic interface type.</returns>
        public bool IsCustomMappedWindowsRuntimeGenericInterfaceType(InteropReferences interopReferences)
        {
            return
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IEnumerable1) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IEnumerator1) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IList1) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IReadOnlyList1) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IDictionary2) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IReadOnlyDictionary2);
        }

        /// <summary>
        /// Checks whether an <see cref="ITypeDescriptor"/> represents a custom-mapped Windows Runtime non-generic interface type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>Whether the type represents a custom-mapped Windows Runtime non-generic interface type.</returns>
        public bool IsCustomMappedWindowsRuntimeNonGenericInterfaceType(InteropReferences interopReferences)
        {
            return
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IDisposable) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IServiceProvider) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.ICommand) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.INotifyCollectionChanged) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.INotifyDataErrorInfo) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.INotifyPropertyChanged) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IAsyncInfo) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IAsyncAction);
        }

        /// <summary>
        /// Checks whether an <see cref="ITypeDescriptor"/> represents a custom-mapped Windows Runtime non-generic delegate type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>Whether the type represents a custom-mapped Windows Runtime non-generic delegate type.</returns>
        public bool IsCustomMappedWindowsRuntimeNonGenericDelegateType(InteropReferences interopReferences)
        {
            return
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.NotifyCollectionChangedEventHandler) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.PropertyChangedEventHandler) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.EventHandler);
        }

        /// <summary>
        /// Checks whether a given type is blittable.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>Whether the type is blittable.</returns>
        public bool IsBlittable(InteropReferences interopReferences)
        {
            // Only value types are possibly blittable
            if (!type.IsValueType)
            {
                return false;
            }

            // Generic instantiations can never be blittable (as they're pointers at the ABI level)
            if (type is GenericInstanceTypeSignature)
            {
                return false;
            }

            TypeDefinition typeDefinition = type.Resolve()!;

            // Enum types are always blittable
            if (typeDefinition.IsEnum)
            {
                return true;
            }

            // All fundamental types are blittable (i.e. primitive types)
            if (IsFundamentalWindowsRuntimeType(type, interopReferences))
            {
                return true;
            }

            // The 'TimeSpan' and 'DateTimeOffset' types are not blittable (even though they're custom-mapped)
            if (SignatureComparer.IgnoreVersion.Equals(type, interopReferences.TimeSpan) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.DateTimeOffset))
            {
                return false;
            }

            // We have some complex struct, so we need to recursively check all fields
            foreach (FieldDefinition fieldDefinition in typeDefinition.Fields)
            {
                // We only care about non-constant instance fields
                if (fieldDefinition.IsStatic || fieldDefinition.IsLiteral)
                {
                    continue;
                }

                // If any fields aren't blittable, then the whole type isn't
                if (!fieldDefinition.Signature!.FieldType.IsBlittable(interopReferences))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks whether a given type is managed (i.e. it requires disposal)..
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>Whether the type is a managed value type.</returns>
        public bool IsManagedValueType(InteropReferences interopReferences)
        {
            if (!type.IsValueType)
            {
                return false;
            }

            // Generic instantiations (i.e. 'Nullable<T>' or 'KeyValuePair<,>') need disposal
            if (type is GenericInstanceTypeSignature)
            {
                return true;
            }

            TypeDefinition typeDefinition = type.Resolve()!;

            // Enum types are always blittable
            if (typeDefinition.IsEnum)
            {
                return false;
            }

            // All fundamental types are blittable (i.e. primitive types)
            if (IsFundamentalWindowsRuntimeType(type, interopReferences))
            {
                return false;
            }

            // The 'TimeSpan' and 'DateTimeOffset' are not blittable, but don't need disposal
            if (SignatureComparer.IgnoreVersion.Equals(type, interopReferences.TimeSpan) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.DateTimeOffset))
            {
                return false;
            }

            // For complex struct types, crawl all fields (same as in 'IsBlittable')
            foreach (FieldDefinition fieldDefinition in typeDefinition.Fields)
            {
                if (fieldDefinition.IsStatic || fieldDefinition.IsLiteral)
                {
                    continue;
                }

                // If any fields are reference types, then the containing type needs disposal
                if (fieldDefinition.Signature!.FieldType.IsValueType)
                {
                    return true;
                }

                // If any fields are managed, then the containing type needs disposal too
                if (fieldDefinition.Signature!.FieldType.IsManagedValueType(interopReferences))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the ABI type for a given type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>The ABi type for the input type.</returns>
        public TypeSignature GetAbiType(InteropReferences interopReferences)
        {
            // All constructed generics will use 'void*' for the ABI type. This applies to both reference
            // types, as well as 'KeyValuePair<,>', which also maps to 'void*', since it's an interface.
            if (type is GenericInstanceTypeSignature)
            {
                return interopReferences.CorLibTypeFactory.Void.MakePointerType();
            }

            TypeDefinition typeDefinition = type.Resolve()!;

            if (typeDefinition.IsValueType)
            {
                // If the type is blittable, then it's the same as the ABI type
                if (type.IsBlittable(interopReferences))
                {
                    return type.ToTypeDefOrRef().ToValueTypeSignature();
                }

                // 'TimeSpan' is custom-mapped and not blittable
                if (SignatureComparer.IgnoreVersion.Equals(type, interopReferences.TimeSpan))
                {
                    return interopReferences.AbiTimeSpan.ToValueTypeSignature();
                }

                // 'DateTimeOffset' also is custom-mapped and not blittable
                if (SignatureComparer.IgnoreVersion.Equals(type, interopReferences.DateTimeOffset))
                {
                    return interopReferences.AbiDateTimeOffset.ToValueTypeSignature();
                }

                // Otherwise, we can rely on the blittable type being defined in the same module under the 'ABI' namespace
                return typeDefinition.DeclaringModule!.CreateTypeReference(
                    ns: (Utf8String)$"ABI.{typeDefinition.Namespace}",
                    name: typeDefinition.Name!).ToValueTypeSignature();
            }

            // We have a few special cases to handle for custom-mapped types that are reference types in C#
            if (typeDefinition.IsClass)
            {
                // 'Type' is a class, but is custom-mapped to the 'TypeName' struct type
                if (SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Type))
                {
                    return interopReferences.AbiType.ToValueTypeSignature();
                }

                // 'Exception' is also a class, but is custom-mapped to the 'HResult' struct type
                if (SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Exception))
                {
                    return interopReferences.AbiException.ToValueTypeSignature();
                }
            }

            // For all other cases (e.g. interfaces, classes, delegates, etc.), the ABI type is always a pointer
            return interopReferences.CorLibTypeFactory.Void.MakePointerType();
        }
    }

    extension(TypeDefinition type)
    {
        /// <summary>
        /// Checks whether a <see cref="TypeDefinition"/> represents a projected Windows Runtime class type.
        /// </summary>
        public bool IsProjectedWindowsRuntimeClassType
        {
            get
            {
                // We only care about classes
                if (type is not { IsClass: true, IsValueType: false, IsDelegate: false })
                {
                    return false;
                }

                // Ignore static types
                if (type.IsStatic)
                {
                    return false;
                }

                // The type also must be a projected type
                return type.IsProjectedWindowsRuntimeType;
            }
        }

        /// <summary>
        /// Checks whether a <see cref="TypeDefinition"/> represents a type that can be constructed (i.e. instantiated).
        /// </summary>
        public bool IsConstructibleType => type is { IsInterface: false, IsAbstract: false };

        /// <summary>
        /// Checks whether a <see cref="TypeDefinition"/> represents a type that can possibly be exposed to Windows Runtime.
        /// </summary>
        public bool IsPossiblyWindowsRuntimeExposedType
        {
            get
            {
                // Only constructible types can possibly be exposed types
                if (!get_IsConstructibleType(type))
                {
                    return false;
                }

                // Only user-defined class types and struct types (not enums) can be exposed types.
                // We ignore delegates too, as generic delegates are already processed separately.
                return type is { IsInterface: false, IsDelegate: false, IsEnum: false, IsByRefLike: false };
            }
        }

        /// <summary>
        /// Checks whether a <see cref="TypeDefinition"/> represents a Windows Runtime managed-only type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>Whether the input type is a Windows Runtime managed-only type.</returns>
        public bool IsWindowsRuntimeManagedOnlyType(InteropReferences interopReferences)
        {
            return type.HasOrInheritsAttribute(interopReferences.WindowsRuntimeManagedOnlyTypeAttribute, interopReferences.CorLibTypeFactory);
        }

        /// <summary>
        /// Gets the Windows Runtime metadata name for a <see cref="TypeDefinition"/>, if available.
        /// </summary>
        /// <returns>The Windows Runtime metadata name from the <c>WindowsRuntimeMetadataAttribute</c>, or <see langword="null"/> if not found.</returns>
        public Utf8String? GetWindowsRuntimeMetadataName()
        {
            CustomAttribute? attribute = type.FindCustomAttributes("WindowsRuntime"u8, "WindowsRuntimeMetadataAttribute"u8).FirstOrDefault();

            return attribute?.Signature?.FixedArguments?[0]?.Element as Utf8String;
        }
    }

    extension(TypeSignature signature)
    {
        /// <summary>
        /// Checks whether a <see cref="TypeSignature"/> is some <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.
        /// </summary>
        /// <returns>Whether the type is some <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.</returns>
        public bool IsConstructedKeyValuePairType(InteropReferences interopReferences)
        {
            return SignatureComparer.IgnoreVersion.Equals((signature as GenericInstanceTypeSignature)?.GenericType, interopReferences.KeyValuePair2);
        }

        /// <summary>
        /// Checks whether a <see cref="TypeSignature"/> is some <see cref="System.Nullable{T}"/> type.
        /// </summary>
        /// <returns>Whether the type is some <see cref="System.Nullable{T}"/> type.</returns>
        public bool IsConstructedNullableValueType(InteropReferences interopReferences)
        {
            return SignatureComparer.IgnoreVersion.Equals((signature as GenericInstanceTypeSignature)?.GenericType, interopReferences.Nullable1);
        }

        /// <summary>
        /// Checks whether a <see cref="GenericInstanceTypeSignature"/> represents a custom-mapped Windows Runtime interface type.
        /// </summary>
        /// <returns>Whether the type represents a custom-mapped Windows Runtime interface type.</returns>
        public bool IsCustomMappedWindowsRuntimeInterfaceType(InteropReferences interopReferences)
        {
            return signature is GenericInstanceTypeSignature genericSignature
                ? genericSignature.GenericType.IsCustomMappedWindowsRuntimeGenericInterfaceType(interopReferences)
                : signature.IsCustomMappedWindowsRuntimeNonGenericInterfaceType(interopReferences);
        }

        /// <summary>
        /// Checks whether a <see cref="GenericInstanceTypeSignature"/> represents a custom-mapped Windows Runtime delegate type.
        /// </summary>
        /// <returns>Whether the type represents a custom-mapped Windows Runtime delegate type.</returns>
        public bool IsCustomMappedWindowsRuntimeDelegateType(InteropReferences interopReferences)
        {
            if (signature is GenericInstanceTypeSignature genericSignature)
            {
                return
                    SignatureComparer.IgnoreVersion.Equals(genericSignature.GenericType, interopReferences.EventHandler1) ||
                    SignatureComparer.IgnoreVersion.Equals(genericSignature.GenericType, interopReferences.EventHandler2);
            }

            // The only non-generic custom-mapped delegate type is 'EventHandler'
            return signature.IsCustomMappedWindowsRuntimeNonGenericDelegateType(interopReferences);
        }

        /// <summary>
        /// Gets the Windows Runtime metadata name for a <see cref="TypeSignature"/>, if available.
        /// </summary>
        /// <returns>The Windows Runtime metadata name from the underlying type's <c>WindowsRuntimeMetadataAttribute</c>, or <see langword="null"/> if not found.</returns>
        /// <remarks>
        /// <para>
        /// This method resolves the underlying type definition from the signature and retrieves its Windows Runtime metadata name.
        /// For generic instance types, it uses the generic type definition. For array types, it uses the base element type.
        /// For other types, it resolves the type definition directly.
        /// </para>
        /// </remarks>
        public Utf8String? GetWindowsRuntimeMetadataName()
        {
            return signature switch
            {
                GenericInstanceTypeSignature generic => generic.GenericType.Resolve()?.GetWindowsRuntimeMetadataName(),
                ArrayTypeSignature array => array.BaseType.GetWindowsRuntimeMetadataName(),
                _ => signature.ToTypeDefOrRef().Resolve()?.GetWindowsRuntimeMetadataName()
            };
        }
    }

    extension(ModuleDefinition module)
    {
        /// <summary>
        /// Checks whether a <see cref="ModuleDefinition"/> is the Windows Runtime assembly.
        /// </summary>
        /// <returns>Whether the module is the Windows Runtime assembly.</returns>
        public bool IsWindowsRuntimeModule => module.Name == WellKnownMetadataNames.WinRTRuntime2ModuleName;

        /// <summary>
        /// Checks whether a <see cref="ModuleDefinition"/> references the Windows Runtime assembly.
        /// </summary>
        /// <returns>Whether the module references the Windows Runtime assembly.</returns>
        public bool ReferencesWindowsRuntimeAssembly => module.ReferencesAssembly(WellKnownMetadataNames.WinRTRuntime2AssemblyName);

        /// <summary>
        /// Checks whether a <see cref="ModuleDefinition"/> references the Windows Runtime assembly version 2.
        /// </summary>
        /// <returns>Whether the module references the Windows Runtime assembly version 2.</returns>
        public bool ReferencesWindowsRuntimeVersion2Assembly
        {
            get
            {
                // Look for the 'WinRT.Runtime.dll' reference, and check if its version is the one for CsWinRT 2.x.
                // We need to enumerate and check all references, as we also expect CsWinRT 3.0 assembly references.
                foreach (AssemblyReference reference in module.EnumerateAssemblyReferences())
                {
                    if (reference.Name == WellKnownMetadataNames.WinRTRuntimeAssemblyName &&
                        reference.Version.Major == 2)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}

/// <summary>
/// Cached, well-known metadata names.
/// </summary>
file static class WellKnownMetadataNames
{
    /// <summary>
    /// The current name of the WinRT runtime assembly.
    /// </summary>
    public static readonly Utf8String WinRTRuntimeAssemblyName = "WinRT.Runtime"u8;

    /// <summary>
    /// The current name of the WinRT runtime assembly.
    /// </summary>
    public static readonly Utf8String WinRTRuntime2AssemblyName = "WinRT.Runtime2"u8;

    /// <summary>
    /// The current name of the WinRT runtime module.
    /// </summary>
    public static readonly Utf8String WinRTRuntime2ModuleName = "WinRT.Runtime2.dll"u8;

    /// <summary>
    /// The <c>"WindowsRuntime"</c> text.
    /// </summary>
    public static readonly Utf8String WindowsRuntime = "WindowsRuntime"u8;

    /// <summary>
    /// The <c>"WindowsRuntimeMetadataAttribute"</c> text.
    /// </summary>
    public static readonly Utf8String WindowsRuntimeMetadataAttribute = "WindowsRuntimeMetadataAttribute"u8;
}