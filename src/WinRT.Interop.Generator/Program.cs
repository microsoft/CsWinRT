// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using ConsoleAppFramework;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.Resolvers;

ConsoleApp.Run(args, InteropGenerator.Run);

internal sealed class InteropGeneratorState
{
    private readonly Dictionary<string, string> _typeHierarchyEntries = new(StringComparer.Ordinal);

    public void TrackTypeHierarchyEntry(string runtimeClassName, string baseRuntimeClassName)
    {
        _typeHierarchyEntries.Add(runtimeClassName, baseRuntimeClassName);
    }
}

internal static class InteropGenerator
{
    /// <summary>
    /// Runs the interop generator to produce the resulting <c>WinRT.Interop.dll</c> assembly.
    /// </summary>
    /// <param name="referencePath">The input .dll paths.</param>
    /// <param name="assemblyPath">The path of the assembly that was built.</param>
    /// <param name="outputDirectory">The output path for the resulting assembly.</param>
    public static void Run(
        string[] referencePath,
        string assemblyPath,
        string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(referencePath);
        ArgumentOutOfRangeException.ThrowIfZero(referencePath.Length, nameof(referencePath));
        ArgumentException.ThrowIfNullOrEmpty(assemblyPath);
        ArgumentException.ThrowIfNullOrEmpty(outputDirectory);

        PathAssemblyResolver pathAssemblyResolver = new(referencePath);
        DefaultMetadataResolver metadataResolver = new(pathAssemblyResolver);

        ModuleDefinition assemblyModule = ModuleDefinition.FromFile(assemblyPath, pathAssemblyResolver.ReaderParameters);

        string winRTRuntimeAssemblyPath = referencePath.First(path => Path.GetFileNameWithoutExtension(path.AsSpan()).SequenceEqual("WinRT.Runtime"));
        ModuleDefinition winRTRuntimeModule = ModuleDefinition.FromFile(winRTRuntimeAssemblyPath, pathAssemblyResolver.ReaderParameters);

        string windowsSdkAssemblyName = referencePath.First(path => Path.GetFileNameWithoutExtension(path.AsSpan()).SequenceEqual("Microsoft.Windows.SDK.NET"));
        ModuleDefinition windowsSdkModule = ModuleDefinition.FromFile(windowsSdkAssemblyName, pathAssemblyResolver.ReaderParameters);

        CorLibTypeSignature objectType = assemblyModule.CorLibTypeFactory.Object;
        TypeReference windowsRuntimeTypeAttributeType = winRTRuntimeModule.CreateTypeReference("WinRT", "WindowsRuntimeTypeAttribute");

        InteropGeneratorState state = new();

        List<GenericInstanceTypeSignature> genericTypes = [];

        foreach (string path in referencePath)
        {
            try
            {
                ModuleDefinition module = ModuleDefinition.FromFile(path, pathAssemblyResolver.ReaderParameters);

                if (!module.AssemblyReferences.Any(static reference => reference.Name?.AsSpan().SequenceEqual("Microsoft.Windows.SDK.NET.dll"u8) is true) &&
                    module.Name?.AsSpan().SequenceEqual("Microsoft.Windows.SDK.NET.dll"u8) is not true)
                {
                    Console.WriteLine($"SKIPPED {Path.GetFileNameWithoutExtension(path)}");

                    continue;
                }

                Console.WriteLine($"Loaded {Path.GetFileNameWithoutExtension(path)}");

                foreach (TypeDefinition type in module.GetAllTypes())
                {
                    if (type.IsClass && !type.IsValueType && !type.IsDelegate &&
                        !(type.IsAbstract && type.IsSealed) &&
                        !SignatureComparer.Default.Equals(type.BaseType, objectType) &&
                        type.HasCustomAttribute("WinRT", "WindowsRuntimeTypeAttribute"))
                    {
                        state.TrackTypeHierarchyEntry(type.FullName, type.BaseType.FullName);
                    }
                }

                foreach (TypeSpecification typeSpecification in module.EnumerateTableMembers<TypeSpecification>(TableIndex.TypeSpec))
                {
                    if (typeSpecification.Resolve() is { IsDelegate: true } &&
                        typeSpecification.Signature is GenericInstanceTypeSignature { GenericType.Name.Value: "TypedEventHandler`2" } typeSignature)
                    {
                        genericTypes.Add(typeSignature);
                    }
                }
            }
            catch
            {
                Console.WriteLine($"FAILED {Path.GetFileNameWithoutExtension(path)}");
            }
        }

        string winRTInteropAssemblyPath = Path.Combine(outputDirectory, "WinRT.Interop.dll");
        AssemblyDefinition winRTInteropAssembly = new("WinRT.Interop", assemblyModule.Assembly?.Version ?? new Version(0, 0, 0, 0));
        ModuleDefinition winRTInteropModule = new("WinRT.Interop");

        winRTInteropModule.AssemblyReferences.Add(new AssemblyReference(assemblyModule.Assembly?.Name, assemblyModule.Assembly?.Version ?? new Version(0, 0, 0, 0)));

        // Create the RVA field types
        InteropTypeDefinitionFactory.RvaFieldsTypes(
            winRTInteropModule.DefaultImporter,
            out TypeDefinition rvaFieldsType,
            out TypeDefinition iidRvaDataType);

        winRTInteropModule.TopLevelTypes.Add(rvaFieldsType);

        foreach (GenericInstanceTypeSignature typeSignature in genericTypes)
        {
            try
            {
                var vftbl = InteropTypeDefinitionFactory.DelegateVftblType(typeSignature, winRTInteropModule.CorLibTypeFactory, winRTInteropModule.DefaultImporter);

                winRTInteropModule.TopLevelTypes.Add(vftbl);
                winRTInteropModule.TopLevelTypes.Add(InteropTypeDefinitionFactory.DelegateReferenceVftblType(typeSignature, winRTInteropModule.CorLibTypeFactory, winRTInteropModule.DefaultImporter));

                var entries = InteropTypeDefinitionFactory.DelegateInterfaceEntriesType(typeSignature, winRTInteropModule.DefaultImporter);
                var impl = InteropTypeDefinitionFactory.DelegateImplType(typeSignature, vftbl, iidRvaDataType, metadataResolver, winRTInteropModule, out FieldDefinition iidRvaField);

                rvaFieldsType.Fields.Add(iidRvaField);

                winRTInteropModule.TopLevelTypes.Add(entries);
                winRTInteropModule.TopLevelTypes.Add(InteropTypeDefinitionFactory.DelegateInterfaceEntriesImplType(typeSignature, entries, impl, metadataResolver, winRTInteropModule));
                winRTInteropModule.TopLevelTypes.Add(impl);
            }
            catch
            {

            }
        }

        winRTInteropModule.Write(winRTInteropAssemblyPath);
    }
}
