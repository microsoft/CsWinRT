// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Errors;

namespace WindowsRuntime.ProjectionWriter.Metadata;

/// <summary>
/// Loads one or more <c>.winmd</c> files and exposes types organized by namespace.
/// </summary>
internal sealed class MetadataCache
{
    /// <summary>Backing field for <see cref="Namespaces"/>.</summary>
    private readonly Dictionary<string, NamespaceMembers> _namespaces = [];

    /// <summary>Backing field for the global type-by-full-name index used by <see cref="Find"/>.</summary>
    private readonly Dictionary<string, TypeDefinition> _typesByFullName = [];

    /// <summary>Backing field for the type-to-source-module-path index used by <see cref="GetSourcePath"/>.</summary>
    private readonly Dictionary<TypeDefinition, string> _typeToModulePath = [];

    /// <summary>Backing field for <see cref="Modules"/>.</summary>
    private readonly List<ModuleDefinition> _modules = [];

    /// <summary>
    /// Gets the loaded namespaces, keyed by namespace name. Each value bag holds the
    /// per-category type lists (<see cref="NamespaceMembers.Types"/> + per-kind splits).
    /// </summary>
    public IReadOnlyDictionary<string, NamespaceMembers> Namespaces => _namespaces;

    /// <summary>
    /// Gets the loaded modules in load order.
    /// </summary>
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

    /// <summary>
    /// Loads the input <c>.winmd</c> files (or directories of <c>.winmd</c> files) into a new metadata cache.
    /// </summary>
    /// <param name="inputs">The input paths.</param>
    /// <returns>The loaded metadata cache.</returns>
    public static MetadataCache Load(IEnumerable<string> inputs)
    {
        // Collect all .winmd files first so the resolver knows about all of them. Dedupe by canonical
        // absolute path so that the same physical file passed via two different but equivalent path
        // strings (e.g. one absolute and one with '..' components, or one explicitly listed as a file
        // and one picked up by an enclosing directory scan) is only loaded once. Loading the same
        // .winmd twice causes duplicate types to be added to NamespaceMembers.Types and ultimately
        // emitted twice in the same output file (CS0101).
        int capacity = inputs is ICollection<string> collection ? collection.Count : 0;
        HashSet<string> seen = new(capacity, StringComparer.OrdinalIgnoreCase);
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
                throw WellKnownProjectionWriterExceptions.InvalidInputPath(input);
            }
        }

        // Sort the file list (case-insensitive ordinal) so first-load-wins behavior in LoadFile
        // is deterministic regardless of filesystem enumeration order.
        winmdFiles.Sort(StringComparer.OrdinalIgnoreCase);

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
            static int Compare(TypeDefinition a, TypeDefinition b) => StringComparer.Ordinal.Compare(a.Name?.Value ?? string.Empty, b.Name?.Value ?? string.Empty);
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
            throw WellKnownProjectionWriterExceptions.MalformedWinmd(path);
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
            // contract winmd and a 3rd-party WinMD that re-exports / forwards them). First-load-wins.
            string fullName = string.IsNullOrEmpty(ns) ? name : ns + "." + name;

            if (!_typesByFullName.TryAdd(fullName, type))
            {
                continue;
            }

            if (!_namespaces.TryGetValue(ns, out NamespaceMembers? members))
            {
                members = new NamespaceMembers(ns);
                _namespaces[ns] = members;
            }

            members.AddType(type);

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
/// <param name="name">The name of the namespace.</param>
internal sealed class NamespaceMembers(string name)
{
    /// <summary>Gets the namespace name (e.g. <c>Windows.Foundation</c>).</summary>
    public string Name { get; } = name;

    /// <summary>Gets the flat list of every type declared in this namespace, in load + sort order.</summary>
    public List<TypeDefinition> Types { get; } = [];

    /// <summary>Gets the interface-category types declared in this namespace.</summary>
    public List<TypeDefinition> Interfaces { get; } = [];

    /// <summary>Gets the runtime-class-category types (excluding attribute classes) declared in this namespace.</summary>
    public List<TypeDefinition> Classes { get; } = [];

    /// <summary>Gets the enum-category types declared in this namespace.</summary>
    public List<TypeDefinition> Enums { get; } = [];

    /// <summary>Gets the struct-category types (excluding API-contract markers) declared in this namespace.</summary>
    public List<TypeDefinition> Structs { get; } = [];

    /// <summary>Gets the delegate-category types declared in this namespace.</summary>
    public List<TypeDefinition> Delegates { get; } = [];

    /// <summary>Gets the attribute-class types declared in this namespace.</summary>
    public List<TypeDefinition> Attributes { get; } = [];

    /// <summary>Gets the API-contract marker types declared in this namespace (a struct sub-category).</summary>
    public List<TypeDefinition> Contracts { get; } = [];

    /// <summary>
    /// Adds <paramref name="type"/> to <see cref="Types"/> and to the matching per-category bucket.
    /// </summary>
    /// <param name="type">The type definition to add.</param>
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
