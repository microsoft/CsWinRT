// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Globalization;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Factories.Callbacks;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Writers;
using static WindowsRuntime.ProjectionWriter.References.WellKnownAttributeNames;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// ABI emission helpers for structs, enums, delegates, interfaces, and classes.
/// Provides predicates and writer helpers used by the per-kind ABI factories.
/// </summary>
internal static partial class AbiTypeHelpers
{
    /// <summary>
    /// Returns the parent class for an interface marked <c>[ExclusiveToAttribute(typeof(T))]</c>.
    /// </summary>
    public static TypeDefinition? GetExclusiveToType(MetadataCache cache, TypeDefinition iface)
    {
        CustomAttribute? attr = iface.GetWindowsFoundationMetadataAttribute(ExclusiveToAttribute);

        if (attr is null || !attr.TryGetFixedArgument(0, out TypeSignature? sig))
        {
            return null;
        }

        return cache.Find(sig.FullName ?? string.Empty);
    }

    /// <summary>
    /// Returns the unique virtual-method name used to refer to <paramref name="method"/> on
    /// <paramref name="type"/>'s vtable: the method's metadata name suffixed with its zero-based
    /// index in the type's method list, so overloads disambiguate (e.g. <c>get_Item_4</c>).
    /// </summary>
    /// <param name="type">The interface declaring the method.</param>
    /// <param name="method">The method whose vtable name to compute.</param>
    /// <returns>The virtual method name (<c>name_index</c>).</returns>
    public static string GetVirtualMethodName(TypeDefinition type, MethodDefinition method)
    {
        // Index of method in the type's method list
        int index = 0;
        foreach (MethodDefinition m in type.Methods)
        {
            if (m == method)
            {
                break;
            }

            index++;
        }
        return method.GetRawName() + "_" + index.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns the metadata-derived name for the return parameter, or the conventional
    /// <c>__return_value__</c> placeholder when the metadata does not name it.
    /// </summary>
    public static string GetReturnParamName(MethodSignatureInfo sig)
    {
        string? n = sig.ReturnParameter?.Name?.Value;

        if (string.IsNullOrEmpty(n))
        {
            return "__return_value__";
        }

        return IdentifierEscaping.EscapeIdentifier(n);
    }

    /// <summary>
    /// Returns the local-variable name for the return parameter on the server side.
    /// <c>abi_marshaler::get_marshaler_local()</c> which prefixes <c>__</c> to the param name.
    /// </summary>
    public static string GetReturnLocalName(MethodSignatureInfo sig)
    {
        return "__" + GetReturnParamName(sig);
    }

    /// <summary>
    /// Returns '__&lt;returnName&gt;Size' — by default '____return_value__Size' for the standard '__return_value__' return param.
    /// </summary>
    public static string GetReturnSizeParamName(MethodSignatureInfo sig)
    {
        return "__" + GetReturnParamName(sig) + "Size";
    }

    /// <summary>
    /// Build a method-to-event map for add/remove accessors of a type.
    /// </summary>
    public static Dictionary<MethodDefinition, EventDefinition>? BuildEventMethodMap(TypeDefinition type)
    {
        if (type.Events.Count == 0)
        {
            return null;
        }

        Dictionary<MethodDefinition, EventDefinition> map = [];
        foreach (EventDefinition evt in type.Events)
        {
            if (evt.AddMethod is MethodDefinition add)
            {
                map[add] = evt;
            }

            if (evt.RemoveMethod is MethodDefinition rem)
            {
                map[rem] = evt;
            }
        }
        return map;
    }

    /// <inheritdoc cref="WriteIidGuidReference(IndentedTextWriter, ProjectionEmitContext, TypeDefinition)"/>
    /// <returns>A callback that writes the IID expression to the writer it's appended to.</returns>
    public static WriteIidGuidReferenceCallback WriteIidGuidReference(ProjectionEmitContext context, TypeDefinition type)
    {
        return new(context, type);
    }

    /// <summary>
    /// Writes the IID GUID literal expression for the given runtime type (used by ABI emission paths).
    /// </summary>
    public static void WriteIidGuidReference(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (type.GenericParameters.Count != 0)
        {
            // Generic interface IID - call the unsafe accessor
            WriteIidGuidPropertyNameCallback iidName = IidExpressionGenerator.WriteIidGuidPropertyName(context, type);
            writer.Write($"{iidName}(null)");
            return;
        }

        (string ns, string nm) = type.Names();

        if (MappedTypes.Get(ns, nm) is { } m && m.MappedName == "IStringable")
        {
            writer.Write("global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IStringable");
            return;
        }

        WriteIidGuidPropertyNameCallback name = IidExpressionGenerator.WriteIidGuidPropertyName(context, type);
        writer.Write($"global::ABI.InterfaceIIDs.{name}");
    }

    /// <summary>
    /// True if the interface has at least one non-special method, property, or non-skipped event.
    /// </summary>
    public static bool HasEmittableMembers(TypeDefinition iface, bool skipExclusiveEvents)
    {
        foreach (MethodDefinition m in iface.Methods)
        {
            if (!m.IsSpecial)
            {
                return true;
            }
        }

        if (iface.Properties.Count > 0)
        {
            return true;
        }

        if (!skipExclusiveEvents && iface.Events.Count > 0)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns the number of methods (including special accessors) on the interface.
    /// </summary>
    public static int CountMethods(TypeDefinition iface)
    {
        return iface.Methods.Count;
    }

    /// <summary>
    /// Returns the number of base classes between <paramref name="classType"/> and <see cref="object"/>.
    /// </summary>
    public static int GetClassHierarchyIndex(MetadataCache cache, TypeDefinition classType)
    {
        if (classType.BaseType is null)
        {
            return 0;
        }

        (string ns, string nm) = classType.BaseType.Names();

        // 'System.Object' is not in any .winmd, so resolving via the cache fails. But AsmResolver
        // can still 'TryResolve' it from the corlib runtime context, which would (incorrectly)
        // make a direct-Object-derivation count as depth 1 instead of 0. Short-circuit here.
        if (ns == "System" && nm == "Object")
        {
            return 0;
        }

        TypeDefinition? baseDef = classType.BaseType as TypeDefinition;

        if (baseDef is null)
        {
            baseDef = classType.BaseType.TryResolve(cache.RuntimeContext);
            baseDef ??= cache.Find(ns, nm);
        }

        if (baseDef is null)
        {
            return 0;
        }

        return GetClassHierarchyIndex(cache, baseDef) + 1;
    }

    /// <summary>
    /// Returns whether two interface types refer to the same interface by namespace+name (used to compare interfaces across module boundaries).
    /// </summary>
    public static bool InterfacesEqualByName(TypeDefinition a, TypeDefinition b)
    {
        if (a == b)
        {
            return true;
        }

        return (a.Namespace?.Value ?? string.Empty) == (b.Namespace?.Value ?? string.Empty)
            && (a.Name?.Value ?? string.Empty) == (b.Name?.Value ?? string.Empty);
    }

}
