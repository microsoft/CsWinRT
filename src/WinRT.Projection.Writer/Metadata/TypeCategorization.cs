// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Models;
using static WindowsRuntime.ProjectionWriter.References.WellKnownAttributeNames;
using static WindowsRuntime.ProjectionWriter.References.WellKnownNamespaces;

namespace WindowsRuntime.ProjectionWriter.Metadata;

/// <summary>
/// Static type categorization helpers, mirroring <c>winmd::reader::get_category</c> and various
/// </summary>
internal static class TypeCategorization
{
    /// <summary>
    /// Determines a type's category (class/interface/enum/struct/delegate).
    /// </summary>
    public static TypeKind GetCategory(TypeDefinition type)
    {
        return type switch
        {
            { IsInterface: true } => TypeKind.Interface,
            { IsEnum: true } => TypeKind.Enum,
            { IsValueType: true } => TypeKind.Struct,
            { IsDelegate: true } => TypeKind.Delegate,
            _ => TypeKind.Class
        };
    }

    /// <summary>
    /// True if this is an API contract struct type.
    /// </summary>
    public static bool IsApiContractType(TypeDefinition type)
    {
        return type.IsStruct && type.HasAttribute(WindowsFoundationMetadata, "ApiContractAttribute");
    }

    /// <summary>
    /// True if this is an interface marked [ExclusiveTo].
    /// </summary>
    public static bool IsExclusiveTo(TypeDefinition type)
    {
        return type.IsInterface && type.HasAttribute(WindowsFoundationMetadata, ExclusiveToAttribute);
    }

    /// <summary>
    /// True if this type is marked [ProjectionInternal].
    /// </summary>
    public static bool IsProjectionInternal(TypeDefinition type)
    {
        return type.HasAttribute(WindowsRuntimeInternal, "ProjectionInternalAttribute");
    }
}

