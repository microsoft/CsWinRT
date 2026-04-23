// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.WinMDGenerator.Models;

namespace WindowsRuntime.WinMDGenerator.Writers;

/// <inheritdoc cref="WinMDWriter"/>
internal sealed partial class WinMDWriter
{
    /// <summary>
    /// Adds a <c>[Guid]</c> attribute to an output type, sourced from the input type's
    /// <c>[Guid]</c> attribute or generated from the type name using SHA1 (UUID v5).
    /// </summary>
    /// <param name="outputType">The output <see cref="TypeDefinition"/> to add the attribute to.</param>
    /// <param name="inputType">The input <see cref="TypeDefinition"/> to read the GUID from.</param>
    private void AddGuidAttribute(TypeDefinition outputType, TypeDefinition inputType)
    {
        // Check if the input type has a '[Guid]' attribute
        foreach (CustomAttribute attribute in inputType.CustomAttributes)
        {
            if (attribute.Constructor?.DeclaringType?.FullName == "System.Runtime.InteropServices.GuidAttribute" &&
                attribute.Signature?.FixedArguments.Count > 0 &&
                attribute.Signature.FixedArguments[0].Element is { } guidElement &&
                Guid.TryParse(guidElement.ToString(), out Guid guid))
            {
                AddGuidAttribute(outputType, guid);

                return;
            }
        }

        // Generate a GUID from the type name using SHA1
        string typeName = inputType.QualifiedName;

        AddGuidAttributeFromName(outputType, typeName);
    }

    /// <summary>
    /// Generates a GUID from a type name using SHA1 hashing (UUID v5 format)
    /// and adds it as a <c>[Guid]</c> attribute on the output type.
    /// </summary>
    /// <param name="outputType">The output <see cref="TypeDefinition"/> to add the attribute to.</param>
    /// <param name="name">The fully-qualified type name to hash.</param>
    private void AddGuidAttributeFromName(TypeDefinition outputType, string name)
    {
        int maxNumberOfBytes = Encoding.UTF8.GetMaxByteCount(name.Length);

        // Get a buffer from the array pool, or stack-allocated if small enough
        byte[]? array = null;
        Span<byte> span = maxNumberOfBytes <= 256
            ? stackalloc byte[256]
            : (array = ArrayPool<byte>.Shared.Rent(maxNumberOfBytes));

        // Encode to UTF8 into the buffer
        int writtenNumberOfBytes = Encoding.UTF8.GetBytes(name, span);

        // Always stack-allocate the hash buffer (since it's constant)
        Span<byte> hash = stackalloc byte[SHA1.HashSizeInBytes];

        // Hash the data (we know we'll always fully fill the buffer).
        // CodeQL [SM02196] Windows Runtime uses UUID v5 SHA1 to generate Guids for parameterized types.
        _ = SHA1.HashData(span[..writtenNumberOfBytes], hash);

        // Return the rented array to the pool, if we have one
        if (array is not null)
        {
            ArrayPool<byte>.Shared.Return(array);
        }

        Guid guid = EncodeGuid(hash);

        AddGuidAttribute(outputType, guid);
    }

    /// <summary>
    /// Adds a <c>[Guid]</c> attribute to the output type with the specified GUID value.
    /// </summary>
    /// <remarks>
    /// The <c>[Guid]</c> attribute constructor in <c>Windows.Foundation.Metadata</c> takes 11 parameters:
    /// <c>(uint, ushort, ushort, byte, byte, byte, byte, byte, byte, byte, byte)</c>,
    /// matching the decomposed fields of a GUID.
    /// </remarks>
    /// <param name="outputType">The output <see cref="TypeDefinition"/> to add the attribute to.</param>
    /// <param name="guid">The GUID value to encode.</param>
    private void AddGuidAttribute(TypeDefinition outputType, Guid guid)
    {
        // Create a reference to the '[Guid]' attribute constructor in 'Windows.Foundation.Metadata'
        TypeReference guidAttrType = GetOrCreateTypeReference(
            @namespace: "Windows.Foundation.Metadata",
            name: "GuidAttribute",
            assemblyName: "Windows.Foundation.FoundationContract");

        byte[] guidBytes = guid.ToByteArray();

        // The 'GuidAttribute(uint, ushort, ushort, byte, byte, byte, byte, byte, byte, byte, byte)' constructor
        MemberReference guidCtor = new(
            parent: guidAttrType,
            name: ".ctor"u8,
            signature: MethodSignature.CreateInstance(
                returnType: _outputModule.CorLibTypeFactory.Void,
                parameterTypes: [
                    _outputModule.CorLibTypeFactory.UInt32,
                    _outputModule.CorLibTypeFactory.UInt16,
                    _outputModule.CorLibTypeFactory.UInt16,
                    _outputModule.CorLibTypeFactory.Byte,
                    _outputModule.CorLibTypeFactory.Byte,
                    _outputModule.CorLibTypeFactory.Byte,
                    _outputModule.CorLibTypeFactory.Byte,
                    _outputModule.CorLibTypeFactory.Byte,
                    _outputModule.CorLibTypeFactory.Byte,
                    _outputModule.CorLibTypeFactory.Byte,
                    _outputModule.CorLibTypeFactory.Byte]));

        // Create the signature for the attribute
        CustomAttributeSignature signature = new()
        {
            FixedArguments =
            {
                new CustomAttributeArgument(_outputModule.CorLibTypeFactory.UInt32, BitConverter.ToUInt32(guidBytes, 0)),
                new CustomAttributeArgument(_outputModule.CorLibTypeFactory.UInt16, BitConverter.ToUInt16(guidBytes, 4)),
                new CustomAttributeArgument(_outputModule.CorLibTypeFactory.UInt16, BitConverter.ToUInt16(guidBytes, 6)),
                new CustomAttributeArgument(_outputModule.CorLibTypeFactory.Byte, guidBytes[8]),
                new CustomAttributeArgument(_outputModule.CorLibTypeFactory.Byte, guidBytes[9]),
                new CustomAttributeArgument(_outputModule.CorLibTypeFactory.Byte, guidBytes[10]),
                new CustomAttributeArgument(_outputModule.CorLibTypeFactory.Byte, guidBytes[11]),
                new CustomAttributeArgument(_outputModule.CorLibTypeFactory.Byte, guidBytes[12]),
                new CustomAttributeArgument(_outputModule.CorLibTypeFactory.Byte, guidBytes[13]),
                new CustomAttributeArgument(_outputModule.CorLibTypeFactory.Byte, guidBytes[14]),
                new CustomAttributeArgument(_outputModule.CorLibTypeFactory.Byte, guidBytes[15])
            }
        };

        outputType.CustomAttributes.Add(new CustomAttribute(guidCtor, signature));
    }

    /// <summary>
    /// Adds a <c>[Version]</c> attribute to the output type.
    /// </summary>
    /// <param name="outputType">The output <see cref="TypeDefinition"/> to add the attribute to.</param>
    /// <param name="version">The version number to encode.</param>
    private void AddVersionAttribute(TypeDefinition outputType, int version)
    {
        TypeReference versionAttrType = GetOrCreateTypeReference(
            @namespace: "Windows.Foundation.Metadata",
            name: "VersionAttribute",
            assemblyName: "Windows.Foundation.FoundationContract");

        MemberReference versionCtor = new(
            parent: versionAttrType,
            name: ".ctor"u8,
            signature: MethodSignature.CreateInstance(_outputModule.CorLibTypeFactory.Void, [_outputModule.CorLibTypeFactory.UInt32]));

        CustomAttributeSignature signature = new()
        {
            FixedArguments = { new CustomAttributeArgument(_outputModule.CorLibTypeFactory.UInt32, (uint)version) }
        };

        outputType.CustomAttributes.Add(new CustomAttribute(versionCtor, signature));
    }

    /// <summary>
    /// Adds an <c>[Activatable]</c> attribute to the output type.
    /// </summary>
    /// <remarks>
    /// When <paramref name="factoryInterface"/> is <see langword="null"/>, uses the single-parameter
    /// constructor <c>ActivatableAttribute(uint)</c> for default activation. When a factory interface
    /// is provided, uses the two-parameter constructor <c>ActivatableAttribute(Type, uint)</c> to
    /// indicate parameterized activation via the factory.
    /// </remarks>
    /// <param name="outputType">The output class <see cref="TypeDefinition"/> to add the attribute to.</param>
    /// <param name="version">The version number for the activatable attribute.</param>
    /// <param name="factoryInterface">The fully-qualified name of the factory interface, or <see langword="null"/> for default activation.</param>
    private void AddActivatableAttribute(TypeDefinition outputType, uint version, string? factoryInterface)
    {
        TypeReference activatableAttrType = GetOrCreateTypeReference(
            @namespace: "Windows.Foundation.Metadata",
            name: "ActivatableAttribute",
            assemblyName: "Windows.Foundation.FoundationContract");

        if (factoryInterface is not null)
        {
            TypeReference systemType = GetOrCreateTypeReference("System", "Type", "mscorlib");

            // Constructor: 'ActivatableAttribute(Type, uint)'
            MemberReference ctor = new(
                parent: activatableAttrType,
                name: ".ctor"u8,
                signature: MethodSignature.CreateInstance(
                    returnType: _outputModule.CorLibTypeFactory.Void,
                    parameterTypes: [
                        systemType.ToTypeSignature(false),
                        _outputModule.CorLibTypeFactory.UInt32]));

            CustomAttributeSignature signature = new()
            {
                FixedArguments =
                {
                    new CustomAttributeArgument(systemType.ToTypeSignature(false), ResolveTypeNameToSignature(factoryInterface)),
                    new CustomAttributeArgument(_outputModule.CorLibTypeFactory.UInt32, version)
                }
            };

            outputType.CustomAttributes.Add(new CustomAttribute(ctor, signature));
        }
        else
        {
            // Constructor: 'ActivatableAttribute(uint)'
            MemberReference ctor = new(
                parent: activatableAttrType,
                name: ".ctor"u8,
                signature: MethodSignature.CreateInstance(_outputModule.CorLibTypeFactory.Void, [_outputModule.CorLibTypeFactory.UInt32]));

            CustomAttributeSignature signature = new()
            {
                FixedArguments = { new CustomAttributeArgument(_outputModule.CorLibTypeFactory.UInt32, version) }
            };

            outputType.CustomAttributes.Add(new CustomAttribute(ctor, signature));
        }
    }

    /// <summary>
    /// Adds a <c>[Static]</c> attribute to the class output type, referencing the static interface.
    /// </summary>
    /// <param name="classOutputType">The output class <see cref="TypeDefinition"/> to add the attribute to.</param>
    /// <param name="version">The version number for the static attribute.</param>
    /// <param name="staticInterfaceName">The fully-qualified name of the static interface.</param>
    private void AddStaticAttribute(TypeDefinition classOutputType, uint version, string staticInterfaceName)
    {
        TypeReference staticAttrType = GetOrCreateTypeReference(
            @namespace: "Windows.Foundation.Metadata",
            name: "StaticAttribute",
            assemblyName: "Windows.Foundation.FoundationContract");

        TypeReference systemType = GetOrCreateTypeReference("System", "Type", "mscorlib");

        MemberReference ctor = new(
            parent: staticAttrType,
            name: ".ctor"u8,
            signature: MethodSignature.CreateInstance(
                returnType: _outputModule.CorLibTypeFactory.Void,
                parameterTypes: [
                    systemType.ToTypeSignature(false),
                    _outputModule.CorLibTypeFactory.UInt32]));

        CustomAttributeSignature signature = new()
        {
            FixedArguments =
            {
                new CustomAttributeArgument(systemType.ToTypeSignature(false), ResolveTypeNameToSignature(staticInterfaceName)),
                new CustomAttributeArgument(_outputModule.CorLibTypeFactory.UInt32, version)
            }
        };

        classOutputType.CustomAttributes.Add(new CustomAttribute(ctor, signature));
    }

    /// <summary>
    /// Adds an <c>[ExclusiveTo]</c> attribute to a synthesized interface, binding it to its owning class.
    /// </summary>
    /// <param name="interfaceType">The synthesized interface <see cref="TypeDefinition"/> to add the attribute to.</param>
    /// <param name="className">The fully-qualified name of the owning runtime class.</param>
    private void AddExclusiveToAttribute(TypeDefinition interfaceType, string className)
    {
        TypeReference exclusiveToAttrType = GetOrCreateTypeReference(
            @namespace: "Windows.Foundation.Metadata",
            name: "ExclusiveToAttribute",
            assemblyName: "Windows.Foundation.FoundationContract");

        TypeReference systemType = GetOrCreateTypeReference("System", "Type", "mscorlib");

        MemberReference ctor = new(
            parent: exclusiveToAttrType,
            name: ".ctor"u8,
            signature: MethodSignature.CreateInstance(_outputModule.CorLibTypeFactory.Void, [systemType.ToTypeSignature(false)]));

        CustomAttributeSignature signature = new()
        {
            FixedArguments = { new CustomAttributeArgument(systemType.ToTypeSignature(false), ResolveTypeNameToSignature(className)) }
        };

        interfaceType.CustomAttributes.Add(new CustomAttribute(ctor, signature));
    }

    /// <summary>
    /// Adds a <c>[Default]</c> attribute to an interface implementation on a class.
    /// </summary>
    /// <remarks>
    /// The <c>[Default]</c> attribute marks the default interface for a Windows Runtime runtime class,
    /// which determines the primary vtable layout and is the interface used when casting
    /// to <c>IInspectable</c>.
    /// </remarks>
    /// <param name="interfaceImpl">The <see cref="InterfaceImplementation"/> to mark as default.</param>
    private void AddDefaultAttribute(InterfaceImplementation interfaceImpl)
    {
        TypeReference defaultAttrType = GetOrCreateTypeReference(
            @namespace: "Windows.Foundation.Metadata",
            name: "DefaultAttribute",
            assemblyName: "Windows.Foundation.FoundationContract");

        MemberReference ctor = new(
            parent: defaultAttrType,
            name: ".ctor"u8,
            signature: MethodSignature.CreateInstance(_outputModule.CorLibTypeFactory.Void));

        interfaceImpl.CustomAttributes.Add(new CustomAttribute(ctor, new CustomAttributeSignature()));
    }

    /// <summary>
    /// Gets the version number for a type from its <c>[Version]</c> attribute, or falls back
    /// to the assembly major version.
    /// </summary>
    /// <param name="type">The <see cref="TypeDefinition"/> to read the version from.</param>
    /// <returns>The version number as an integer.</returns>
    private int GetVersion(TypeDefinition type)
    {
        return type.VersionAttributeValue ?? Version.Parse(_version).Major;
    }

    /// <summary>
    /// Copies custom attributes from a source metadata element to a target metadata element,
    /// filtering out attributes that are handled separately by the generator or not meaningful for WinMD.
    /// </summary>
    /// <param name="source">The source element to copy attributes from.</param>
    /// <param name="target">The target element to copy attributes to.</param>
    private void CopyCustomAttributes(IHasCustomAttribute source, IHasCustomAttribute target)
    {
        foreach (CustomAttribute attribute in source.CustomAttributes)
        {
            if (!ShouldCopyAttribute(attribute, _runtimeContext))
            {
                continue;
            }

            MemberReference? importedCtor = ImportAttributeConstructor(attribute.Constructor);
            if (importedCtor is null)
            {
                continue;
            }

            CustomAttributeSignature clonedSignature = CloneAttributeSignature(attribute.Signature);
            target.CustomAttributes.Add(new CustomAttribute(importedCtor, clonedSignature));
        }
    }

    /// <summary>
    /// Determines whether a custom attribute should be copied to the output WinMD.
    /// </summary>
    /// <remarks>
    /// Filters out attributes that are handled separately by the generator (e.g., <c>[Guid]</c>,
    /// <c>[Version]</c>), compiler-generated attributes (e.g., from <c>System.Runtime.CompilerServices</c>),
    /// non-public attribute types, and attributes with unreadable signatures.
    /// </remarks>
    /// <param name="attribute">The custom attribute to evaluate.</param>
    /// <param name="runtimeContext">The runtime context for resolving attribute types, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the attribute should be copied; otherwise, <see langword="false"/>.</returns>
    private static bool ShouldCopyAttribute(CustomAttribute attribute, RuntimeContext? runtimeContext)
    {
        string? attributeTypeName = attribute.Constructor?.DeclaringType?.FullName;

        if (attributeTypeName is null)
        {
            return false;
        }

        // Skip attributes already handled separately by the generator
        if (attributeTypeName is
            "System.Runtime.InteropServices.GuidAttribute" or
            "WindowsRuntime.Xaml.GeneratedCustomPropertyProviderAttribute" or
            "Windows.Foundation.Metadata.VersionAttribute" or
            "System.Reflection.DefaultMemberAttribute")
        {
            return false;
        }

        // Skip compiler-generated attributes not meaningful for WinMD
        if (attributeTypeName.StartsWith("System.Runtime.CompilerServices.", StringComparison.Ordinal))
        {
            return false;
        }

        // Skip non-public attribute types (if resolvable)
        if (runtimeContext is not null
            && attribute.Constructor?.DeclaringType is { } attributeType
            && attributeType is not TypeDefinition
            && attributeType.Resolve(runtimeContext, out TypeDefinition? attributeTypeDefinition) == ResolutionStatus.Success
            && !attributeTypeDefinition!.IsPublic && !attributeTypeDefinition.IsNestedPublic)
        {
            return false;
        }

        // Skip attributes with unreadable signatures
        return attribute.Signature is not null;
    }

    /// <summary>
    /// Imports an attribute constructor reference into the output module.
    /// </summary>
    /// <remarks>
    /// Attribute constructor parameters must use CLR types (<see cref="Type"/>, <c>System.String</c>, etc.)
    /// not Windows Runtime projected types (<c>TypeName</c>, <c>HString</c>), because the custom attribute blob
    /// serializer only supports primitives, <see cref="Type"/>, <c>System.String</c>, and enum types.
    /// </remarks>
    /// <param name="ctor">The constructor to import.</param>
    /// <returns>The imported <see cref="MemberReference"/>, or <see langword="null"/> if the constructor is invalid.</returns>
    private MemberReference? ImportAttributeConstructor(ICustomAttributeType? ctor)
    {
        if (ctor?.DeclaringType is null || ctor.Signature is not MethodSignature methodSignature)
        {
            return null;
        }

        ITypeDefOrRef importedType = ImportTypeReference(ctor.DeclaringType);

        // Attribute constructor parameters must use CLR types ('System.Type', 'System.String', etc.)
        // not Windows Runtime projected types ('TypeName', 'HString'), because the custom attribute blob
        // serializer only supports primitives, 'System.Type', 'System.String', and enum types.
        TypeSignature[] importedParams = [.. methodSignature.ParameterTypes
            .Select(ImportTypeSignatureForAttribute)];

        MethodSignature importedSignature = MethodSignature.CreateInstance(
            _outputModule.CorLibTypeFactory.Void,
            importedParams);

        return new MemberReference(importedType, ".ctor", importedSignature);
    }

    /// <summary>
    /// Clones a custom attribute signature, remapping type references to the output module.
    /// </summary>
    /// <param name="inputSignature">The input attribute signature to clone, or <see langword="null"/>.</param>
    /// <returns>A new <see cref="CustomAttributeSignature"/> with remapped type references.</returns>
    private CustomAttributeSignature CloneAttributeSignature(CustomAttributeSignature? inputSignature)
    {
        if (inputSignature is null)
        {
            return new CustomAttributeSignature();
        }

        CustomAttributeSignature outputSignature = new();

        foreach (CustomAttributeArgument arg in inputSignature.FixedArguments)
        {
            outputSignature.FixedArguments.Add(CloneAttributeArgument(arg));
        }

        foreach (CustomAttributeNamedArgument namedArg in inputSignature.NamedArguments)
        {
            TypeSignature mappedArgType = MapTypeSignatureToOutput(namedArg.Argument.ArgumentType);
            CustomAttributeArgument clonedInnerArg = CloneAttributeArgument(namedArg.Argument);

            outputSignature.NamedArguments.Add(new CustomAttributeNamedArgument(
                memberType: namedArg.MemberType,
                memberName: namedArg.MemberName,
                argumentType: mappedArgType,
                argument: clonedInnerArg));
        }

        return outputSignature;
    }

    /// <summary>
    /// Imports a type signature for use in attribute constructor parameters.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="MapTypeSignatureToOutput"/>, this preserves CLR types
    /// (<see cref="Type"/>, <c>System.String</c>) that custom attribute blobs require.
    /// Enum types are imported so the blob encoder can resolve the underlying type.
    /// </remarks>
    /// <param name="signature">The type signature to import.</param>
    /// <returns>The imported <see cref="TypeSignature"/> suitable for attribute parameter use.</returns>
    private TypeSignature ImportTypeSignatureForAttribute(TypeSignature signature)
    {
        if (signature is CorLibTypeSignature)
        {
            return signature;
        }

        if (signature is TypeDefOrRefSignature typeDefOrRefSignature)
        {
            string? fullName = typeDefOrRefSignature.Type?.FullName;

            // 'System.Type' must stay as 'System.Type' for attribute blob encoding
            if (fullName == "System.Type")
            {
                return GetOrCreateTypeReference("System", "Type", "mscorlib").ToTypeSignature(false);
            }

            // Enum types: import the reference so the blob encoder can resolve the underlying type
            TypeDefinition? resolved = SafeResolve(typeDefOrRefSignature.Type);
            if (resolved is not null && resolved.IsEnum)
            {
                return ImportTypeReference(typeDefOrRefSignature.Type!).ToTypeSignature(typeDefOrRefSignature.IsValueType);
            }

            // For other types, import directly without Windows Runtime mapping
            return ImportTypeReference(typeDefOrRefSignature.Type!).ToTypeSignature(typeDefOrRefSignature.IsValueType);
        }

        // Fallback: use the standard mapping
        return MapTypeSignatureToOutput(signature);
    }

    /// <summary>
    /// Clones a single custom attribute argument, remapping type references.
    /// </summary>
    /// <remarks>
    /// Uses attribute-safe type imports via <see cref="ImportTypeSignatureForAttribute"/> to preserve CLR types
    /// required by the blob encoder. Type-valued elements (stored as <see cref="TypeSignature"/>) are
    /// remapped using <see cref="MapTypeSignatureToOutput"/>.
    /// </remarks>
    /// <param name="arg">The attribute argument to clone.</param>
    /// <returns>A new <see cref="CustomAttributeArgument"/> with remapped type references.</returns>
    private CustomAttributeArgument CloneAttributeArgument(CustomAttributeArgument arg)
    {
        TypeSignature mappedType = ImportTypeSignatureForAttribute(arg.ArgumentType);
        CustomAttributeArgument clonedArg = new(mappedType);

        if (arg.IsNullArray)
        {
            clonedArg.IsNullArray = true;
        }
        else
        {
            foreach (object? element in arg.Elements)
            {
                // Type-valued elements are stored as TypeSignature and need remapping
                clonedArg.Elements.Add(element is TypeSignature typeSignature
                    ? MapTypeSignatureToOutput(typeSignature)
                    : element);
            }
        }

        return clonedArg;
    }

    /// <summary>
    /// Determines whether a type is publicly accessible (public or nested public).
    /// </summary>
    /// <remarks>
    /// For <see cref="TypeReference"/> and <see cref="TypeSpecification"/> instances, accessibility
    /// cannot be determined from the reference alone, so they are assumed accessible.
    /// </remarks>
    /// <param name="type">The type reference to check.</param>
    /// <returns><see langword="true"/> if the type is publicly accessible; otherwise, <see langword="false"/>.</returns>
    private static bool IsPubliclyAccessible(ITypeDefOrRef type)
    {
        if (type is TypeDefinition typeDef)
        {
            return typeDef.IsPublic || typeDef.IsNestedPublic;
        }

        // For type references and specs, assume accessible
        return true;
    }

    /// <summary>
    /// Encodes a GUID from a SHA1 hash following the UUID v5 format (RFC 4122).
    /// </summary>
    /// <remarks>
    /// Windows Runtime uses UUID v5 SHA1 hashing to generate GUIDs for parameterized types.
    /// The method sets the version nibble to 5 and the variant bits to RFC 4122,
    /// handling endianness for the first three GUID fields.
    /// </remarks>
    /// <param name="hash">The SHA1 hash bytes (at least 16 bytes).</param>
    /// <returns>The encoded <see cref="Guid"/>.</returns>
    private static Guid EncodeGuid(ReadOnlySpan<byte> hash)
    {
        Span<byte> guidBytes = stackalloc byte[16];

        hash[..16].CopyTo(guidBytes);

        if (BitConverter.IsLittleEndian)
        {
            // Swap bytes of int a (bytes 0-3)
            (guidBytes[0], guidBytes[3]) = (guidBytes[3], guidBytes[0]);
            (guidBytes[1], guidBytes[2]) = (guidBytes[2], guidBytes[1]);

            // Swap bytes of short b (bytes 4-5)
            (guidBytes[4], guidBytes[5]) = (guidBytes[5], guidBytes[4]);

            // Swap bytes of short c (bytes 6-7) and encode version
            byte t = guidBytes[6];
            guidBytes[6] = guidBytes[7];
            guidBytes[7] = (byte)((t & 0x0F) | 0x50); // version 5
        }
        else
        {
            // Set version to 5 (SHA1)
            guidBytes[7] = (byte)((guidBytes[7] & 0x0F) | 0x50);
        }

        // Set variant to RFC 4122
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);

        return new Guid(guidBytes);
    }

    /// <summary>
    /// Resolves a fully-qualified type name to a <see cref="TypeSignature"/> for use in custom attribute arguments.
    /// Uses the output TypeDefinition directly to avoid creating self-referencing TypeReferences.
    /// </summary>
    private TypeSignature ResolveTypeNameToSignature(string qualifiedTypeName)
    {
        return _typeDefinitionMapping.TryGetValue(qualifiedTypeName, out TypeDeclaration? declaration) && declaration.OutputType is not null
            ? declaration.OutputType.ToTypeSignature()
            : throw new InvalidOperationException($"Type '{qualifiedTypeName}' not found in the output type definitions.");
    }
}