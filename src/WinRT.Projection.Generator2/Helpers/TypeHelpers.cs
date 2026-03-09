// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Metadata.Tables;
using WindowsRuntime.ProjectionGenerator.Models;

namespace WindowsRuntime.ProjectionGenerator.Helpers;

/// <summary>
/// Provides helper methods for inspecting and classifying Windows Runtime type definitions.
/// </summary>
internal static class TypeHelpers
{
    /// <summary>
    /// Gets the <see cref="TypeCategory"/> for a given type definition.
    /// </summary>
    public static TypeCategory GetCategory(TypeDefinition type)
    {
        return type.BaseType?.FullName switch
        {
            "System.Enum" => TypeCategory.Enum,
            "System.MulticastDelegate" => TypeCategory.Delegate,
            "System.ValueType" => IsApiContractType(type) ? TypeCategory.ApiContract : TypeCategory.Struct,
            "System.Attribute" => TypeCategory.Attribute,
            _ when type.IsInterface => TypeCategory.Interface,
            _ => TypeCategory.Class
        };
    }

    /// <summary>
    /// Checks whether a type has a specific custom attribute.
    /// </summary>
    public static bool HasAttribute(IHasCustomAttribute member, string attributeNamespace, string attributeName)
    {
        return GetAttribute(member, attributeNamespace, attributeName) is not null;
    }

    /// <summary>
    /// Gets a custom attribute by namespace and name, if present.
    /// </summary>
    public static CustomAttribute? GetAttribute(IHasCustomAttribute member, string attributeNamespace, string attributeName)
    {
        foreach (CustomAttribute attribute in member.CustomAttributes)
        {
            ITypeDefOrRef? attrType = GetAttributeDeclaringType(attribute);

            if (attrType is not null &&
                attrType.Name == attributeName &&
                attrType.Namespace == attributeNamespace)
            {
                return attribute;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks whether a type is a flags enum.
    /// </summary>
    public static bool IsFlagsEnum(TypeDefinition type)
    {
        return type.BaseType?.FullName == "System.Enum" &&
               HasAttribute(type, "System", "FlagsAttribute");
    }

    /// <summary>
    /// Checks whether a type is an API contract type (a struct with ApiContractAttribute).
    /// </summary>
    public static bool IsApiContractType(TypeDefinition type)
    {
        return type.BaseType?.FullName == "System.ValueType" &&
               HasAttribute(type, "Windows.Foundation.Metadata", "ApiContractAttribute");
    }

    /// <summary>
    /// Checks whether a type is an attribute class (extends System.Attribute).
    /// </summary>
    public static bool IsAttributeType(TypeDefinition type)
    {
        return type.BaseType?.FullName == "System.Attribute";
    }

    /// <summary>
    /// Checks whether a class is static (abstract + sealed).
    /// </summary>
    public static bool IsStaticClass(TypeDefinition type)
    {
        return type.IsAbstract && type.IsSealed;
    }

    /// <summary>
    /// Checks whether an interface is marked with ExclusiveToAttribute.
    /// </summary>
    public static bool IsExclusiveTo(TypeDefinition type)
    {
        return HasAttribute(type, "Windows.Foundation.Metadata", "ExclusiveToAttribute");
    }

    /// <summary>
    /// Gets the type that an exclusive interface is exclusive to.
    /// </summary>
    public static TypeDefinition? GetExclusiveToType(TypeDefinition type)
    {
        CustomAttribute? attr = GetAttribute(type, "Windows.Foundation.Metadata", "ExclusiveToAttribute");

        return attr?.Signature?.FixedArguments is { Count: > 0 } args && args[0].Element is TypeSignature typeSig
            ? (typeSig as TypeDefOrRefSignature)?.Type?.Resolve()
            : null;
    }

    /// <summary>
    /// Checks whether a type is marked with ProjectionInternalAttribute.
    /// </summary>
    public static bool IsProjectionInternal(TypeDefinition type)
    {
        return HasAttribute(type, "Windows.Foundation.Metadata", "ProjectionInternalAttribute");
    }

    /// <summary>
    /// Checks whether a type has generic parameters (is parameterized/generic).
    /// </summary>
    public static bool IsGenericType(TypeDefinition type)
    {
        return type.GenericParameters.Count > 0;
    }

    /// <summary>
    /// Gets the default interface of a runtime class.
    /// </summary>
    public static ITypeDefOrRef? GetDefaultInterface(TypeDefinition type)
    {
        foreach (InterfaceImplementation interfaceImpl in type.Interfaces)
        {
            if (IsDefaultInterface(interfaceImpl))
            {
                return interfaceImpl.Interface;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks whether an interface implementation has the DefaultAttribute.
    /// </summary>
    public static bool IsDefaultInterface(InterfaceImplementation interfaceImpl)
    {
        return HasAttribute(interfaceImpl, "Windows.Foundation.Metadata", "DefaultAttribute");
    }

    /// <summary>
    /// Checks whether an interface implementation is overridable.
    /// </summary>
    public static bool IsOverridable(InterfaceImplementation interfaceImpl)
    {
        return HasAttribute(interfaceImpl, "Windows.Foundation.Metadata", "OverridableAttribute");
    }

    /// <summary>
    /// Checks whether a method is a remove_* event handler.
    /// </summary>
    public static bool IsRemoveOverload(MethodDefinition method)
    {
        string? name = method.Name;

        return name?.StartsWith("remove_", StringComparison.Ordinal) ?? false;
    }

    /// <summary>
    /// Checks whether a method should not throw exceptions.
    /// </summary>
    public static bool IsNoExcept(MethodDefinition method)
    {
        return HasAttribute(method, "Windows.Foundation.Metadata", "NoExceptionAttribute");
    }

    /// <summary>
    /// Gets the Invoke method of a delegate type.
    /// </summary>
    public static MethodDefinition? GetDelegateInvoke(TypeDefinition delegateType)
    {
        foreach (MethodDefinition method in delegateType.Methods)
        {
            if (method.Name == "Invoke")
            {
                return method;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the property methods (getter, setter) for a property.
    /// </summary>
    public static (MethodDefinition? Getter, MethodDefinition? Setter) GetPropertyMethods(PropertyDefinition property)
    {
        MethodDefinition? getter = null;
        MethodDefinition? setter = null;

        foreach (MethodSemantics semantics in property.Semantics)
        {
            if ((semantics.Attributes & MethodSemanticsAttributes.Getter) != 0)
            {
                getter = semantics.Method;
            }
            else if ((semantics.Attributes & MethodSemanticsAttributes.Setter) != 0)
            {
                setter = semantics.Method;
            }
        }

        return (getter, setter);
    }

    /// <summary>
    /// Gets the event methods (add, remove) for an event.
    /// </summary>
    public static (MethodDefinition? Add, MethodDefinition? Remove) GetEventMethods(EventDefinition evt)
    {
        MethodDefinition? add = null;
        MethodDefinition? remove = null;

        foreach (MethodSemantics semantics in evt.Semantics)
        {
            if ((semantics.Attributes & MethodSemanticsAttributes.AddOn) != 0)
            {
                add = semantics.Method;
            }
            else if ((semantics.Attributes & MethodSemanticsAttributes.RemoveOn) != 0)
            {
                remove = semantics.Method;
            }
        }

        return (add, remove);
    }

    /// <summary>
    /// Checks whether a type has a parameterless constructor.
    /// </summary>
    public static bool HasDefaultConstructor(TypeDefinition type)
    {
        foreach (MethodDefinition method in type.Methods)
        {
            if (method.Name == ".ctor" &&
                !method.IsStatic &&
                method.Signature is { ParameterTypes.Count: 0 })
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the contract version from ContractVersionAttribute, if present.
    /// </summary>
    public static int? GetContractVersion(TypeDefinition type)
    {
        CustomAttribute? attr = GetAttribute(type, "Windows.Foundation.Metadata", "ContractVersionAttribute");

        if (attr?.Signature?.FixedArguments is not { Count: > 0 } args)
        {
            return null;
        }

        // The version is the last uint argument in any constructor overload
        for (int i = args.Count - 1; i >= 0; i--)
        {
            if (args[i].Element is uint version)
            {
                return unchecked((int)version);
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the declaring type of a custom attribute's constructor.
    /// </summary>
    private static ITypeDefOrRef? GetAttributeDeclaringType(CustomAttribute attribute)
    {
        return attribute.Constructor switch
        {
            MethodDefinition { DeclaringType: { } type } => type,
            MemberReference { Parent: ITypeDefOrRef type } => type,
            _ => null
        };
    }
}
