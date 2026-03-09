// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using AsmResolver.PE.DotNet.Metadata.Tables;
using ConsoleAppFramework;
using WindowsRuntime.ProjectionGenerator.Errors;
using WindowsRuntime.ProjectionGenerator.Helpers;
using WindowsRuntime.ProjectionGenerator.Models;

namespace WindowsRuntime.ProjectionGenerator.Generation;

/// <summary>
/// The main projection generator that produces C# source files from Windows Runtime metadata.
/// </summary>
internal static partial class ProjectionGenerator
{
    /// <summary>
    /// The <c>tdWindowsRuntime</c> (0x4000) metadata flag, not exposed by AsmResolver's <see cref="TypeAttributes"/> enum.
    /// </summary>
    private const TypeAttributes TypeAttributesWindowsRuntime = (TypeAttributes)0x4000;
    /// <summary>
    /// Runs the projection generator with the specified input.
    /// </summary>
    /// <param name="inputPath">The input path (a .winmd file path, a response file path starting with '@', or a .zip debug repro file).</param>
    /// <param name="token">The cancellation token.</param>
    public static void Run([Argument] string inputPath, CancellationToken token)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        // Parse arguments based on input type
        ProjectionGeneratorArgs args = ParseInput(inputPath, token);

        // Optionally save debug repro
        if (args.DebugReproDirectory is not null)
        {
            SaveDebugRepro(args);
        }

        // Load all input modules
        List<ModuleDefinition> modules = LoadModules(args);

        // Create namespace filters
        NamespaceFilter filter = new(args.IncludeNamespaces, args.ExcludeNamespaces);
        NamespaceFilter additionFilter = new(args.IncludeNamespaces, args.AdditionExcludeNamespaces);

        // Collect all types grouped by namespace
        Dictionary<string, List<TypeDefinition>> namespaceTypes = CollectNamespaceTypes(modules, filter);

        if (args.IsVerbose)
        {
            foreach (string inputFile in args.InputFilePaths)
            {
                Console.WriteLine($"input: {inputFile}");
            }

            Console.WriteLine($"output: {args.OutputDirectory}");
        }

        // Ensure output directory exists
        _ = Directory.CreateDirectory(args.OutputDirectory);

        // Generate InterfaceIIDs file (unless reference projection)
        if (!args.IsReferenceProjection)
        {
            GenerateInterfaceIIDs(args, namespaceTypes);
        }

        // Process each namespace
        foreach ((string namespaceName, List<TypeDefinition> types) in namespaceTypes)
        {
            token.ThrowIfCancellationRequested();

            try
            {
                GenerateNamespaceFile(args, namespaceName, types, filter, additionFilter);
            }
            catch (Exception e) when (!e.IsWellKnown())
            {
                throw WellKnownProjectionExceptions.TypeProcessingFailed(namespaceName, "", e);
            }
        }

        // Generate supporting files
        GenerateSupportingFiles(args, namespaceTypes);

        stopwatch.Stop();

        if (args.IsVerbose)
        {
            Console.WriteLine($"time: {stopwatch.ElapsedMilliseconds}ms");
        }
    }

    /// <summary>
    /// Parses input and returns the generator arguments.
    /// </summary>
    private static ProjectionGeneratorArgs ParseInput(string inputPath, CancellationToken token)
    {
        ProjectionGeneratorArgs args;

        // If it's a .zip file, unpack debug repro
        if (inputPath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            string responseFilePath = UnpackDebugRepro(inputPath, token);
            args = ProjectionGeneratorArgs.ParseFromResponseFile(responseFilePath);
        }
        else if (inputPath is ['@', .. string responsePath])
        {
            // If it starts with '@', it's a response file
            args = ProjectionGeneratorArgs.ParseFromResponseFile(responsePath);
        }
        else
        {
            // Otherwise, treat as direct .winmd file path
            args = new ProjectionGeneratorArgs
            {
                InputFilePaths = [inputPath],
                OutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "output")
            };
        }

        args.Token = token;

        return args;
    }

    /// <summary>
    /// Loads all modules from the input file paths.
    /// </summary>
    /// <remarks>
    /// WinMD files use a special runtime version (255.255) that AsmResolver's default loader
    /// doesn't handle. We use a custom <see cref="RuntimeContext"/> with <c>.NETCoreApp 10.0</c>
    /// to provide the correct reader parameters for loading these files.
    /// </remarks>
    private static List<ModuleDefinition> LoadModules(ProjectionGeneratorArgs args)
    {
        List<ModuleDefinition> modules = [];

        // Create a RuntimeContext that can handle .winmd files (which have runtime version 255.255)
        RuntimeContext runtimeContext = new(new DotNetRuntimeInfo(".NETCoreApp", new Version(10, 0)));
        ModuleReaderParameters readerParameters = runtimeContext.DefaultReaderParameters;

        foreach (string path in args.InputFilePaths)
        {
            if (!File.Exists(path))
            {
                throw WellKnownProjectionExceptions.InputFileNotFound(path);
            }

            try
            {
                ModuleDefinition module = ModuleDefinition.FromFile(path, readerParameters);
                modules.Add(module);
            }
            catch (Exception e) when (!e.IsWellKnown())
            {
                throw WellKnownProjectionExceptions.ModuleLoadFailed(path, e);
            }
        }

        return modules;
    }

    /// <summary>
    /// Collects all types from loaded modules grouped by namespace, applying the filter.
    /// </summary>
    private static Dictionary<string, List<TypeDefinition>> CollectNamespaceTypes(
        List<ModuleDefinition> modules,
        NamespaceFilter filter)
    {
        Dictionary<string, List<TypeDefinition>> result = new(StringComparer.Ordinal);

        foreach (ModuleDefinition module in modules)
        {
            foreach (TypeDefinition type in module.TopLevelTypes)
            {
                string? ns = type.Namespace?.Value;

                if (ns is null or "<Module>")
                {
                    continue;
                }

                // Skip types that don't have the tdWindowsRuntime flag
                if (!type.Attributes.HasFlag(TypeAttributesWindowsRuntime) &&
                    !type.Attributes.HasFlag(TypeAttributes.Import))
                {
                    continue;
                }

                string fullName = $"{ns}.{type.Name}";

                if (!filter.Includes(ns) && !filter.Includes(fullName))
                {
                    continue;
                }

                if (!result.TryGetValue(ns, out List<TypeDefinition>? typeList))
                {
                    typeList = [];
                    result[ns] = typeList;
                }

                typeList.Add(type);
            }
        }

        // Sort types within each namespace by name for deterministic output
        foreach (List<TypeDefinition> types in result.Values)
        {
            types.Sort((a, b) => string.Compare(a.Name?.Value, b.Name?.Value, StringComparison.Ordinal));
        }

        return result;
    }

    /// <summary>
    /// Generates the InterfaceIIDs file containing GUID properties for all interfaces.
    /// </summary>
    private static void GenerateInterfaceIIDs(
        ProjectionGeneratorArgs args,
        Dictionary<string, List<TypeDefinition>> namespaceTypes)
    {
        CodeWriter writer = new();

        WriteFileHeader(writer);
        writer.WriteLine("using System;");
        writer.WriteLine("using System.Runtime.InteropServices;");
        writer.WriteLine();
        writer.WriteLine("namespace ABI");

        using (writer.WriteBlock())
        {
            writer.WriteLine("internal static partial class InterfaceIIDs");

            using (writer.WriteBlock())
            {
                HashSet<string> emittedInterfaces = new(StringComparer.Ordinal);

                foreach ((string ns, List<TypeDefinition> types) in namespaceTypes.OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
                {
                    foreach (TypeDefinition type in types)
                    {
                        if (TypeHelpers.IsGenericType(type))
                        {
                            continue;
                        }

                        string? typeName = type.Name?.Value;

                        if (typeName is null)
                        {
                            continue;
                        }

                        TypeMappings.MappedType? mapping = TypeMappings.GetMappedType(ns, typeName);

                        if (mapping is { EmitAbi: false })
                        {
                            continue;
                        }

                        TypeCategory category = TypeHelpers.GetCategory(type);

                        switch (category)
                        {
                            case TypeCategory.Interface:
                            case TypeCategory.Delegate:
                                WriteIIDGuidProperty(writer, type, emittedInterfaces);
                                break;
                            case TypeCategory.Enum:
                            case TypeCategory.Struct:
                                WriteIIDReferenceGuidProperty(writer, type, emittedInterfaces);
                                break;
                            case TypeCategory.Class:
                                WriteIIDGuidPropertiesForClassInterfaces(writer, type, emittedInterfaces);
                                break;
                            case TypeCategory.Attribute:
                            case TypeCategory.ApiContract:
                            default:
                                break;
                        }
                    }
                }
            }
        }

        string content = writer.ToString();
        string outputPath = Path.Combine(args.OutputDirectory, "GeneratedInterfaceIIDs.cs");

        WriteOutputFile(outputPath, content);
    }

    /// <summary>
    /// Generates a namespace file containing all projected and ABI types for a namespace.
    /// </summary>
    private static void GenerateNamespaceFile(
        ProjectionGeneratorArgs args,
        string namespaceName,
        List<TypeDefinition> types,
        NamespaceFilter filter,
        NamespaceFilter additionFilter)
    {
        // TODO: These filters will be used for namespace filtering logic
        _ = filter;
        _ = additionFilter;

        CodeWriter writer = new();

        WriteFileHeader(writer);
        WriteUsingStatements(writer);

        bool hasWrittenAnyType = false;

        // Phase 1: Assembly attributes (if not reference projection)
        if (!args.IsReferenceProjection)
        {
            foreach (TypeDefinition type in types)
            {
                if (TypeHelpers.IsGenericType(type))
                {
                    continue;
                }

                string? typeName = type.Name?.Value;
                string ns = type.Namespace?.Value ?? "";

                if (typeName is null)
                {
                    continue;
                }

                TypeMappings.MappedType? mapping = TypeMappings.GetMappedType(ns, typeName);

                if (mapping is { EmitAbi: false })
                {
                    continue;
                }

                TypeCategory category = TypeHelpers.GetCategory(type);
                ProjectedTypeWriter.WriteAssemblyAttributes(writer, type, category, args);
            }
        }

        // Phase 2: Projected types
        writer.WriteLine($"namespace {namespaceName}");

        using (writer.WriteBlock())
        {
            foreach (TypeDefinition type in types)
            {
                string? typeName = type.Name?.Value;
                string ns = type.Namespace?.Value ?? "";

                if (typeName is null)
                {
                    continue;
                }

                // Skip mapped types (they already exist in .NET) and generic type definitions
                TypeMappings.MappedType? mapping = TypeMappings.GetMappedType(ns, typeName);

                if (mapping is not null || TypeHelpers.IsGenericType(type))
                {
                    hasWrittenAnyType = true;
                    continue;
                }

                TypeCategory category = TypeHelpers.GetCategory(type);

                try
                {
                    switch (category)
                    {
                        case TypeCategory.Class:
                            ProjectedTypeWriter.WriteClass(writer, type, args);
                            hasWrittenAnyType = true;
                            break;
                        case TypeCategory.Delegate:
                            ProjectedTypeWriter.WriteDelegate(writer, type, args);
                            hasWrittenAnyType = true;
                            break;
                        case TypeCategory.Enum:
                            ProjectedTypeWriter.WriteEnum(writer, type, args);
                            hasWrittenAnyType = true;
                            break;
                        case TypeCategory.Interface:
                            ProjectedTypeWriter.WriteInterface(writer, type, args);
                            hasWrittenAnyType = true;
                            break;
                        case TypeCategory.Struct:
                            ProjectedTypeWriter.WriteStruct(writer, type, args);
                            hasWrittenAnyType = true;
                            break;
                        case TypeCategory.Attribute:
                            ProjectedTypeWriter.WriteAttribute(writer, type, args);
                            hasWrittenAnyType = true;
                            break;
                        case TypeCategory.ApiContract:
                            ProjectedTypeWriter.WriteContract(writer, type, args);
                            hasWrittenAnyType = true;
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception e) when (!e.IsWellKnown())
                {
                    throw WellKnownProjectionExceptions.TypeProcessingFailed(ns, typeName, e);
                }
            }
        }

        if (!hasWrittenAnyType)
        {
            return;
        }

        // Phase 3: ABI types (if not reference projection)
        if (!args.IsReferenceProjection)
        {
            writer.WriteLine();
            writer.WriteLine($"namespace ABI.{namespaceName}");

            using (writer.WriteBlock())
            {
                foreach (TypeDefinition type in types)
                {
                    if (TypeHelpers.IsGenericType(type))
                    {
                        continue;
                    }

                    string? typeName = type.Name?.Value;
                    string ns = type.Namespace?.Value ?? "";

                    if (typeName is null)
                    {
                        continue;
                    }

                    TypeMappings.MappedType? mapping = TypeMappings.GetMappedType(ns, typeName);

                    if (mapping is { EmitAbi: false })
                    {
                        continue;
                    }

                    TypeCategory category = TypeHelpers.GetCategory(type);

                    if (category is TypeCategory.ApiContract or TypeCategory.Attribute)
                    {
                        continue;
                    }

                    try
                    {
                        AbiTypeWriter.WriteAbiType(writer, type, category, args);
                    }
                    catch (Exception e) when (!e.IsWellKnown())
                    {
                        throw WellKnownProjectionExceptions.TypeProcessingFailed(ns, typeName, e);
                    }
                }
            }
        }

        string content = writer.ToString();
        string outputPath = Path.Combine(args.OutputDirectory, $"{namespaceName}.cs");

        WriteOutputFile(outputPath, content);
    }

    /// <summary>
    /// Generates supporting files (base type mappings, etc.).
    /// </summary>
    private static void GenerateSupportingFiles(
        ProjectionGeneratorArgs args,
        Dictionary<string, List<TypeDefinition>> namespaceTypes)
    {
        // Generate base type mapping helper if there are any class types with base classes
        Dictionary<string, string> baseTypeMappings = new(StringComparer.Ordinal);

        foreach ((string ns, List<TypeDefinition> types) in namespaceTypes)
        {
            foreach (TypeDefinition type in types)
            {
                TypeCategory category = TypeHelpers.GetCategory(type);

                if (category != TypeCategory.Class)
                {
                    continue;
                }

                ITypeDefOrRef? baseType = type.BaseType;

                if (baseType is null)
                {
                    continue;
                }

                string? baseNs = baseType.Namespace;
                string? baseName = baseType.Name;

                if (baseNs is null or "System" || baseName is null)
                {
                    continue;
                }

                string fullName = $"{type.Namespace}.{type.Name}";
                string fullBaseName = $"{baseNs}.{baseName}";

                baseTypeMappings[fullName] = fullBaseName;
            }
        }

        if (baseTypeMappings.Count > 0)
        {
            CodeWriter writer = new();

            WriteFileHeader(writer);
            writer.WriteMultiline("""
                using System;

                namespace WinRT
                {
                    internal static class ProjectionTypesInitializer
                    {
                        internal static readonly System.Collections.Generic.Dictionary<string, string> TypeNameToBaseTypeNameMapping = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.Ordinal)
                        {
                """);

            writer.IncreaseIndent();
            writer.IncreaseIndent();
            writer.IncreaseIndent();

            foreach ((string key, string value) in baseTypeMappings.OrderBy(kvp => kvp.Key, StringComparer.Ordinal))
            {
                writer.WriteLine($"[\"{key}\"] = \"{value}\",");
            }

            writer.DecreaseIndent();
            writer.DecreaseIndent();
            writer.DecreaseIndent();

            writer.WriteMultiline("""
                        };

                        [System.Runtime.CompilerServices.ModuleInitializer]
                        internal static void InitializeProjectionTypes()
                        {
                            ComWrappersSupport.RegisterProjectionTypeBaseTypeMapping(TypeNameToBaseTypeNameMapping);
                        }
                    }
                }
                """);

            string content = writer.ToString();
            string outputPath = Path.Combine(args.OutputDirectory, "WinRTBaseTypeMappingHelper.cs");

            WriteOutputFile(outputPath, content);
        }
    }

    /// <summary>
    /// Writes the auto-generated file header comment.
    /// </summary>
    private static void WriteFileHeader(CodeWriter writer)
    {
        writer.WriteLine("// <auto-generated>DO NOT EDIT</auto-generated>");
        writer.WriteLine("// This file was auto-generated by cswinrtprojgen.");
        writer.WriteLine();
    }

    /// <summary>
    /// Writes the standard using statements for generated files.
    /// </summary>
    private static void WriteUsingStatements(CodeWriter writer)
    {
        writer.WriteLine("using System;");
        writer.WriteLine("using System.Collections;");
        writer.WriteLine("using System.Collections.Generic;");
        writer.WriteLine("using System.ComponentModel;");
        writer.WriteLine("using System.Diagnostics;");
        writer.WriteLine("using System.Runtime.CompilerServices;");
        writer.WriteLine("using System.Runtime.InteropServices;");
        writer.WriteLine("using WinRT;");
        writer.WriteLine();
        writer.WriteLine("#pragma warning disable 0169"); // unused private field
        writer.WriteLine("#pragma warning disable 0414"); // assigned but never used
        writer.WriteLine("#pragma warning disable CS0649"); // never assigned
        writer.WriteLine();
    }

    /// <summary>
    /// Writes generated content to an output file.
    /// </summary>
    private static void WriteOutputFile(string path, string content)
    {
        try
        {
            File.WriteAllText(path, content);
        }
        catch (Exception e) when (!e.IsWellKnown())
        {
            throw WellKnownProjectionExceptions.FileWriteFailed(path, e);
        }
    }

    // GUID-related helper methods

    /// <summary>
    /// Writes a GUID property for an interface's IID.
    /// </summary>
    private static void WriteIIDGuidProperty(
        CodeWriter writer,
        TypeDefinition type,
        HashSet<string> emittedInterfaces)
    {
        CustomAttribute? guidAttribute = TypeHelpers.GetAttribute(type, "Windows.Foundation.Metadata", "GuidAttribute");

        if (guidAttribute is null)
        {
            return;
        }

        string propertyName = GetIIDPropertyName(type);

        if (!emittedInterfaces.Add(propertyName))
        {
            return;
        }

        Guid guid = ExtractGuid(guidAttribute);

        writer.WriteLine($"/// <summary>Gets the IID for the <c>{type.Namespace}.{type.Name}</c> type.</summary>");
        writer.Write($"internal static ref readonly Guid {propertyName} => ");
        WriteGuidPropertyBody(writer, guid);
        writer.WriteLine();
    }

    /// <summary>
    /// Writes a GUID property for an IReference implementation of a value type.
    /// </summary>
    private static void WriteIIDReferenceGuidProperty(
        CodeWriter writer,
        TypeDefinition type,
        HashSet<string> emittedInterfaces)
    {
        // Value types need IReference<T> IIDs computed from their signature.
        // This is a placeholder that will need the signature-based GUID computation.
        _ = writer;
        _ = type;
        _ = emittedInterfaces;
    }

    /// <summary>
    /// Writes GUID properties for a class's interfaces.
    /// </summary>
    private static void WriteIIDGuidPropertiesForClassInterfaces(
        CodeWriter writer,
        TypeDefinition type,
        HashSet<string> emittedInterfaces)
    {
        foreach (InterfaceImplementation ifaceImpl in type.Interfaces)
        {
            ITypeDefOrRef? iface = ifaceImpl.Interface;

            if (iface is null)
            {
                continue;
            }

            if (iface.Resolve() is TypeDefinition ifaceType)
            {
                WriteIIDGuidProperty(writer, ifaceType, emittedInterfaces);
            }
        }
    }

    /// <summary>
    /// Gets the IID property name for a type.
    /// </summary>
    private static string GetIIDPropertyName(TypeDefinition type)
    {
        string escapedName = TypeNameHelpers.EscapeTypeNameForIdentifier($"{type.Namespace}.{type.Name}");
        return $"IID_{escapedName}";
    }

    /// <summary>
    /// Extracts a <see cref="Guid"/> from a GuidAttribute custom attribute.
    /// </summary>
    private static Guid ExtractGuid(CustomAttribute guidAttribute)
    {
        if (guidAttribute.Signature?.FixedArguments is { Count: 11 } args)
        {
            uint a = (uint)args[0].Element!;
            ushort b = (ushort)args[1].Element!;
            ushort c = (ushort)args[2].Element!;
            byte d = (byte)args[3].Element!;
            byte e = (byte)args[4].Element!;
            byte f = (byte)args[5].Element!;
            byte g = (byte)args[6].Element!;
            byte h = (byte)args[7].Element!;
            byte i = (byte)args[8].Element!;
            byte j = (byte)args[9].Element!;
            byte k = (byte)args[10].Element!;

            return new Guid(a, b, c, d, e, f, g, h, i, j, k);
        }

        return Guid.Empty;
    }

    /// <summary>
    /// Writes the GUID property body with inline ReadOnlySpan and Unsafe.As.
    /// </summary>
    private static void WriteGuidPropertyBody(CodeWriter writer, Guid guid)
    {
        byte[] bytes = guid.ToByteArray();

        writer.Write("ref System.Runtime.CompilerServices.Unsafe.As<byte, Guid>(ref System.Runtime.InteropServices.MemoryMarshal.GetReference(");
        writer.Write($"(System.ReadOnlySpan<byte>)[{string.Join(", ", bytes.Select(b => $"0x{b:X2}"))}]");
        writer.Write("));");
    }
}
