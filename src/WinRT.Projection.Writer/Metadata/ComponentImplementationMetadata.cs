// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionWriter.Metadata;

/// <summary>
/// Resolves authored Windows Runtime types from the managed implementation assemblies (<c>.dll</c>)
/// of the component(s) being projected, by full name. The managed assemblies carry implementation
/// details (such as the <c>static</c> fields backing XAML dependency properties) that are not
/// present in the <c>.winmd</c> metadata, so consumers resolve the managed type here and inspect it
/// directly.
/// </summary>
/// <remarks>
/// Loading only builds a name index over the assemblies' types. No per-type analysis is performed
/// up front: callers resolve just the types they care about (see
/// <see cref="ComponentStaticConstructorAnalyzer"/>), keeping the cost proportional to what is used.
/// </remarks>
internal sealed class ComponentImplementationMetadata
{
    /// <summary>The authored types indexed by full name (ordinal, the default string equality).</summary>
    private readonly Dictionary<string, TypeDefinition> _typesByFullName;

    private ComponentImplementationMetadata(Dictionary<string, TypeDefinition> typesByFullName)
    {
        _typesByFullName = typesByFullName;
    }

    /// <summary>
    /// Loads the managed implementation assemblies and indexes their types by full name. When the
    /// input is empty, the returned instance resolves nothing and <see cref="Resolve"/> always
    /// returns <see langword="null"/>.
    /// </summary>
    /// <param name="assemblyPaths">The managed implementation assembly paths to load.</param>
    /// <returns>The loaded metadata.</returns>
    public static ComponentImplementationMetadata Load(IEnumerable<string> assemblyPaths)
    {
        Dictionary<string, TypeDefinition> typesByFullName = [];

        foreach (string assemblyPath in assemblyPaths)
        {
            ModuleDefinition module = ModuleDefinition.FromFile(assemblyPath);

            foreach (TypeDefinition type in module.GetAllTypes())
            {
                string fullName = type.FullName;

                // Skip the '<Module>' pseudo-type and only index the first definition for a given
                // full name (multiple component assemblies should never define the same type)
                if (string.IsNullOrEmpty(fullName) || type.Name?.Value is "<Module>")
                {
                    continue;
                }

                _ = typesByFullName.TryAdd(fullName, type);
            }
        }

        return new ComponentImplementationMetadata(typesByFullName);
    }

    /// <summary>
    /// Resolves the authored type with the given full name from the loaded implementation
    /// assemblies, or <see langword="null"/> if no such type is present (e.g. a framework type, or
    /// when no implementation assemblies were loaded).
    /// </summary>
    /// <param name="fullName">The full name of the type to resolve.</param>
    /// <returns>The resolved <see cref="TypeDefinition"/>, or <see langword="null"/>.</returns>
    public TypeDefinition? Resolve(string fullName)
    {
        return _typesByFullName.GetValueOrDefault(fullName);
    }

    /// <summary>
    /// Gets whether the managed implementation assemblies define a type with the given full name.
    /// </summary>
    public bool Contains(string fullName)
    {
        return _typesByFullName.ContainsKey(fullName);
    }
}
