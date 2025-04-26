// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Serialized;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using ConsoleAppFramework;
using WindowsRuntime.InteropGenerator.Factories;

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
    public sealed class PathAssemblyResolver : IAssemblyResolver
    {
        private readonly string[] _referencePath;
        private readonly ConcurrentDictionary<string, AssemblyDefinition> _assemblyCache;

        public PathAssemblyResolver(string[] referencePath)
        {
            _referencePath = referencePath;
            _assemblyCache = new ConcurrentDictionary<string, AssemblyDefinition>(concurrencyLevel: 1, capacity: referencePath.Length);
        }

        public void AddToCache(AssemblyDescriptor descriptor, AssemblyDefinition definition)
        {
            _ = _assemblyCache.TryAdd(descriptor.Name!, definition);
        }

        public void ClearCache()
        {
            _assemblyCache.Clear();
        }

        public bool HasCached(AssemblyDescriptor descriptor)
        {
            return _assemblyCache.ContainsKey(descriptor.Name!);
        }

        public bool RemoveFromCache(AssemblyDescriptor descriptor)
        {
            return _assemblyCache.TryRemove(descriptor.Name!, out _);
        }

        public AssemblyDefinition? Resolve(AssemblyDescriptor assembly)
        {
            string assemblyPath = _referencePath.First(path => Path.GetFileNameWithoutExtension(path.AsSpan()).SequenceEqual(assembly.Name));

            return AssemblyDefinition.FromFile(assemblyPath);
        }
    }

    public sealed class PathAssemblyResolver2 : IAssemblyResolver
    {
        private static readonly SignatureComparer s_higherVersionCompuer = new(SignatureComparisonFlags.AcceptNewerVersions);

        private readonly IEnumerable<string> _assemblyPaths;

        private readonly ConcurrentDictionary<AssemblyDescriptor, AssemblyDefinition> _cache = new(SignatureComparer.Default);

        public ModuleReaderParameters ReaderParameters { get; }

        public PathAssemblyResolver2(IEnumerable<string> paths)
        {
            _assemblyPaths = paths.ToArray();
            ReaderParameters = new RuntimeContext(new DotNetRuntimeInfo(), this).DefaultReaderParameters;
        }

        public AssemblyDefinition? Resolve(AssemblyDescriptor assembly)
        {
            if (_cache.TryGetValue(assembly, out var cachedDefinition))
            {
                return cachedDefinition;
            }

            if (assembly.Name is null) return null;
            var foundAsm = _assemblyPaths.FirstOrDefault(path => path.EndsWith(assembly.Name + ".dll") || path.EndsWith(assembly.Name + ".exe"));

            if (foundAsm is null) return null;

            return _cache.GetOrAdd(assembly, _ => AssemblyDefinition.FromFile(foundAsm, ReaderParameters));
        }

        public void AddToCache(AssemblyDescriptor descriptor, AssemblyDefinition definition)
        {
            if (!s_higherVersionCompuer.Equals(descriptor, definition))
            {
                throw new ArgumentException("Assembly does not fit descriptor", nameof(definition));
            }

            if (!_cache.TryAdd(descriptor, definition))
            {
                throw new ArgumentException("Cache already contains an assembly for the descriptor", nameof(descriptor));
            }
        }

        public bool RemoveFromCache(AssemblyDescriptor descriptor) => _cache.TryRemove(descriptor, out _);

        public bool HasCached(AssemblyDescriptor descriptor) => _cache.ContainsKey(descriptor);

        public void ClearCache() => _cache.Clear();
    }

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

        ModuleDefinition assemblyModule = ModuleDefinition.FromFile(assemblyPath);

        string winRTRuntimeAssemblyPath = referencePath.First(path => Path.GetFileNameWithoutExtension(path.AsSpan()).SequenceEqual("WinRT.Runtime"));
        ModuleDefinition winRTRuntimeModule = ModuleDefinition.FromFile(winRTRuntimeAssemblyPath);

        string windowsSdkAssemblyName = referencePath.First(path => Path.GetFileNameWithoutExtension(path.AsSpan()).SequenceEqual("Microsoft.Windows.SDK.NET"));
        ModuleDefinition windowsSdkModule = ModuleDefinition.FromFile(windowsSdkAssemblyName);

        CorLibTypeSignature objectType = assemblyModule.CorLibTypeFactory.Object;
        TypeReference windowsRuntimeTypeAttributeType = winRTRuntimeModule.CreateTypeReference("WinRT", "WindowsRuntimeTypeAttribute");


        InteropGeneratorState state = new();

        List<GenericInstanceTypeSignature> genericTypes = [];

        foreach (string path in referencePath)
        {
            try
            {
                ModuleDefinition module = ModuleDefinition.FromFile(path);

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

                    if (type.HasCustomAttribute("WinRT", "WindowsRuntimeTypeAttribute") && type.GenericParameters.Count > 0)
                    {
                    }

                    if (type.Namespace?.AsSpan().StartsWith("System"u8) is false &&
                        type.Namespace?.AsSpan().StartsWith("ABI."u8) is false)
                    {
                        foreach (var method in type.Methods)
                        {
                            foreach (var parameter in method.Parameters)
                            {
                                if (parameter.ParameterType is GenericInstanceTypeSignature sig)
                                {
                                    //genericType ??= sig;
                                }
                            }
                        }
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
        AssemblyDefinition winRTInteropAssembly = new("WinRT.Interop", assemblyModule.Assembly?.Version ?? new Version(1, 0, 0, 0));
        ModuleDefinition winRTInteropModule = new("WinRT.Interop");

        foreach (GenericInstanceTypeSignature typeSignature in genericTypes)
        {
            try
            {
                winRTInteropModule.TopLevelTypes.Add(InteropTypeDefinitionFactory.DelegateVftblType(typeSignature, winRTInteropModule.CorLibTypeFactory, winRTInteropModule.DefaultImporter));
                winRTInteropModule.TopLevelTypes.Add(InteropTypeDefinitionFactory.DelegateReferenceVftblType(typeSignature, winRTInteropModule.CorLibTypeFactory, winRTInteropModule.DefaultImporter));
                winRTInteropModule.TopLevelTypes.Add(InteropTypeDefinitionFactory.DelegateInterfaceEntriesType(typeSignature, winRTInteropModule.DefaultImporter));
            }
            catch
            {

            }
        }

        winRTInteropModule.Write(winRTInteropAssemblyPath);
    }
}
