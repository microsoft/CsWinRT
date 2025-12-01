// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.InteropGenerator.References;

#pragma warning disable IDE0046

namespace WindowsRuntime.InteropGenerator.Helpers;

/// <inheritdoc cref="SignatureGenerator"/>
internal partial class SignatureGenerator
{
    /// <summary>
    /// Tries to get the signature of a constructed generic type.
    /// </summary>
    /// <param name="typeSignature"><inheritdoc cref="GetSignature" path="/param[@name='type']/node()"/></param>
    /// <param name="interopReferences"><inheritdoc cref="GetSignature" path="/param[@name='interopReferences']/node()"/></param>
    /// <param name="useWindowsUIXamlProjections"><inheritdoc cref="GetSignature" path="/param[@name='useWindowsUIXamlProjections']/node()"/></param>
    /// <returns>The resulting signature, or <see langword="null"/> in case of failures.</returns>
    private static string? GenericInstance(GenericInstanceTypeSignature typeSignature, InteropReferences interopReferences, bool useWindowsUIXamlProjections)
    {
        // If we fail to get the IID of the generic interface, we can't do anything else
        if (!GuidGenerator.TryGetIIDFromWellKnownInterfaceIIDsOrAttribute(typeSignature.GenericType, interopReferences, out Guid interfaceIid))
        {
            return null;
        }

        DefaultInterpolatedStringHandler handler = new(0, 0, null, initialBuffer: stackalloc char[256]);

        // Append the leading 'pinterface' prefix and the PIID of the generic interface
        handler.AppendLiteral("pinterface({");
        handler.AppendFormatted(interfaceIid);
        handler.AppendLiteral("}");

        // Append the signatures of all type arguments
        foreach (TypeSignature argumentSignature in typeSignature.TypeArguments)
        {
            handler.AppendFormatted(';');
            handler.AppendFormatted(GetSignature(argumentSignature, interopReferences, useWindowsUIXamlProjections));
        }

        handler.AppendFormatted(')');

        return handler.ToStringAndClear();
    }

    /// <summary>
    /// Tries to get the signature of an enum type.
    /// </summary>
    /// <param name="typeFullName">The full name of the enum type.</param>
    /// <param name="typeDefinition">The <see cref="TypeDefinition"/> to generate the signature for.</param>
    /// <param name="interopReferences"><inheritdoc cref="GetSignature" path="/param[@name='interopReferences']/node()"/></param>
    /// <returns>The resulting signature, or <see langword="null"/> in case of failures.</returns>
    private static string? Enum(
        string typeFullName,
        TypeDefinition typeDefinition,
        InteropReferences interopReferences)
    {
        // For '[Flags]' enum types, the underlying types is always 'uint', otherwise 'int'
        string underlyingTypeSignature = typeDefinition.HasCustomAttribute(interopReferences.FlagsAttribute)
            ? UIntSignature
            : IntSignature;

        return $"enum({typeFullName};{underlyingTypeSignature})";
    }

    /// <summary>
    /// Tries to get the signature of a value type.
    /// </summary>
    /// <param name="typeFullName">The full name of the value type.</param>
    /// <param name="typeDefinition">The <see cref="TypeDefinition"/> to generate the signature for.</param>
    /// <param name="interopReferences"><inheritdoc cref="GetSignature" path="/param[@name='interopReferences']/node()"/></param>
    /// <param name="useWindowsUIXamlProjections"><inheritdoc cref="GetSignature" path="/param[@name='useWindowsUIXamlProjections']/node()"/></param>
    /// <returns>The resulting signature, or <see langword="null"/> in case of failures.</returns>
    private static string? ValueType(
        string typeFullName,
        TypeDefinition typeDefinition,
        InteropReferences interopReferences,
        bool useWindowsUIXamlProjections)
    {
        // Valid projected value types must always have at least one field
        if (typeDefinition.Fields.Count == 0)
        {
            return null;
        }

        DefaultInterpolatedStringHandler handler = new(0, 0, null, initialBuffer: stackalloc char[256]);

        // Append the leading 'struct' prefix and the type name (value types don't use an IID)
        handler.AppendLiteral("struct(");
        handler.AppendFormatted(typeFullName);

        foreach (FieldDefinition field in typeDefinition.Fields)
        {
            // Static fields don't affect the signature. In theory,
            // projected value types can't have static fields anyway.
            if (field.IsStatic)
            {
                continue;
            }

            // Ensure that we have a readable signature for the field type
            if (field.Signature is not { FieldType: TypeSignature fieldSignature })
            {
                continue;
            }

            handler.AppendFormatted(';');
            handler.AppendFormatted(GetSignature(fieldSignature, interopReferences, useWindowsUIXamlProjections));
        }

        handler.AppendFormatted(')');

        return handler.ToStringAndClear();
    }

    /// <summary>
    /// Tries to get the signature of a delegate type.
    /// </summary>
    /// <param name="typeDefinition">The <see cref="TypeDefinition"/> to generate the signature for.</param>
    /// <param name="interopReferences"><inheritdoc cref="GetSignature" path="/param[@name='interopReferences']/node()"/></param>
    private static string? Delegate(TypeDefinition typeDefinition, InteropReferences interopReferences)
    {
        // Just like for generic instantiations, we need to resolve the IID for the type first
        if (!GuidGenerator.TryGetIIDFromWellKnownInterfaceIIDsOrAttribute(typeDefinition, interopReferences, out Guid iid))
        {
            return null;
        }

        // Delegate type signatures just have the IID of the delegate interface within brackets
        return $"delegate({{{iid}}})";
    }

    /// <summary>
    /// Tries to get the signature of a class type.
    /// </summary>
    /// <param name="typeFullName">The full name of the class type.</param>
    /// <param name="typeDefinition">The <see cref="TypeDefinition"/> to generate the signature for.</param>
    /// <param name="interopReferences"><inheritdoc cref="GetSignature" path="/param[@name='interopReferences']/node()"/></param>
    /// <param name="useWindowsUIXamlProjections"><inheritdoc cref="GetSignature" path="/param[@name='useWindowsUIXamlProjections']/node()"/></param>
    /// <returns>The resulting signature, or <see langword="null"/> in case of failures.</returns>
    private static string? Class(
        string typeFullName,
        TypeDefinition typeDefinition,
        InteropReferences interopReferences,
        bool useWindowsUIXamlProjections)
    {
        // If we can resolve the default interface type from the projected runtime class, use it
        if (TryGetDefaultInterfaceFromAttribute(typeDefinition, interopReferences, out TypeSignature? defaultInterface))
        {
            return $"rc({typeFullName};{GetSignature(defaultInterface, interopReferences, useWindowsUIXamlProjections)})";
        }

        // Otherwise, get the IID from the type definition and use it
        if (!GuidGenerator.TryGetIIDFromWellKnownInterfaceIIDsOrAttribute(typeDefinition, interopReferences, out Guid iid))
        {
            return null;
        }

        return $"{{{iid}}}";
    }

    /// <summary>
    /// Tries to get the signature of an interface type.
    /// </summary>
    /// <param name="typeDefinition">The <see cref="TypeDefinition"/> to generate the signature for.</param>
    /// <param name="interopReferences"><inheritdoc cref="GetSignature" path="/param[@name='interopReferences']/node()"/></param>
    private static string? Interface(TypeDefinition typeDefinition, InteropReferences interopReferences)
    {
        // For all interface types, we should always be able to resolve their IID
        if (!GuidGenerator.TryGetIIDFromWellKnownInterfaceIIDsOrAttribute(typeDefinition, interopReferences, out Guid iid))
        {
            return null;
        }

        return $"{{{iid}}}";
    }

    /// <summary>
    /// Tries to get the signature of a boxed type.
    /// </summary>
    /// <param name="typeSignature"><inheritdoc cref="GetSignature" path="/param[@name='type']/node()"/></param>
    /// <param name="typeDefinition">The <see cref="TypeDefinition"/> to generate the signature for.</param>
    /// <param name="interopReferences"><inheritdoc cref="GetSignature" path="/param[@name='interopReferences']/node()"/></param>
    /// <param name="useWindowsUIXamlProjections"><inheritdoc cref="GetSignature" path="/param[@name='useWindowsUIXamlProjections']/node()"/></param>
    /// <returns>The resulting signature, or <see langword="null"/> in case of failures.</returns>
    private static string? Box(
        BoxedTypeSignature typeSignature,
        TypeDefinition typeDefinition,
        InteropReferences interopReferences,
        bool useWindowsUIXamlProjections)
    {
        // Boxed type signatures are only used to represent 'IReference<T>' instantiations
        // for delegate types. Any other underlying type is not valid and can't be handled.
        if (!typeDefinition.IsClass || !typeDefinition.IsDelegate)
        {
            return null;
        }

        // Get the IID for 'Nullable<T>', which is the one we use for 'IReference<T>'
        _ = WellKnownInterfaceIIDs.TryGetGUID(interopReferences.Nullable1, interopReferences, out Guid iid);

        // Construct the signature for the boxed delegate (the base type will be the possibly constructed delegate)
        return $"pinterface({{{iid}}};{GetSignature(typeSignature.BaseType, interopReferences, useWindowsUIXamlProjections)})";
    }

    /// <summary>
    /// Tries to get the signature of an array type.
    /// </summary>
    /// <param name="typeSignature"><inheritdoc cref="GetSignature" path="/param[@name='type']/node()"/></param>
    /// <param name="interopReferences"><inheritdoc cref="GetSignature" path="/param[@name='interopReferences']/node()"/></param>
    /// <param name="useWindowsUIXamlProjections"><inheritdoc cref="GetSignature" path="/param[@name='useWindowsUIXamlProjections']/node()"/></param>
    private static string? Array(SzArrayTypeSignature typeSignature, InteropReferences interopReferences, bool useWindowsUIXamlProjections)
    {
        return $"pinterface({{61c17707-2d65-11e0-9ae8-d48564015472}};{GetSignature(typeSignature.BaseType, interopReferences, useWindowsUIXamlProjections)})";
    }
}
