// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionWriter.Metadata;

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
