// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.References;

#pragma warning disable IDE0072

namespace WindowsRuntime.InteropGenerator.Helpers;

/// <summary>
/// A generator for signature of Windows Runtime types.
/// </summary>
internal static partial class SignatureGenerator
{
    /// <summary>
    /// Generates the Windows Runtime signature for the specified type.
    /// </summary>
    /// <param name="type">The <see cref="AsmResolver.DotNet.Signatures.TypeSignature"/> to generate the signature for.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences"> The <see cref="InteropReferences"/> instance to use. </param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    /// <returns>The resulting signature for <paramref name="type"/>.</returns>
    public static string GetSignature(
        TypeSignature type,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        bool useWindowsUIXamlProjections)
    {
        // If we have a registered signature for a mapped type, just return that directly
        if (TypeMapping.TryFindMappedTypeSignature(type.FullName, useWindowsUIXamlProjections, out string? mappedSignature))
        {
            return mappedSignature;
        }

        // We need to be able to resolve the type definition to compute several kinds of signatures.
        // Also, all input type signatures here are expected to be resolvable, so validate that too.
        if (type.Resolve() is not TypeDefinition typeDefinition)
        {
            goto Failure;
        }

        // Get the full name of the type from the mapping of custom-mapped types, otherwise use the type name
        if (!TypeMapping.TryFindMappedTypeName(typeDefinition.FullName, useWindowsUIXamlProjections, out string? typeFullName))
        {
            typeFullName = typeDefinition.FullName;
        }

        // Try to compute the signature, possibly recursing over all type arguments, if any
        string? signature = type.ElementType switch
        {
            ElementType.I1 => SByteSignature,
            ElementType.U1 => ByteSignature,
            ElementType.I2 => ShortSignature,
            ElementType.U2 => UShortSignature,
            ElementType.I4 => IntSignature,
            ElementType.U4 => UIntSignature,
            ElementType.I8 => LongSignature,
            ElementType.U8 => ULongSignature,
            ElementType.R4 => FloatSignature,
            ElementType.R8 => DoubleSignature,
            ElementType.Boolean => BoolSignature,
            ElementType.Char => CharSignature,
            ElementType.Object => ObjectSignature,
            ElementType.String => StringSignature,
            ElementType.Type => TypeSignature,
            ElementType.GenericInst => GenericInstance((GenericInstanceTypeSignature)type, interopDefinitions, interopReferences, useWindowsUIXamlProjections),
            ElementType.ValueType when typeDefinition.IsClass && typeDefinition.IsEnum => Enum(typeFullName, typeDefinition, interopReferences),
            ElementType.ValueType when typeDefinition.IsClass && type.IsTypeOfGuid(interopReferences) => GuidSignature,
            ElementType.ValueType when typeDefinition.IsClass => ValueType(typeFullName, typeDefinition, interopDefinitions, interopReferences, useWindowsUIXamlProjections),
            ElementType.Class when typeDefinition.IsClass && typeDefinition.IsDelegate => Delegate(typeDefinition, interopDefinitions, interopReferences, useWindowsUIXamlProjections),
            ElementType.Class when typeDefinition.IsClass => Class(typeFullName, typeDefinition, interopDefinitions, interopReferences, useWindowsUIXamlProjections),
            ElementType.Class when typeDefinition.IsInterface => Interface(typeDefinition, interopDefinitions, interopReferences, useWindowsUIXamlProjections),
            ElementType.Boxed => Box((BoxedTypeSignature)type, typeDefinition, interopDefinitions, interopReferences, useWindowsUIXamlProjections),
            ElementType.SzArray => Array((SzArrayTypeSignature)type, interopDefinitions, interopReferences, useWindowsUIXamlProjections),
            _ => null
        };

        // We've successfully computed a signature, so return it. We should generally always be
        // able to compute a Windows Runtime signature for all types. Possible issues might be:
        //   - There's been some codegen/API changes in the projections, and 'cswinrtinteropgen' was
        //     either not updated correctly, or somehow a mismatched version has been loaded.
        //   - Some input types that are not Windows Runtime types haven't been filtered out.
        if (signature is not null)
        {
            return signature;
        }

    Failure:
        throw WellKnownInteropExceptions.TypeSignatureGenerationError(type);
    }

    /// <summary>
    /// Tries to resolve the IID for the specified type signature by checking well-known Windows Runtime
    /// interfaces and, if necessary, the type's <see cref="System.Runtime.InteropServices.GuidAttribute"/>.
    /// </summary>
    /// <param name="type">The type descriptor to try to get the IID for.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="iid">The resulting <see cref="Guid"/> value, if found.</param>
    /// <returns>Whether <paramref name="iid"/> was successfully retrieved.</returns>
    private static bool TryGetIIDFromWellKnownInterfaceIIDsOrAttribute(
        ITypeDescriptor type,
        bool useWindowsUIXamlProjections,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        out Guid iid)
    {
        // First try to get the IID from the custom-mapped types mapping
        if (WellKnownInterfaceIIDs.TryGetGUID(
            interfaceType: type,
            useWindowsUIXamlProjections: useWindowsUIXamlProjections,
            interopReferences: interopReferences,
            guid: out iid))
        {
            return true;
        }

        // If we can resolve the type, try to retrieve the IID from the '[Guid]' attribute on it
        if (type.Resolve() is TypeDefinition typeDefinition)
        {
            return TryGetIIDFromAttribute(
                typeDefinition,
                interopDefinitions,
                interopReferences,
                out iid);
        }

        iid = Guid.Empty;

        return false;
    }

    /// <summary>
    /// Attempts to retrieve the IID for the specified type by checking the <see cref="System.Runtime.InteropServices.GuidAttribute"/>
    /// attribute applied to it, if present. The type is assumed to be some projected Windows Runtime interface or delegate type.
    /// </summary>
    /// <param name="type">The type descriptor to try to get the IID for.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="iid">The resulting <see cref="Guid"/> value, if found.</param>
    /// <returns>Whether <paramref name="iid"/> was successfully retrieved.</returns>
    private static bool TryGetIIDFromAttribute(
        TypeDefinition type,
        InteropDefinitions interopDefinitions,
        InteropReferences interopReferences,
        out Guid iid)
    {
        // If the type had the '[Guid]' attribute directly on it, get it from there (this is the case for interfaces)
        if (type.TryGetGuidAttribute(interopReferences, out iid))
        {
            return true;
        }

        // For delegates, try to get the projected type from the right projection .dll, as they will have the '[Guid]' attribute on them.
        // These are only needed to generate signatures, so we hide them from the reference assemblies, as they're not useful there.
        if (type.IsDelegate)
        {
            // Determine the right implementation projection .dll to use for the lookup
            ModuleDefinition? projectionModule = type.IsProjectedWindowsSdkType
                ? interopDefinitions.WindowsRuntimeSdkProjectionModule
                : type.IsProjectedWindowsSdkXamlType
                    ? interopDefinitions.WindowsRuntimeSdkXamlProjectionModule
                    : interopDefinitions.WindowsRuntimeProjectionModule;

            // Try to get the implementation type via a fast lookup, if we did get a valid projection module
            if (projectionModule?.GetTopLevelTypesLookup().TryGetValue((type.Namespace, type.Name), out TypeDefinition? projectedType) is true)
            {
                return projectedType.TryGetGuidAttribute(interopReferences, out iid);
            }
        }

        iid = Guid.Empty;

        return false;
    }

    /// <summary>
    /// Attempts to retrieve the default interface signature from the <c>[WindowsRuntimeDefaultInterface]</c>
    /// attribute on the <c>WindowsRuntimeDefaultInterfaces</c> lookup type in the projection assembly, for the
    /// specified type, which is assumed to be some projected Windows Runtime class.
    /// </summary>
    /// <param name="type">The type definition to inspect for the default interface attribute.</param>
    /// <param name="interopDefinitions">The <see cref="InteropDefinitions"/> instance to use.</param>
    /// <param name="defaultInterface">The <see cref="TypeSignature"/> instance for the default interface for <paramref name="type"/>, if found.</param>
    /// <returns>Whether <paramref name="defaultInterface"/> was successfully retrieved.</returns>
    private static bool TryGetDefaultInterfaceFromAttribute(
        TypeDefinition type,
        InteropDefinitions interopDefinitions,
        [NotNullWhen(true)] out TypeSignature? defaultInterface)
    {
        // Determine the right implementation projection .dll (see notes above)
        ModuleDefinition? projectionModule = type.IsProjectedWindowsSdkType
            ? interopDefinitions.WindowsRuntimeSdkProjectionModule
            : type.IsProjectedWindowsSdkXamlType
                ? interopDefinitions.WindowsRuntimeSdkXamlProjectionModule
                : interopDefinitions.WindowsRuntimeProjectionModule;

        // Use the cached default interfaces lookup for O(1) lookups by (Namespace, Name) key
        if (projectionModule?.GetDefaultInterfacesLookup().TryGetValue((type.Namespace, type.Name), out TypeSignature? signature) is true)
        {
            defaultInterface = signature;

            return true;
        }

        defaultInterface = null;

        return false;
    }
}
