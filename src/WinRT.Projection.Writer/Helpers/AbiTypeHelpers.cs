// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Extensions;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// ABI emission helpers (structs, enums, delegates, interfaces, classes).
/// Mirrors the C++ <c>write_abi_*</c> family. Initial port: emits the foundational
/// ABI scaffolding only; full marshaller/vtable emission to be filled in later.
/// </summary>
internal static class AbiTypeHelpers
{
    /// <summary>Returns whether the given type can be passed across the ABI boundary without per-field marshalling (struct layout matches the ABI representation).</summary>
    public static bool IsTypeBlittable(MetadataCache cache, TypeDefinition type)
    {
        TypeCategory cat = TypeCategorization.GetCategory(type);
        if (cat == TypeCategory.Enum) { return true; }
        if (cat != TypeCategory.Struct) { return false; }
        // struct itself has a mapped-type entry, return based on its RequiresMarshaling flag
        // BEFORE walking fields. This is critical for XAML structs like Duration / KeyTime /
        // RepeatBehavior which are self-mapped with RequiresMarshaling=false but have a
        // TimeSpan field (Windows.Foundation.TimeSpan -> System.TimeSpan with RequiresMarshaling=true).
        // Without this check, the field walk would incorrectly classify them as non-blittable.
        (string ns, string name) = type.Names();
        if (MappedTypes.Get(ns, name) is { } mapping)
        {
            return !mapping.RequiresMarshaling;
        }
        // Walk fields - all must be blittable
        foreach (FieldDefinition field in type.Fields)
        {
            if (field.IsStatic || field.Signature is null) { continue; }
            if (!IsFieldTypeBlittable(cache, field.Signature.FieldType)) { return false; }
        }
        return true;
    }

    internal static bool IsFieldTypeBlittable(MetadataCache cache, AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (sig is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib)
        {
            //   return (type != fundamental_type::String);
            // i.e. ALL fundamentals (including Boolean, Char) are considered blittable here;
            // only String is non-blittable. Object isn't a fundamental in C++; handled below.
            return corlib.ElementType switch
            {
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.String => false,
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Object => false,
                _ => true
            };
        }
        // For TypeRef/TypeDef, resolve and check blittability.
        if (sig is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature todr)
        {
            string fNs = todr.Type?.Namespace?.Value ?? string.Empty;
            string fName = todr.Type?.Name?.Value ?? string.Empty;
            // System.Guid is a fundamental blittable type .
            // Same applies to System.IntPtr / UIntPtr (used in some struct layouts).
            if (fNs == "System" && (fName == "Guid" || fName == "IntPtr" || fName == "UIntPtr"))
            {
                return true;
            }
            // Mapped struct types: blittable iff the mapping does NOT require marshalling
            MappedType? mapped = MappedTypes.Get(fNs, fName);
            if (mapped is not null && mapped.RequiresMarshaling) { return false; }
            if (todr.Type is TypeDefinition td)
            {
                return IsTypeBlittable(cache, td);
            }
            // Cross-module: try metadata cache.
            if (todr.Type is TypeReference tr)
            {
                (string ns, string name) = tr.Names();
                TypeDefinition? resolved = cache.Find(ns + "." + name);
                if (resolved is not null) { return IsTypeBlittable(cache, resolved); }
            }
            return false;
        }
        return false;
    }

    /// <summary>
    /// Resolves a <see cref="AsmResolver.DotNet.Signatures.TypeDefOrRefSignature"/> to its
    /// <see cref="TypeDefinition"/>, handling both in-assembly (already a TypeDefinition) and
    /// cross-assembly/TypeRef-row references via the metadata cache. Returns <c>null</c> when
    /// the reference cannot be resolved.
    /// </summary>
    internal static TypeDefinition? TryResolveStructTypeDef(MetadataCache cache, AsmResolver.DotNet.Signatures.TypeDefOrRefSignature tdr)
    {
        if (tdr.Type is TypeDefinition td) { return td; }
        if (tdr.Type is TypeReference tr)
        {
            (string ns, string name) = tr.Names();
            return cache.Find(ns + "." + name);
        }
        return null;
    }

    /// <summary>Returns the (possibly mapped) namespace of a type signature, or 'System' for fundamentals.</summary>
    internal static string GetMappedNamespace(AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        // Fundamentals (string, bool, int, etc.) live in 'System' for ArrayMarshaller path purposes.
        if (sig is AsmResolver.DotNet.Signatures.CorLibTypeSignature) { return "System"; }
        AsmResolver.DotNet.ITypeDefOrRef? td = null;
        if (sig is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature tds) { td = tds.Type; }
        else if (sig is AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature gi) { td = gi.GenericType; }
        if (td is null) { return string.Empty; }
        (string typeNs, string typeName) = td.Names();
        MappedType? mapped = MappedTypes.Get(typeNs, typeName);
        return mapped is not null ? mapped.MappedNamespace : typeNs;
    }

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
                AsmResolver.DotNet.Signatures.CustomAttributeArgument arg = attr.Signature.FixedArguments[j];
                if (arg.Element is AsmResolver.DotNet.Signatures.TypeSignature sig)
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
        if (ifaceRef is TypeSpecification ts && ts.Signature is AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature gi)
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
        return (method.Name?.Value ?? string.Empty) + "_" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Returns the metadata-derived name for the return parameter, or the C++ default <c>__return_value__</c>.
    /// Mirrors <c>method_signature::return_param_name()</c> in <c>helpers.h</c>.
    /// </summary>
    internal static string GetReturnParamName(MethodSig sig)
    {
        string? n = sig.ReturnParam?.Name?.Value;
        if (string.IsNullOrEmpty(n)) { return "__return_value__"; }
        return CSharpKeywords.IsKeyword(n) ? "@" + n : n;
    }

    /// <summary>
    /// Returns the local-variable name for the return parameter on the server side.
    /// <c>abi_marshaler::get_marshaler_local()</c> which prefixes <c>__</c> to the param name.
    /// </summary>
    internal static string GetReturnLocalName(MethodSig sig)
    {
        return "__" + GetReturnParamName(sig);
    }

    /// <summary>Returns '__&lt;returnName&gt;Size' (matches C++ '__%Size' convention) — by default '____return_value__Size' for the standard '__return_value__' return param.</summary>
    internal static string GetReturnSizeParamName(MethodSig sig)
    {
        return "__" + GetReturnParamName(sig) + "Size";
    }

    /// <summary>Build a method-to-event map for add/remove accessors of a type.</summary>
    internal static System.Collections.Generic.Dictionary<MethodDefinition, EventDefinition>? BuildEventMethodMap(TypeDefinition type)
    {
        if (type.Events.Count == 0) { return null; }
        System.Collections.Generic.Dictionary<MethodDefinition, EventDefinition> map = new();
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
    internal static bool IsDelegateInvokeNativeSupported(MetadataCache cache, MethodSig sig)
    {
        AsmResolver.DotNet.Signatures.TypeSignature? rt = sig.ReturnType;
        if (rt is not null)
        {
            if (rt.IsHResultException()) { return false; }
            if (!(IsBlittablePrimitive(cache, rt) || IsAnyStruct(cache, rt) || rt.IsString() || IsRuntimeClassOrInterface(cache, rt) || rt.IsObject() || rt.IsGenericInstance() || IsComplexStruct(cache, rt))) { return false; }
        }
        foreach (ParamInfo p in sig.Params)
        {
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
            {
                if (p.Type is AsmResolver.DotNet.Signatures.SzArrayTypeSignature szP)
                {
                    if (IsBlittablePrimitive(cache, szP.BaseType)) { continue; }
                    if (IsAnyStruct(cache, szP.BaseType)) { continue; }
                }
                return false;
            }
            if (cat != ParamCategory.In) { return false; }
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

    /// <summary>True if the type signature is a Nullable&lt;T&gt; where T is a primitive
    /// supported by an ABI.System.&lt;T&gt;Marshaller (e.g. UInt64Marshaller, Int32Marshaller, etc.).
    /// Returns the fully-qualified marshaller name in <paramref name="marshallerName"/>.</summary>
    internal static bool TryGetNullablePrimitiveMarshallerName(AsmResolver.DotNet.Signatures.TypeSignature sig, out string? marshallerName)
    {
        marshallerName = null;
        if (sig is not AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature gi) { return false; }
        AsmResolver.DotNet.ITypeDefOrRef gt = gi.GenericType;
        string ns = gt?.Namespace?.Value ?? string.Empty;
        string name = gt?.Name?.Value ?? string.Empty;
        // In WinMD metadata, Nullable<T> is encoded as Windows.Foundation.IReference<T>.
        // It only later gets projected to System.Nullable<T> by the projection layer.
        bool isNullable = (ns == "System" && name == "Nullable`1")
            || (ns == "Windows.Foundation" && name == "IReference`1");
        if (!isNullable) { return false; }
        if (gi.TypeArguments.Count != 1) { return false; }
        AsmResolver.DotNet.Signatures.TypeSignature arg = gi.TypeArguments[0];
        // Map primitive corlib element type to its ABI marshaller name.
        if (arg is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib)
        {
            string? mn = corlib.ElementType switch
            {
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean => "Boolean",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char => "Char",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I1 => "SByte",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U1 => "Byte",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I2 => "Int16",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U2 => "UInt16",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I4 => "Int32",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U4 => "UInt32",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I8 => "Int64",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U8 => "UInt64",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.R4 => "Single",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.R8 => "Double",
                _ => null
            };
            if (mn is null) { return false; }
            marshallerName = "ABI.System." + mn + "Marshaller";
            return true;
        }
        return false;
    }

    /// <summary>
    /// True if the type is a mapped value type that requires marshalling between projected and ABI
    /// representations (e.g. Windows.Foundation.DateTime &lt;-&gt; System.DateTimeOffset,
    /// Windows.Foundation.TimeSpan &lt;-&gt; System.TimeSpan, Windows.Foundation.HResult &lt;-&gt; System.Exception).
    /// These types use 'global::ABI.&lt;MappedNamespace&gt;.&lt;MappedName&gt;' as their ABI representation
    /// and need an explicit marshaller call ('global::ABI.&lt;MappedNamespace&gt;.&lt;MappedName&gt;Marshaller.ConvertToUnmanaged'/
    /// 'ConvertToManaged') to convert values across the boundary.
    /// </summary>
    private static bool IsMappedMarshalingValueType(AsmResolver.DotNet.Signatures.TypeSignature sig, out string mappedNs, out string mappedName)
    {
        mappedNs = string.Empty;
        mappedName = string.Empty;
        AsmResolver.DotNet.ITypeDefOrRef? td = null;
        if (sig is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature tds) { td = tds.Type; }
        if (td is null) { return false; }
        (string ns, string name) = td.Names();
        // The set of mapped types that use the 'value-type marshaller' pattern (DateTime, TimeSpan, HResult).
        // Uri is also a mapped marshalling type but it's a reference type (handled via UriMarshaller separately).
        if (ns == "Windows.Foundation")
        {
            if (name == "DateTime") { mappedNs = "System"; mappedName = "DateTimeOffset"; return true; }
            if (name == "TimeSpan") { mappedNs = "System"; mappedName = "TimeSpan"; return true; }
            if (name == "HResult") { mappedNs = "System"; mappedName = "Exception"; return true; }
        }
        return false;
    }

    /// <summary>True if the type is a mapped value type that needs ABI marshalling (excluding HResult, handled separately).</summary>
    internal static bool IsMappedAbiValueType(AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (!IsMappedMarshalingValueType(sig, out _, out string mappedName)) { return false; }
        // HResult/Exception is treated specially in many places; this helper is for DateTime/TimeSpan only.
        return mappedName != "Exception";
    }

    /// <summary>Returns the ABI type name for a mapped value type (e.g. 'global::ABI.System.TimeSpan').</summary>
    internal static string GetMappedAbiTypeName(AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (!IsMappedMarshalingValueType(sig, out string ns, out string name)) { return string.Empty; }
        return "global::ABI." + ns + "." + name;
    }

    /// <summary>Returns the marshaller class name for a mapped value type (e.g. 'global::ABI.System.TimeSpanMarshaller').</summary>
    internal static string GetMappedMarshallerName(AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (!IsMappedMarshalingValueType(sig, out string ns, out string name)) { return string.Empty; }
        return "global::ABI." + ns + "." + name + "Marshaller";
    }

    /// <summary>True if the type signature represents an enum (resolves cross-module typerefs).</summary>
    internal static bool IsEnumType(MetadataCache cache, AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (sig is not AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td) { return false; }
        if (td.Type is TypeDefinition def)
        {
            return TypeCategorization.GetCategory(def) == TypeCategory.Enum;
        }
        if (td.Type is TypeReference tr)
        {
            (string ns, string name) = tr.Names();
            TypeDefinition? resolved = cache.Find(ns + "." + name);
            return resolved is not null && TypeCategorization.GetCategory(resolved) == TypeCategory.Enum;
        }
        return false;
    }

    /// <summary>Returns the marshaller name for the inner type T of <c>Nullable&lt;T&gt;</c>.
    /// Mirrors the truth pattern: e.g. for <c>Nullable&lt;DateTimeOffset&gt;</c> returns
    /// <c>global::ABI.System.DateTimeOffsetMarshaller</c>; for primitives like <c>Nullable&lt;int&gt;</c>
    /// returns <c>global::ABI.System.Int32Marshaller</c>.</summary>
    internal static string GetNullableInnerMarshallerName(IndentedTextWriter writer, ProjectionEmitContext context, AsmResolver.DotNet.Signatures.TypeSignature innerType)
    {
        // Primitives (Int32, Int64, Boolean, etc.) live in ABI.System with the canonical .NET name.
        if (innerType is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib)
        {
            string typeName = corlib.ElementType switch
            {
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean => "Boolean",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char => "Char",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I1 => "SByte",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U1 => "Byte",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I2 => "Int16",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U2 => "UInt16",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I4 => "Int32",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U4 => "UInt32",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I8 => "Int64",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U8 => "UInt64",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.R4 => "Single",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.R8 => "Double",
                _ => "",
            };
            if (!string.IsNullOrEmpty(typeName))
            {
                return "global::ABI.System." + typeName + "Marshaller";
            }
        }
        // For non-primitive types (DateTimeOffset, TimeSpan, struct/enum types), use GetMarshallerFullName.
        return GetMarshallerFullName(writer, context, innerType);
    }

    /// <summary>Strips <c>ByReferenceTypeSignature</c> and <c>CustomModifierTypeSignature</c> wrappers
    /// to get the underlying type signature.</summary>
    internal static AsmResolver.DotNet.Signatures.TypeSignature StripByRefAndCustomModifiers(AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        AsmResolver.DotNet.Signatures.TypeSignature current = sig;
        while (true)
        {
            if (current is AsmResolver.DotNet.Signatures.ByReferenceTypeSignature br) { current = br.BaseType; continue; }
            if (current is AsmResolver.DotNet.Signatures.CustomModifierTypeSignature cm) { current = cm.BaseType; continue; }
            return current;
        }
    }

    /// <summary>True if the type signature represents a WinRT runtime class, interface, or delegate (reference type marshallable via *Marshaller).</summary>
    internal static bool IsRuntimeClassOrInterface(MetadataCache cache, AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (sig is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td)
        {
            // Same-module: use the resolved category directly.
            if (td.Type is TypeDefinition def)
            {
                TypeCategory cat = TypeCategorization.GetCategory(def);
                return cat is TypeCategory.Class or TypeCategory.Interface or TypeCategory.Delegate;
            }
            // Cross-module typeref: try to resolve via the metadata cache to check category.
            string ns = td.Type?.Namespace?.Value ?? string.Empty;
            string name = td.Type?.Name?.Value ?? string.Empty;
            if (ns == "System")
            {
                return name switch
                {
                    "Uri" or "Type" or "IDisposable" or "Exception" => true,
                    _ => false,
                };
            }
            if (cache is not null)
            {
                TypeDefinition? resolved = cache.Find(ns + "." + name);
                if (resolved is not null)
                {
                    TypeCategory cat = TypeCategorization.GetCategory(resolved);
                    return cat is TypeCategory.Class or TypeCategory.Interface or TypeCategory.Delegate;
                }
            }
            // Unresolved cross-assembly TypeRef (e.g. a referenced winmd we don't have loaded).
            // Fall back to the signature's encoding: WinRT metadata distinguishes value types
            // (encoded as ValueType) from reference types (encoded as Class). If the signature
            // has IsValueType == false, then it MUST be one of class/interface/delegate (since
            // primitives/enums/strings/object are encoded with their own element type). This
            // mirrors how the original code's abi_marshaler abstraction handles unknown types — it
            // dispatches based on the metadata semantics, not on resolution.
            return !td.IsValueType;
        }
        return false;
    }

    /// <summary>Returns the full marshaller name (e.g. <c>global::ABI.Windows.Foundation.UriMarshaller</c>).
    /// When the marshaller would land in the writer's current ABI namespace, returns just the
    /// short marshaller class name (e.g. <c>BasicStructMarshaller</c>) —.
    /// elides the qualifier in same-namespace contexts.</summary>
    internal static string GetMarshallerFullName(IndentedTextWriter writer, ProjectionEmitContext context, AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (sig is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td)
        {
            string ns = td.Type?.Namespace?.Value ?? string.Empty;
            string name = td.Type?.Name?.Value ?? string.Empty;
            // Apply mapped type remapping (e.g. System.Uri -> Windows.Foundation.Uri)
            MappedType? mapped = MappedTypes.Get(ns, name);
            if (mapped is not null)
            {
                ns = mapped.MappedNamespace;
                name = mapped.MappedName;
            }
            string nameStripped = IdentifierEscaping.StripBackticks(name);
            // If the writer is currently in the matching ABI namespace, drop the qualifier.
            if (context.InAbiNamespace && string.Equals(context.CurrentNamespace, ns, System.StringComparison.Ordinal))
            {
                return nameStripped + "Marshaller";
            }
            return "global::ABI." + ns + "." + nameStripped + "Marshaller";
        }
        return "global::ABI.Object.Marshaller";
    }

    internal static string GetParamName(ParamInfo p, string? paramNameOverride)
    {
        string name = paramNameOverride ?? p.Parameter.Name ?? "param";
        return CSharpKeywords.IsKeyword(name) ? "@" + name : name;
    }

    internal static string GetParamLocalName(ParamInfo p, string? paramNameOverride)
    {
        // For local helper variables (e.g. __<name>), strip the @ escape since `__event` is valid.
        return paramNameOverride ?? p.Parameter.Name ?? "param";
    }

    /// <summary>True if the type is a blittable primitive (or enum) directly representable
    /// at the ABI: bool/byte/sbyte/short/ushort/int/uint/long/ulong/float/double/char and enums.</summary>
    internal static bool IsBlittablePrimitive(MetadataCache cache, AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (sig is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib)
        {
            return corlib.ElementType is
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean or
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I1 or
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U1 or
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I2 or
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U2 or
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I4 or
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U4 or
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I8 or
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U8 or
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.R4 or
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.R8 or
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char;
        }
        // Enum (TypeDefOrRef-based value type with non-Object base) - same module or cross-module
        if (sig is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td)
        {
            if (td.Type is TypeDefinition def && TypeCategorization.GetCategory(def) == TypeCategory.Enum)
            {
                return true;
            }
            // Cross-module enum: try to resolve via the metadata cache.
            if (td.Type is TypeReference tr)
            {
                (string ns, string name) = tr.Names();
                TypeDefinition? resolved = cache.Find(ns + "." + name);
                if (resolved is not null && TypeCategorization.GetCategory(resolved) == TypeCategory.Enum)
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>True for any struct type that can be passed directly across the WinRT ABI
    /// (no per-field marshalling required). This includes blittable structs and "almost-blittable"
    /// structs that have only primitive fields like bool/char (whose C# layout matches the WinRT ABI).
    /// Excludes structs with reference type fields (string/object/runtime classes/etc.).</summary>
    /// <summary>True for structs that have at least one reference type field (string, generic
    /// instance Nullable&lt;T&gt;, etc.). These need per-field marshalling via the *Marshaller class
    /// (ConvertToUnmanaged/ConvertToManaged/Dispose).</summary>
    internal static bool IsComplexStruct(MetadataCache cache, AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (sig is not AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td) { return false; }
        TypeDefinition? def = td.Type as TypeDefinition;
        if (def is null && td.Type is TypeReference tr)
        {
            (string ns, string name) = tr.Names();
            if (ns == "System" && name == "Guid") { return false; }
            def = cache.Find(ns + "." + name);
        }
        if (def is null) { return false; }
        TypeCategory cat = TypeCategorization.GetCategory(def);
        if (cat != TypeCategory.Struct) { return false; }
        // RequiresMarshaling, regardless of inner field layout. So for mapped types like
        // Duration, KeyTime, RepeatBehavior (RequiresMarshaling=false), they're never "complex".
        {
            string sNs = td.Type?.Namespace?.Value ?? string.Empty;
            string sName = td.Type?.Name?.Value ?? string.Empty;
            MappedType? sMapped = MappedTypes.Get(sNs, sName);
            if (sMapped is not null) { return false; }
        }
        // A struct is "complex" if it has any field that is not a blittable primitive nor an
        // almost-blittable struct (i.e. has a string/object/Nullable<T>/etc. field).
        foreach (FieldDefinition field in def.Fields)
        {
            if (field.IsStatic || field.Signature is null) { continue; }
            AsmResolver.DotNet.Signatures.TypeSignature ft = field.Signature.FieldType;
            if (IsBlittablePrimitive(cache, ft)) { continue; }
            if (IsAnyStruct(cache, ft)) { continue; }
            return true;
        }
        return false;
    }

    internal static bool IsAnyStruct(MetadataCache cache, AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (sig is not AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td) { return false; }
        TypeDefinition? def = td.Type as TypeDefinition;
        if (def is null && td.Type is TypeReference trEarly)
        {
            (string ns, string name) = trEarly.Names();
            if (ns == "System" && name == "Guid") { return true; }
            def = cache.Find(ns + "." + name);
        }
        if (def is null) { return false; }
        // Special case: mapped struct types short-circuit based on RequiresMarshaling, mirroring
        // C++ is_type_blittable: 'auto mapping = get_mapped_type(...); return !mapping->requires_marshaling'.
        // Only applies to actual structs (not mapped interfaces like IAsyncAction).
        if (TypeCategorization.GetCategory(def) == TypeCategory.Struct)
        {
            string sNs = td.Type?.Namespace?.Value ?? string.Empty;
            string sName = td.Type?.Name?.Value ?? string.Empty;
            MappedType? sMapped = MappedTypes.Get(sNs, sName);
            if (sMapped is not null) { return !sMapped.RequiresMarshaling; }
        }
        TypeCategory cat = TypeCategorization.GetCategory(def);
        if (cat != TypeCategory.Struct) { return false; }
        // Reject if any instance field is a reference type (string/object/runtime class/etc.).
        foreach (FieldDefinition field in def.Fields)
        {
            if (field.IsStatic || field.Signature is null) { continue; }
            AsmResolver.DotNet.Signatures.TypeSignature ft = field.Signature.FieldType;
            if (ft is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibField)
            {
                if (corlibField.ElementType is
                    AsmResolver.PE.DotNet.Metadata.Tables.ElementType.String or
                    AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Object)
                { return false; }
                continue;
            }
            // Recurse: nested struct must also pass IsAnyStruct, otherwise reject.
            if (IsBlittablePrimitive(cache, ft)) { continue; }
            if (IsAnyStruct(cache, ft)) { continue; }
            return false;
        }
        return true;
    }

    /// <summary>Returns the ABI type name for a blittable struct (the projected type name).</summary>
    internal static string GetBlittableStructAbiType(IndentedTextWriter writer, ProjectionEmitContext context, AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        _ = writer;
        // Mapped value types (DateTime/TimeSpan) use the ABI type, not the projected type.
        if (IsMappedAbiValueType(sig)) { return GetMappedAbiTypeName(sig); }
        IndentedTextWriter __scratchProj = new();
        MethodFactory.WriteProjectedSignature(__scratchProj, context, sig, false);
        return __scratchProj.ToString();
    }

    /// <summary>Returns the ABI struct type name for a complex struct (e.g. global::ABI.Windows.Web.Http.HttpProgress).
    /// When the writer is currently in the matching ABI namespace, returns just the
    /// short type name (e.g. <c>HttpProgress</c>) to mirror the original code which uses the
    /// unqualified name in same-namespace contexts.</summary>
    internal static string GetAbiStructTypeName(IndentedTextWriter writer, ProjectionEmitContext context, AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (sig is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td)
        {
            string ns = td.Type?.Namespace?.Value ?? string.Empty;
            string name = td.Type?.Name?.Value ?? string.Empty;
            // If this struct is mapped, use the mapped namespace+name (e.g.
            // 'Windows.UI.Xaml.Interop.TypeName' is mapped to 'System.Type', so the ABI struct
            // is 'global::ABI.System.Type', not 'global::ABI.Windows.UI.Xaml.Interop.TypeName').
            MappedType? mapped = MappedTypes.Get(ns, name);
            if (mapped is not null)
            {
                ns = mapped.MappedNamespace;
                name = mapped.MappedName;
            }
            string nameStripped = IdentifierEscaping.StripBackticks(name);
            // If the writer is currently in the matching ABI namespace, drop the qualifier.
            if (context.InAbiNamespace && string.Equals(context.CurrentNamespace, ns, System.StringComparison.Ordinal))
            {
                return nameStripped;
            }
            return "global::ABI." + ns + "." + nameStripped;
        }
        return "global::ABI.Object";
    }

    internal static string GetAbiPrimitiveType(MetadataCache cache, AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        if (sig is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib)
        {
            return corlib.ElementType switch
            {
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean => "bool",
                AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char => "char",
                _ => GetAbiFundamentalTypeFromCorLib(corlib.ElementType),
            };
        }
        // Enum: use the projected enum type as the ABI signature (truth pattern).
        if (sig is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td)
        {
            TypeDefinition? def = td.Type as TypeDefinition;
            if (def is null && td.Type is TypeReference tr)
            {
                (string ns, string name) = tr.Names();
                def = cache.Find(ns + "." + name);
            }
            if (def is not null && TypeCategorization.GetCategory(def) == TypeCategory.Enum)
            {
                return cache is null ? "int" : GetProjectedEnumName(def);
            }
        }
        return "int";
    }

    private static string GetProjectedEnumName(TypeDefinition def)
    {
        (string ns, string name) = def.Names();
        // Apply mapped-type translation so consumers see the projected (.NET) enum name
        // (e.g. Windows.UI.Xaml.Interop.NotifyCollectionChangedAction →
        // System.Collections.Specialized.NotifyCollectionChangedAction). Mirrors the same
        // remapping that WriteTypedefName performs.
        MappedType? mapped = MappedTypes.Get(ns, name);
        if (mapped is not null)
        {
            ns = mapped.MappedNamespace;
            name = mapped.MappedName;
        }
        return string.IsNullOrEmpty(ns) ? "global::" + name : "global::" + ns + "." + name;
    }

    private static string GetAbiFundamentalTypeFromCorLib(AsmResolver.PE.DotNet.Metadata.Tables.ElementType et)
    {
        return et switch
        {
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I1 => "sbyte",
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U1 => "byte",
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I2 => "short",
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U2 => "ushort",
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I4 => "int",
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U4 => "uint",
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I8 => "long",
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U8 => "ulong",
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.R4 => "float",
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.R8 => "double",
            _ => "int",
        };
    }
}
