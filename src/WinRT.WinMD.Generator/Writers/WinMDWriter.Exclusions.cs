// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.WinMDGenerator.Errors;
using WindowsRuntime.WinMDGenerator.Helpers;

namespace WindowsRuntime.WinMDGenerator.Writers;

/// <inheritdoc cref="WinMDWriter"/>
internal sealed partial class WinMDWriter
{
    /// <summary>
    /// Validates exclusions before producing a type's metadata or synthesized interfaces.
    /// </summary>
    private void ValidateMetadataExclusions(TypeDefinition type)
    {
        HashSet<string> omittedMembers = type.IsClass && !type.IsValueType && !type.IsDelegate
            ? CollectCustomMappedMemberNames(type)
            : [];

        // Abstract and excluded managed bases become System.Object in WinMD
        // Their original references are not exported and do not need signature validation
        if (type.BaseType is { } baseType &&
            !IsErasedBaseType(baseType))
        {
            ValidateExportedTypeReference(baseType, type.FullName);
        }

        foreach (InterfaceImplementation implementation in type.Interfaces)
        {
            if (implementation.Interface is not { } iface ||
                TypeMapper.ImplementedInterfacesWithoutMapping.Contains(GetInterfaceFullName(iface)) ||
                (!type.IsInterface &&
                 (!IsPubliclyAccessible(iface) ||
                  ResolveInterface(iface)?.IsExcludedFromWindowsMetadata() == true)))
            {
                continue;
            }

            ValidateExportedTypeReference(iface, type.FullName);
        }

        foreach (InterfaceImplementation implementation in GatherAllInterfaces(type))
        {
            if (implementation.Interface is { } iface && _mapper.HasMappingForType(GetInterfaceFullName(iface)))
            {
                ValidateExportedTypeReference(iface, type.FullName);
            }
        }

        foreach (FieldDefinition field in type.Fields)
        {
            if (type.IsValueType && !type.IsEnum && !field.IsStatic && field.HasExcludeFromWindowsMetadata())
            {
                throw WellKnownWinMDExceptions.InvalidMetadataExclusion(
                    field.FullName, "instance fields of exported structs define the Windows Runtime layout.");
            }

            if (type.IsValueType && !type.IsEnum && field.IsPublic && !field.IsStatic)
            {
                ValidateExportedTypeSignature(field.Signature!.FieldType, field.FullName);
            }
        }

        foreach (MethodDefinition method in type.Methods)
        {
            bool isAccessor = type.Properties.Any(property => property.GetMethod == method || property.SetMethod == method) ||
                              type.Events.Any(@event => @event.AddMethod == method || @event.RemoveMethod == method);

            if (isAccessor && method.HasExcludeFromWindowsMetadata())
            {
                throw WellKnownWinMDExceptions.InvalidMetadataExclusion(
                    method.FullName, "apply the attribute to the whole property or event, not an accessor.");
            }

            if (!method.IsPublic ||
                type.IsValueType ||
                (type.IsDelegate && method.Name != "Invoke") ||
                method.IsExcludedFromWindowsMetadata() ||
                omittedMembers.Contains(method.Name?.Value ?? "") ||
                (method.IsSpecialName &&
                 !method.IsConstructor &&
                 !isAccessor))
            {
                continue;
            }

            ValidateExportedTypeSignature(method.Signature!.ReturnType, method.FullName);

            foreach (TypeSignature parameter in method.Signature.ParameterTypes)
            {
                ValidateExportedTypeSignature(parameter, method.FullName);
            }
        }

        if (type.IsClass && !type.IsValueType && !type.IsDelegate)
        {
            ValidateRequiredInterfaceMembers(type);
        }
    }

    /// <summary>
    /// Gets whether a managed base reference is replaced with <c>System.Object</c> in the WinMD.
    /// </summary>
    /// <param name="baseType">The managed base type reference.</param>
    /// <returns>Whether the base is abstract or explicitly excluded from component metadata.</returns>
    private bool IsErasedBaseType(ITypeDefOrRef baseType)
    {
        return SafeResolve(baseType) is { } definition &&
               (definition.IsAbstract || definition.IsExcludedFromWindowsMetadata());
    }

    /// <summary>
    /// Prevents exclusions from removing implementations required by retained interface contracts.
    /// </summary>
    private void ValidateRequiredInterfaceMembers(TypeDefinition type)
    {
        List<InterfaceImplementation> interfaces = GatherAllInterfaces(type);
        HashSet<string> mappedAncestors = [];

        // Keep ancestors that still supply required members of an exported mapped interface
        foreach (InterfaceImplementation implementation in interfaces)
        {
            if (implementation.Interface is not { } iface ||
                !_mapper.HasMappingForType(GetInterfaceFullName(iface)) ||
                ResolveInterface(iface) is not { } definition)
            {
                continue;
            }

            TypeSignature[]? arguments = iface is TypeSpecification { Signature: GenericInstanceTypeSignature generic }
                ? [.. generic.TypeArguments]
                : null;

            foreach (InterfaceImplementation ancestor in GatherAllInterfaces(definition, arguments))
            {
                if (ancestor.Interface is { } inherited)
                {
                    _ = mappedAncestors.Add(inherited.FullName);
                }
            }
        }

        // Check retained contracts against their explicit or inherited managed implementations
        foreach (InterfaceImplementation implementation in interfaces)
        {
            if (implementation.Interface is not { } iface ||
                !IsPubliclyAccessible(iface) ||
                (TypeMapper.ImplementedInterfacesWithoutMapping.Contains(GetInterfaceFullName(iface)) &&
                 !mappedAncestors.Contains(iface.FullName)) ||
                ResolveInterface(iface) is not { } definition ||
                definition.IsExcludedFromWindowsMetadata())
            {
                continue;
            }

            TypeSignature[]? arguments = iface is TypeSpecification { Signature: GenericInstanceTypeSignature generic }
                ? [.. generic.TypeArguments]
                : null;

            foreach (MethodDefinition contractMethod in definition.Methods)
            {
                if (!contractMethod.IsPublic || contractMethod.IsStatic || contractMethod.IsExcludedFromWindowsMetadata())
                {
                    continue;
                }

                MethodSignature signature = arguments is null ? contractMethod.Signature! : MethodSignature.CreateInstance(
                    ResolveGenericArg(contractMethod.Signature!.ReturnType, arguments),
                    [.. contractMethod.Signature.ParameterTypes.Select(parameter => ResolveGenericArg(parameter, arguments))]);

                // MemberRefs on constructed interfaces can retain generic parameters in their signatures
                MethodDefinition? method = type.MethodImplementations
                    .Where(entry => entry.Declaration is { DeclaringType: { } declaringType } declaration &&
                                    declaringType.FullName == iface.FullName &&
                                    declaration.Name == contractMethod.Name &&
                                    (SignatureComparer.Default.Equals(declaration.Signature, contractMethod.Signature) ||
                                     SignatureComparer.Default.Equals(declaration.Signature, signature)))
                    .Select(static entry => entry.Body as MethodDefinition)
                    .FirstOrDefault() ?? FindMatchingMethod(type, contractMethod, interfaceGenericArgs: arguments);

                if (method?.IsExcludedFromWindowsMetadata() == true)
                {
                    throw WellKnownWinMDExceptions.InvalidMetadataExclusion(
                        method.FullName, $"the member is required by retained interface '{iface.FullName}'.");
                }
            }
        }
    }

    /// <summary>
    /// Resolves either a constructed or a non-generic interface.
    /// </summary>
    private TypeDefinition? ResolveInterface(ITypeDefOrRef type)
    {
        return type is TypeSpecification { Signature: GenericInstanceTypeSignature generic }
            ? SafeResolve(generic.GenericType)
            : SafeResolve(type);
    }

    /// <summary>
    /// Gets whether an explicit implementation belongs to an excluded interface or interface member.
    /// </summary>
    private bool IsIgnoredInterfaceImplementation(MethodImplementation implementation)
    {
        return implementation.Declaration is { DeclaringType: { } declaringType } declaration &&
               ResolveInterface(declaringType) is { } type &&
               (type.IsExcludedFromWindowsMetadata() ||
                type.Methods.Any(method =>
                   method.Name == declaration.Name &&
                   SignatureComparer.Default.Equals(method.Signature, declaration.Signature) &&
                   method.IsExcludedFromWindowsMetadata()));
    }

    /// <summary>
    /// Gets whether a method explicitly implements an excluded interface member.
    /// </summary>
    private bool IsIgnoredInterfaceImplementation(
        TypeDefinition type,
        MethodDefinition method)
    {
        return type.MethodImplementations.Any(implementation =>
            implementation.Body == method && IsIgnoredInterfaceImplementation(implementation));
    }

    /// <summary>
    /// Rejects excluded types anywhere within an exported signature.
    /// </summary>
    private void ValidateExportedTypeSignature(
        TypeSignature signature,
        string declaration)
    {
        signature = signature.StripCustomModifiers();

        switch (signature)
        {
            case GenericInstanceTypeSignature generic:
                ValidateExportedTypeReference(generic.GenericType, declaration);

                foreach (TypeSignature argument in generic.TypeArguments)
                {
                    ValidateExportedTypeSignature(argument, declaration);
                }

                break;
            case TypeDefOrRefSignature named:
                ValidateExportedTypeReference(named.Type, declaration);
                break;
            case TypeSpecificationSignature wrapped:
                ValidateExportedTypeSignature(wrapped.BaseType, declaration);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Guards both normal discovery and on-demand imports against excluded type references.
    /// </summary>
    private void ValidateExportedTypeReference(
        ITypeDefOrRef reference,
        string declaration)
    {
        if (reference is TypeSpecification specification)
        {
            ValidateExportedTypeSignature(specification.Signature!, declaration);
        }
        else if (SafeResolve(reference) is { } type && type.IsExcludedFromWindowsMetadata())
        {
            throw WellKnownWinMDExceptions.IgnoredTypeInExportedSignature(declaration, type.FullName);
        }
    }
}