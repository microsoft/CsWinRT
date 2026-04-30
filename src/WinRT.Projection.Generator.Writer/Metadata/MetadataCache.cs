// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionGenerator.Writer;

/// <summary>
/// Mirrors the C++ <c>winmd::reader::cache</c> from the WinMD library.
/// Loads one or more <c>.winmd</c> files and exposes types organized by namespace.
/// </summary>
internal sealed class MetadataCache
{
    private readonly Dictionary<string, NamespaceMembers> _namespaces = new(StringComparer.Ordinal);
    private readonly Dictionary<string, TypeDefinition> _typesByFullName = new(StringComparer.Ordinal);
    private readonly Dictionary<TypeDefinition, string> _typeToModulePath = new();
    private readonly List<ModuleDefinition> _modules = new();

    public IReadOnlyDictionary<string, NamespaceMembers> Namespaces => _namespaces;

    public IReadOnlyList<ModuleDefinition> Modules => _modules;

    public static MetadataCache Load(IEnumerable<string> inputs)
    {
        MetadataCache cache = new();
        foreach (string input in inputs)
        {
            cache.LoadOne(input);
        }
        return cache;
    }

    private void LoadOne(string path)
    {
        if (Directory.Exists(path))
        {
            foreach (string winmd in Directory.EnumerateFiles(path, "*.winmd", SearchOption.AllDirectories))
            {
                LoadFile(winmd);
            }
            return;
        }
        if (File.Exists(path))
        {
            LoadFile(path);
            return;
        }
        throw new FileNotFoundException($"Input metadata file/directory not found: {path}", path);
    }

    private void LoadFile(string path)
    {
        ModuleDefinition module = ModuleDefinition.FromFile(path, new AsmResolver.DotNet.Serialized.ModuleReaderParameters(), createRuntimeContext: false);
        _modules.Add(module);
        string moduleFilePath = path;

        foreach (TypeDefinition type in module.TopLevelTypes)
        {
            string ns = type.Namespace?.Value ?? string.Empty;
            string name = type.Name?.Value ?? string.Empty;

            // Skip the <Module> pseudo-type
            if (name == "<Module>")
            {
                continue;
            }

            if (!_namespaces.TryGetValue(ns, out NamespaceMembers? members))
            {
                members = new NamespaceMembers(ns);
                _namespaces[ns] = members;
            }
            members.AddType(type);

            string fullName = string.IsNullOrEmpty(ns) ? name : ns + "." + name;
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
        return Find(fullName) ?? throw new InvalidOperationException($"Required type '{fullName}' not found in metadata.");
    }
}

/// <summary>
/// Mirrors the C++ <c>cache::namespace_members</c>: the types in a particular namespace,
/// organized by category.
/// </summary>
internal sealed class NamespaceMembers
{
    public string Name { get; }

    public List<TypeDefinition> Types { get; } = new();
    public List<TypeDefinition> Interfaces { get; } = new();
    public List<TypeDefinition> Classes { get; } = new();
    public List<TypeDefinition> Enums { get; } = new();
    public List<TypeDefinition> Structs { get; } = new();
    public List<TypeDefinition> Delegates { get; } = new();
    public List<TypeDefinition> Attributes { get; } = new();
    public List<TypeDefinition> Contracts { get; } = new();

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
