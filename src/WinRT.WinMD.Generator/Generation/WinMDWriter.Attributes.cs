// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.WinMDGenerator.Models;

namespace WindowsRuntime.WinMDGenerator.Generation;

/// <inheritdoc cref="WinMDWriter"/>
internal sealed partial class WinMDWriter
{
    /// <summary>
    /// Adds a <c>[Guid]</c> attribute to an output type, sourced from the input type's
    /// <c>[GuidAttribute]</c> or generated from the type name using SHA1 (UUID v5).
    /// </summary>
    /// <param name="outputType">The output <see cref="TypeDefinition"/> to add the attribute to.</param>
    /// <param name="inputType">The input <see cref="TypeDefinition"/> to read the GUID from.</param>
    private void AddGuidAttribute(TypeDefinition outputType, TypeDefinition inputType)
    {
        // Check if the input type has a GuidAttribute
        foreach (CustomAttribute attr in inputType.CustomAttributes)
        {
            if (attr.Constructor?.DeclaringType?.FullName == "System.Runtime.InteropServices.GuidAttribute" &&
                attr.Signature?.FixedArguments.Count > 0 &&
                attr.Signature.FixedArguments[0].Element is { } guidElement &&
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
        Guid guid;
        // CodeQL [SM02196] WinRT uses UUID v5 SHA1 to generate Guids for parameterized types.
#pragma warning disable CA5350
        byte[] hash = SHA1.HashData(Encoding.UTF8.GetBytes(name));
        guid = EncodeGuid(hash);
#pragma warning restore CA5350

        AddGuidAttribute(outputType, guid);
    }

    /// <summary>
    /// Adds a <c>[Guid]</c> attribute to the output type with the specified GUID value.
    /// </summary>
    /// <remarks>
    /// The <c>GuidAttribute</c> constructor in <c>Windows.Foundation.Metadata</c> takes 11 parameters:
    /// <c>(uint, ushort, ushort, byte, byte, byte, byte, byte, byte, byte, byte)</c>,
    /// matching the decomposed fields of a GUID.
    /// </remarks>
    /// <param name="outputType">The output <see cref="TypeDefinition"/> to add the attribute to.</param>
    /// <param name="guid">The GUID value to encode.</param>
    private void AddGuidAttribute(TypeDefinition outputType, Guid guid)
    {
        // Create a reference to the GuidAttribute constructor in Windows.Foundation.Metadata
        TypeReference guidAttrType = GetOrCreateTypeReference(
            "Windows.Foundation.Metadata", "GuidAttribute", "Windows.Foundation.FoundationContract");

        byte[] guidBytes = guid.ToByteArray();

        // The GuidAttribute constructor takes (uint, ushort, ushort, byte, byte, byte, byte, byte, byte, byte, byte)
        MemberReference guidCtor = new(guidAttrType, ".ctor",
            MethodSignature.CreateInstance(
                _outputModule.CorLibTypeFactory.Void,
                [_outputModule.CorLibTypeFactory.UInt32,
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

        CustomAttributeSignature sig = new();
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.UInt32, BitConverter.ToUInt32(guidBytes, 0)));
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.UInt16, BitConverter.ToUInt16(guidBytes, 4)));
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.UInt16, BitConverter.ToUInt16(guidBytes, 6)));
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.Byte, guidBytes[8]));
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.Byte, guidBytes[9]));
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.Byte, guidBytes[10]));
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.Byte, guidBytes[11]));
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.Byte, guidBytes[12]));
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.Byte, guidBytes[13]));
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.Byte, guidBytes[14]));
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.Byte, guidBytes[15]));

        outputType.CustomAttributes.Add(new CustomAttribute(guidCtor, sig));
    }

    /// <summary>
    /// Adds a <c>[Version]</c> attribute to the output type.
    /// </summary>
    /// <param name="outputType">The output <see cref="TypeDefinition"/> to add the attribute to.</param>
    /// <param name="version">The version number to encode.</param>
    private void AddVersionAttribute(TypeDefinition outputType, int version)
    {
        TypeReference versionAttrType = GetOrCreateTypeReference(
            "Windows.Foundation.Metadata", "VersionAttribute", "Windows.Foundation.FoundationContract");

        MemberReference versionCtor = new(versionAttrType, ".ctor",
            MethodSignature.CreateInstance(
                _outputModule.CorLibTypeFactory.Void,
                [_outputModule.CorLibTypeFactory.UInt32]));

        CustomAttributeSignature sig = new();
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.UInt32, (uint)version));

        outputType.CustomAttributes.Add(new CustomAttribute(versionCtor, sig));
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
            "Windows.Foundation.Metadata", "ActivatableAttribute", "Windows.Foundation.FoundationContract");

        if (factoryInterface != null)
        {
            // Constructor: ActivatableAttribute(Type, uint)
            TypeReference systemType = GetOrCreateTypeReference("System", "Type", "mscorlib");
            MemberReference ctor = new(activatableAttrType, ".ctor",
                MethodSignature.CreateInstance(
                    _outputModule.CorLibTypeFactory.Void,
                    [systemType.ToTypeSignature(false),
                    _outputModule.CorLibTypeFactory.UInt32]));

            CustomAttributeSignature sig = new();
            sig.FixedArguments.Add(new CustomAttributeArgument(systemType.ToTypeSignature(false), ResolveTypeNameToSignature(factoryInterface)));
            sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.UInt32, version));

            outputType.CustomAttributes.Add(new CustomAttribute(ctor, sig));
        }
        else
        {
            // Constructor: ActivatableAttribute(uint)
            MemberReference ctor = new(activatableAttrType, ".ctor",
                MethodSignature.CreateInstance(
                    _outputModule.CorLibTypeFactory.Void,
                    [_outputModule.CorLibTypeFactory.UInt32]));

            CustomAttributeSignature sig = new();
            sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.UInt32, version));

            outputType.CustomAttributes.Add(new CustomAttribute(ctor, sig));
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
            "Windows.Foundation.Metadata", "StaticAttribute", "Windows.Foundation.FoundationContract");

        TypeReference systemType = GetOrCreateTypeReference("System", "Type", "mscorlib");
        MemberReference ctor = new(staticAttrType, ".ctor",
            MethodSignature.CreateInstance(
                _outputModule.CorLibTypeFactory.Void,
                [systemType.ToTypeSignature(false),
                _outputModule.CorLibTypeFactory.UInt32]));

        CustomAttributeSignature sig = new();
        sig.FixedArguments.Add(new CustomAttributeArgument(systemType.ToTypeSignature(false), ResolveTypeNameToSignature(staticInterfaceName)));
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.UInt32, version));

        classOutputType.CustomAttributes.Add(new CustomAttribute(ctor, sig));
    }

    /// <summary>
    /// Adds an <c>[ExclusiveTo]</c> attribute to a synthesized interface, binding it to its owning class.
    /// </summary>
    /// <param name="interfaceType">The synthesized interface <see cref="TypeDefinition"/> to add the attribute to.</param>
    /// <param name="className">The fully-qualified name of the owning runtime class.</param>
    private void AddExclusiveToAttribute(TypeDefinition interfaceType, string className)
    {
        TypeReference exclusiveToAttrType = GetOrCreateTypeReference(
            "Windows.Foundation.Metadata", "ExclusiveToAttribute", "Windows.Foundation.FoundationContract");

        TypeReference systemType = GetOrCreateTypeReference("System", "Type", "mscorlib");
        MemberReference ctor = new(exclusiveToAttrType, ".ctor",
            MethodSignature.CreateInstance(
                _outputModule.CorLibTypeFactory.Void,
                [systemType.ToTypeSignature(false)]));

        CustomAttributeSignature sig = new();
        sig.FixedArguments.Add(new CustomAttributeArgument(systemType.ToTypeSignature(false), ResolveTypeNameToSignature(className)));

        interfaceType.CustomAttributes.Add(new CustomAttribute(ctor, sig));
    }

    /// <summary>
    /// Adds a <c>[Default]</c> attribute to an interface implementation on a class.
    /// </summary>
    /// <remarks>
    /// The <c>[Default]</c> attribute marks the default interface for a WinRT runtime class,
    /// which determines the primary vtable layout and is the interface used when casting
    /// to <c>IInspectable</c>.
    /// </remarks>
    /// <param name="interfaceImpl">The <see cref="InterfaceImplementation"/> to mark as default.</param>
    private void AddDefaultAttribute(InterfaceImplementation interfaceImpl)
    {
        TypeReference defaultAttrType = GetOrCreateTypeReference(
            "Windows.Foundation.Metadata", "DefaultAttribute", "Windows.Foundation.FoundationContract");

        MemberReference ctor = new(defaultAttrType, ".ctor",
            MethodSignature.CreateInstance(_outputModule.CorLibTypeFactory.Void));

        interfaceImpl.CustomAttributes.Add(new CustomAttribute(ctor, new CustomAttributeSignature()));
    }

    /// <summary>
    /// Checks whether a type already has a <c>[Version]</c> or <c>[ContractVersion]</c> attribute.
    /// </summary>
    /// <param name="type">The <see cref="TypeDefinition"/> to check.</param>
    /// <returns><see langword="true"/> if the type has a version attribute; otherwise, <see langword="false"/>.</returns>
    private static bool HasVersionAttribute(TypeDefinition type)
    {
        return type.CustomAttributes.Any(
            attr => attr.Constructor?.DeclaringType?.Name?.Value is "VersionAttribute" or "ContractVersionAttribute");
    }

    /// <summary>
    /// Gets the version number for a type from its <c>[Version]</c> attribute, or falls back
    /// to the assembly major version.
    /// </summary>
    /// <param name="type">The <see cref="TypeDefinition"/> to read the version from.</param>
    /// <returns>The version number as an integer.</returns>
    private int GetVersion(TypeDefinition type)
    {
        foreach (CustomAttribute attr in type.CustomAttributes)
        {
            if (attr.Constructor?.DeclaringType?.Name?.Value == "VersionAttribute" &&
                attr.Signature?.FixedArguments.Count > 0 &&
                attr.Signature.FixedArguments[0].Element is uint version)
            {
                return (int)version;
            }
        }

        return Version.Parse(_version).Major;
    }

    /// <summary>
    /// Copies custom attributes from a source metadata element to a target metadata element,
    /// filtering out attributes that are handled separately by the generator or not meaningful for WinMD.
    /// </summary>
    /// <param name="source">The source element to copy attributes from.</param>
    /// <param name="target">The target element to copy attributes to.</param>
    private void CopyCustomAttributes(IHasCustomAttribute source, IHasCustomAttribute target)
    {
        foreach (CustomAttribute attr in source.CustomAttributes)
        {
            if (!ShouldCopyAttribute(attr, _runtimeContext))
            {
                continue;
            }

            MemberReference? importedCtor = ImportAttributeConstructor(attr.Constructor);
            if (importedCtor == null)
            {
                continue;
            }

            CustomAttributeSignature clonedSig = CloneAttributeSignature(attr.Signature);
            target.CustomAttributes.Add(new CustomAttribute(importedCtor, clonedSig));
        }
    }

    /// <summary>
    /// Determines whether a custom attribute should be copied to the output WinMD.
    /// </summary>
    /// <remarks>
    /// Filters out attributes that are handled separately by the generator (e.g., <c>GuidAttribute</c>,
    /// <c>VersionAttribute</c>), compiler-generated attributes (e.g., from <c>System.Runtime.CompilerServices</c>),
    /// non-public attribute types, and attributes with unreadable signatures.
    /// </remarks>
    /// <param name="attr">The custom attribute to evaluate.</param>
    /// <param name="runtimeContext">The runtime context for resolving attribute types, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if the attribute should be copied; otherwise, <see langword="false"/>.</returns>
    private static bool ShouldCopyAttribute(CustomAttribute attr, RuntimeContext? runtimeContext)
    {
        string? attrTypeName = attr.Constructor?.DeclaringType?.FullName;

        if (attrTypeName is null)
        {
            return false;
        }

        // Skip attributes already handled separately by the generator
        if (attrTypeName is
            "System.Runtime.InteropServices.GuidAttribute" or
            "WindowsRuntime.Xaml.GeneratedCustomPropertyProviderAttribute" or
            "Windows.Foundation.Metadata.VersionAttribute" or
            "System.Reflection.DefaultMemberAttribute")
        {
            return false;
        }

        // Skip compiler-generated attributes not meaningful for WinMD
        if (attrTypeName.StartsWith("System.Runtime.CompilerServices.", StringComparison.Ordinal))
        {
            return false;
        }

        // Skip non-public attribute types (if resolvable)
        if (runtimeContext is not null
            && attr.Constructor?.DeclaringType is { } attrType
            && attrType is not TypeDefinition
            && attrType.Resolve(runtimeContext, out TypeDefinition? attrTypeDef) == ResolutionStatus.Success
            && !attrTypeDef!.IsPublic && !attrTypeDef.IsNestedPublic)
        {
            return false;
        }

        // Skip attributes with unreadable signatures
        return attr.Signature != null;
    }

    /// <summary>
    /// Imports an attribute constructor reference into the output module.
    /// </summary>
    /// <remarks>
    /// Attribute constructor parameters must use CLR types (<c>System.Type</c>, <c>System.String</c>, etc.)
    /// not WinRT projected types (<c>TypeName</c>, <c>HString</c>), because the custom attribute blob
    /// serializer only supports primitives, <c>System.Type</c>, <c>System.String</c>, and enum types.
    /// </remarks>
    /// <param name="ctor">The constructor to import.</param>
    /// <returns>The imported <see cref="MemberReference"/>, or <see langword="null"/> if the constructor is invalid.</returns>
    private MemberReference? ImportAttributeConstructor(ICustomAttributeType? ctor)
    {
        if (ctor?.DeclaringType == null || ctor.Signature is not MethodSignature methodSig)
        {
            return null;
        }

        ITypeDefOrRef importedType = ImportTypeReference(ctor.DeclaringType);

        // Attribute constructor parameters must use CLR types (System.Type, System.String, etc.)
        // not WinRT projected types (TypeName, HString), because the custom attribute blob
        // serializer only supports primitives, System.Type, System.String, and enum types.
        TypeSignature[] importedParams = [.. methodSig.ParameterTypes
            .Select(ImportTypeSignatureForAttribute)];

        MethodSignature importedSig = MethodSignature.CreateInstance(
            _outputModule.CorLibTypeFactory.Void,
            importedParams);

        return new MemberReference(importedType, ".ctor", importedSig);
    }

    /// <summary>
    /// Clones a custom attribute signature, remapping type references to the output module.
    /// </summary>
    /// <param name="inputSig">The input attribute signature to clone, or <see langword="null"/>.</param>
    /// <returns>A new <see cref="CustomAttributeSignature"/> with remapped type references.</returns>
    private CustomAttributeSignature CloneAttributeSignature(CustomAttributeSignature? inputSig)
    {
        if (inputSig == null)
        {
            return new CustomAttributeSignature();
        }

        CustomAttributeSignature outputSig = new();

        foreach (CustomAttributeArgument arg in inputSig.FixedArguments)
        {
            outputSig.FixedArguments.Add(CloneAttributeArgument(arg));
        }

        foreach (CustomAttributeNamedArgument namedArg in inputSig.NamedArguments)
        {
            TypeSignature mappedArgType = MapTypeSignatureToOutput(namedArg.Argument.ArgumentType);
            CustomAttributeArgument clonedInnerArg = CloneAttributeArgument(namedArg.Argument);

            outputSig.NamedArguments.Add(new CustomAttributeNamedArgument(
                namedArg.MemberType,
                namedArg.MemberName,
                mappedArgType,
                clonedInnerArg));
        }

        return outputSig;
    }

    /// <summary>
    /// Imports a type signature for use in attribute constructor parameters.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="MapTypeSignatureToOutput"/>, this preserves CLR types
    /// (<c>System.Type</c>, <c>System.String</c>) that custom attribute blobs require.
    /// Enum types are imported so the blob encoder can resolve the underlying type.
    /// </remarks>
    /// <param name="sig">The type signature to import.</param>
    /// <returns>The imported <see cref="TypeSignature"/> suitable for attribute parameter use.</returns>
    private TypeSignature ImportTypeSignatureForAttribute(TypeSignature sig)
    {
        if (sig is CorLibTypeSignature)
        {
            return sig;
        }

        if (sig is TypeDefOrRefSignature typeDefOrRefSig)
        {
            string? fullName = typeDefOrRefSig.Type?.FullName;

            // System.Type must stay as System.Type for attribute blob encoding
            if (fullName == "System.Type")
            {
                return GetOrCreateTypeReference("System", "Type", "mscorlib").ToTypeSignature(false);
            }

            // Enum types: import the reference so the blob encoder can resolve the underlying type
            TypeDefinition? resolved = SafeResolve(typeDefOrRefSig.Type);
            if (resolved != null && resolved.IsEnum)
            {
                return ImportTypeReference(typeDefOrRefSig.Type!).ToTypeSignature(typeDefOrRefSig.IsValueType);
            }

            // For other types, import directly without WinRT mapping
            return ImportTypeReference(typeDefOrRefSig.Type!).ToTypeSignature(typeDefOrRefSig.IsValueType);
        }

        // Fallback: use the standard mapping
        return MapTypeSignatureToOutput(sig);
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
                clonedArg.Elements.Add(element is TypeSignature typeSig
                    ? MapTypeSignatureToOutput(typeSig)
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
    /// WinRT uses UUID v5 SHA1 hashing to generate GUIDs for parameterized types.
    /// The method sets the version nibble to 5 and the variant bits to RFC 4122,
    /// handling endianness for the first three GUID fields.
    /// </remarks>
    /// <param name="hash">The SHA1 hash bytes (at least 16 bytes).</param>
    /// <returns>The encoded <see cref="Guid"/>.</returns>
    private static Guid EncodeGuid(byte[] hash)
    {
        byte[] guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);

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
        return _typeDefinitionMapping.TryGetValue(qualifiedTypeName, out TypeDeclaration? declaration) && declaration.OutputType != null
            ? declaration.OutputType.ToTypeSignature()
            : throw new InvalidOperationException($"Type '{qualifiedTypeName}' not found in the output type definitions.");
    }
}