// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis;
using WindowsRuntime.Generator.Helpers;
using WindowsRuntime.Generator.References;
using WindowsRuntime.InteropGenerator.Helpers;

#pragma warning disable IDE0046, IDE0072

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// Resolves Windows Runtime identities using compiler-visible metadata, without loading implementation assemblies.
/// </summary>
/// <param name="compilation">The consuming compilation.</param>
/// <param name="windowsRuntimeAssembly">The assembly containing the intercepted marshalling API.</param>
/// <param name="token">The cancellation token.</param>
internal sealed partial class WindowsRuntimeIidResolver(
    Compilation compilation,
    IAssemblySymbol windowsRuntimeAssembly,
    CancellationToken token)
{
    /// <summary>
    /// Guards against malformed recursive value metadata.
    /// </summary>
    private readonly HashSet<ITypeSymbol> _activeTypes = new(SymbolEqualityComparer.Default);

    /// <summary>
    /// Tries to compute the IID represented by a closed type.
    /// </summary>
    /// <param name="type">The call's type argument.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use UWP XAML mappings.</param>
    /// <param name="iid">The computed IID.</param>
    /// <returns>Whether the type has a statically known Windows Runtime IID.</returns>
    public bool TryGetIid(
        ITypeSymbol type,
        bool useWindowsUIXamlProjections,
        out Guid iid)
    {
        token.ThrowIfCancellationRequested();

        iid = default;

        if (type.TypeKind == TypeKind.Dynamic)
        {
            return false;
        }

        // 'object' represents 'IInspectable', whose IID is not derived from a signature
        if (type.SpecialType == SpecialType.System_Object)
        {
            return WellKnownWindowsRuntimeIids.TryGetIid("System.Object", useWindowsUIXamlProjections, out iid);
        }

        // Parameterized identities include nullable values, key/value pairs, and reference arrays
        if (type is IArrayTypeSymbol or INamedTypeSymbol { IsGenericType: true })
        {
            if (!TryGetSignature(type, useWindowsUIXamlProjections, out string? signature))
            {
                return false;
            }

            iid = GuidGenerator.Generate(signature);

            return true;
        }

        // A type can have a parameter signature without having an IID of its own
        if (type.IsValueType ||
            type.SpecialType == SpecialType.System_String ||
            !TryGetDefinition(type, out INamedTypeSymbol? definition, out string? name) ||
            name == "System.EventHandler")
        {
            return false;
        }

        // Mapped types (e.g. 'System.IDisposable') carry their own IID in the mapping table
        if (TypeMapping.TryFindMappedTypeSignature(name, useWindowsUIXamlProjections, out _))
        {
            return TypeMapping.TryFindMappedTypeIID(name, useWindowsUIXamlProjections, out iid);
        }

        // Interfaces and delegates declare their IID directly with a '[Guid]' attribute
        if (TryGetDefinitionIid(definition, name, useWindowsUIXamlProjections, out iid))
        {
            return true;
        }

        // Only runtime classes can have an IID that comes from another type (their default interface)
        if (definition.TypeKind != TypeKind.Class)
        {
            return false;
        }

        // The default interface is recorded by name, as it's often exclusive-to and not declared by the class
        if (!TryGetDefaultInterface(definition, name, out INamedTypeSymbol? defaultInterface))
        {
            return false;
        }

        // The IID of a runtime class is the IID of its default interface
        return TryGetIid(defaultInterface, useWindowsUIXamlProjections, out iid);
    }

    /// <summary>
    /// Computes a supported Windows Runtime parameter signature.
    /// </summary>
    /// <param name="type">The input type.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use UWP XAML mappings.</param>
    /// <param name="signature">The resulting signature.</param>
    /// <returns>Whether sufficient, valid metadata is available.</returns>
    private bool TryGetSignature(
        ITypeSymbol type,
        bool useWindowsUIXamlProjections,
        [NotNullWhen(true)] out string? signature)
    {
        token.ThrowIfCancellationRequested();

        signature = null;

        if (!_activeTypes.Add(type))
        {
            return false;
        }

        bool success = TryGetSignatureCore(type, useWindowsUIXamlProjections, out signature);

        _ = _activeTypes.Remove(type);

        return success;
    }

    /// <inheritdoc cref="TryGetSignature"/>
    private bool TryGetSignatureCore(
        ITypeSymbol type,
        bool useWindowsUIXamlProjections,
        [NotNullWhen(true)] out string? signature)
    {
        signature = null;

        if (type.TypeKind == TypeKind.Dynamic)
        {
            return false;
        }

        signature = type.SpecialType switch
        {
            SpecialType.System_Boolean => "b1",
            SpecialType.System_Byte => "u1",
            SpecialType.System_Char => "c2",
            SpecialType.System_Int16 => "i2",
            SpecialType.System_UInt16 => "u2",
            SpecialType.System_Int32 => "i4",
            SpecialType.System_UInt32 => "u4",
            SpecialType.System_Int64 => "i8",
            SpecialType.System_UInt64 => "u8",
            SpecialType.System_Single => "f4",
            SpecialType.System_Double => "f8",
            SpecialType.System_String => "string",
            SpecialType.System_Object => "cinterface(IInspectable)",
            _ => null
        };

        if (signature is not null)
        {
            return true;
        }

        if (type is IArrayTypeSymbol { IsSZArray: true, ElementType: not IArrayTypeSymbol } array &&
            TryGetSignature(array.ElementType, useWindowsUIXamlProjections, out string? elementSignature))
        {
            signature = $"pinterface({{61c17707-2d65-11e0-9ae8-d48564015472}};{elementSignature})";

            return true;
        }

        if (!TryGetDefinition(type, out INamedTypeSymbol? definition, out string? name))
        {
            return false;
        }

        if (name == "System.Guid")
        {
            signature = "g16";

            return true;
        }

        if (TypeMapping.TryFindMappedTypeSignature(name, useWindowsUIXamlProjections, out signature))
        {
            return true;
        }

        if (type is INamedTypeSymbol { IsGenericType: true } genericType)
        {
            return TryGetGenericSignature(genericType, name, useWindowsUIXamlProjections, out signature);
        }

        if (TryGetDefinitionIid(definition, name, useWindowsUIXamlProjections, out Guid iid))
        {
            signature = definition.TypeKind == TypeKind.Delegate ? $"delegate({{{iid}}})" : $"{{{iid}}}";

            return true;
        }

        // The default interface is often exclusive-to, so it is recorded by name and not among the class's interfaces
        if (definition.TypeKind == TypeKind.Class &&
            TryGetDefaultInterface(definition, name, out INamedTypeSymbol? defaultInterface) &&
            TryGetSignature(defaultInterface, useWindowsUIXamlProjections, out string? defaultInterfaceSignature))
        {
            signature = $"rc({name};{defaultInterfaceSignature})";

            return true;
        }

        if (definition.TypeKind == TypeKind.Enum)
        {
            signature = $"enum({name};{(definition.EnumUnderlyingType?.SpecialType == SpecialType.System_UInt32 ? "u4" : "i4")})";

            return true;
        }

        return definition.TypeKind == TypeKind.Struct &&
            TryGetStructSignature(definition, name, useWindowsUIXamlProjections, out signature);
    }

    /// <summary>
    /// Computes a signature for a supported closed generic type.
    /// </summary>
    /// <param name="type">The constructed type.</param>
    /// <param name="name">Its definition's metadata name.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use UWP XAML mappings.</param>
    /// <param name="signature">The resulting parameterized signature.</param>
    /// <returns>Whether the generic definition and all arguments are supported.</returns>
    private bool TryGetGenericSignature(
        INamedTypeSymbol type,
        string name,
        bool useWindowsUIXamlProjections,
        [NotNullWhen(true)] out string? signature)
    {
        signature = null;

        if ((!SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, windowsRuntimeAssembly) &&
             !IsFrameworkDefinition(type.OriginalDefinition, name)) ||
            !TryGetDefinitionIid(type.OriginalDefinition, name, useWindowsUIXamlProjections, out Guid definitionIid) ||
            (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T &&
             type.TypeArguments[0] is INamedTypeSymbol nullableArgument &&
             nullableArgument.OriginalDefinition.GetFullyQualifiedMetadataName() == "System.Collections.Generic.KeyValuePair`2"))
        {
            return false;
        }

        using PooledArrayBuilder<string> arguments = new();

        foreach (ITypeSymbol argument in type.TypeArguments)
        {
            if (argument is IArrayTypeSymbol ||
                !TryGetSignature(argument, useWindowsUIXamlProjections, out string? argumentSignature))
            {
                return false;
            }

            arguments.Add(argumentSignature);
        }

        signature = $"pinterface({{{definitionIid}}};{string.Join(";", arguments.WrittenSpan)})";

        return true;
    }

    /// <summary>
    /// Computes a struct signature in metadata field order.
    /// </summary>
    /// <param name="type">The struct definition.</param>
    /// <param name="name">Its metadata name.</param>
    /// <param name="useWindowsUIXamlProjections">Whether to use UWP XAML mappings.</param>
    /// <param name="signature">The resulting value signature.</param>
    /// <returns>Whether every instance field has a supported signature.</returns>
    private bool TryGetStructSignature(
        INamedTypeSymbol type,
        string name,
        bool useWindowsUIXamlProjections,
        [NotNullWhen(true)] out string? signature)
    {
        signature = null;

        using PooledArrayBuilder<string> fields = new();

        foreach (ISymbol member in type.GetMembers())
        {
            if (member is not IFieldSymbol { IsStatic: false } field)
            {
                continue;
            }

            if (!TryGetSignature(field.Type, useWindowsUIXamlProjections, out string? fieldSignature))
            {
                return false;
            }

            fields.Add(fieldSignature);
        }

        if (fields.Count == 0)
        {
            return false;
        }

        signature = $"struct({name};{string.Join(";", fields.WrittenSpan)})";

        return true;
    }
}
