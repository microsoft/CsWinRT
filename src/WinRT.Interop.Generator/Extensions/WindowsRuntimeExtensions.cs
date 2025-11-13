// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
        /// Checks whether a <see cref="TypeDefinition"/> represents a fundamental Windows Runtime type.
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
        public bool IsKeyValuePairType(InteropReferences interopReferences)
        {
            return SignatureComparer.IgnoreVersion.Equals((signature as GenericInstanceTypeSignature)?.GenericType, interopReferences.KeyValuePair2);
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
            return SignatureComparer.IgnoreVersion.Equals(signature, interopReferences.EventHandler);
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
        /// Checks whether a <see cref="ModuleDefinition"/> is or references the Windows Runtime assembly.
        /// </summary>
        /// <returns>Whether the module is or references the Windows Runtime assembly.</returns>
        public bool IsOrReferencesWindowsRuntimeAssembly
        {
            get
            {
                // If the assembly references the Windows Runtime assembly, gather it
                foreach (AssemblyReference reference in module.AssemblyReferences)
                {
                    if (reference.Name?.AsSpan().SequenceEqual(InteropNames.WinRTRuntime2DllNameUtf8[..^4]) is true)
                    {
                        return true;
                    }
                }

                // Otherwise, check if it's the Windows Runtime assembly itself
                return module.Name?.AsSpan().SequenceEqual(InteropNames.WinRTRuntime2DllNameUtf8) is true;
            }
        }

        /// <summary>
        /// Checks whether a <see cref="ModuleDefinition"/> is the Windows Runtime assembly.
        /// </summary>
        /// <returns>Whether the module is the Windows Runtime assembly.</returns>
        public bool IsWindowsRuntimeAssembly => module.Name?.AsSpan().SequenceEqual(InteropNames.WinRTRuntimeDllNameUtf8) is true;

        /// <summary>
        /// Checks whether a <see cref="ModuleDefinition"/> references 'WinRT.Runtime.dll' version 2.
        /// </summary>
        /// <returns>Whether the module references 'WinRT.Runtime.dll' version 2.</returns>
        public bool ReferencesWinRTRuntimeDllVersion2
        {
            get
            {
                // Get the 'WinRT.Runtime.dll' reference, and check if its version is the one for CsWinRT 2.x
                foreach (AssemblyReference reference in module.AssemblyReferences)
                {
                    if (reference.Name?.AsSpan().SequenceEqual(InteropNames.WinRTRuntimeDllNameUtf8[..^4]) is true)
                    {
                        return reference.Version.Major == 2;
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
    /// The <c>"WindowsRuntime"</c> text.
    /// </summary>
    public static readonly Utf8String WindowsRuntime = "WindowsRuntime"u8;

    /// <summary>
    /// The <c>"WindowsRuntimeMetadataAttribute"</c> text.
    /// </summary>
    public static readonly Utf8String WindowsRuntimeMetadataAttribute = "WindowsRuntimeMetadataAttribute"u8;
}
