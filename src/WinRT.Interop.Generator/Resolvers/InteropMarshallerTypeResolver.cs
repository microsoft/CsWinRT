// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Resolvers;

/// <summary>
/// A resolver for marshaller types for Windows Runtime types.
/// </summary>
internal static class InteropMarshallerTypeResolver
{
    /// <summary>
    /// Gets the marshaller type for a specified type.
    /// </summary>
    /// <param name="type">The type to get the marshaller type for.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="emitState">The emit state for this invocation.</param>
    /// <returns>The marshaller type for <paramref name="type"/>.</returns>
    public static InteropMarshallerType GetMarshallerType(
        TypeSignature type,
        InteropReferences interopReferences,
        InteropGeneratorEmitState emitState)
    {
        // First handle constructed generic types (which can be either value types or reference types)
        if (type is GenericInstanceTypeSignature)
        {
            // For 'Nullable<T>' return types, we need the marshaller for the instantiated 'T' type,
            // as that will contain the unboxing methods. The 'T' in this case can be a custom-mapped
            // primitive type or a projected value type. Technically speaking it can never be a
            // 'KeyValuePair<,>' or 'Nullable<T>', because both of those are interface types in the
            // Windows Runtime type system, meaning they can't be boxed like value types.
            if (type.TryGetNullableUnderlyingType(interopReferences, out TypeSignature? underlyingType))
            {
                return GetMarshallerType(underlyingType, interopReferences, emitState);
            }

            // For all other generic instantiations (including 'KeyValuePair<,>'), we can just look the marshaller
            // types up. All those marshaller types will always be generated in the same 'WinRT.Interop.dll'.
            ITypeDefOrRef marshallerType = emitState.LookupTypeDefinition(type, "Marshaller");

            return new(type, interopReferences, marshallerType);
        }

        // Special case 'object', we'll directly use 'WindowsRuntimeObjectMarshaller' for it
        if (type.IsTypeOfObject())
        {
            return new(type, interopReferences, interopReferences.WindowsRuntimeObjectMarshaller);
        }

        // For custom-mapped types and manually projected types, get the marshaller type from 'WinRT.Runtime.dll'
        if (type.IsFundamentalWindowsRuntimeType(interopReferences) ||
            type.IsCustomMappedWindowsRuntimeNonGenericInterfaceType(interopReferences) ||
            type.IsCustomMappedWindowsRuntimeNonGenericDelegateType(interopReferences) ||
            type.IsCustomMappedWindowsRuntimeNonGenericStructOrClassType(interopReferences) ||
            type.IsManuallyProjectedWindowsRuntimeNonGenericInterfaceType(interopReferences) ||
            type.IsManuallyProjectedWindowsRuntimeNonGenericDelegateType(interopReferences) ||
            type.IsManuallyProjectedWindowsRuntimeNonGenericStructOrClassType(interopReferences))
        {
            ITypeDefOrRef marshallerType = interopReferences.WindowsRuntimeModule.CreateTypeReference(
                ns: $"ABI.{type.Namespace}",
                name: $"{type.Name}Marshaller");

            return new(type, interopReferences, marshallerType);
        }
        else
        {
            // Determine the right assembly reference for this projected type
            AssemblyReference projectionAssembly = type.IsProjectedWindowsSdkXamlType
                ? interopReferences.WinRTSdkXamlProjection
                : type.IsProjectedWindowsSdkType
                    ? interopReferences.WinRTSdkProjection
                    : interopReferences.WinRTProjection;

            // In all other cases, the marshaller type will be in the right merged projection
            ITypeDefOrRef marshallerType = projectionAssembly.CreateTypeReference(
                ns: $"ABI.{type.Namespace}",
                name: $"{type.Name}Marshaller");

            return new(type, interopReferences, marshallerType);
        }
    }
}
