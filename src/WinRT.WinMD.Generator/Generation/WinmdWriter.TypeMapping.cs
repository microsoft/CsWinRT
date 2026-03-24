// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.WinMDGenerator.Discovery;
using AssemblyAttributes = AsmResolver.PE.DotNet.Metadata.Tables.AssemblyAttributes;

namespace WindowsRuntime.WinMDGenerator.Generation;

internal sealed partial class WinmdWriter
{
    /// <summary>
    /// Maps a type signature from the input module to the output module.
    /// </summary>
    private TypeSignature MapTypeSignatureToOutput(TypeSignature inputSig)
    {
        // Handle CorLib types
        if (inputSig is CorLibTypeSignature corLib)
        {
#pragma warning disable IDE0072 // Switch already has default case handling all other element types
            return corLib.ElementType switch
            {
                ElementType.Boolean => _outputModule.CorLibTypeFactory.Boolean,
                ElementType.Char => _outputModule.CorLibTypeFactory.Char,
                ElementType.I1 => _outputModule.CorLibTypeFactory.SByte,
                ElementType.U1 => _outputModule.CorLibTypeFactory.Byte,
                ElementType.I2 => _outputModule.CorLibTypeFactory.Int16,
                ElementType.U2 => _outputModule.CorLibTypeFactory.UInt16,
                ElementType.I4 => _outputModule.CorLibTypeFactory.Int32,
                ElementType.U4 => _outputModule.CorLibTypeFactory.UInt32,
                ElementType.I8 => _outputModule.CorLibTypeFactory.Int64,
                ElementType.U8 => _outputModule.CorLibTypeFactory.UInt64,
                ElementType.R4 => _outputModule.CorLibTypeFactory.Single,
                ElementType.R8 => _outputModule.CorLibTypeFactory.Double,
                ElementType.String => _outputModule.CorLibTypeFactory.String,
                ElementType.Object => _outputModule.CorLibTypeFactory.Object,
                ElementType.I => _outputModule.CorLibTypeFactory.IntPtr,
                ElementType.U => _outputModule.CorLibTypeFactory.UIntPtr,
                ElementType.Void => _outputModule.CorLibTypeFactory.Void,
                _ => _outputModule.CorLibTypeFactory.Object
            };
#pragma warning restore IDE0072
        }

        // Handle SZArray (single-dimensional zero-based arrays)
        if (inputSig is SzArrayTypeSignature szArray)
        {
            return new SzArrayTypeSignature(MapTypeSignatureToOutput(szArray.BaseType));
        }

        // Handle generic instance types
        if (inputSig is GenericInstanceTypeSignature genericInst)
        {
            ITypeDefOrRef importedType = ImportTypeReference(genericInst.GenericType);
            TypeSignature[] mappedArgs = [.. genericInst.TypeArguments
                .Select(MapTypeSignatureToOutput)];
            return new GenericInstanceTypeSignature(importedType, genericInst.IsValueType, mappedArgs);
        }

        // Handle generic method/type parameters
        if (inputSig is GenericParameterSignature genericParam)
        {
            return new GenericParameterSignature(_outputModule, genericParam.ParameterType, genericParam.Index);
        }

        // Handle ByRef
        if (inputSig is ByReferenceTypeSignature byRef)
        {
            return new ByReferenceTypeSignature(MapTypeSignatureToOutput(byRef.BaseType));
        }

        // Handle TypeDefOrRefSignature
        if (inputSig is TypeDefOrRefSignature typeDefOrRef)
        {
            ITypeDefOrRef importedRef = ImportTypeReference(typeDefOrRef.Type);
            return new TypeDefOrRefSignature(importedRef, typeDefOrRef.IsValueType);
        }

        // Fallback: import the type
        return _outputModule.CorLibTypeFactory.Object;
    }

    /// <summary>
    /// Imports a type reference from the input module to the output module.
    /// </summary>
    private ITypeDefOrRef ImportTypeReference(ITypeDefOrRef type)
    {
        if (type is TypeDefinition typeDef)
        {
            // Check if we've already processed this type into the output module
            string qualifiedName = AssemblyAnalyzer.GetQualifiedName(typeDef);
            if (_typeDefinitionMapping.TryGetValue(qualifiedName, out TypeDeclaration? declaration) && declaration.OutputType != null)
            {
                return declaration.OutputType;
            }

            // Otherwise create a type reference
            return GetOrCreateTypeReference(
                AssemblyAnalyzer.GetEffectiveNamespace(typeDef) ?? "",
                typeDef.Name!.Value,
                _inputModule.Assembly?.Name?.Value ?? "mscorlib");
        }

        if (type is TypeReference typeRef)
        {
            string ns = typeRef.Namespace?.Value ?? "";
            string name = typeRef.Name!.Value;
            string assembly = GetAssemblyNameFromScope(typeRef.Scope);
            return GetOrCreateTypeReference(ns, name, assembly);
        }

        if (type is TypeSpecification typeSpec)
        {
            // For type specs, we need to create a new TypeSpecification in the output
            TypeSignature mappedSig = MapTypeSignatureToOutput(typeSpec.Signature!);
            return new TypeSpecification(mappedSig);
        }

        return GetOrCreateTypeReference("System", "Object", "mscorlib");
    }

    private static string GetAssemblyNameFromScope(IResolutionScope? scope)
    {
        return scope switch
        {
            AssemblyReference asmRef => asmRef.Name?.Value ?? "mscorlib",
            ModuleDefinition mod => mod.Assembly?.Name?.Value ?? "mscorlib",
            _ => "mscorlib"
        };
    }

    private TypeReference GetOrCreateTypeReference(string @namespace, string name, string assemblyName)
    {
        string fullName = string.IsNullOrEmpty(@namespace) ? name : $"{@namespace}.{name}";

        if (_typeReferenceCache.TryGetValue(fullName, out TypeReference? cached))
        {
            return cached;
        }

        AssemblyReference assemblyRef = GetOrCreateAssemblyReference(assemblyName);
        TypeReference typeRef = new(_outputModule, assemblyRef, @namespace, name);
        _typeReferenceCache[fullName] = typeRef;
        return typeRef;
    }

    private AssemblyReference GetOrCreateAssemblyReference(string assemblyName)
    {
        if (_assemblyReferenceCache.TryGetValue(assemblyName, out AssemblyReference? cached))
        {
            return cached;
        }

        AssemblyAttributes flags = string.CompareOrdinal(assemblyName, "mscorlib") == 0
            ? 0
            : AssemblyAttributes.ContentWindowsRuntime;

        AssemblyReference asmRef = new(assemblyName, new Version(0xFF, 0xFF, 0xFF, 0xFF))
        {
            Attributes = flags,
        };

        if (string.CompareOrdinal(assemblyName, "mscorlib") == 0)
        {
            asmRef.PublicKeyOrToken = [0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89];
        }

        _outputModule.AssemblyReferences.Add(asmRef);
        _assemblyReferenceCache[assemblyName] = asmRef;
        return asmRef;
    }
}