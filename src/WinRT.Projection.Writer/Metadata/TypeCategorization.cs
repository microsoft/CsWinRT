// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver;
using AsmResolver.DotNet;

using static WindowsRuntime.ProjectionWriter.References.WellKnownNamespaces;

using static WindowsRuntime.ProjectionWriter.References.WellKnownAttributeNames;

namespace WindowsRuntime.ProjectionWriter.Metadata;

/// <summary>
/// Categorization of a Windows Runtime type definition.
/// </summary>
internal enum TypeCategory
{
    Interface,
    Class,
    Enum,
    Struct,
    Delegate,
}

/// <summary>
/// Static type categorization helpers, mirroring <c>winmd::reader::get_category</c> and various
/// </summary>
internal static class TypeCategorization
{
    /// <summary>
    /// Determines a type's category (class/interface/enum/struct/delegate).
    /// </summary>
    public static TypeCategory GetCategory(TypeDefinition type)
    {
        if (type.IsInterface)
        {
            return TypeCategory.Interface;
        }
        ITypeDefOrRef? baseType = type.BaseType;
        if (baseType is null)
        {
            return TypeCategory.Class;
        }
        Utf8String? baseNs = baseType.Namespace;
        Utf8String? baseName = baseType.Name;
        if (baseNs == "System" && baseName == "Enum")
        {
            return TypeCategory.Enum;
        }
        if (baseNs == "System" && baseName == "ValueType")
        {
            return TypeCategory.Struct;
        }
        if (baseNs == "System" && baseName == "MulticastDelegate")
        {
            return TypeCategory.Delegate;
        }
        return TypeCategory.Class;
    }

    /// <summary>
    /// True if this is an Attribute-derived class.
    /// </summary>
    public static bool IsAttributeType(TypeDefinition type)
    {
        if (GetCategory(type) != TypeCategory.Class)
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
        return GetCategory(type) == TypeCategory.Struct &&
               HasAttribute(type, WindowsFoundationMetadata, "ApiContractAttribute");
    }

    /// <summary>
    /// True if this type is a static class (abstract+sealed).
    /// </summary>
    public static bool IsStatic(TypeDefinition type)
    {
        return GetCategory(type) == TypeCategory.Class && type.IsAbstract && type.IsSealed;
    }

    /// <summary>
    /// True if this is an interface marked [ExclusiveTo].
    /// </summary>
    public static bool IsExclusiveTo(TypeDefinition type)
    {
        return GetCategory(type) == TypeCategory.Interface &&
               HasAttribute(type, WindowsFoundationMetadata, ExclusiveToAttribute);
    }

    /// <summary>
    /// True if this is a [Flags] enum.
    /// </summary>
    public static bool IsFlagsEnum(TypeDefinition type)
    {
        return GetCategory(type) == TypeCategory.Enum &&
               HasAttribute(type, "System", "FlagsAttribute");
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
        return HasAttribute(type, WindowsRuntimeInternal, "ProjectionInternalAttribute");
    }

    /// <summary>
    /// True if this type's CustomAttributes contains the given attribute.
    /// </summary>
    public static bool HasAttribute(IHasCustomAttribute member, string ns, string name)
    {
        for (int i = 0; i < member.CustomAttributes.Count; i++)
        {
            CustomAttribute attr = member.CustomAttributes[i];
            ITypeDefOrRef? type = attr.Constructor?.DeclaringType;
            if (type is not null && type.Namespace == ns && type.Name == name)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets the matching CustomAttribute or null.
    /// </summary>
    public static CustomAttribute? GetAttribute(IHasCustomAttribute member, string ns, string name)
    {
        for (int i = 0; i < member.CustomAttributes.Count; i++)
        {
            CustomAttribute attr = member.CustomAttributes[i];
            ITypeDefOrRef? type = attr.Constructor?.DeclaringType;
            if (type is not null && type.Namespace == ns && type.Name == name)
            {
                return attr;
            }
        }
        return null;
    }
}
