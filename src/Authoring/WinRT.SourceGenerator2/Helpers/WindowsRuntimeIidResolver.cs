// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using Microsoft.CodeAnalysis;
using WindowsRuntime.Generator.Helpers;
using WindowsRuntime.Generator.References;
using WindowsRuntime.InteropGenerator.Helpers;

#pragma warning disable IDE0046

namespace WindowsRuntime.SourceGenerator;

/// <summary>
/// Resolves Windows Runtime IIDs using compiler-visible metadata, without loading implementation assemblies.
/// </summary>
internal sealed class WindowsRuntimeIidResolver
{
    /// <summary>
    /// The resolver for the metadata of the types to get IIDs for.
    /// </summary>
    private readonly WindowsRuntimeMetadataResolver _metadata;

    /// <summary>
    /// The generator for the signatures that parameterized IIDs are derived from.
    /// </summary>
    private readonly WindowsRuntimeSignatureGenerator _signatures;

    /// <summary>
    /// The cancellation token.
    /// </summary>
    private readonly CancellationToken _token;

    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeIidResolver"/> instance with the specified parameters.
    /// </summary>
    /// <param name="compilation">The consuming compilation.</param>
    /// <param name="windowsRuntimeAssembly">The assembly containing the intercepted marshalling API.</param>
    /// <param name="token">The cancellation token.</param>
    public WindowsRuntimeIidResolver(
        Compilation compilation,
        IAssemblySymbol windowsRuntimeAssembly,
        CancellationToken token)
    {
        _metadata = new(compilation, windowsRuntimeAssembly, token);
        _signatures = new(_metadata, token);
        _token = token;
    }

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
        _token.ThrowIfCancellationRequested();

        // We don't support 'dynamic', only explicitly statically-visible types that we can resolve
        if (type.TypeKind == TypeKind.Dynamic)
        {
            iid = default;

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
            if (!_signatures.TryGetSignature(type, useWindowsUIXamlProjections, out string? signature))
            {
                iid = default;

                return false;
            }

            iid = GuidGenerator.Generate(signature);

            return true;
        }

        // A type can have a parameter signature without having an IID of its own
        if (type.IsValueType ||
            type.SpecialType == SpecialType.System_String ||
            !_metadata.TryGetDefinition(type, out INamedTypeSymbol? definition, out string? name))
        {
            iid = default;

            return false;
        }

        // Mapped types (e.g. 'System.IDisposable') carry their own IID in the mapping table
        if (TypeMapping.TryFindMappedTypeSignature(name, useWindowsUIXamlProjections, out _))
        {
            return TypeMapping.TryFindMappedTypeIID(name, useWindowsUIXamlProjections, out iid);
        }

        // Interfaces and delegates declare their IID directly with a '[Guid]' attribute
        if (_metadata.TryGetDefinitionIid(definition, name, useWindowsUIXamlProjections, out iid))
        {
            return true;
        }

        // Only runtime classes can have an IID that comes from another type (their default interface)
        if (definition.TypeKind != TypeKind.Class)
        {
            iid = default;

            return false;
        }

        // The default interface is recorded by name, as it's often exclusive-to and not declared by the class
        if (!_metadata.TryGetDefaultInterface(definition, name, out INamedTypeSymbol? defaultInterface))
        {
            iid = default;

            return false;
        }

        // The IID of a runtime class is the IID of its default interface
        return TryGetIid(defaultInterface, useWindowsUIXamlProjections, out iid);
    }
}
