// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.WinMDGenerator.Errors;
using WindowsRuntime.WinMDGenerator.Helpers;
using WindowsRuntime.WinMDGenerator.Models;
using AssemblyAttributes = AsmResolver.PE.DotNet.Metadata.Tables.AssemblyAttributes;

namespace WindowsRuntime.WinMDGenerator.Generation;

/// <summary>
/// Writes a WinMD file from analyzed assembly types using AsmResolver.
/// </summary>
internal sealed partial class WinMDWriter
{
    private readonly string _version;
    private readonly TypeMapper _mapper;
    private readonly ModuleDefinition _inputModule;
    private readonly RuntimeContext? _runtimeContext;

    // Output WinMD module and assembly
    private readonly ModuleDefinition _outputModule;

    // Tracking for type definitions in the output WinMD
    private readonly Dictionary<string, TypeDeclaration> _typeDefinitionMapping = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AssemblyReference> _assemblyReferenceCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TypeReference> _typeReferenceCache = new(StringComparer.Ordinal);

    /// <summary>
    /// Creates a new <see cref="WinMDWriter"/> instance.
    /// </summary>
    public WinMDWriter(
        string assemblyName,
        string version,
        TypeMapper mapper,
        ModuleDefinition inputModule)
    {
        _version = version;
        _mapper = mapper;
        _inputModule = inputModule;
        _runtimeContext = inputModule.RuntimeContext;

        // Create the output WinMD module
        _outputModule = new ModuleDefinition(assemblyName + ".winmd")
        {
            RuntimeVersion = "WindowsRuntime 1.4"
        };

        // Replace the default mscorlib reference with the WinMD-style one (v255.255.255.255 with PKT)
        AssemblyReference defaultCorLib = (AssemblyReference)_outputModule.CorLibTypeFactory.CorLibScope;
        defaultCorLib.Version = new Version(0xFF, 0xFF, 0xFF, 0xFF);
        defaultCorLib.PublicKeyOrToken = [0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89];
        _assemblyReferenceCache["mscorlib"] = defaultCorLib;

        // Create the output assembly with WindowsRuntime flag (keep reference alive via module)
        _ = new AssemblyDefinition(assemblyName, new Version(version))
        {
            Modules = { _outputModule },
            Attributes = AssemblyAttributes.ContentWindowsRuntime,
            HashAlgorithm = AsmResolver.PE.DotNet.Metadata.Tables.AssemblyHashAlgorithm.Sha1
        };
    }

    /// <summary>
    /// Safely resolves a type reference, returning null instead of throwing if the type
    /// cannot be found (e.g., output-only synthesized types not in the input assembly).
    /// </summary>
    private TypeDefinition? SafeResolve(ITypeDefOrRef? typeRef)
    {
        return typeRef is TypeDefinition td
            ? td
            : typeRef is not null
                && _runtimeContext is not null
                && typeRef.Resolve(_runtimeContext, out TypeDefinition? resolved) == ResolutionStatus.Success
                ? resolved
                : null;
    }

    /// <summary>
    /// Processes a public type from the input assembly and adds it to the WinMD.
    /// </summary>
    public void ProcessType(TypeDefinition inputType)
    {
        string qualifiedName = inputType.QualifiedName;

        if (_typeDefinitionMapping.ContainsKey(qualifiedName))
        {
            return;
        }

        if (inputType.IsEnum)
        {
            AddEnumType(inputType);
        }
        else if (inputType.IsDelegate)
        {
            AddDelegateType(inputType);
        }
        else if (inputType.IsInterface)
        {
            AddInterfaceType(inputType);
        }
        else if (inputType.IsValueType)
        {
            AddStructType(inputType);
        }
        else if (inputType.IsClass)
        {
            AddClassType(inputType);
        }
    }

    /// <summary>
    /// Writes the WinMD to the specified path.
    /// </summary>
    /// <param name="outputPath">The file path to write the WinMD to.</param>
    public void Write(string outputPath)
    {
        try
        {
            _outputModule.Write(outputPath);
        }
        catch (Exception e)
        {
            throw WellKnownWinMDExceptions.WinMDWriteError(e);
        }
    }
}