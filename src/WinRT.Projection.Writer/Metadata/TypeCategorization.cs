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
    /// True if this is an Attribute-derived class.
    /// </summary>
    public static bool IsAttributeType(TypeDefinition type)
    {
        if (GetCategory(type) != TypeKind.Class)
        {
            return false;
        }

        // Check immediate base type for System.Attribute (winmd attribute types extend it directly).
        ITypeDefOrRef? cur = type.BaseType;
        while (cur is not null)
        {
            if (cur.Namespace == "System" && cur.Name == "Attribute")
            {
                return true;
            }

            // For attributes, the base type chain is short and we typically stop at a TypeRef
            // pointing to System.Attribute. We don't try to resolve further.
            return false;
        }
        return false;
    }

    /// <summary>
    /// True if this is an API contract struct type.
    /// </summary>
    public static bool IsApiContractType(TypeDefinition type)
    {
        return GetCategory(type) == TypeKind.Struct &&
               type.HasAttribute(WindowsFoundationMetadata, "ApiContractAttribute");
    }

    /// <summary>
    /// True if this type is a static class (abstract+sealed).
    /// </summary>
    public static bool IsStatic(TypeDefinition type)
    {
        return GetCategory(type) == TypeKind.Class && type.IsAbstract && type.IsSealed;
    }

    /// <summary>
    /// True if this is an interface marked [ExclusiveTo].
    /// </summary>
    public static bool IsExclusiveTo(TypeDefinition type)
    {
        return GetCategory(type) == TypeKind.Interface &&
               type.HasAttribute(WindowsFoundationMetadata, ExclusiveToAttribute);
    }

    /// <summary>
    /// True if this is a [Flags] enum.
    /// </summary>
    public static bool IsFlagsEnum(TypeDefinition type)
    {
        return GetCategory(type) == TypeKind.Enum &&
               type.HasAttribute("System", "FlagsAttribute");
    }

    /// <summary>
    /// True if this is a generic type (has type parameters).
    /// </summary>
    public static bool IsGeneric(TypeDefinition type)
    {
        return type.GenericParameters.Count > 0;
    }

    /// <summary>
    /// True if this type is marked [ProjectionInternal].
    /// </summary>
    public static bool IsProjectionInternal(TypeDefinition type)
    {
        return type.HasAttribute(WindowsRuntimeInternal, "ProjectionInternalAttribute");
    }
}
