// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.WinMDGenerator.Models;
using AssemblyAttributes = AsmResolver.PE.DotNet.Metadata.Tables.AssemblyAttributes;

namespace WindowsRuntime.WinMDGenerator.Writers;

/// <inheritdoc cref="WinMDWriter"/>
internal sealed partial class WinMDWriter
{
    /// <summary>
    /// Maps a type signature from the input module to the output module, applying Windows Runtime type mappings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the core type mapping method used throughout the writer. It handles:
    /// </para>
    /// <list type="bullet">
    ///   <item>CorLib primitive types (remapped to the output module's CorLib factory).</item>
    ///   <item><c>SzArray</c> types (single-dimensional zero-based arrays).</item>
    ///   <item>Generic instance types, including <see cref="Span{T}"/>/<see cref="ReadOnlySpan{T}"/> → <c>T[]</c> mapping.</item>
    ///   <item>Generic parameter signatures (<c>!0</c>, <c>!1</c>, etc.).</item>
    ///   <item>By-reference types.</item>
    ///   <item><see cref="TypeDefOrRefSignature"/> with Windows Runtime mapping (e.g., <see cref="IDisposable"/> → <c>IClosable</c>).</item>
    /// </list>
    /// </remarks>
    /// <param name="inputSignature">The input type signature to map.</param>
    /// <returns>The mapped <see cref="TypeSignature"/> for use in the output WinMD.</returns>
    private TypeSignature MapTypeSignatureToOutput(TypeSignature inputSignature)
    {
        // Handle 'CorLib' types
        if (inputSignature is CorLibTypeSignature corLib)
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
        if (inputSignature is SzArrayTypeSignature szArray)
        {
            return new SzArrayTypeSignature(MapTypeSignatureToOutput(szArray.BaseType));
        }

        // Handle generic instance types
        if (inputSignature is GenericInstanceTypeSignature genericInst)
        {
            string genericTypeName = genericInst.GenericType.QualifiedName;

            // Map 'Span<T>' and 'ReadOnlySpan<T>' to T[] (SzArray) for Windows Runtime
            // 'ReadOnlySpan<T>' → PassArray (in), 'Span<T>' → FillArray (out without BYREF)
            if (genericTypeName is "System.Span`1" or "System.ReadOnlySpan`1"
                && genericInst.TypeArguments.Count == 1)
            {
                return new SzArrayTypeSignature(MapTypeSignatureToOutput(genericInst.TypeArguments[0]));
            }

            // Check if the generic type itself has a Windows Runtime mapping (e.g., 'IList`1' -> 'IVector`1')
            if (_mapper.HasMappingForType(genericTypeName))
            {
                MappedType mapping = _mapper.GetMappedType(genericTypeName);
                (string @namespace, string name, string assembly, _, bool isValueType) = mapping.GetMapping();
                ITypeDefOrRef mappedType = GetOrCreateTypeReference(@namespace, name, assembly);
                TypeSignature[] mappedArgs = [.. genericInst.TypeArguments.Select(MapTypeSignatureToOutput)];
                return new GenericInstanceTypeSignature(mappedType, isValueType, mappedArgs);
            }

            ITypeDefOrRef importedType = ImportTypeReference(genericInst.GenericType);
            TypeSignature[] importedArgs = [.. genericInst.TypeArguments.Select(MapTypeSignatureToOutput)];
            return new GenericInstanceTypeSignature(importedType, genericInst.IsValueType, importedArgs);
        }

        // Handle generic method/type parameters
        if (inputSignature is GenericParameterSignature genericParam)
        {
            return new GenericParameterSignature(_outputModule, genericParam.ParameterType, genericParam.Index);
        }

        // Handle ByRef
        if (inputSignature is ByReferenceTypeSignature byRef)
        {
            return new ByReferenceTypeSignature(MapTypeSignatureToOutput(byRef.BaseType));
        }

        // Handle TypeDefOrRefSignature
        if (inputSignature is TypeDefOrRefSignature typeDefOrRef)
        {
            string typeName = typeDefOrRef.Type.QualifiedName;

            // Check if the type has a Windows Runtime mapping (e.g., 'IDisposable' -> 'IClosable', 'Type' -> 'TypeName')
            if (_mapper.HasMappingForType(typeName))
            {
                MappedType mapping = _mapper.GetMappedType(typeName);
                (string @namespace, string name, string assembly, _, bool isValueType) = mapping.GetMapping();
                ITypeDefOrRef mappedType = GetOrCreateTypeReference(@namespace, name, assembly);
                return new TypeDefOrRefSignature(mappedType, isValueType);
            }

            ITypeDefOrRef importedRef = ImportTypeReference(typeDefOrRef.Type);
            return new TypeDefOrRefSignature(importedRef, typeDefOrRef.IsValueType);
        }

        // Fallback: import the type
        return _outputModule.CorLibTypeFactory.Object;
    }

    /// <summary>
    /// Imports a type reference from the input module to the output module.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method handles three kinds of type references:
    /// </para>
    /// <list type="bullet">
    ///   <item><see cref="TypeDefinition"/>: checks if already processed in the output module, processes on demand
    ///     if public, or creates an external type reference.</item>
    ///   <item><see cref="TypeReference"/>: looks up the output mapping, resolves Windows Runtime contract assembly names
    ///     via <c>[WindowsRuntimeMetadata]</c> attribute, and creates a type reference.</item>
    ///   <item><see cref="TypeSpecification"/>: creates a new specification with a mapped signature.</item>
    /// </list>
    /// </remarks>
    /// <param name="type">The type reference to import.</param>
    /// <returns>The imported <see cref="ITypeDefOrRef"/> for use in the output WinMD.</returns>
    private ITypeDefOrRef ImportTypeReference(ITypeDefOrRef type)
    {
        if (type is TypeDefinition typeDef)
        {
            // Check if we've already processed this type into the output module
            string qualifiedName = typeDef.QualifiedName;
            if (_typeDefinitionMapping.TryGetValue(qualifiedName, out TypeDeclaration? declaration) && declaration.OutputType != null)
            {
                return declaration.OutputType;
            }

            // If this is a public type from the input module, process it on demand
            if (typeDef.IsPublic || typeDef.IsNestedPublic)
            {
                ProcessType(typeDef);
                if (_typeDefinitionMapping.TryGetValue(qualifiedName, out declaration) && declaration.OutputType != null)
                {
                    return declaration.OutputType;
                }
            }

            // External type or non-Windows Runtime type — create a type reference
            return GetOrCreateTypeReference(
                typeDef.EffectiveNamespace ?? "",
                typeDef.Name!.Value,
                _inputModule.Assembly?.Name?.Value ?? "mscorlib");
        }

        if (type is TypeReference typeRef)
        {
            string @namespace = typeRef.Namespace?.Value ?? "";
            string name = typeRef.Name!.Value;
            string fullName = string.IsNullOrEmpty(@namespace) ? name : $"{@namespace}.{name}";

            // Check if this type is in the output module (same-assembly reference)
            if (_typeDefinitionMapping.TryGetValue(fullName, out TypeDeclaration? declaration) && declaration.OutputType != null)
            {
                return declaration.OutputType;
            }

            // For Windows Runtime types from projection assemblies, use the Windows Runtime contract assembly name
            // from the '[WindowsRuntimeMetadata]' attribute instead of the projection assembly name.
            // E.g., 'StackPanel' from 'Microsoft.WinUI' → 'Microsoft.UI.Xaml' in the WinMD.
            string assembly = GetAssemblyNameFromScope(typeRef.Scope);
            TypeDefinition? resolvedType = SafeResolve(typeRef);
            if (resolvedType != null)
            {
                string? winrtAssembly = resolvedType.WindowsRuntimeAssemblyName;
                if (winrtAssembly != null)
                {
                    assembly = winrtAssembly;
                }
            }

            return GetOrCreateTypeReference(@namespace, name, assembly);
        }

        if (type is TypeSpecification typeSpec)
        {
            // For type specs, we need to create a new TypeSpecification in the output
            TypeSignature mappedSignature = MapTypeSignatureToOutput(typeSpec.Signature!);
            return new TypeSpecification(mappedSignature);
        }

        return GetOrCreateTypeReference("System", "Object", "mscorlib");
    }

    /// <summary>
    /// Gets the assembly name from a resolution scope.
    /// </summary>
    /// <param name="scope">The resolution scope to extract the assembly name from.</param>
    /// <returns>The assembly name, defaulting to <c>"mscorlib"</c> if not determinable.</returns>
    private static string GetAssemblyNameFromScope(IResolutionScope? scope)
    {
        return scope switch
        {
            AssemblyReference assemblyReference => assemblyReference.Name?.Value ?? "mscorlib",
            ModuleDefinition mod => mod.Assembly?.Name?.Value ?? "mscorlib",
            _ => "mscorlib"
        };
    }

    /// <summary>
    /// Ensures an <see cref="ITypeDefOrRef"/> is a <see cref="TypeReference"/>, not a <see cref="TypeDefinition"/>.
    /// </summary>
    /// <remarks>
    /// WinMD convention requires that interface implementations use <c>TypeRef</c> even for same-module types
    /// (TypeDef redirection per the WinMD spec). This method converts <see cref="TypeDefinition"/> instances
    /// to <see cref="TypeReference"/> instances pointing to the output assembly.
    /// </remarks>
    /// <param name="type">The type to ensure is a reference.</param>
    /// <returns>A <see cref="TypeReference"/> if the input was a <see cref="TypeDefinition"/>; otherwise, the original type.</returns>
    private ITypeDefOrRef EnsureTypeReference(ITypeDefOrRef type)
    {
        if (type is TypeDefinition typeDef)
        {
            string @namespace = typeDef.EffectiveNamespace ?? "";
            string name = typeDef.Name!.Value;
            string assembly = _outputModule.Assembly?.Name?.Value ?? "mscorlib";
            return GetOrCreateTypeReference(@namespace, name, assembly);
        }

        return type;
    }

    /// <summary>
    /// Gets or creates a <see cref="TypeReference"/> in the output module for the specified type.
    /// </summary>
    /// <remarks>
    /// Results are cached by fully-qualified type name to avoid creating duplicate references.
    /// The type reference is scoped to the assembly reference obtained from <see cref="GetOrCreateAssemblyReference"/>.
    /// </remarks>
    /// <param name="namespace">The type's namespace.</param>
    /// <param name="name">The type's simple name.</param>
    /// <param name="assemblyName">The assembly name to reference.</param>
    /// <returns>The cached or newly created <see cref="TypeReference"/>.</returns>
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

    /// <summary>
    /// Gets or creates an <see cref="AssemblyReference"/> in the output module for the specified assembly.
    /// </summary>
    /// <remarks>
    /// Non-mscorlib assemblies receive the <c>ContentWindowsRuntime</c> flag.
    /// The mscorlib reference includes the standard public key token.
    /// Results are cached by assembly name.
    /// </remarks>
    /// <param name="assemblyName">The name of the assembly to reference.</param>
    /// <returns>The cached or newly created <see cref="AssemblyReference"/>.</returns>
    private AssemblyReference GetOrCreateAssemblyReference(string assemblyName)
    {
        if (_assemblyReferenceCache.TryGetValue(assemblyName, out AssemblyReference? cached))
        {
            return cached;
        }

        AssemblyAttributes flags = string.CompareOrdinal(assemblyName, "mscorlib") == 0
            ? 0
            : AssemblyAttributes.ContentWindowsRuntime;

        AssemblyReference assemblyReference = new(assemblyName, new Version(0xFF, 0xFF, 0xFF, 0xFF))
        {
            Attributes = flags,
        };

        if (string.CompareOrdinal(assemblyName, "mscorlib") == 0)
        {
            assemblyReference.PublicKeyOrToken = [0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89];
        }

        _outputModule.AssemblyReferences.Add(assemblyReference);
        _assemblyReferenceCache[assemblyName] = assemblyReference;
        return assemblyReference;
    }
}