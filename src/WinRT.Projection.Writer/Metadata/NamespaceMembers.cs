// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Resolvers;

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
        TypeKind category = TypeKindResolver.Resolve(type);
        switch (category)
        {
            case TypeKind.Interface:
                Interfaces.Add(type);
                break;
            case TypeKind.Class:
                if (type.IsAttributeType)
                {
                    Attributes.Add(type);
                }
                else
                {
                    Classes.Add(type);
                }

                break;
            case TypeKind.Enum:
                Enums.Add(type);
                break;
            case TypeKind.Struct:
                if (type.IsApiContractType)
                {
                    Contracts.Add(type);
                }
                else
                {
                    Structs.Add(type);
                }

                break;
            case TypeKind.Delegate:
                Delegates.Add(type);
                break;
        }
    }
}
