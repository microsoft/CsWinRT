// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
    /// <param name="interopReferences"> The <see cref="InteropReferences"/> instance to use. </param>
    /// <param name="useWindowsUIXamlProjections">Whether to use <c>Windows.UI.Xaml</c> projections.</param>
    /// <returns>The resulting signature for <paramref name="type"/>.</returns>
    public static string GetSignature(
        TypeSignature type,
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
            ElementType.GenericInst => GenericInstance((GenericInstanceTypeSignature)type, interopReferences, useWindowsUIXamlProjections),
            ElementType.ValueType when typeDefinition.IsClass && typeDefinition.IsEnum => Enum(typeFullName, typeDefinition, interopReferences, useWindowsUIXamlProjections),
            ElementType.ValueType when typeDefinition.IsClass && type.IsTypeOfGuid(interopReferences) => GuidSignature,
            ElementType.ValueType when typeDefinition.IsClass => ValueType(typeFullName, typeDefinition, interopReferences, useWindowsUIXamlProjections),
            ElementType.Class when typeDefinition.IsClass && typeDefinition.IsDelegate => Delegate(typeDefinition, interopReferences, useWindowsUIXamlProjections),
            ElementType.Class when typeDefinition.IsClass => Class(typeFullName, typeDefinition, interopReferences, useWindowsUIXamlProjections),
            ElementType.Class when typeDefinition.IsInterface => Interface(typeDefinition, interopReferences, useWindowsUIXamlProjections),
            ElementType.Boxed => Box((BoxedTypeSignature)type, typeDefinition, interopReferences, useWindowsUIXamlProjections),
            ElementType.SzArray => Array((SzArrayTypeSignature)type, interopReferences, useWindowsUIXamlProjections),
            _ => null
        };

        // We've successfully computed a signature, so return it. We should generally always be
        // able to compute a Windows Runtime signature for all types. Possible issues might be:
        //   - There's been some codegen/API changes in the projections, and 'cswinrtgen' was
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
    /// Attempts to retrieve the default interface signature from the <c>[WindowsRuntimeDefaultInterface]</c>
    /// attribute applied to the specified type, which is assumed to be some projected Windows Runtime class.
    /// </summary>
    /// <param name="type">The type definition to inspect for the default interface attribute.</param>
    /// <param name="interopReferences">The <see cref="InteropReferences"/> instance to use.</param>
    /// <param name="defaultInterface">The <see cref="TypeSignature"/> instance for the default interface for <paramref name="type"/>, if found.</param>
    /// <returns>Whether <paramref name="defaultInterface"/> was successfully retrieved.</returns>
    private static bool TryGetDefaultInterfaceFromAttribute(
        TypeDefinition type,
        InteropReferences interopReferences,
        [NotNullWhen(true)] out TypeSignature? defaultInterface)
    {
        if (type.TryGetCustomAttribute(interopReferences.WindowsRuntimeDefaultInterfaceAttribute, out CustomAttribute? customAttribute))
        {
            if (customAttribute.Signature is { FixedArguments: [{ Element: TypeSignature signature }, ..] })
            {
                defaultInterface = signature;

                return true;
            }
        }

        defaultInterface = null;

        return false;
    }
}
