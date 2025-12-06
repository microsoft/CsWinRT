// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.Generation;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Factories;

/// <summary>
/// A factory to rewrite interop method definitons, and add marshalling code as needed.
/// </summary>
internal static partial class InteropMethodRewriteFactory
{
    /// <summary>
    /// Get the marshaller type for a specified value type.
    /// </summary>
    /// <param name="type">The value type to get the marshaller type for.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="emitState">The emit state for this invocation.</param>
    /// <returns>The marshaller type for <paramref name="type"/>.</returns>
    private static ITypeDefOrRef GetValueTypeMarshallerType(
        TypeSignature type,
        InteropReferences interopReferences,
        InteropGeneratorEmitState emitState)
    {
        // For generic instantiations (the only one possible is 'KeyValuePair<,>'
        // here), the marshaller type will be in the same 'WinRT.Interop.dll'.
        if (type is GenericInstanceTypeSignature)
        {
            return emitState.LookupTypeDefinition(type, "Marshaller");
        }

        // For primitive types, the marshaller type is in 'WinRT.Runtime.dll'.
        // In this case we can also rely on all types being under 'System'.
        if (type.IsFundamentalWindowsRuntimeType(interopReferences))
        {
            return interopReferences.WindowsRuntimeModule.CreateTypeReference(
                ns: "ABI.System"u8,
                name: $"{type.Name}Marshaller");
        }

        // 'TimeSpan' is custom-mapped and not blittable
        if (SignatureComparer.IgnoreVersion.Equals(type, interopReferences.TimeSpan))
        {
            return interopReferences.TimeSpanMarshaller;
        }

        // 'DateTimeOffset' also is custom-mapped and not blittable
        if (SignatureComparer.IgnoreVersion.Equals(type, interopReferences.DateTimeOffset))
        {
            return interopReferences.DateTimeOffsetMarshaller;
        }

        // In all other cases, the marshaller type will be in the declared assembly
        return type.Resolve()!.DeclaringModule!.CreateTypeReference(
            ns: $"ABI.{type.Namespace}",
            name: $"{type.Name}Marshaller");
    }

    /// <summary>
    /// Get the marshaller type for a specified reference type.
    /// </summary>
    /// <param name="type">The reference type to get the marshaller type for.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="emitState">The emit state for this invocation.</param>
    /// <returns>The marshaller type for <paramref name="type"/>.</returns>
    private static ITypeDefOrRef GetReferenceTypeMarshallerType(
        TypeSignature type,
        InteropReferences interopReferences,
        InteropGeneratorEmitState emitState)
    {
        // Just like for value types, generic types have marshaller types in 'WinRT.Interop.dll'
        if (type is GenericInstanceTypeSignature)
        {
            return emitState.LookupTypeDefinition(type, "Marshaller");
        }

        // Special case 'object', we'll directly use 'WindowsRuntimeObjectMarshaller' for it
        if (type.IsTypeOfObject(interopReferences))
        {
            return interopReferences.WindowsRuntimeObjectMarshaller;
        }

        // For custom-mapped types, get the marshaller type from 'WinRT.Runtime.dll'
        if (type.IsCustomMappedWindowsRuntimeNonGenericInterfaceType(interopReferences) ||
            type.IsCustomMappedWindowsRuntimeNonGenericDelegateType(interopReferences))
        {
            return interopReferences.WindowsRuntimeModule.CreateTypeReference(
                ns: $"ABI.{type.Namespace}",
                name: $"{type.Name}Marshaller");
        }

        // In all other cases, the marshaller type will be in the declared assembly. Note that this
        // also includes special manually projected types, such as 'AsyncActionCompletedHandler'.
        // Even though those types are in 'WinRT.Runtime.dll', the marshaller type will also be
        // there, so trying to resolve it via the declaring module like for other types is fine.
        return type.Resolve()!.DeclaringModule!.CreateTypeReference(
            ns: $"ABI.{type.Namespace}",
            name: $"{type.Name}Marshaller");
    }
}