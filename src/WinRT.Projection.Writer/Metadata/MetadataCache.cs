// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Errors;
using WindowsRuntime.ProjectionWriter.Extensions;

namespace WindowsRuntime.ProjectionWriter.Metadata;

/// <summary>
/// Loads one or more <c>.winmd</c> files and exposes types organized by namespace.
/// </summary>
internal sealed class MetadataCache
{
    private readonly Dictionary<string, NamespaceMembers> _namespaces = [];
    private readonly Dictionary<string, TypeDefinition> _typesByFullName = [];
    private readonly Dictionary<TypeDefinition, string> _typeToModulePath = [];
    private readonly List<ModuleDefinition> _modules = [];

    public IReadOnlyDictionary<string, NamespaceMembers> Namespaces => _namespaces;

    public IReadOnlyList<ModuleDefinition> Modules => _modules;

    /// <summary>
    /// The shared <see cref="RuntimeContext"/> used for resolving TypeRefs in the loaded .winmd files.
    /// All .winmd files share an mscorlib reference (v255.255.255.255 with the standard PKT), so we can
    /// safely use a single runtime context for all of them.
    /// </summary>
    public RuntimeContext RuntimeContext { get; }

    private MetadataCache(RuntimeContext runtimeContext)
    {
        RuntimeContext = runtimeContext;
    }

    public static MetadataCache Load(IEnumerable<string> inputs)
    {
        // Collect all .winmd files first so the resolver knows about all of them. Dedupe by canonical
        // absolute path so that the same physical file passed via two different but equivalent path
        // strings (e.g. one absolute and one with '..' components, or one explicitly listed as a file
        // and one picked up by an enclosing directory scan) is only loaded once. Loading the same
        // .winmd twice causes duplicate types to be added to NamespaceMembers.Types and ultimately
        // emitted twice in the same output file (CS0101).
#pragma warning disable IDE0028 // Use collection expression -- needs StringComparer.OrdinalIgnoreCase
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
#pragma warning restore IDE0028
        List<string> winmdFiles = [];
        foreach (string input in inputs)
        {
            if (Directory.Exists(input))
            {
                foreach (string path in Directory.EnumerateFiles(input, "*.winmd", SearchOption.AllDirectories))
                {
                    string canonical = Path.GetFullPath(path);
                    if (seen.Add(canonical))
                    {
                        winmdFiles.Add(canonical);
                    }
                }
            }
            else if (File.Exists(input))
            {
                string canonical = Path.GetFullPath(input);
                if (seen.Add(canonical))
                {
                    winmdFiles.Add(canonical);
                }
            }
            else
            {
                throw new FileNotFoundException($"Input metadata file/directory not found: {input}", input);
            }
        }

        // Set up a PathAssemblyResolver scoped to the input .winmd files
        // (and any sibling .winmd files in their directories) so type forwards and cross-references resolve.
        string[] searchDirectories = winmdFiles
            .Select(Path.GetDirectoryName)
            .Where(d => d is not null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray()!;

        PathAssemblyResolver resolver = PathAssemblyResolver.FromSearchDirectories(searchDirectories);

        // For .winmd files, mscorlib is referenced as v255.255.255.255. We use a synthetic runtime info
        // (NetCoreApp 10.0 to match the project's TFM) — the actual version isn't important for resolving
        // .winmd-internal TypeRefs since all .winmd cross-references go through PathAssemblyResolver
        // by name. The AsmResolver runtime context just needs to be valid to bypass the implicit
        // "probe runtime from PE image" path that fails for .winmd files (v255.255).
        DotNetRuntimeInfo targetRuntime = DotNetRuntimeInfo.NetCoreApp(10, 0);
        RuntimeContext runtimeContext = new(targetRuntime, resolver);

        MetadataCache cache = new(runtimeContext);
        foreach (string winmd in winmdFiles)
        {
            cache.LoadFile(winmd);
        }
        cache.SortMembersByName();
        return cache;
    }

    /// <summary>
    /// Sorts each namespace's <see cref="NamespaceMembers.Types"/> list alphabetically by type name
    /// so all downstream iteration produces deterministic output.
    /// </summary>
    private void SortMembersByName()
    {
        foreach (NamespaceMembers members in _namespaces.Values)
        {
            static int Compare(TypeDefinition a, TypeDefinition b) => System.StringComparer.Ordinal.Compare(a.Name?.Value ?? string.Empty, b.Name?.Value ?? string.Empty);
            members.Types.Sort(Compare);
            members.Interfaces.Sort(Compare);
            members.Classes.Sort(Compare);
            members.Enums.Sort(Compare);
            members.Structs.Sort(Compare);
            members.Delegates.Sort(Compare);
            members.Attributes.Sort(Compare);
            members.Contracts.Sort(Compare);
        }
    }

    private void LoadFile(string path)
    {
        AssemblyDefinition assemblyDefinition = RuntimeContext.LoadAssembly(path);
        if (assemblyDefinition.Modules is not [ModuleDefinition module])
        {
            throw new System.BadImageFormatException($"Expected exactly one module in '{path}'.");
        }
        _modules.Add(module);
        string moduleFilePath = path;

        foreach (TypeDefinition type in module.TopLevelTypes)
        {
            (string ns, string name) = type.Names();

            // Skip the <Module> pseudo-type
            if (name == "<Module>")
            {
                continue;
            }

            // Dedupe by full type name. Multiple input .winmd files can legitimately define types
            // with the same full name (e.g. WindowsRuntime.Internal types appearing in both
            // WindowsRuntime.Internal.winmd and cswinrt.winmd, or types showing up in both an SDK
            // contract winmd and a 3rd-party WinMD that re-exports/forwards them). The C++ cswinrt
            // tool silently dedupes via 'std::map<full_name, TypeDef>' in its cache; the C# port
            // mirrors that here so the same input set produces semantically identical output.
            // First-load-wins matches the C++ behavior (the map's insert is "no overwrite").
            string fullName = string.IsNullOrEmpty(ns) ? name : ns + "." + name;
            if (_typesByFullName.ContainsKey(fullName))
            {
                continue;
            }

            if (!_namespaces.TryGetValue(ns, out NamespaceMembers? members))
            {
                members = new NamespaceMembers(ns);
                _namespaces[ns] = members;
            }
            members.AddType(type);

            _typesByFullName[fullName] = type;
            _typeToModulePath[type] = moduleFilePath;
        }
    }

    /// <summary>
    /// Gets the file path of the .winmd that contributed the given type.
    /// </summary>
    public string GetSourcePath(TypeDefinition type)
    {
        return _typeToModulePath.TryGetValue(type, out string? path) ? path : string.Empty;
    }

    /// <summary>
    /// Looks up a type by full name (namespace + "." + name).
    /// </summary>
    public TypeDefinition? Find(string fullName)
    {
        return _typesByFullName.TryGetValue(fullName, out TypeDefinition? type) ? type : null;
    }

    /// <summary>
    /// Gets a type by full name, throwing if not found.
    /// </summary>
    public TypeDefinition FindRequired(string fullName)
    {
        return Find(fullName) ?? throw WellKnownProjectionWriterExceptions.CannotResolveType(fullName);
    }
}

/// <summary>
/// The types in a particular namespace, organized by category.
/// </summary>
internal sealed class NamespaceMembers
{
    public string Name { get; }

    public List<TypeDefinition> Types { get; } = [];
    public List<TypeDefinition> Interfaces { get; } = [];
    public List<TypeDefinition> Classes { get; } = [];
    public List<TypeDefinition> Enums { get; } = [];
    public List<TypeDefinition> Structs { get; } = [];
    public List<TypeDefinition> Delegates { get; } = [];
    public List<TypeDefinition> Attributes { get; } = [];
    public List<TypeDefinition> Contracts { get; } = [];

    public NamespaceMembers(string name)
    {
        Name = name;
    }

    public void AddType(TypeDefinition type)
    {
        Types.Add(type);
        TypeCategory category = TypeCategorization.GetCategory(type);
        switch (category)
        {
            case TypeCategory.Interface:
                Interfaces.Add(type);
                break;
            case TypeCategory.Class:
                if (TypeCategorization.IsAttributeType(type))
                {
                    Attributes.Add(type);
                }
                else
                {
                    Classes.Add(type);
                }
                break;
            case TypeCategory.Enum:
                Enums.Add(type);
                break;
            case TypeCategory.Struct:
                if (TypeCategorization.IsApiContractType(type))
                {
                    Contracts.Add(type);
                }
                else
                {
                    Structs.Add(type);
                }
                break;
            case TypeCategory.Delegate:
                Delegates.Add(type);
                break;
        }
    }
}