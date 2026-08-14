// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.Generator;
using WindowsRuntime.Generator.References;
using WindowsRuntime.InteropGenerator.References;

#pragma warning disable IDE0046

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
        public bool IsProjectedWindowsRuntimeType => member.HasCustomAttribute(WellKnownMetadataNames.WindowsRuntime, WellKnownMetadataNames.WindowsRuntimeTypeAttribute);

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
                // Types from 'Microsoft.Windows.SDK.NET.dll' belong to the SDK projection .dll. We check
                // the declaring assembly name to reliably determine the origin of the type. We also optimize
                // when an UTF8 value is available to avoid redundant UTF8 transcoding work.
                //
                // Note: we also need for 'WinRT.Sdk.Projection.dll' here, in case we got this type descriptor
                // from a type signature used in an attribute over a resolved projected type. That is, in that
                // case we would lose the original context for the ref assembly, and instead we'd see the scope
                // as being from the generated implementation .dll. We still want to make sure to detect those
                // types as projected from the right set, otherwise e.g. computing the type signature would fail.
                if (type.Scope?.GetAssembly() is { Name: Utf8String name })
                {
                    return
                        name.AsSpan().SequenceEqual(InteropNames.WindowsSDKAssemblyNameUtf8) ||
                        name.AsSpan().SequenceEqual(InteropNames.WindowsRuntimeSdkProjectionAssemblyNameUtf8);
                }

                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the type is a projected Windows SDK XAML type (from <c>Microsoft.Windows.UI.Xaml.dll</c>).
        /// </summary>
        public bool IsProjectedWindowsSdkXamlType
        {
            get
            {
                // Types from 'Microsoft.Windows.UI.Xaml.dll' belong to the XAML projection .dll. Here we do the
                // same checks as above, and also check for 'WinRT.Sdk.Xaml.projection.dll' for the same reason.
                if (type.Scope?.GetAssembly() is { Name: Utf8String name })
                {
                    return
                        name.AsSpan().SequenceEqual(InteropNames.WindowsSDKXamlAssemblyNameUtf8) ||
                        name.AsSpan().SequenceEqual(InteropNames.WindowsRuntimeSdkXamlProjectionAssemblyNameUtf8);
                }

                return false;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the type comes from an authored Windows Runtime component assembly.
        /// </summary>
        /// <remarks>
        /// This says nothing about the type itself. Component assemblies are marked with an assembly level attribute,
        /// and they also contain plenty of types that are not projected at all, so this is only the first half of the
        /// question. Use <c>IsComponentWindowsRuntimeType</c> to ask whether the type is a Windows Runtime type.
        /// </remarks>
        public bool IsFromComponentAssembly => type.Scope?.GetAssembly() is { IsWindowsRuntimeComponentAssembly: true };

        /// <summary>
        /// Gets a value indicating whether the type is from a Windows Runtime reference projection assembly.
        /// </summary>
        /// <remarks>
        /// Types in a reference projection assembly (marked with <c>[WindowsRuntimeReferenceAssembly]</c>) are
        /// projected Windows Runtime types, but they do not carry the per-type <c>[WindowsRuntimeMetadata]</c>
        /// attribute that implementation projections use (it is stripped from reference projections). This mirrors
        /// how authored component assemblies expose projected types without that attribute (the <c>IsComponentWindowsRuntimeType</c> extension property).
        /// </remarks>
        public bool IsReferenceProjectionWindowsRuntimeType => type.Scope?.GetAssembly() is { IsWindowsRuntimeReferenceAssembly: true };

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
            if (SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Boolean) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.String) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Single) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Double) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.UInt16) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.UInt32) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.UInt64) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Int16) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Int32) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Int64) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Char) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Byte) ||
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.Object))
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
                SignatureComparer.IgnoreVersion.Equals(type, interopReferences.IActivationFactory) ||
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
            if (!type.GetIsValueType(interopReferences.RuntimeContext))
            {
                return false;
            }

            // Generic instantiations can never be blittable (as they're pointers at the ABI level)
            if (type is GenericInstanceTypeSignature)
            {
                return false;
            }

            TypeDefinition typeDefinition = type.Resolve(interopReferences.RuntimeContext);

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
            if (!type.GetIsValueType(interopReferences.RuntimeContext))
            {
                return false;
            }

            // Generic instantiations (i.e. 'Nullable<T>' or 'KeyValuePair<,>') need disposal
            if (type is GenericInstanceTypeSignature)
            {
                return true;
            }

            TypeDefinition typeDefinition = type.Resolve(interopReferences.RuntimeContext);

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
            if (!type.GetIsValueType(interopReferences.RuntimeContext))
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

            TypeDefinition typeDefinition = type.Resolve(interopReferences.RuntimeContext);

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
            if (type.GetIsValueType(interopReferences.RuntimeContext))
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
                return interopReferences.Void.MakePointerType();
            }

            TypeDefinition typeDefinition = type.Resolve(interopReferences.RuntimeContext);

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

                // Determine the right assembly reference for this projected type
                AssemblyReference projectionAssembly = type.IsProjectedWindowsSdkType
                    ? interopReferences.WinRTSdkProjection
                    : type.IsProjectedWindowsSdkXamlType
                        ? interopReferences.WinRTSdkXamlProjection
                        : typeDefinition.IsFromComponentAssembly
                            ? interopReferences.WinRTComponent
                            : interopReferences.WinRTProjection;

                // For all types that get here, their ABI types will be in the right projection assembly, under the 'ABI' namespace
                return projectionAssembly.CreateTypeReference(
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
            return interopReferences.Void.MakePointerType();
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

                // The type also must be a projected type (recognized either by its '[WindowsRuntimeMetadata]'
                // attribute, for implementation projections, or by being defined in a reference projection
                // assembly, which doesn't carry that per-type attribute).
                return type.IsProjectedWindowsRuntimeType || type.IsReferenceProjectionWindowsRuntimeType;
            }
        }

        /// <summary>
        /// Tries to get the projected Windows Runtime class that a generated implementable base class stands for.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="runtimeClassType">The projected Windows Runtime class type, if the type is an implementable base.</param>
        /// <returns>Whether the type is a generated implementable base class for a runtime class.</returns>
        /// <remarks>
        /// These are the abstract base classes CsWinRT generates when a projection is built with
        /// <c>CsWinRTImplementWinMDTypes</c>, so that Windows Runtime types declared in existing metadata can be
        /// implemented (authored) in C#. They carry <c>[WindowsRuntimeImplementableClass(typeof(&lt;class&gt;))]</c>.
        /// Factory bases are deliberately excluded: an activation factory is not an instance of the class it
        /// activates, so it must not take on that class's identity.
        /// </remarks>
        public bool TryGetImplementableRuntimeClassType(InteropReferences interopReferences, [NotNullWhen(true)] out TypeSignature? runtimeClassType)
        {
            return type.TryGetImplementableRuntimeClassType(interopReferences.WindowsRuntimeImplementableClassAttribute, out runtimeClassType);
        }

        /// <summary>
        /// Tries to read the projected Windows Runtime class type from a given marker attribute on a type.
        /// </summary>
        /// <param name="attributeType">The marker attribute type to look for.</param>
        /// <param name="runtimeClassType">The projected Windows Runtime class type, if the marker is present.</param>
        /// <returns>Whether the marker was present.</returns>
        private bool TryGetImplementableRuntimeClassType(TypeReference attributeType, [NotNullWhen(true)] out TypeSignature? runtimeClassType)
        {
            foreach (CustomAttribute attribute in type.CustomAttributes)
            {
                // Match '[<attribute>(typeof(<CLASS_TYPE>))]'
                if (!SignatureComparer.IgnoreVersion.Equals(attribute.Constructor?.DeclaringType, attributeType))
                {
                    continue;
                }

                if (attribute.Signature is { FixedArguments: [{ Element: TypeSignature classType }] })
                {
                    runtimeClassType = classType;

                    return true;
                }
            }

            runtimeClassType = null;

            return false;
        }

        /// <summary>
        /// Tries to get the projected Windows Runtime class that a type implements by deriving from one of the
        /// abstract base classes CsWinRT generates for authoring Windows Runtime types declared in existing metadata.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <param name="runtimeClassType">The projected Windows Runtime class type being implemented, if any.</param>
        /// <returns>Whether the type implements a Windows Runtime class declared in existing metadata.</returns>
        /// <remarks>
        /// The nearest base carrying the marker wins, so a type deriving from a generated base for a derived runtime
        /// class reports that derived class rather than one of its ancestors.
        /// </remarks>
        public bool TryGetImplementedRuntimeClassType(InteropReferences interopReferences, [NotNullWhen(true)] out TypeSignature? runtimeClassType)
        {
            for (TypeDefinition? current = type; current is not null;)
            {
                if (current.TryGetImplementableRuntimeClassType(interopReferences, out runtimeClassType))
                {
                    return true;
                }

                current = current.BaseType?.Resolve(interopReferences.RuntimeContext);
            }

            runtimeClassType = null;

            return false;
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
            return type.HasOrInheritsAttribute(interopReferences.WindowsRuntimeManagedOnlyTypeAttribute, interopReferences.RuntimeContext);
        }

        /// <summary>
        /// Gets the Windows Runtime metadata name for a <see cref="TypeDefinition"/> (i.e. the source <c>.winmd</c> module name).
        /// </summary>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <returns>The Windows Runtime metadata name, or <see langword="null"/> if not found.</returns>
        /// <remarks>
        /// <para>
        /// The type -> source <c>.winmd</c> stem mapping is no longer carried on each projected type. It lives on the
        /// centralized <c>ABI.WindowsRuntimeMetadataTypes</c> lookup type in the implementation projection (so the
        /// build-time-only metadata can be trimmed away when unused). This selects the right implementation projection for
        /// the type (via <see cref="GetImplementationProjectionModule"/>) and looks the type up by namespace and name.
        /// </para>
        /// <para>
        /// This is the authoritative value the interop type-name marker must agree with: the projection writer encodes the
        /// very same stem into the <c>[UnsafeAccessorType]</c> references it emits into that implementation projection. It
        /// works uniformly for types resolved from implementation projections and from reference projections (the latter
        /// don't carry the lookup type, but resolve to the same implementation projection that does).
        /// </para>
        /// </remarks>
        public Utf8String? GetWindowsRuntimeMetadataName(InteropDefinitions interopDefinitions)
        {
            if (type.GetImplementationProjectionModule(interopDefinitions) is { } projectionModule &&
                projectionModule.GetWindowsRuntimeMetadataTypesLookup().TryGetValue((type.Namespace, type.Name), out Utf8String? metadataName))
            {
                return metadataName;
            }

            return null;
        }

        /// <summary>
        /// Gets the implementation projection module that contains the marshalling code for a projected <see cref="TypeDefinition"/>.
        /// </summary>
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <returns>
        /// The <see cref="ModuleDefinition"/> for the implementation projection the type belongs to: the Windows SDK projection
        /// (<c>WinRT.Sdk.Projection.dll</c>), the Windows SDK XAML projection (<c>WinRT.Sdk.Xaml.Projection.dll</c>), or the merged
        /// third-party projection (<c>WinRT.Projection.dll</c>); or <see langword="null"/> if that projection is not available.
        /// </returns>
        public ModuleDefinition? GetImplementationProjectionModule(InteropDefinitions interopDefinitions)
        {
            return type.IsProjectedWindowsSdkType
                ? interopDefinitions.WindowsRuntimeSdkProjectionModule
                : type.IsProjectedWindowsSdkXamlType
                    ? interopDefinitions.WindowsRuntimeSdkXamlProjectionModule
                    : interopDefinitions.WindowsRuntimeProjectionModule;
        }

        /// <summary>
        /// Checks whether a <see cref="TypeDefinition"/> represents a Windows Runtime attribute type.
        /// </summary>
        /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
        /// <returns>Whether the type represents a Windows Runtime attribute type.</returns>
        /// <remarks>
        /// <para>
        /// Windows Runtime attribute types are metadata-only: they are never activated and never cross the ABI
        /// boundary, so the projection deliberately emits no marshalling infrastructure for them (no default
        /// interface, no IID, no <c>Impl</c> type, no marshaller). They are still projected as ordinary .NET
        /// attribute types, and can appear as generic type arguments in managed code (e.g.
        /// <c>type.GetCustomAttributes&lt;SomeAttribute&gt;()</c> creates an <c>IEnumerable&lt;SomeAttribute&gt;</c>
        /// instantiation), so they have to be treated as plain managed types, exactly like any type that is
        /// not projected at all.
        /// </para>
        /// <para>
        /// Windows Runtime attribute types always derive <em>directly</em> from <see cref="Attribute"/>, as the
        /// Windows Runtime type system does not allow an attribute to derive from another one. Checking the
        /// immediate base type is therefore both sufficient and much cheaper than walking the base class chain.
        /// </para>
        /// </remarks>
        public bool IsWindowsRuntimeAttributeType(InteropReferences interopReferences)
        {
            return
                type.BaseType is { } baseType &&
                SignatureComparer.IgnoreVersion.Equals(baseType, interopReferences.Attribute);
        }

        /// <summary>
        /// Gets a value indicating whether the type is a Windows Runtime type authored in a component assembly.
        /// </summary>
        /// <remarks>
        /// Only public, non nested types make it into the <c>.winmd</c> a component produces. The rest are ordinary
        /// managed types (internal helpers, and compiler generated ones such as the nested binding classes the XAML
        /// compiler emits), and they carry none of the metadata, such as an IID, that marshalling code needs. This
        /// lives here rather than next to <c>IsFromComponentAssembly</c> because accessibility is only known
        /// once the type is resolved.
        /// </remarks>
        public bool IsComponentWindowsRuntimeType => type.IsFromComponentAssembly && type is { IsPublic: true, DeclaringType: null };
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
        /// <param name="runtimeContext">The <see cref="RuntimeContext"/> to use to resolve the delegate type.</param>
        /// <returns>The <see cref="MethodSignature"/> for the <c>Invoke</c> method for the input delegate type.</returns>
        public MethodSignature GetDelegateInvokeMethodSignature(RuntimeContext runtimeContext)
        {
            // Get the 'Invoke' method of the delegate type (this will remove the type arguments)
            MethodDefinition invokeMethod = signature.Resolve(runtimeContext).GetMethod("Invoke"u8);

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

            TypeDefinition type = signature.Resolve(interopReferences.RuntimeContext);

            // Windows Runtime attribute types carry no marshalling code at all, so they behave exactly like
            // types that are not projected (see the remarks on the check below for the full rationale).
            if (type.IsWindowsRuntimeAttributeType(interopReferences))
            {
                return false;
            }

            // For all other cases, just check that the type is projected. This will also include manually
            // projected types that are defined in 'WinRT.Runtime.dll' (same attributes). Public types from
            // authored component assemblies, and types from reference projection assemblies, are also
            // considered Windows Runtime types (they don't carry the per-type '[WindowsRuntimeMetadata]' attribute).
            return type.IsProjectedWindowsRuntimeType || type.IsComponentWindowsRuntimeType || type.IsReferenceProjectionWindowsRuntimeType;
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

            TypeDefinition type = signature.Resolve(interopReferences.RuntimeContext);

            // Windows Runtime attribute types carry no marshalling code at all (same as above)
            if (type.IsWindowsRuntimeAttributeType(interopReferences))
            {
                return false;
            }

            // For all other cases, first check that the type is projected. Public types from authored
            // component assemblies, and types from reference projection assemblies, are also considered
            // projected, even without '[WindowsRuntimeMetadata]'.
            if (!type.IsProjectedWindowsRuntimeType && !type.IsComponentWindowsRuntimeType && !type.IsReferenceProjectionWindowsRuntimeType)
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
        /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
        /// <returns>The Windows Runtime metadata name for the underlying type, or <see langword="null"/> if not found.</returns>
        /// <remarks>
        /// <para>
        /// This method resolves the underlying type definition from the signature and retrieves its Windows Runtime metadata name.
        /// For generic instance types, it uses the generic type definition. For array types, it uses the base element type.
        /// For other types, it resolves the type definition directly. The metadata name is recovered from the implementation
        /// projection for types coming from reference projections (see the <see cref="TypeDefinition"/> overload of this method).
        /// </para>
        /// </remarks>
        public Utf8String? GetWindowsRuntimeMetadataName(InteropDefinitions interopDefinitions)
        {
            RuntimeContext? runtimeContext = interopDefinitions.RuntimeContext;

            return signature switch
            {
                GenericInstanceTypeSignature generic => generic.GenericType.Resolve(runtimeContext).GetWindowsRuntimeMetadataName(interopDefinitions),
                ArrayTypeSignature array => array.BaseType.Resolve(runtimeContext).GetWindowsRuntimeMetadataName(interopDefinitions),
                _ => signature.ToTypeDefOrRef().Resolve(runtimeContext).GetWindowsRuntimeMetadataName(interopDefinitions)
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
        /// Checks whether a <see cref="ModuleDefinition"/> belongs to the .NET base class library (BCL), i.e. the
        /// default set of libraries shipped by the .NET SDK (the runtime shared frameworks). Detection is based on
        /// the assembly's public key token (see <see cref="BaseClassLibraryIdentity"/>).
        /// </summary>
        /// <returns>Whether the module is a .NET base class library / framework assembly.</returns>
        public bool IsBaseClassLibraryModule => BaseClassLibraryIdentity.IsBaseClassLibraryPublicKeyToken(module.Assembly?.GetPublicKeyToken());

        /// <summary>
        /// Checks whether a <see cref="ModuleDefinition"/> targets a legacy or portable runtime (i.e. .NET Standard
        /// or .NET Framework), as opposed to a modern .NET runtime, based on its corlib scope.
        /// </summary>
        /// <returns>Whether the module targets a legacy or portable runtime.</returns>
        /// <remarks>
        /// The entire interop generator infrastructure identifies well-known types (including custom-mapped types
        /// such as <c>IEnumerable&lt;T&gt;</c>) by comparing against type references scoped to the modern .NET corlib
        /// (e.g. <c>System.Runtime</c>), which is also the corlib the emit phase uses. Modules targeting a legacy or
        /// portable runtime declare those same types against a different corlib (<c>netstandard</c> or <c>mscorlib</c>),
        /// which <c>AsmResolver</c>'s <c>SignatureComparer</c> treats as a distinct scope. As a result, the generator
        /// cannot match (and therefore cannot marshal) their types, and attempting to do so would fail during emit.
        /// Such modules could in principle still use custom-mapped types that need marshalling, but for simplicity
        /// they are skipped entirely.
        /// </remarks>
        public bool TargetsLegacyRuntime
        {
            get
            {
                Utf8String? corLibName = module.CorLibTypeFactory.CorLibScope?.Name;

                return corLibName == WellKnownMetadataNames.NetStandardAssemblyName ||
                       corLibName == WellKnownMetadataNames.MSCorLibAssemblyName;
            }
        }

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
    /// The assembly name of the .NET Standard corlib (used by portable assemblies).
    /// </summary>
    public static readonly Utf8String NetStandardAssemblyName = "netstandard"u8;

    /// <summary>
    /// The assembly name of the .NET Framework corlib (used by legacy .NET Framework assemblies).
    /// </summary>
    public static readonly Utf8String MSCorLibAssemblyName = "mscorlib"u8;

    /// <summary>
    /// The <c>"WindowsRuntime"</c> text.
    /// </summary>
    public static readonly Utf8String WindowsRuntime = "WindowsRuntime"u8;

    /// <summary>
    /// The <c>"WindowsRuntime.InteropServices"</c> text.
    /// </summary>
    public static readonly Utf8String WindowsRuntimeInteropServices = "WindowsRuntime.InteropServices"u8;

    /// <summary>
    /// The <c>"WindowsRuntimeTypeAttribute"</c> text.
    /// </summary>
    public static readonly Utf8String WindowsRuntimeTypeAttribute = "WindowsRuntimeTypeAttribute"u8;

    /// <summary>
    /// The <c>"WindowsRuntimeReferenceAssemblyAttribute"</c> text.
    /// </summary>
    public static readonly Utf8String WindowsRuntimeReferenceAssemblyAttribute = "WindowsRuntimeReferenceAssemblyAttribute"u8;

    /// <summary>
    /// The <c>"WindowsRuntimeComponentAssemblyAttribute"</c> text.
    /// </summary>
    public static readonly Utf8String WindowsRuntimeComponentAssemblyAttribute = "WindowsRuntimeComponentAssemblyAttribute"u8;
}
