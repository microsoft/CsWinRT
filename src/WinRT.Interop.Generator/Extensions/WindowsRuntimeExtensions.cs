// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for Windows Runtime types.
/// </summary>
internal static class WindowsRuntimeExtensions
{
    extension(IHasCustomAttribute member)
    {
        /// <summary>
        /// Checks whether a <see cref="IHasCustomAttribute"/> represents a projected Windows Runtime type.
        /// </summary>
        /// <returns>Whether the type represents a projected Windows Runtime type.</returns>
        public bool IsProjectedWindowsRuntimeType => member.HasCustomAttribute(WellKnownMetadataNames.WindowsRuntime, WellKnownMetadataNames.WindowsRuntimeMetadataAttribute);

        /// <summary>
        /// Checks whether a <see cref="IHasCustomAttribute"/> (expected to be an <see cref="AssemblyDefinition"/>) represents a Windows Runtime reference assembly.
        /// </summary>
        /// <returns>Whether the module represents a Windows Runtime reference assembly.</returns>
        public bool IsWindowsRuntimeReferenceAssembly => member.HasCustomAttribute(WellKnownMetadataNames.WindowsRuntimeInteropServices, WellKnownMetadataNames.WindowsRuntimeReferenceAssemblyAttribute);

        /// <summary>
        /// Checks whether a <see cref="IHasCustomAttribute"/> (expected to be an <see cref="AssemblyDefinition"/>) represents a Windows Runtime component assembly.
        /// </summary>
        /// <returns>Whether the module represents a Windows Runtime component assembly.</returns>
        public bool IsWindowsRuntimeComponentAssembly => member.HasCustomAttribute(WellKnownMetadataNames.WindowsRuntimeInteropServices, WellKnownMetadataNames.WindowsRuntimeComponentAssemblyAttribute);

        /// <summary>
        /// Attempts to retrieve the IID from the <see cref="System.Runtime.InteropServices.GuidAttribute"/> applied to the specified metadata member.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="iid">The resulting <see cref="Guid"/> value, if found.</param>
        /// <returns>Whether <paramref name="iid"/> was successfully retrieved.</returns>
        public bool TryGetGuidAttribute(InteropReferences interopReferences, out Guid iid)
        {
            if (member.TryGetCustomAttribute(interopReferences.GuidAttribute, out CustomAttribute? customAttribute))
            {
                if (customAttribute.Signature is { FixedArguments: [{ Element: Utf8String guidString }, ..] })
                {
                    return Guid.TryParse(guidString.Value, out iid);
                }
            }

            iid = Guid.Empty;

            return false;
        }
    }

    extension(ITypeDescriptor type)
    {
        /// <summary>
        /// Gets a value indicating whether the type is a projected Windows SDK type (not custom-mapped or manually-projected).
        /// </summary>
        public bool IsProjectedWindowsSdkType
        {
            get
            {
                // Because only the Windows SDK can define Windows Runtime types in the 'Windows.*' namespaces,
                // we can use this to determine the right implementation projection .dll to use for the lookup.
                // We also optimize when an UTF8 value is available to avoid redundant UTF8 transcoding work.
                return type switch
                {
                    ITypeDefOrRef { Namespace: Utf8String ns } => ns.AsSpan().StartsWith("Windows."u8),
                    ITypeDefOrRef => false,
                    { Namespace: string ns } => ns.StartsWith("Windows."),
                    _ => false
                };
            }
        }

        /// <summary>
        /// Checks whether an <see cref="ITypeDescriptor"/> is some <see cref="Guid"/> type.
        /// </summary>
        /// <returns>Whether the type is some <see cref="Guid"/> type.</returns>
        public bool IsTypeOfGuid(InteropReferences interopReferences)
        {
            return SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Guid);
        }

        /// <summary>
        /// Checks whether an <see cref="ITypeDescriptor"/> is some <see cref="Type"/> type.
        /// </summary>
        /// <returns>Whether the type is some <see cref="Type"/> type.</returns>
        public bool IsTypeOfType(InteropReferences interopReferences)
        {
            return SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Type);
        }

        /// <summary>
        /// Checks whether an <see cref="ITypeDescriptor"/> is some <see cref="Exception"/> type.
        /// </summary>
        /// <returns>Whether the type is some <see cref="Exception"/> type.</returns>
        public bool IsTypeOfException(InteropReferences interopReferences)
        {
            return SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Exception);
        }

        /// <summary>
        /// Checks whether an <see cref="ITypeDescriptor"/> is some <see cref="string"/> type.
        /// </summary>
        public bool IsTypeOfString()
        {
            return type is CorLibTypeSignature { ElementType: ElementType.String };
        }

        /// <summary>
        /// Checks whether an <see cref="ITypeDescriptor"/> is some <see cref="object"/> type.
        /// </summary>
        /// <returns>Whether the type is some <see cref="object"/> type.</returns>
        public bool IsTypeOfObject()
        {
            return type is CorLibTypeSignature { ElementType: ElementType.Object };
        }

        /// <summary>
        /// Checks whether an <see cref="ITypeDescriptor"/> is a <see cref="void"/> pointer type.
        /// </summary>
        /// <returns>Whether the type is a <see cref="void"/> pointer type.</returns>
        public bool IsTypeOfVoidPointer()
        {
            return type is PointerTypeSignature { BaseType: CorLibTypeSignature { ElementType: ElementType.Void } };
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
        /// Checks whether an <see cref="ITypeDescriptor"/> represents a custom-mapped Windows Runtime non-generic struct or class type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>Whether the type represents a custom-mapped Windows Runtime non-generic struct or class type.</returns>
        /// <remarks>
        /// This method doesn't check for interface types and delegate types. Use the other variants below for that.
        /// </remarks>
        public bool IsCustomMappedWindowsRuntimeNonGenericStructOrClassType(InteropReferences interopReferences)
        {
            return
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.TimeSpan) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.DateTimeOffset) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Exception) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Type) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Uri) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Matrix3x2) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Matrix4x4) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Plane) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Quaternion) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Vector2) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Vector3) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Vector4) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.DataErrorsChangedEventArgs) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.PropertyChangedEventArgs) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.NotifyCollectionChangedAction) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.NotifyCollectionChangedEventArgs);
        }

        /// <summary>
        /// Checks whether an <see cref="ITypeDescriptor"/> represents a manually-projected Windows Runtime non-generic struct or class type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>Whether the type represents a manually-projected Windows Runtime non-generic struct or class type.</returns>
        /// <remarks>
        /// This method doesn't check for interface types and delegate types. Use the other variants below for that.
        /// </remarks>
        public bool IsManuallyProjectedWindowsRuntimeNonGenericStructOrClassType(InteropReferences interopReferences)
        {
            return
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.CollectionChange) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.AsyncStatus) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.PropertyType) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Point) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Rect) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Size) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.EventRegistrationToken) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.InputStreamOptions);
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
        /// Checks whether an <see cref="ITypeDescriptor"/> represents a manually-projected Windows Runtime generic interface type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>Whether the type represents a manually-projected Windows Runtime generic interface type.</returns>
        public bool IsManuallyProjectedWindowsRuntimeGenericInterfaceType(InteropReferences interopReferences)
        {
            return
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IAsyncActionWithProgress1) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IAsyncOperation1) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IAsyncOperationWithProgress2) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IMapChangedEventArgs1) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IObservableMap2) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IObservableVector1);
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
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IEnumerable) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IEnumerator) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IList) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.INotifyCollectionChanged) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.INotifyDataErrorInfo) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.INotifyPropertyChanged);
        }

        /// <summary>
        /// Checks whether an <see cref="ITypeDescriptor"/> represents a manually-projected Windows Runtime non-generic interface type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>Whether the type represents a manually-projected Windows Runtime non-generic interface type.</returns>
        public bool IsManuallyProjectedWindowsRuntimeNonGenericInterfaceType(InteropReferences interopReferences)
        {
            return
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IAsyncAction) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IAsyncInfo) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IVectorChangedEventArgs) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IStringable) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IBuffer) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IMemoryBufferReference) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IInputStream) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IOutputStream) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IRandomAccessStream);
        }

        /// <summary>
        /// Checks whether an <see cref="ITypeDescriptor"/> represents a custom-mapped Windows Runtime generic delegate type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>Whether the type represents a custom-mapped Windows Runtime generic delegate type.</returns>
        public bool IsCustomMappedWindowsRuntimeGenericDelegateType(InteropReferences interopReferences)
        {
            return
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.EventHandler1) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.EventHandler2);
        }

        /// <summary>
        /// Checks whether an <see cref="ITypeDescriptor"/> represents a manually-projected Windows Runtime generic delegate type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>Whether the type represents a manually-projected Windows Runtime generic delegate type.</returns>
        public bool IsManuallyProjectedWindowsRuntimeGenericDelegateType(InteropReferences interopReferences)
        {
            return
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.AsyncActionProgressHandler1) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.AsyncActionWithProgressCompletedHandler1) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.AsyncOperationCompletedHandler1) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.AsyncOperationProgressHandler2) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.AsyncOperationWithProgressCompletedHandler2) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.MapChangedEventHandler2) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.VectorChangedEventHandler1);
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
        /// Checks whether an <see cref="ITypeDescriptor"/> represents a manually-projected Windows Runtime non-generic delegate type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>Whether the type represents a manually-projected Windows Runtime non-generic delegate type.</returns>
        public bool IsManuallyProjectedWindowsRuntimeNonGenericDelegateType(InteropReferences interopReferences)
        {
            return SignatureComparer.IgnoreVersion.Equals(type, interopReferences.AsyncActionCompletedHandler);
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
        /// Checks whether a given type is managed (i.e. it requires disposal).
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

                // If any fields are reference types, then the containing type needs disposal.
                // The only special case is for fields of type 'Exception', as the ABI type
                // for it is actually an unmanaged value type, which doesn't need disposal.
                if (!fieldDefinition.Signature!.FieldType.IsValueType)
                {
                    return !fieldDefinition.Signature.FieldType.IsTypeOfException(interopReferences);
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
        /// Checks whether a given type needs tracker support (when marshalled as a CCW).
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>Whether the type requires tracker support.</returns>
        public bool IsTrackerSupportRequired(InteropReferences interopReferences)
        {
            // Check reference types first, as there's fewer special cases to handle
            if (!type.IsValueType)
            {
                // 'string' objects don't need tracker support, as they can't reference anything
                if (type.IsTypeOfString())
                {
                    return false;
                }

                // For array types, tracker support is required if the element type requires it.
                // E.g. an 'int[]' or a 'string[]' array doesn't need it, but 'object[]' does.
                if (type is SzArrayTypeSignature arrayType)
                {
                    return arrayType.BaseType.IsTrackerSupportRequired(interopReferences);
                }

                // For all other cases, we assume tracker support is required, to be safe
                return true;
            }

            // For generic value types (i.e. 'Nullable<T>' or 'KeyValuePair<,>'), we only need
            // tracker support if any of the type arguments actually requires tracker support.
            if (type is GenericInstanceTypeSignature genericType)
            {
                foreach (TypeSignature typeArgument in genericType.TypeArguments)
                {
                    if (typeArgument.IsTrackerSupportRequired(interopReferences))
                    {
                        return true;
                    }
                }

                return false;
            }

            TypeDefinition typeDefinition = type.Resolve()!;

            // Enum types are blittable, so they never need tracker support
            if (typeDefinition.IsEnum)
            {
                return false;
            }

            // All fundamental types are blittable, so same as for enum types
            if (IsFundamentalWindowsRuntimeType(type, interopReferences))
            {
                return false;
            }

            // The 'TimeSpan' and 'DateTimeOffset' types are not blittable, but they're also unmanaged
            if (SignatureComparer.IgnoreVersion.Equals(type, interopReferences.TimeSpan) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.DateTimeOffset))
            {
                return false;
            }

            // For complex struct types, crawl all fields (same as the methods above)
            foreach (FieldDefinition fieldDefinition in typeDefinition.Fields)
            {
                if (fieldDefinition.IsStatic || fieldDefinition.IsLiteral)
                {
                    continue;
                }

                // If any fields need tracker support, then the containing type needs it too
                if (fieldDefinition.Signature!.FieldType.IsTrackerSupportRequired(interopReferences))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets whether a given type has an ABI type that is a reference type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>Whether the input type has an ABI type that is a reference type.</returns>
        public bool HasReferenceAbiType(InteropReferences interopReferences)
        {
            // All constructed generics will use 'void*' for the ABI type
            if (type is GenericInstanceTypeSignature)
            {
                return true;
            }

            // All other value types will never have a reference type as the ABI type
            if (type.IsValueType)
            {
                return false;
            }

            // 'Type' is a class, but is custom-mapped to the 'TypeName' struct type
            if (SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Type))
            {
                return false;
            }

            // 'Exception' is also a class, but is custom-mapped to the 'HResult' struct type
            if (SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Exception))
            {
                return false;
            }

            // For all other cases (e.g. interfaces, classes, delegates, etc.), the ABI type is always a pointer
            return true;
        }

        /// <summary>
        /// Gets the ABI type for a given type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>The ABI type for the input type.</returns>
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
                return interopReferences.WinRTProjection.CreateTypeReference(
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

        /// <summary>
        /// Gets the raw ABI type for a given type (without unwrapping).
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>The raw ABI type for the input type.</returns>
        public TypeSignature GetRawAbiType(InteropReferences interopReferences)
        {
            TypeSignature abiType = type.GetAbiType(interopReferences);

            // If the ABI type is 'void*', the marshaller types return it as 'WindowsRuntimeObjectReferenceValue'.
            // This allows callers to do proper lifetime management. For all other cases, the ABI type is the same.
            return abiType.IsTypeOfVoidPointer()
                ? interopReferences.WindowsRuntimeObjectReferenceValue.ToValueTypeSignature()
                : abiType;
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
                // Projected Windows Runtime classes can't be generic
                if (type.HasGenericParameters)
                {
                    return false;
                }

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
            return type.HasOrInheritsAttribute(interopReferences.WindowsRuntimeManagedOnlyTypeAttribute);
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
        /// Tries to extract the underlying type from a constructed <see cref="Nullable{T}"/> type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="underlyingType">The underlying nullable type, if the input type is a constructed <see cref="Nullable{T}"/> type.</param>
        /// <returns>Whether <paramref name="underlyingType"/> was successfully retrieved.</returns>
        public bool TryGetNullableUnderlyingType(InteropReferences interopReferences, [NotNullWhen(true)] out TypeSignature? underlyingType)
        {
            // First check that we have some constructed generic value type.
            // We also check that we have a single type argument to narrow down.
            if (signature is not GenericInstanceTypeSignature { IsValueType: true, TypeArguments: [TypeSignature typeArgument] } genericSignature)
            {
                underlyingType = null;

                return false;
            }

            // Check that we actually have a constructed 'Nullable<T>' type
            if (!SignatureComparer.IgnoreVersion.Equals(genericSignature.GenericType, interopReferences.Nullable1))
            {
                underlyingType = null;

                return false;
            }

            underlyingType = typeArgument;

            return true;
        }

        /// <summary>
        /// Gets the <see cref="MethodSignature"/> for the <c>Invoke</c> method of a given delegate type.
        /// </summary>
        /// <param name="module">The <see cref="ModuleDefinition"/> to use to import the delegate before resolving it.</param>
        /// <returns>The <see cref="MethodSignature"/> for the <c>Invoke</c> method for the input delegate type.</returns>
        public MethodSignature GetDelegateInvokeMethodSignature(ModuleDefinition module)
        {
            // Get the 'Invoke' method of the delegate type (this will remove the type arguments)
            MethodDefinition invokeMethod = signature.Resolve(module)!.GetMethod("Invoke"u8);

            // Construct the generic signature for the method with the context of the input delegate.
            // We can use this to get all the parameters, which might be any combination of explicitly
            // declared types, and constructed generic type parameters. Also, any number of them. If
            // the input delegate type is not generic instead, we just return the method signature as is.
            return signature is GenericInstanceTypeSignature genericSignature
                ? invokeMethod.Signature!.InstantiateGenericTypes(new GenericContext(genericSignature, null))
                : invokeMethod.Signature!;
        }

        /// <summary>
        /// Checks whether a <see cref="TypeSignature"/> is some <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>Whether the type is some <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.</returns>
        public bool IsConstructedKeyValuePairType(InteropReferences interopReferences)
        {
            return SignatureComparer.IgnoreVersion.Equals((signature as GenericInstanceTypeSignature)?.GenericType, interopReferences.KeyValuePair2);
        }

        /// <inheritdoc cref="IsConstructedKeyValuePairType(TypeSignature, InteropReferences)"/>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="keyType">The resulting key type, if the type did represent a <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.</param>
        /// <param name="valueType">The resulting value type, if the type did represent a <see cref="System.Collections.Generic.KeyValuePair{TKey, TValue}"/> type.</param>
        public bool IsConstructedKeyValuePairType(
            InteropReferences interopReferences,
            [NotNullWhen(true)] out TypeSignature? keyType,
            [NotNullWhen(true)] out TypeSignature? valueType)
        {
            // If the signature is not generic, it can't possibly be a 'KeyValuePair<TKey, TValue>' instantiation
            if (signature is not GenericInstanceTypeSignature genericSignature)
            {
                keyType = null;
                valueType = null;

                return false;
            }

            // Same check as overload above
            if (!SignatureComparer.IgnoreVersion.Equals(genericSignature.GenericType, interopReferences.KeyValuePair2))
            {
                keyType = null;
                valueType = null;

                return false;
            }

            keyType = genericSignature.TypeArguments[0];
            valueType = genericSignature.TypeArguments[1];

            return true;
        }

        /// <summary>
        /// Checks whether a <see cref="TypeSignature"/> is some <see cref="Nullable{T}"/> type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>Whether the type is some <see cref="Nullable{T}"/> type.</returns>
        public bool IsConstructedNullableValueType(InteropReferences interopReferences)
        {
            return SignatureComparer.IgnoreVersion.Equals((signature as GenericInstanceTypeSignature)?.GenericType, interopReferences.Nullable1);
        }

        /// <summary>
        /// Checks whether a <see cref="TypeSignature"/> is some <see cref="Span{T}"/> or <see cref="ReadOnlySpan{T}"/> type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>Whether the type is some <see cref="Span{T}"/> or <see cref="ReadOnlySpan{T}"/> type.</returns>
        public bool IsConstructedSpanOrReadOnlySpanType(InteropReferences interopReferences)
        {
            if (signature is not GenericInstanceTypeSignature genericSignature)
            {
                return false;
            }

            // Check for both 'Span<T>' and 'ReadOnlySpan<T>'
            return
                SignatureComparer.IgnoreVersion.Equals(genericSignature.GenericType, interopReferences.Span1) ||
                SignatureComparer.IgnoreVersion.Equals(genericSignature.GenericType, interopReferences.ReadOnlySpan1);
        }

        /// <summary>
        /// Checks whether a <see cref="TypeSignature"/> represents a custom-mapped Windows Runtime interface type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>Whether the type represents a custom-mapped Windows Runtime interface type.</returns>
        public bool IsCustomMappedWindowsRuntimeInterfaceType(InteropReferences interopReferences)
        {
            return signature is GenericInstanceTypeSignature genericSignature
                ? genericSignature.GenericType.IsCustomMappedWindowsRuntimeGenericInterfaceType(interopReferences)
                : signature.IsCustomMappedWindowsRuntimeNonGenericInterfaceType(interopReferences);
        }

        /// <summary>
        /// Checks whether a <see cref="TypeSignature"/> represents a manually-projected Windows Runtime interface type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>Whether the type represents a manually-projected Windows Runtime interface type.</returns>
        public bool IsManuallyProjectedWindowsRuntimeInterfaceType(InteropReferences interopReferences)
        {
            return signature is GenericInstanceTypeSignature genericSignature
                ? genericSignature.GenericType.IsManuallyProjectedWindowsRuntimeGenericInterfaceType(interopReferences)
                : signature.IsManuallyProjectedWindowsRuntimeNonGenericInterfaceType(interopReferences);
        }

        /// <summary>
        /// Checks whether a <see cref="TypeSignature"/> represents a custom-mapped Windows Runtime delegate type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>Whether the type represents a custom-mapped Windows Runtime delegate type.</returns>
        public bool IsCustomMappedWindowsRuntimeDelegateType(InteropReferences interopReferences)
        {
            return signature is GenericInstanceTypeSignature genericSignature
                ? genericSignature.GenericType.IsCustomMappedWindowsRuntimeGenericDelegateType(interopReferences)
                : signature.IsCustomMappedWindowsRuntimeNonGenericDelegateType(interopReferences);
        }

        /// <summary>
        /// Checks whether a <see cref="TypeSignature"/> represents a Windows Runtime type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>Whether the type represents a Windows Runtime type.</returns>
        public bool IsWindowsRuntimeType(InteropReferences interopReferences)
        {
            // Check SZ arrays first, as that's the simplest case to handle.
            // Arrays are Windows Runtime types if the element type is one.
            if (signature is SzArrayTypeSignature arrayType)
            {
                // Windows Runtime only allows single-dimensional (and not jagged) arrays
                if (arrayType.BaseType is SzArrayTypeSignature)
                {
                    return false;
                }

                // Validate that the element type of the array is itself a Windows Runtime type
                return arrayType.BaseType.IsWindowsRuntimeType(interopReferences);
            }

            // Check constructed generics next, as they can only be a handful of well-known cases
            if (signature is GenericInstanceTypeSignature genericInstance)
            {
                // For constructed generics, check if it's one of the custom-mapped Windows Runtime generic types.
                // No other generic instantiations are valid (and 3rd party components can't define generic types).
                if (!genericInstance.GenericType.IsCustomMappedWindowsRuntimeGenericDelegateType(interopReferences) &&
                    !genericInstance.GenericType.IsCustomMappedWindowsRuntimeGenericInterfaceType(interopReferences) &&
                    !genericInstance.GenericType.IsManuallyProjectedWindowsRuntimeGenericDelegateType(interopReferences) &&
                    !genericInstance.GenericType.IsManuallyProjectedWindowsRuntimeGenericInterfaceType(interopReferences) &&
                    !genericInstance.IsConstructedKeyValuePairType(interopReferences) &&
                    !genericInstance.IsConstructedNullableValueType(interopReferences))
                {
                    return false;
                }

                // Check whether all type arguments are also Windows Runtime types (otherwise the whole type is not)
                foreach (TypeSignature typeArgument in genericInstance.TypeArguments)
                {
                    // While arrays can be Windows Runtime types, they are not allowed to be used
                    // as type arguments for generic type instantiations, so we check for that.
                    if (typeArgument is SzArrayTypeSignature)
                    {
                        return false;
                    }

                    // Otherwise, do the usual validation for all type arguments
                    if (!typeArgument.IsWindowsRuntimeType(interopReferences))
                    {
                        return false;
                    }
                }

                return true;
            }

            // If the type is a fundamental or custom-mapped type, then it's a Windows Runtime type
            if (signature.IsFundamentalWindowsRuntimeType(interopReferences) ||
                signature.IsCustomMappedWindowsRuntimeNonGenericStructOrClassType(interopReferences) ||
                signature.IsCustomMappedWindowsRuntimeNonGenericInterfaceType(interopReferences) ||
                signature.IsCustomMappedWindowsRuntimeNonGenericDelegateType(interopReferences) ||
                signature.IsManuallyProjectedWindowsRuntimeNonGenericStructOrClassType(interopReferences) ||
                signature.IsManuallyProjectedWindowsRuntimeNonGenericInterfaceType(interopReferences) ||
                signature.IsManuallyProjectedWindowsRuntimeNonGenericDelegateType(interopReferences))
            {
                return true;
            }

            TypeDefinition type = signature.Resolve()!;

            // For all other cases, just check that the type is projected. This will also include
            // manually projected types that are defined in 'WinRT.Runtime.dll' (same attributes).
            return type.IsProjectedWindowsRuntimeType;
        }

        /// <summary>
        /// Checks whether a <see cref="TypeSignature"/> represents a Windows Runtime type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>Whether the type represents a Windows Runtime type.</returns>
        public bool IsNotExclusiveToWindowsRuntimeType(InteropReferences interopReferences)
        {
            // Same checks as above for SZ arrays, except that we also filter out '[exclusiveto]' interfaces
            if (signature is SzArrayTypeSignature arrayType)
            {
                return
                    arrayType.BaseType is not SzArrayTypeSignature &&
                    arrayType.BaseType.IsNotExclusiveToWindowsRuntimeType(interopReferences);
            }

            // Check constructed generics next, (same as above)
            if (signature is GenericInstanceTypeSignature genericInstance)
            {
                // Filter out invalid generic instantiations (same as above)
                if (!genericInstance.GenericType.IsCustomMappedWindowsRuntimeGenericDelegateType(interopReferences) &&
                    !genericInstance.GenericType.IsCustomMappedWindowsRuntimeGenericInterfaceType(interopReferences) &&
                    !genericInstance.GenericType.IsManuallyProjectedWindowsRuntimeGenericDelegateType(interopReferences) &&
                    !genericInstance.GenericType.IsManuallyProjectedWindowsRuntimeGenericInterfaceType(interopReferences) &&
                    !genericInstance.IsConstructedKeyValuePairType(interopReferences) &&
                    !genericInstance.IsConstructedNullableValueType(interopReferences))
                {
                    return false;
                }

                // Check whether all type arguments are also not '[exclusiveto]' Windows Runtime types
                foreach (TypeSignature typeArgument in genericInstance.TypeArguments)
                {
                    // Arrays are disallowed as type arguments (same as above)
                    if (typeArgument is SzArrayTypeSignature)
                    {
                        return false;
                    }

                    // Otherwise, do the usual validation for all type arguments
                    if (!typeArgument.IsNotExclusiveToWindowsRuntimeType(interopReferences))
                    {
                        return false;
                    }
                }

                return true;
            }

            // Check for fundamental or custom-mapped types (same as above)
            if (signature.IsFundamentalWindowsRuntimeType(interopReferences) ||
                signature.IsCustomMappedWindowsRuntimeNonGenericStructOrClassType(interopReferences) ||
                signature.IsCustomMappedWindowsRuntimeNonGenericInterfaceType(interopReferences) ||
                signature.IsCustomMappedWindowsRuntimeNonGenericDelegateType(interopReferences) ||
                signature.IsManuallyProjectedWindowsRuntimeNonGenericStructOrClassType(interopReferences) ||
                signature.IsManuallyProjectedWindowsRuntimeNonGenericInterfaceType(interopReferences) ||
                signature.IsManuallyProjectedWindowsRuntimeNonGenericDelegateType(interopReferences))
            {
                return true;
            }

            TypeDefinition type = signature.Resolve()!;

            // For all other cases, first check that the type is projected (same as above)
            if (!type.IsProjectedWindowsRuntimeType)
            {
                return false;
            }

            // We don't really have a way to check for '[exclusiveto]' interfaces directly, since they
            // don't have anything in metadata that states that. However, '[exclusiveto]' interfaces
            // are not public, so we can just use that to determine if that's the case for this type.
            return !type.IsInterface || type.Attributes.HasFlag(TypeAttributes.Public);
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
        public bool IsWindowsRuntimeModule => module.Name == WellKnownMetadataNames.WinRTRuntimeModuleName;

        /// <summary>
        /// Checks whether a <see cref="ModuleDefinition"/> references the Windows Runtime assembly.
        /// </summary>
        /// <returns>Whether the module references the Windows Runtime assembly.</returns>
        public bool ReferencesWindowsRuntimeAssembly => module.ReferencesAssembly(WellKnownMetadataNames.WinRTRuntimeAssemblyName);

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
    /// The current name of the WinRT runtime module.
    /// </summary>
    public static readonly Utf8String WinRTRuntimeModuleName = "WinRT.Runtime.dll"u8;

    /// <summary>
    /// The <c>"WindowsRuntime"</c> text.
    /// </summary>
    public static readonly Utf8String WindowsRuntime = "WindowsRuntime"u8;

    /// <summary>
    /// The <c>"WindowsRuntime.InteropServices"</c> text.
    /// </summary>
    public static readonly Utf8String WindowsRuntimeInteropServices = "WindowsRuntime.InteropServices"u8;

    /// <summary>
    /// The <c>"WindowsRuntimeMetadataAttribute"</c> text.
    /// </summary>
    public static readonly Utf8String WindowsRuntimeMetadataAttribute = "WindowsRuntimeMetadataAttribute"u8;

    /// <summary>
    /// The <c>"WindowsRuntimeReferenceAssemblyAttribute"</c> text.
    /// </summary>
    public static readonly Utf8String WindowsRuntimeReferenceAssemblyAttribute = "WindowsRuntimeReferenceAssemblyAttribute"u8;

    /// <summary>
    /// The <c>"WindowsRuntimeComponentAssemblyAttribute"</c> text.
    /// </summary>
    public static readonly Utf8String WindowsRuntimeComponentAssemblyAttribute = "WindowsRuntimeComponentAssemblyAttribute"u8;
}