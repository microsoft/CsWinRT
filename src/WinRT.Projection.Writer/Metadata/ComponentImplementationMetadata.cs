// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.References;

namespace WindowsRuntime.ProjectionWriter.Metadata;

/// <summary>
/// Indexes implementation details of the authored Windows Runtime component(s) being projected,
/// read from their managed implementation assemblies (<c>.dll</c>). These details are not present
/// in the <c>.winmd</c> metadata, so they have to be read from the compiled assemblies directly.
/// </summary>
/// <remarks>
/// The only detail currently tracked is whether an authored type registers any XAML dependency
/// properties (modelled as <c>static</c> fields of type <c>DependencyProperty</c>), which drives
/// whether the generated activation factory needs to force the authored type's class constructor
/// to run before the type is activated.
/// </remarks>
internal sealed class ComponentImplementationMetadata
{
    /// <summary>
    /// Per-type record: whether the type itself declares a <c>static</c> <c>DependencyProperty</c>
    /// field, and the full name of its base type (used to walk authored base types).
    /// </summary>
    private readonly record struct TypeRecord(bool DeclaresDependencyProperty, string? BaseTypeFullName);

    /// <summary>The authored types indexed by full name.</summary>
    private readonly Dictionary<string, TypeRecord> _typesByFullName;

    private ComponentImplementationMetadata(Dictionary<string, TypeRecord> typesByFullName)
    {
        _typesByFullName = typesByFullName;
    }

    /// <summary>
    /// Loads the metadata for the given managed implementation assemblies. When the input is empty,
    /// the returned instance is empty and <see cref="RequiresStaticConstructor"/> conservatively
    /// reports that every type needs its class constructor.
    /// </summary>
    /// <param name="assemblyPaths">The managed implementation assembly paths to scan.</param>
    /// <returns>The loaded metadata.</returns>
    public static ComponentImplementationMetadata Load(IEnumerable<string> assemblyPaths)
    {
        // Type full names are compared ordinally, which is the default string equality used here
        Dictionary<string, TypeRecord> typesByFullName = [];

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

                _ = typesByFullName.TryAdd(fullName, new TypeRecord(DeclaresDependencyProperty(type), type.BaseType?.FullName));
            }
        }

        return new ComponentImplementationMetadata(typesByFullName);
    }

    /// <summary>
    /// Determines whether the activation factory for the authored type identified by
    /// <paramref name="typeFullName"/> must force the type's class constructor to run before
    /// activation, i.e. whether the type (or any authored base type) registers a dependency property.
    /// </summary>
    /// <param name="typeFullName">The full name of the authored type.</param>
    /// <returns><see langword="true"/> if the static constructor is required; otherwise, <see langword="false"/>.</returns>
    /// <remarks>
    /// If the type is not found among the scanned implementation assemblies, this returns
    /// <see langword="true"/>: we cannot prove the constructor is unnecessary, so it is kept (this
    /// preserves the previous behavior whenever no implementation assemblies are available). The
    /// hierarchy walk stops at the first base type outside the scanned assemblies (e.g. a framework
    /// base type), because framework dependency properties are registered by the framework itself.
    /// </remarks>
    public bool RequiresStaticConstructor(string typeFullName)
    {
        if (!_typesByFullName.ContainsKey(typeFullName))
        {
            return true;
        }

        HashSet<string> visited = [];
        string? current = typeFullName;

        while (current is not null && visited.Add(current) && _typesByFullName.TryGetValue(current, out TypeRecord record))
        {
            if (record.DeclaresDependencyProperty)
            {
                return true;
            }

            current = record.BaseTypeFullName;
        }

        return false;
    }

    /// <summary>
    /// Returns whether <paramref name="type"/> declares any <c>static</c> field whose type is the
    /// XAML <c>DependencyProperty</c> (in either <c>Microsoft.UI.Xaml</c> or <c>Windows.UI.Xaml</c>).
    /// </summary>
    /// <param name="type">The type to inspect.</param>
    /// <returns><see langword="true"/> if the type declares such a field; otherwise, <see langword="false"/>.</returns>
    private static bool DeclaresDependencyProperty(TypeDefinition type)
    {
        foreach (FieldDefinition field in type.Fields)
        {
            if (!field.IsStatic)
            {
                continue;
            }

            TypeSignature? fieldType = field.Signature?.FieldType;

            if (fieldType is null)
            {
                continue;
            }

            (string ns, string name) = fieldType.Names();

            if (name == WellKnownTypeNames.DependencyProperty &&
                (ns == WellKnownNamespaces.MicrosoftUIXaml || ns == WellKnownNamespaces.WindowsUIXaml))
            {
                return true;
            }
        }

        return false;
    }
}
