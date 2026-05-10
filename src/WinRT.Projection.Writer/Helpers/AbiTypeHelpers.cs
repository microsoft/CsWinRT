// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using System.Collections.Generic;
using System.Globalization;
using WindowsRuntime.ProjectionWriter.Extensions;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Writers;

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
    internal static TypeDefinition? GetExclusiveToType(MetadataCache cache, TypeDefinition iface)
    {
        if (cache is null) { return null; }
        for (int i = 0; i < iface.CustomAttributes.Count; i++)
        {
            CustomAttribute attr = iface.CustomAttributes[i];
            ITypeDefOrRef? attrType = attr.Constructor?.DeclaringType;
            if (attrType is null) { continue; }
            if (attrType.Namespace?.Value != "Windows.Foundation.Metadata" ||
                attrType.Name?.Value != "ExclusiveToAttribute") { continue; }
            if (attr.Signature is null) { continue; }
            for (int j = 0; j < attr.Signature.FixedArguments.Count; j++)
            {
                CustomAttributeArgument arg = attr.Signature.FixedArguments[j];
                if (arg.Element is TypeSignature sig)
                {
                    string fullName = sig.FullName ?? string.Empty;
                    TypeDefinition? td = cache.Find(fullName);
                    if (td is not null) { return td; }
                }
                else if (arg.Element is string s)
                {
                    TypeDefinition? td = cache.Find(s);
                    if (td is not null) { return td; }
                }
            }
        }
        return null;
    }

    /// <summary>Resolves an InterfaceImpl's interface reference to a TypeDefinition (same module or via metadata cache).</summary>
    internal static TypeDefinition? ResolveInterfaceTypeDef(MetadataCache cache, ITypeDefOrRef ifaceRef)
    {
        if (ifaceRef is TypeDefinition td) { return td; }
        if (ifaceRef is TypeSpecification ts && ts.Signature is GenericInstanceTypeSignature gi)
        {
            ITypeDefOrRef? gen = gi.GenericType;
            if (gen is TypeDefinition gtd) { return gtd; }
            if (gen is TypeReference gtr)
            {
                (string ns, string nm) = gtr.Names();
                return cache.Find(ns + "." + nm);
            }
        }
        if (ifaceRef is TypeReference tr)
        {
            (string ns, string nm) = tr.Names();
            return cache.Find(ns + "." + nm);
        }
        return null;
    }
    public static string GetVMethodName(TypeDefinition type, MethodDefinition method)
    {
        // Index of method in the type's method list
        int index = 0;
        foreach (MethodDefinition m in type.Methods)
        {
            if (m == method) { break; }
            index++;
        }
        return (method.Name?.Value ?? string.Empty) + "_" + index.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns the metadata-derived name for the return parameter, or the conventional
    /// <c>__return_value__</c> placeholder when the metadata does not name it.
    /// </summary>
    internal static string GetReturnParamName(MethodSignatureInfo sig)
    {
        string? n = sig.ReturnParam?.Name?.Value;
        if (string.IsNullOrEmpty(n)) { return "__return_value__"; }
        return CSharpKeywords.IsKeyword(n) ? "@" + n : n;
    }

    /// <summary>
    /// Returns the local-variable name for the return parameter on the server side.
    /// <c>abi_marshaler::get_marshaler_local()</c> which prefixes <c>__</c> to the param name.
    /// </summary>
    internal static string GetReturnLocalName(MethodSignatureInfo sig)
    {
        return "__" + GetReturnParamName(sig);
    }

    /// <summary>Returns '__&lt;returnName&gt;Size' — by default '____return_value__Size' for the standard '__return_value__' return param.</summary>
    internal static string GetReturnSizeParamName(MethodSignatureInfo sig)
    {
        return "__" + GetReturnParamName(sig) + "Size";
    }

    /// <summary>Build a method-to-event map for add/remove accessors of a type.</summary>
    internal static Dictionary<MethodDefinition, EventDefinition>? BuildEventMethodMap(TypeDefinition type)
    {
        if (type.Events.Count == 0) { return null; }
        Dictionary<MethodDefinition, EventDefinition> map = [];
        foreach (EventDefinition evt in type.Events)
        {
            if (evt.AddMethod is MethodDefinition add) { map[add] = evt; }
            if (evt.RemoveMethod is MethodDefinition rem) { map[rem] = evt; }
        }
        return map;
    }

    /// <summary>Writes the IID GUID literal expression for the given runtime type (used by ABI emission paths).</summary>
    public static void WriteIidGuidReference(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (type.GenericParameters.Count != 0)
        {
            // Generic interface IID - call the unsafe accessor
            IIDExpressionWriter.WriteIidGuidPropertyName(writer, context, type);
            writer.Write("(null)");
            return;
        }
        (string ns, string nm) = type.Names();
        if (MappedTypes.Get(ns, nm) is { } m && m.MappedName == "IStringable")
        {
            writer.Write("global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IStringable");
            return;
        }
        writer.Write("global::ABI.InterfaceIIDs.");
        IIDExpressionWriter.WriteIidGuidPropertyName(writer, context, type);
    }

    /// <summary>True if EmitNativeDelegateBody can emit a real (non-throw) body for this signature.</summary>
    internal static bool IsDelegateInvokeNativeSupported(MetadataCache cache, MethodSignatureInfo sig)
    {
        TypeSignature? rt = sig.ReturnType;
        if (rt is not null)
        {
            if (rt.IsHResultException()) { return false; }
            if (!(IsBlittablePrimitive(cache, rt) || IsAnyStruct(cache, rt) || rt.IsString() || IsRuntimeClassOrInterface(cache, rt) || rt.IsObject() || rt.IsGenericInstance() || IsComplexStruct(cache, rt))) { return false; }
        }
        foreach (ParameterInfo p in sig.Params)
        {
            ParameterCategory cat = ParameterCategoryResolver.GetParamCategory(p);
            if (cat is ParameterCategory.PassArray or ParameterCategory.FillArray)
            {
                if (p.Type is SzArrayTypeSignature szP)
                {
                    if (IsBlittablePrimitive(cache, szP.BaseType)) { continue; }
                    if (IsAnyStruct(cache, szP.BaseType)) { continue; }
                }
                return false;
            }
            if (cat != ParameterCategory.In) { return false; }
            if (p.Type.IsHResultException()) { return false; }
            if (IsBlittablePrimitive(cache, p.Type)) { continue; }
            if (IsAnyStruct(cache, p.Type)) { continue; }
            if (p.Type.IsString()) { continue; }
            if (IsRuntimeClassOrInterface(cache, p.Type)) { continue; }
            if (p.Type.IsObject()) { continue; }
            if (p.Type.IsGenericInstance()) { continue; }
            if (IsComplexStruct(cache, p.Type)) { continue; }
            return false;
        }
        return true;
    }

    /// <summary>True if the interface has at least one non-special method, property, or non-skipped event.</summary>
    internal static bool HasEmittableMembers(TypeDefinition iface, bool skipExclusiveEvents)
    {
        foreach (MethodDefinition m in iface.Methods)
        {
            if (!m.IsSpecial()) { return true; }
        }
        foreach (PropertyDefinition _ in iface.Properties) { return true; }
        if (!skipExclusiveEvents)
        {
            foreach (EventDefinition _ in iface.Events) { return true; }
        }
        return false;
    }

    /// <summary>Returns the number of methods (including special accessors) on the interface.</summary>
    internal static int CountMethods(TypeDefinition iface)
    {
        int count = 0;
        foreach (MethodDefinition _ in iface.Methods) { count++; }
        return count;
    }

    /// <summary>Returns the number of base classes between <paramref name="classType"/> and <see cref="object"/>.</summary>
    internal static int GetClassHierarchyIndex(MetadataCache cache, TypeDefinition classType)
    {
        if (classType.BaseType is null) { return 0; }
        (string ns, string nm) = classType.BaseType.Names();
        if (ns == "System" && nm == "Object") { return 0; }
        TypeDefinition? baseDef = classType.BaseType as TypeDefinition;
        if (baseDef is null)
        {
            try { baseDef = classType.BaseType.Resolve(cache.RuntimeContext); }
            catch { baseDef = null; }
            baseDef ??= cache.Find(string.IsNullOrEmpty(ns) ? nm : (ns + "." + nm));
        }
        if (baseDef is null) { return 0; }
        return GetClassHierarchyIndex(cache, baseDef) + 1;
    }

    internal static bool InterfacesEqualByName(TypeDefinition a, TypeDefinition b)
    {
        if (a == b) { return true; }
        return (a.Namespace?.Value ?? string.Empty) == (b.Namespace?.Value ?? string.Empty)
            && (a.Name?.Value ?? string.Empty) == (b.Name?.Value ?? string.Empty);
    }

    /// <summary>Strips <c>ByReferenceTypeSignature</c> and <c>CustomModifierTypeSignature</c> wrappers
    /// to get the underlying type signature.</summary>
    internal static TypeSignature StripByRefAndCustomModifiers(TypeSignature sig)
    {
        TypeSignature current = sig;
        while (true)
        {
            if (current is ByReferenceTypeSignature br) { current = br.BaseType; continue; }
            if (current is CustomModifierTypeSignature cm) { current = cm.BaseType; continue; }
            return current;
        }
    }

    internal static string GetParamName(ParameterInfo p, string? paramNameOverride)
    {
        string name = paramNameOverride ?? p.Parameter.Name ?? "param";
        return CSharpKeywords.IsKeyword(name) ? "@" + name : name;
    }

    internal static string GetParamLocalName(ParameterInfo p, string? paramNameOverride)
    {
        // For local helper variables (e.g. __<name>), strip the @ escape since `__event` is valid.
        return paramNameOverride ?? p.Parameter.Name ?? "param";
    }

}
