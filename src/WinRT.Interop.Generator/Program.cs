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
using WindowsRuntime.InteropGenerator.Builders;
using WindowsRuntime.InteropGenerator.References;
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

        ModuleDefinition assemblyModule = ModuleDefinition.FromFile(assemblyPath, pathAssemblyResolver.ReaderParameters);

        string winRTRuntimeAssemblyPath = referencePath.First(path => Path.GetFileNameWithoutExtension(path.AsSpan()).SequenceEqual("WinRT.Runtime"));
        ModuleDefinition winRTRuntimeModule = ModuleDefinition.FromFile(winRTRuntimeAssemblyPath, pathAssemblyResolver.ReaderParameters);

        string winRTRuntime2AssemblyPath = referencePath.First(path => Path.GetFileNameWithoutExtension(path.AsSpan()).SequenceEqual("WinRT.Runtime2"));
        ModuleDefinition winRTRuntime2Module = ModuleDefinition.FromFile(winRTRuntime2AssemblyPath, pathAssemblyResolver.ReaderParameters);

        string windowsSdkAssemblyName = referencePath.First(path => Path.GetFileNameWithoutExtension(path.AsSpan()).SequenceEqual("Microsoft.Windows.SDK.NET"));
        ModuleDefinition windowsSdkModule = ModuleDefinition.FromFile(windowsSdkAssemblyName, pathAssemblyResolver.ReaderParameters);

        CorLibTypeSignature objectType = assemblyModule.CorLibTypeFactory.Object;
        TypeReference windowsRuntimeTypeAttributeType = winRTRuntimeModule.CreateTypeReference("WinRT", "WindowsRuntimeTypeAttribute");

        InteropGeneratorState state = new();

        List<GenericInstanceTypeSignature> genericTypes = [];
        List<GenericInstanceTypeSignature> keyValuePairTypes = [];

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

                    if (typeSpecification.Resolve() is { IsValueType: true } &&
                        typeSpecification.Signature is GenericInstanceTypeSignature { GenericType.Name.Value: "KeyValuePair`2" } keyValuePairType)
                    {
                        keyValuePairTypes.Add(keyValuePairType);
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
        ModuleDefinition winRTInteropModule = new("WinRT.Interop", assemblyModule.OriginalTargetRuntime.GetDefaultCorLib());

        winRTInteropModule.AssemblyReferences.Add(new AssemblyReference(assemblyModule.Assembly?.Name, assemblyModule.Assembly?.Version ?? new Version(0, 0, 0, 0)));
        winRTInteropModule.AssemblyReferences.Add(new AssemblyReference(winRTRuntime2Module.Assembly?.Name, winRTRuntime2Module.Assembly?.Version ?? new Version(0, 0, 0, 0)));
        winRTInteropModule.MetadataResolver = new DefaultMetadataResolver(pathAssemblyResolver);

        // Setup the well known items to use when emitting code
        WellKnownInteropDefinitions wellKnownInteropDefinitions = new(winRTInteropModule);
        WellKnownInteropReferences wellKnownInteropReferences = new(winRTInteropModule, winRTRuntime2Module);

        foreach (GenericInstanceTypeSignature typeSignature in genericTypes)
        {
            try
            {
                // Define the 'DelegateImpl' type (with the delegate interface vtable implementation)
                InteropTypeDefinitionBuilder.Delegate.ImplType(
                    delegateType: typeSignature,
                    wellKnownInteropDefinitions: wellKnownInteropDefinitions,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: winRTInteropModule,
                    implType: out TypeDefinition delegateImplType,
                    iidRvaField: out _);

                // Define the 'DelegateReferenceImpl' type (with the boxed delegate interface vtable implementation)
                InteropTypeDefinitionBuilder.Delegate.ReferenceImplType(
                    delegateType: typeSignature,
                    wellKnownInteropDefinitions: wellKnownInteropDefinitions,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: winRTInteropModule,
                    implType: out TypeDefinition delegateReferenceImplType,
                    iidRvaField: out _);

                // Define the 'DelegateInterfaceEntriesImpl' type (with the 'ComWrappers' interface entries implementation)
                InteropTypeDefinitionBuilder.Delegate.InterfaceEntriesImplType(
                    delegateType: typeSignature,
                    delegateImplType: delegateImplType,
                    delegateReferenceImplType: delegateReferenceImplType,
                    wellKnownInteropDefinitions: wellKnownInteropDefinitions,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: winRTInteropModule,
                    implType: out TypeDefinition delegateInterfaceEntriesImplType);

                // Define the 'NativeDelegate' type (with the extension method implementation)
                InteropTypeDefinitionBuilder.Delegate.NativeDelegateType(
                    delegateType: typeSignature,
                    wellKnownInteropDefinitions: wellKnownInteropDefinitions,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: winRTInteropModule,
                    nativeDelegateType: out TypeDefinition nativeDelegateType);

                // Define the 'ComWrappersCallback' type (with the 'IComWrappersCallback' implementation)
                InteropTypeDefinitionBuilder.Delegate.ComWrappersCallbackType(
                    delegateType: typeSignature,
                    delegateImplType: delegateImplType,
                    nativeDelegateType: nativeDelegateType,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: winRTInteropModule,
                    out TypeDefinition delegateComWrappersCallbackType);

                // Define the 'ComWrappersMarshallerAttribute' type
                InteropTypeDefinitionBuilder.Delegate.ComWrappersMarshallerAttribute(
                    delegateType: typeSignature,
                    delegateReferenceImplType: delegateReferenceImplType,
                    delegateInterfaceEntriesImplType: delegateInterfaceEntriesImplType,
                    delegateComWrappersCallbackType: delegateComWrappersCallbackType,
                    wellKnownInteropDefinitions: wellKnownInteropDefinitions,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: winRTInteropModule,
                    out TypeDefinition delegateComWrappersMarshallerType);

                // Define the 'Marshaller' type (with the static marshaller methods)
                InteropTypeDefinitionBuilder.Delegate.Marshaller(
                    delegateType: typeSignature,
                    delegateImplType: delegateImplType,
                    delegateReferenceImplType: delegateReferenceImplType,
                    delegateComWrappersCallbackType: delegateComWrappersCallbackType,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: winRTInteropModule,
                    marshallerType: out _);

                // Define the proxy type (for the type map)
                InteropTypeDefinitionBuilder.Delegate.Proxy(
                    delegateType: typeSignature,
                    delegateComWrappersMarshallerAttributeType: delegateComWrappersMarshallerType,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: winRTInteropModule,
                    out _);
            }
            catch
            {
            }
        }

        foreach (GenericInstanceTypeSignature typeSignature in keyValuePairTypes)
        {
            try
            {
                // Define the 'DelegateImpl' type (with the delegate interface vtable implementation)
                InteropTypeDefinitionBuilder.KeyValuePair.ImplType(
                    keyValuePairType: typeSignature,
                    wellKnownInteropDefinitions: wellKnownInteropDefinitions,
                    wellKnownInteropReferences: wellKnownInteropReferences,
                    module: winRTInteropModule,
                    implType: out TypeDefinition delegateImplType,
                    iidRvaField: out _);
            }
            catch
            {
            }
        }

        // Add all top level internal types to the interop module
        winRTInteropModule.TopLevelTypes.Add(wellKnownInteropDefinitions.RvaFields);
        winRTInteropModule.TopLevelTypes.Add(wellKnownInteropDefinitions.IUnknownVftbl);
        winRTInteropModule.TopLevelTypes.Add(wellKnownInteropDefinitions.IInspectableVftbl);
        winRTInteropModule.TopLevelTypes.Add(wellKnownInteropDefinitions.DelegateVftbl);
        winRTInteropModule.TopLevelTypes.Add(wellKnownInteropDefinitions.DelegateReferenceVftbl);
        winRTInteropModule.TopLevelTypes.Add(wellKnownInteropDefinitions.DelegateInterfaceEntries);
        winRTInteropModule.TopLevelTypes.Add(wellKnownInteropDefinitions.IKeyValuePairVftbl);
        winRTInteropModule.TopLevelTypes.Add(wellKnownInteropDefinitions.IKeyValuePairInterfaceEntries);

        // Emit the interop .dll to disk
        winRTInteropModule.Write(winRTInteropAssemblyPath);
    }
}
