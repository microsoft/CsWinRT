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

namespace WindowsRuntime.WinMDGenerator.Writers;

/// <summary>
/// Writes a WinMD file from analyzed assembly types using AsmResolver.
/// </summary>
/// <remarks>
/// <para>
/// This is the core writer class for the WinMD generator. It transforms .NET type definitions from a
/// compiled assembly into Windows Runtime metadata format (<c>.winmd</c>). The writer handles the
/// full spectrum of Windows Runtime type system requirements:
/// </para>
/// <list type="bullet">
///   <item>Mapping .NET types to their Windows Runtime equivalents (e.g., <see cref="IList{T}"/> → <c>IVector&lt;T&gt;</c>).</item>
///   <item>Synthesizing Windows Runtime-required interfaces (<c>IFooClass</c>, <c>IFooFactory</c>, <c>IFooStatic</c>).</item>
///   <item>Emitting Windows Runtime metadata attributes (<c>[Guid]</c>, <c>[Version]</c>, <c>[Activatable]</c>).</item>
///   <item>Converting .NET naming conventions to Windows Runtime conventions (e.g., <c>set_</c> → <c>put_</c>).</item>
/// </list>
/// <para>
/// The writer is split across multiple partial class files, each handling a specific aspect of
/// WinMD generation. See <see cref="ProcessType"/> as the main entry point for type processing.
/// </para>
/// </remarks>
internal sealed partial class WinMDWriter
{
    /// <summary>
    /// The assembly version string (e.g. <c>"1.0.0.0"</c>) used for the output WinMD assembly.
    /// </summary>
    private readonly string _version;

    /// <summary>
    /// The <see cref="TypeMapper"/> instance used to map .NET types to their Windows Runtime equivalents.
    /// </summary>
    private readonly TypeMapper _mapper;

    /// <summary>
    /// The input <see cref="ModuleDefinition"/> from the compiled assembly being analyzed.
    /// </summary>
    private readonly ModuleDefinition _inputModule;

    /// <summary>
    /// The runtime context used for resolving type references across assemblies.
    /// May be <see langword="null"/> if the input module has no runtime context.
    /// </summary>
    private readonly RuntimeContext? _runtimeContext;

    /// <summary>
    /// The output <see cref="ModuleDefinition"/> representing the WinMD file being constructed.
    /// </summary>
    private readonly ModuleDefinition _outputModule;

    /// <summary>
    /// Maps fully-qualified type names to their <see cref="TypeDeclaration"/> entries,
    /// tracking both input and output type definitions.
    /// </summary>
    private readonly Dictionary<string, TypeDeclaration> _typeDefinitionMapping = new(StringComparer.Ordinal);

    /// <summary>
    /// Cache of assembly references already created in the output module, keyed by assembly name.
    /// </summary>
    private readonly Dictionary<string, AssemblyReference> _assemblyReferenceCache = new(StringComparer.Ordinal);

    /// <summary>
    /// Cache of type references already created in the output module, keyed by fully-qualified type name.
    /// </summary>
    private readonly Dictionary<string, TypeReference> _typeReferenceCache = new(StringComparer.Ordinal);

    /// <summary>
    /// Creates a new <see cref="WinMDWriter"/> instance.
    /// </summary>
    /// <param name="assemblyName">The name for the output WinMD assembly.</param>
    /// <param name="version">The version string for the output assembly (e.g. <c>"1.0.0.0"</c>).</param>
    /// <param name="mapper">The <see cref="TypeMapper"/> for .NET-to-Windows Runtime type mapping.</param>
    /// <param name="inputModule">The input <see cref="ModuleDefinition"/> to read types from.</param>
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

        // Replace the default 'mscorlib' reference with the WinMD-style one ('v255.255.255.255' with PKT)
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
    /// Safely resolves a type reference, returning <see langword="null"/> instead of throwing if the type
    /// cannot be found (e.g., output-only synthesized types not in the input assembly).
    /// </summary>
    /// <param name="typeRef">The type reference to resolve, or <see langword="null"/>.</param>
    /// <returns>The resolved <see cref="TypeDefinition"/>, or <see langword="null"/> if the type cannot be resolved.</returns>
    private TypeDefinition? SafeResolve(ITypeDefOrRef? typeRef)
    {
        // If the reference is already a resolved definition, return it directly
        if (typeRef is TypeDefinition typeDefinition)
        {
            return typeDefinition;
        }

        // We need both a non-null reference and a runtime context to attempt resolution
        if (typeRef is null || _runtimeContext is null)
        {
            return null;
        }

        // Try to resolve the type reference through the runtime context's assembly resolver.
        // This can fail for types in contract assemblies that aren't loaded, which is expected.
        if (typeRef.Resolve(_runtimeContext, out TypeDefinition? resolved) == ResolutionStatus.Success)
        {
            return resolved;
        }

        return null;
    }

    /// <summary>
    /// Processes a public type from the input assembly and adds it to the output WinMD.
    /// </summary>
    /// <remarks>
    /// Dispatches to the appropriate type-specific handler based on the kind of type
    /// (enum, delegate, interface, struct, or class). Each handler is responsible for
    /// creating the corresponding WinMD type definition with correct attributes and members.
    /// Types that have already been processed are skipped.
    /// </remarks>
    /// <param name="inputType">The <see cref="TypeDefinition"/> from the input assembly to process.</param>
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