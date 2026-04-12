// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.WinMDGenerator.Discovery;

namespace WindowsRuntime.WinMDGenerator.Generation;

internal sealed partial class WinmdWriter
{
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
        string typeName = AssemblyAnalyzer.GetQualifiedName(inputType);
        AddGuidAttributeFromName(outputType, typeName);
    }

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
                _outputModule.CorLibTypeFactory.Byte));

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

    private void AddVersionAttribute(TypeDefinition outputType, int version)
    {
        TypeReference versionAttrType = GetOrCreateTypeReference(
            "Windows.Foundation.Metadata", "VersionAttribute", "Windows.Foundation.FoundationContract");

        MemberReference versionCtor = new(versionAttrType, ".ctor",
            MethodSignature.CreateInstance(
                _outputModule.CorLibTypeFactory.Void,
                _outputModule.CorLibTypeFactory.UInt32));

        CustomAttributeSignature sig = new();
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.UInt32, (uint)version));

        outputType.CustomAttributes.Add(new CustomAttribute(versionCtor, sig));
    }

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
                    systemType.ToTypeSignature(),
                    _outputModule.CorLibTypeFactory.UInt32));

            CustomAttributeSignature sig = new();
            sig.FixedArguments.Add(new CustomAttributeArgument(systemType.ToTypeSignature(), ResolveTypeNameToSignature(factoryInterface)));
            sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.UInt32, version));

            outputType.CustomAttributes.Add(new CustomAttribute(ctor, sig));
        }
        else
        {
            // Constructor: ActivatableAttribute(uint)
            MemberReference ctor = new(activatableAttrType, ".ctor",
                MethodSignature.CreateInstance(
                    _outputModule.CorLibTypeFactory.Void,
                    _outputModule.CorLibTypeFactory.UInt32));

            CustomAttributeSignature sig = new();
            sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.UInt32, version));

            outputType.CustomAttributes.Add(new CustomAttribute(ctor, sig));
        }
    }

    private void AddStaticAttribute(TypeDefinition classOutputType, uint version, string staticInterfaceName)
    {
        TypeReference staticAttrType = GetOrCreateTypeReference(
            "Windows.Foundation.Metadata", "StaticAttribute", "Windows.Foundation.FoundationContract");

        TypeReference systemType = GetOrCreateTypeReference("System", "Type", "mscorlib");
        MemberReference ctor = new(staticAttrType, ".ctor",
            MethodSignature.CreateInstance(
                _outputModule.CorLibTypeFactory.Void,
                systemType.ToTypeSignature(),
                _outputModule.CorLibTypeFactory.UInt32));

        CustomAttributeSignature sig = new();
        sig.FixedArguments.Add(new CustomAttributeArgument(systemType.ToTypeSignature(), ResolveTypeNameToSignature(staticInterfaceName)));
        sig.FixedArguments.Add(new CustomAttributeArgument(_outputModule.CorLibTypeFactory.UInt32, version));

        classOutputType.CustomAttributes.Add(new CustomAttribute(ctor, sig));
    }

    private void AddExclusiveToAttribute(TypeDefinition interfaceType, string className)
    {
        TypeReference exclusiveToAttrType = GetOrCreateTypeReference(
            "Windows.Foundation.Metadata", "ExclusiveToAttribute", "Windows.Foundation.FoundationContract");

        TypeReference systemType = GetOrCreateTypeReference("System", "Type", "mscorlib");
        MemberReference ctor = new(exclusiveToAttrType, ".ctor",
            MethodSignature.CreateInstance(
                _outputModule.CorLibTypeFactory.Void,
                systemType.ToTypeSignature()));

        CustomAttributeSignature sig = new();
        sig.FixedArguments.Add(new CustomAttributeArgument(systemType.ToTypeSignature(), ResolveTypeNameToSignature(className)));

        interfaceType.CustomAttributes.Add(new CustomAttribute(ctor, sig));
    }

    private void AddDefaultAttribute(InterfaceImplementation interfaceImpl)
    {
        TypeReference defaultAttrType = GetOrCreateTypeReference(
            "Windows.Foundation.Metadata", "DefaultAttribute", "Windows.Foundation.FoundationContract");

        MemberReference ctor = new(defaultAttrType, ".ctor",
            MethodSignature.CreateInstance(_outputModule.CorLibTypeFactory.Void));

        interfaceImpl.CustomAttributes.Add(new CustomAttribute(ctor, new CustomAttributeSignature()));
    }

    private static bool HasVersionAttribute(TypeDefinition type)
    {
        return type.CustomAttributes.Any(
            attr => attr.Constructor?.DeclaringType?.Name?.Value == "VersionAttribute");
    }

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
    private void CopyCustomAttributes(IHasCustomAttribute source, IHasCustomAttribute target)
    {
        foreach (CustomAttribute attr in source.CustomAttributes)
        {
            if (!ShouldCopyAttribute(attr))
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
    private static bool ShouldCopyAttribute(CustomAttribute attr)
    {
        string? attrTypeName = attr.Constructor?.DeclaringType?.FullName;

        if (attrTypeName is null)
        {
            return false;
        }

        // Skip attributes already handled separately by the generator
        if (attrTypeName is
            "System.Runtime.InteropServices.GuidAttribute" or
            "WinRT.GeneratedBindableCustomPropertyAttribute" or
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
        TypeDefinition? attrTypeDef = attr.Constructor?.DeclaringType?.Resolve();
        if (attrTypeDef != null && !attrTypeDef.IsPublic && !attrTypeDef.IsNestedPublic)
        {
            return false;
        }

        // Skip attributes with unreadable signatures
        return attr.Signature != null;
    }

    /// <summary>
    /// Imports an attribute constructor reference into the output module.
    /// </summary>
    private MemberReference? ImportAttributeConstructor(ICustomAttributeType? ctor)
    {
        if (ctor?.DeclaringType == null || ctor.Signature is not MethodSignature methodSig)
        {
            return null;
        }

        ITypeDefOrRef importedType = ImportTypeReference(ctor.DeclaringType);

        TypeSignature[] mappedParams = [.. methodSig.ParameterTypes
            .Select(MapTypeSignatureToOutput)];

        MethodSignature importedSig = MethodSignature.CreateInstance(
            _outputModule.CorLibTypeFactory.Void,
            mappedParams);

        return new MemberReference(importedType, ".ctor", importedSig);
    }

    /// <summary>
    /// Clones a custom attribute signature, remapping type references to the output module.
    /// </summary>
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
    /// Clones a single custom attribute argument, remapping type references.
    /// </summary>
    private CustomAttributeArgument CloneAttributeArgument(CustomAttributeArgument arg)
    {
        TypeSignature mappedType = MapTypeSignatureToOutput(arg.ArgumentType);
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
    /// Encodes a GUID from a SHA1 hash (UUID v5 format).
    /// </summary>
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