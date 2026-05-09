// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Extensions;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// ABI emission helpers (structs, enums, delegates, interfaces, classes).
/// Mirrors the C++ <c>write_abi_*</c> family. Initial port: emits the foundational
/// ABI scaffolding only; full marshaller/vtable emission to be filled in later.
/// </summary>
internal static partial class CodeWriters
{
    /// <summary>Mirrors C++ <c>is_type_blittable</c> partially.</summary>
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
            // System.Guid is a fundamental blittable type (mirrors C++ guid_type which falls
            // through to the [&](auto&&) catch-all returning true in is_type_blittable).
            // Same applies to System.IntPtr / UIntPtr (used in some struct layouts).
            if (fNs == "System" && (fName == "Guid" || fName == "IntPtr" || fName == "UIntPtr"))
            {
                return true;
            }
            // Mapped struct types: blittable iff the mapping does NOT require marshalling
            // (mirrors C++ is_type_blittable for mapped struct_type case).
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

    /// <summary>
    /// Returns the interop assembly path for an array marshaller of a given element type.
    /// The interop generator names array marshallers <c>ABI.&lt;typeNamespace&gt;.&lt;&lt;assembly&gt;ElementName&gt;ArrayMarshaller</c>
    /// (typeNamespace prefix outside the brackets, and the element inside the brackets uses just the
    /// type name without its namespace because depth=0 in the interop generator's AppendRawTypeName).
    /// </summary>
    private static string GetArrayMarshallerInteropPath(AsmResolver.DotNet.Signatures.TypeSignature elementType)
    {
        // The 'encodedElement' passed in uses the depth>0 form (assembly + hyphenated namespace + name),
        // but inside the array brackets the interop generator uses the depth=0 form (assembly + just name).
        // Re-encode the element with the top-level form for accurate matching.
        string topLevelElement = EncodeArrayElementName(elementType);
        // Resolve the element's namespace to determine the path prefix.
        string ns = GetMappedNamespace(elementType);
        if (string.IsNullOrEmpty(ns))
        {
            return "ABI.<" + topLevelElement + ">ArrayMarshaller, WinRT.Interop";
        }
        return "ABI." + ns + ".<" + topLevelElement + ">ArrayMarshaller, WinRT.Interop";
    }

    /// <summary>Returns the (possibly mapped) namespace of a type signature, or 'System' for fundamentals.</summary>
    private static string GetMappedNamespace(AsmResolver.DotNet.Signatures.TypeSignature sig)
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
    /// Encodes the array element type name as the interop generator's AppendRawTypeName at depth=0:
    /// fundamentals use their short C# name; typedefs use just the type name (no namespace) prefixed
    /// with the assembly marker; generic instances include their assembly marker, name, and type arguments.
    /// </summary>
    private static string EncodeArrayElementName(AsmResolver.DotNet.Signatures.TypeSignature elementType)
    {
        System.Text.StringBuilder sb = new();
        EncodeArrayElementNameInto(sb, elementType);
        return sb.ToString();
    }

    private static void EncodeArrayElementNameInto(System.Text.StringBuilder sb, AsmResolver.DotNet.Signatures.TypeSignature sig)
    {
        // Special case for System.Guid: matches C++ guid_type handler in write_interop_dll_type_name.
        // The depth=0 (top-level array element) form drops the namespace prefix and uses just the
        // assembly marker + type name, so for Guid this becomes "<#corlib>Guid".
        if (sig is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature gtd
            && gtd.Type?.Namespace?.Value == "System"
            && gtd.Type?.Name?.Value == "Guid")
        {
            sb.Append("<#corlib>Guid");
            return;
        }
        switch (sig)
        {
            case AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib:
                EncodeFundamental(sb, corlib, TypedefNameType.Projected);
                return;
            case AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td:
                EncodeArrayElementForTypeDef(sb, td.Type, generic_args: null);
                return;
            case AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature gi:
                EncodeArrayElementForTypeDef(sb, gi.GenericType, generic_args: gi.TypeArguments);
                return;
            default:
                sb.Append(sig.FullName);
                return;
        }
    }

    private static void EncodeArrayElementForTypeDef(System.Text.StringBuilder sb, AsmResolver.DotNet.ITypeDefOrRef type, System.Collections.Generic.IList<AsmResolver.DotNet.Signatures.TypeSignature>? generic_args)
    {
        (string typeNs, string typeName) = type.Names();
        // Apply mapped-type remapping (e.g. Windows.Foundation.IReference -> System.Nullable).
        MappedType? mapped = MappedTypes.Get(typeNs, typeName);
        if (mapped is not null)
        {
            typeNs = mapped.MappedNamespace;
            typeName = mapped.MappedName;
        }
        // Replace generic arity backtick with apostrophe.
        typeName = typeName.Replace('`', '\'');

        // Assembly marker prefix. Pass the type so that third-party (e.g. component-authored)
        // types resolve to their actual assembly name (e.g. <AuthoringTest>) instead of
        // defaulting to <#Windows>.
        sb.Append(GetInteropAssemblyMarker(typeNs, typeName, mapped, type));
        // Top-level: just the type name (no namespace).
        sb.Append(typeName);

        // Generic arguments use the standard EncodeInteropTypeNameInto (depth > 0).
        if (generic_args is { Count: > 0 })
        {
            sb.Append('<');
            for (int i = 0; i < generic_args.Count; i++)
            {
                if (i > 0) { sb.Append('|'); }
                EncodeInteropTypeNameInto(sb, generic_args[i], TypedefNameType.Projected);
            }
            sb.Append('>');
        }
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
    /// Returns the local-variable name for the return parameter on the server side. Mirrors C++
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

    /// <summary>
    /// Emits a real Do_Abi (CCW) body for the cases we can handle. Mirrors C++
    /// <c>write_abi_method_call_marshalers</c> (<c>code_writers.h:6682</c>) which
    /// unconditionally emits a real body via the <c>abi_marshaler</c> abstraction
    /// for every WinRT-valid signature.
    /// </summary>
    internal static void EmitDoAbiBodyIfSimple(IndentedTextWriter writer, ProjectionEmitContext context, MethodSig sig, string ifaceFullName, string methodName)
    {
        AsmResolver.DotNet.Signatures.TypeSignature? rt = sig.ReturnType;

        // String params drive whether we need HString header allocation in the body.
        bool hasStringParams = false;
        foreach (ParamInfo p in sig.Params)
        {
            if (p.Type.IsString()) { hasStringParams = true; break; }
        }
        bool returnIsReceiveArrayDoAbi = rt is AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSzAbi
            && (IsBlittablePrimitive(context.Cache, retSzAbi.BaseType) || IsAnyStruct(context.Cache, retSzAbi.BaseType)
                || retSzAbi.BaseType.IsString() || IsRuntimeClassOrInterface(context.Cache, retSzAbi.BaseType) || retSzAbi.BaseType.IsObject()
                || IsComplexStruct(context.Cache, retSzAbi.BaseType));
        bool returnIsHResultExceptionDoAbi = rt is not null && rt.IsHResultException();
        bool returnIsString = rt is not null && rt.IsString();
        bool returnIsRefType = rt is not null && (IsRuntimeClassOrInterface(context.Cache, rt) || rt.IsObject() || rt.IsGenericInstance());
        bool returnIsGenericInstance = rt is not null && rt.IsGenericInstance();
        bool returnIsBlittableStruct = rt is not null && IsAnyStruct(context.Cache, rt);

        bool isGetter = methodName.StartsWith("get_", System.StringComparison.Ordinal);
        bool isSetter = methodName.StartsWith("put_", System.StringComparison.Ordinal);
        bool isAddEvent = methodName.StartsWith("add_", System.StringComparison.Ordinal);
        bool isRemoveEvent = methodName.StartsWith("remove_", System.StringComparison.Ordinal);

        if (isAddEvent || isRemoveEvent)
        {
            // Events go through dedicated EmitDoAbiAddEvent / EmitDoAbiRemoveEvent paths
            // upstream (see lines 1153-1159). If we reach here for an event accessor it's a
            // generator bug. Defensive guard against future regressions.
            throw new System.InvalidOperationException(
                $"EmitDoAbiBodyIfSimple: unexpectedly called for event accessor '{methodName}' " +
                $"on '{ifaceFullName}'. Events should dispatch through EmitDoAbiAddEvent / EmitDoAbiRemoveEvent.");
        }

        writer.Write("\n{\n");
        string retParamName = GetReturnParamName(sig);
        string retSizeParamName = GetReturnSizeParamName(sig);
        // The local name for the unmarshalled return value mirrors C++
        // 'abi_marshaler::get_marshaler_local()' which prefixes '__' to the param name.
        // For the default '__return_value__' param this becomes '____return_value__'.
        string retLocalName = "__" + retParamName;
        // at the TOP of the method body (before local declarations and the try block). The
        // actual call sites later in the body just reference the already-declared accessor.
        // For a generic-instance return type, the accessor is named ConvertToUnmanaged_<retParamName>.
        // Skip Nullable<T> returns: those use <T>Marshaller.BoxToUnmanaged at the call site
        // instead of the generic-instance UnsafeAccessor (V3-M7).
        if (returnIsGenericInstance && !(rt is not null && rt.IsNullableT()))
        {
            string interopTypeName = EncodeInteropTypeName(rt!, TypedefNameType.ABI) + ", WinRT.Interop";
            IndentedTextWriter __scratchProjectedTypeName = new();
            WriteProjectedSignature(__scratchProjectedTypeName, context, rt!, false);
            string projectedTypeName = __scratchProjectedTypeName.ToString();
            writer.Write("    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToUnmanaged\")]\n");
            writer.Write("    static extern WindowsRuntimeObjectReferenceValue ConvertToUnmanaged_");
            writer.Write(retParamName);
            writer.Write("([UnsafeAccessorType(\"");
            writer.Write(interopTypeName);
            writer.Write("\")] object _, ");
            writer.Write(projectedTypeName);
            writer.Write(" value);\n\n");
        }

        // Hoist [UnsafeAccessor] declarations for Out generic-instance params:
        // ConvertToUnmanaged_<name> wraps the projected value into a WindowsRuntimeObjectReferenceValue.
        // The body's writeback later references these already-declared accessors.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.Out) { continue; }
            AsmResolver.DotNet.Signatures.TypeSignature uOut = StripByRefAndCustomModifiers(p.Type);
            if (!uOut.IsGenericInstance()) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string interopTypeName = EncodeInteropTypeName(uOut, TypedefNameType.ABI) + ", WinRT.Interop";
            IndentedTextWriter __scratchProjectedTypeName = new();
            WriteProjectedSignature(__scratchProjectedTypeName, context, uOut, false);
            string projectedTypeName = __scratchProjectedTypeName.ToString();
            writer.Write("    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToUnmanaged\")]\n");
            writer.Write("    static extern WindowsRuntimeObjectReferenceValue ConvertToUnmanaged_");
            writer.Write(raw);
            writer.Write("([UnsafeAccessorType(\"");
            writer.Write(interopTypeName);
            writer.Write("\")] object _, ");
            writer.Write(projectedTypeName);
            writer.Write(" value);\n\n");
        }
        // ConvertToUnmanaged_<param> and the return-array ConvertToUnmanaged_<retParam> to the
        // top of the method body, before locals and the try block. The actual call sites later
        // in the body reference these already-declared accessors.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.ReceiveArray) { continue; }
            string raw = p.Parameter.Name ?? "param";
            AsmResolver.DotNet.Signatures.SzArrayTypeSignature sza = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)StripByRefAndCustomModifiers(p.Type);
            IndentedTextWriter __scratchElementProjected = new();
            WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(sza.BaseType));
            string elementProjected = __scratchElementProjected.ToString();
            string elementInteropArg = EncodeInteropTypeName(sza.BaseType, TypedefNameType.Projected);

            _ = elementInteropArg;
            string marshallerPath = GetArrayMarshallerInteropPath(sza.BaseType);
            string elementAbi = sza.BaseType.IsString() || IsRuntimeClassOrInterface(context.Cache, sza.BaseType) || sza.BaseType.IsObject()
                ? "void*"
                : IsComplexStruct(context.Cache, sza.BaseType)
                    ? GetAbiStructTypeName(writer, context, sza.BaseType)
                    : IsAnyStruct(context.Cache, sza.BaseType)
                        ? GetBlittableStructAbiType(writer, context, sza.BaseType)
                        : GetAbiPrimitiveType(context.Cache, sza.BaseType);
            writer.Write("    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToUnmanaged\")]\n");
            writer.Write("    static extern void ConvertToUnmanaged_");
            writer.Write(raw);
            writer.Write("([UnsafeAccessorType(\"");
            writer.Write(marshallerPath);
            writer.Write("\")] object _, ReadOnlySpan<");
            writer.Write(elementProjected);
            writer.Write("> span, out uint length, out ");
            writer.Write(elementAbi);
            writer.Write("* data);\n\n");
        }
        if (returnIsReceiveArrayDoAbi && rt is AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSzHoist)
        {
            IndentedTextWriter __scratchElementProjected = new();
            WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(retSzHoist.BaseType));
            string elementProjected = __scratchElementProjected.ToString();
            string elementAbi = retSzHoist.BaseType.IsString() || IsRuntimeClassOrInterface(context.Cache, retSzHoist.BaseType) || retSzHoist.BaseType.IsObject()
                ? "void*"
                : IsComplexStruct(context.Cache, retSzHoist.BaseType)
                    ? GetAbiStructTypeName(writer, context, retSzHoist.BaseType)
                    : IsAnyStruct(context.Cache, retSzHoist.BaseType)
                        ? GetBlittableStructAbiType(writer, context, retSzHoist.BaseType)
                        : GetAbiPrimitiveType(context.Cache, retSzHoist.BaseType);
            string elementInteropArg = EncodeInteropTypeName(retSzHoist.BaseType, TypedefNameType.Projected);

            _ = elementInteropArg;
            string marshallerPath = GetArrayMarshallerInteropPath(retSzHoist.BaseType);
            writer.Write("    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToUnmanaged\")]\n");
            writer.Write("    static extern void ConvertToUnmanaged_");
            writer.Write(retParamName);
            writer.Write("([UnsafeAccessorType(\"");
            writer.Write(marshallerPath);
            writer.Write("\")] object _, ReadOnlySpan<");
            writer.Write(elementProjected);
            writer.Write("> span, out uint length, out ");
            writer.Write(elementAbi);
            writer.Write("* data);\n\n");
        }
        // the OUT pointer(s). The actual assignment happens inside the try block.
        if (rt is not null)
        {
            if (returnIsString)
            {
                writer.Write("    string ");
                writer.Write(retLocalName);
                writer.Write(" = default;\n");
            }
            else if (returnIsRefType)
            {
                IndentedTextWriter __scratchProjected = new();
                WriteProjectedSignature(__scratchProjected, context, rt, false);
                string projected = __scratchProjected.ToString();
                writer.Write("    ");
                writer.Write(projected);
                writer.Write(" ");
                writer.Write(retLocalName);
                writer.Write(" = default;\n");
            }
            else if (returnIsReceiveArrayDoAbi)
            {
                IndentedTextWriter __scratchProjected = new();
                WriteProjectedSignature(__scratchProjected, context, rt, false);
                string projected = __scratchProjected.ToString();
                writer.Write("    ");
                writer.Write(projected);
                writer.Write(" ");
                writer.Write(retLocalName);
                writer.Write(" = default;\n");
            }
            else
            {
                IndentedTextWriter __scratchProjected = new();
                WriteProjectedSignature(__scratchProjected, context, rt, false);
                string projected = __scratchProjected.ToString();
                writer.Write("    ");
                writer.Write(projected);
                writer.Write(" ");
                writer.Write(retLocalName);
                writer.Write(" = default;\n");
            }
        }

        if (rt is not null)
        {
            if (returnIsReceiveArrayDoAbi)
            {
                writer.Write("    *");
                writer.Write(retParamName);
                writer.Write(" = default;\n");
                writer.Write("    *");
                writer.Write(retSizeParamName);
                writer.Write(" = default;\n");
            }
            else
            {
                writer.Write("    *");
                writer.Write(retParamName);
                writer.Write(" = default;\n");
            }
        }
        // For each out parameter, clear the destination and declare a local.
        // NOTE: Ref params (WinRT 'in T' / 'ref const T') are READ-ONLY inputs from the caller's
        // perspective. Do NOT zero *<name> (it's the input value) and do NOT declare a local
        // (we read directly via *<name>).
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.Out) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string ptr = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            writer.Write("    *");
            writer.Write(ptr);
            writer.Write(" = default;\n");
        }
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.Out) { continue; }
            string raw = p.Parameter.Name ?? "param";
            // Use the projected (non-ABI) type for the local variable.
            // Strip ByRef and CustomModifier wrappers to get the underlying base type.
            AsmResolver.DotNet.Signatures.TypeSignature underlying = StripByRefAndCustomModifiers(p.Type);
            IndentedTextWriter __scratchProjected = new();
            WriteProjectedSignature(__scratchProjected, context, underlying, false);
            string projected = __scratchProjected.ToString();
            writer.Write("    ");
            writer.Write(projected);
            writer.Write(" __");
            writer.Write(raw);
            writer.Write(" = default;\n");
        }
        // For each ReceiveArray parameter (out T[]), zero the destination + size out pointers
        // and declare a managed array local. The managed call passes 'out __<name>' and after
        // the call we copy to the ABI buffer via UnsafeAccessor.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.ReceiveArray) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string ptr = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            AsmResolver.DotNet.Signatures.SzArrayTypeSignature sza = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)StripByRefAndCustomModifiers(p.Type);
            IndentedTextWriter __scratchElementProjected = new();
            WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(sza.BaseType));
            string elementProjected = __scratchElementProjected.ToString();
            writer.Write("    *");
            writer.Write(ptr);
            writer.Write(" = default;\n");
            writer.Write("    *__");
            writer.Write(raw);
            writer.Write("Size = default;\n");
            writer.Write("    ");
            writer.Write(elementProjected);
            writer.Write("[] __");
            writer.Write(raw);
            writer.Write(" = default;\n");
        }
        // For each blittable array (PassArray / FillArray) parameter, declare a Span<T> local that
        // wraps the (length, pointer) pair from the ABI signature.
        // For non-blittable element types (string/runtime class/object), declare InlineArray16<T> +
        // ArrayPool fallback then CopyToManaged via UnsafeAccessor.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature sz) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string ptr = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            IndentedTextWriter __scratchElementProjected = new();
            WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(sz.BaseType));
            string elementProjected = __scratchElementProjected.ToString();
            bool isBlittableElem = IsBlittablePrimitive(context.Cache, sz.BaseType) || IsAnyStruct(context.Cache, sz.BaseType);
            if (isBlittableElem)
            {
                writer.Write("    ");
                writer.Write(cat == ParamCategory.PassArray ? "ReadOnlySpan<" : "Span<");
                writer.Write(elementProjected);
                writer.Write("> __");
                writer.Write(raw);
                writer.Write(" = new(");
                writer.Write(ptr);
                writer.Write(", (int)__");
                writer.Write(raw);
                writer.Write("Size);\n");
            }
            else
            {
                // Non-blittable element: InlineArray16<T> + ArrayPool<T> with size from ABI.
                writer.Write("\n    Unsafe.SkipInit(out InlineArray16<");
                writer.Write(elementProjected);
                writer.Write("> __");
                writer.Write(raw);
                writer.Write("_inlineArray);\n");
                writer.Write("    ");
                writer.Write(elementProjected);
                writer.Write("[] __");
                writer.Write(raw);
                writer.Write("_arrayFromPool = null;\n");
                writer.Write("    Span<");
                writer.Write(elementProjected);
                writer.Write("> __");
                writer.Write(raw);
                writer.Write(" = __");
                writer.Write(raw);
                writer.Write("Size <= 16\n        ? __");
                writer.Write(raw);
                writer.Write("_inlineArray[..(int)__");
                writer.Write(raw);
                writer.Write("Size]\n        : (__");
                writer.Write(raw);
                writer.Write("_arrayFromPool = global::System.Buffers.ArrayPool<");
                writer.Write(elementProjected);
                writer.Write(">.Shared.Rent((int)__");
                writer.Write(raw);
                writer.Write("Size));\n");
            }
        }
        writer.Write("    try\n    {\n");

        // For non-blittable PassArray params (read-only input arrays), emit CopyToManaged_<name>
        // via UnsafeAccessor to convert the native ABI buffer into the managed Span<T> the
        // delegate sees. For FillArray params, the buffer is fresh storage the user delegate
        // fills — the post-call writeback loop handles that. (Mirrors C++ which only emits the
        // pre-call CopyToManaged for PassArray, see write_copy_to_managed.)
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.PassArray) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
            if (IsBlittablePrimitive(context.Cache, szArr.BaseType) || IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string ptr = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            IndentedTextWriter __scratchElementProjected = new();
            WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(szArr.BaseType));
            string elementProjected = __scratchElementProjected.ToString();
            string elementInteropArg = EncodeInteropTypeName(szArr.BaseType, TypedefNameType.Projected);

            _ = elementInteropArg;
            // For complex structs, the data param is the ABI struct pointer (e.g. BasicStruct*).
            // The Do_Abi parameter we receive is void* (per V3R3-M8), so the call-site needs an
            // explicit (T*) cast to bridge the type. For ref-types (string/runtime-class/object),
            // the data param is void** and the cast is (void**).
            string dataParamType;
            string dataCastExpr;
            if (IsComplexStruct(context.Cache, szArr.BaseType))
            {
                string abiStructName = GetAbiStructTypeName(writer, context, szArr.BaseType);
                dataParamType = abiStructName + "* data";
                dataCastExpr = "(" + abiStructName + "*)" + ptr;
            }
            else
            {
                dataParamType = "void** data";
                dataCastExpr = "(void**)" + ptr;
            }
            writer.Write("        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"CopyToManaged\")]\n");
            writer.Write("        static extern void CopyToManaged_");
            writer.Write(raw);
            writer.Write("([UnsafeAccessorType(\"");
            writer.Write(GetArrayMarshallerInteropPath(szArr.BaseType));
            writer.Write("\")] object _, uint length, ");
            writer.Write(dataParamType);
            writer.Write(", Span<");
            writer.Write(elementProjected);
            writer.Write("> span);\n");
            writer.Write("        CopyToManaged_");
            writer.Write(raw);
            writer.Write("(null, __");
            writer.Write(raw);
            writer.Write("Size, ");
            writer.Write(dataCastExpr);
            writer.Write(", __");
            writer.Write(raw);
            writer.Write(");\n");
        }

        // For generic instance ABI input parameters, emit local UnsafeAccessor delegates and locals
        // first so the call site can reference them.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            if (p.Type.IsNullableT())
            {
                // Nullable<T> param (server-side): use <T>Marshaller.UnboxToManaged. Mirrors truth pattern.
                string rawName = p.Parameter.Name ?? "param";
                string callName = CSharpKeywords.IsKeyword(rawName) ? "@" + rawName : rawName;
                AsmResolver.DotNet.Signatures.TypeSignature inner = p.Type.GetNullableInnerType()!;
                string innerMarshaller = GetNullableInnerMarshallerName(writer, context, inner);
                writer.Write("        var __arg_");
                writer.Write(rawName);
                writer.Write(" = ");
                writer.Write(innerMarshaller);
                writer.Write(".UnboxToManaged(");
                writer.Write(callName);
                writer.Write(");\n");
            }
            else if (p.Type.IsGenericInstance())
            {
                string rawName = p.Parameter.Name ?? "param";
                string callName = CSharpKeywords.IsKeyword(rawName) ? "@" + rawName : rawName;
                string interopTypeName = EncodeInteropTypeName(p.Type, TypedefNameType.ABI) + ", WinRT.Interop";
                IndentedTextWriter __scratchProjectedTypeName = new();
                WriteProjectedSignature(__scratchProjectedTypeName, context, p.Type, false);
                string projectedTypeName = __scratchProjectedTypeName.ToString();
                writer.Write("        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToManaged\")]\n");
                writer.Write("        static extern ");
                writer.Write(projectedTypeName);
                writer.Write(" ConvertToManaged_arg_");
                writer.Write(rawName);
                writer.Write("([UnsafeAccessorType(\"");
                writer.Write(interopTypeName);
                writer.Write("\")] object _, void* value);\n");
                writer.Write("        var __arg_");
                writer.Write(rawName);
                writer.Write(" = ConvertToManaged_arg_");
                writer.Write(rawName);
                writer.Write("(null, ");
                writer.Write(callName);
                writer.Write(");\n");
            }
        }

        if (returnIsString)
        {
            writer.Write("        ");
            writer.Write(retLocalName);
            writer.Write(" = ");
        }
        else if (returnIsRefType)
        {
            writer.Write("        ");
            writer.Write(retLocalName);
            writer.Write(" = ");
        }
        else if (returnIsReceiveArrayDoAbi)
        {
            // For T[] return: assign to existing local.
            writer.Write("        ");
            writer.Write(retLocalName);
            writer.Write(" = ");
        }
        else if (rt is not null)
        {
            writer.Write("        ");
            writer.Write(retLocalName);
            writer.Write(" = ");
        }
        else
        {
            writer.Write("        ");
        }

        if (isGetter)
        {
            string propName = methodName[4..];
            writer.Write("ComInterfaceDispatch.GetInstance<");
            writer.Write(ifaceFullName);
            writer.Write(">((ComInterfaceDispatch*)thisPtr).");
            writer.Write(propName);
            writer.Write(";\n");
        }
        else if (isSetter)
        {
            string propName = methodName[4..];
            writer.Write("ComInterfaceDispatch.GetInstance<");
            writer.Write(ifaceFullName);
            writer.Write(">((ComInterfaceDispatch*)thisPtr).");
            writer.Write(propName);
            writer.Write(" = ");
            EmitDoAbiParamArgConversion(writer, context, sig.Params[0]);
            writer.Write(";\n");
        }
        else
        {
            writer.Write("ComInterfaceDispatch.GetInstance<");
            writer.Write(ifaceFullName);
            writer.Write(">((ComInterfaceDispatch*)thisPtr).");
            writer.Write(methodName);
            writer.Write("(");
            for (int i = 0; i < sig.Params.Count; i++)
            {
                if (i > 0) { writer.Write(",\n  "); }
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                if (cat == ParamCategory.Out)
                {
                    string raw = p.Parameter.Name ?? "param";
                    writer.Write("out __");
                    writer.Write(raw);
                }
                else if (cat == ParamCategory.Ref)
                {
                    // WinRT 'in T' / 'ref const T' is a read-only by-ref input on the ABI side
                    // (pointer to a value the native caller owns). On the C# delegate / interface
                    // side it's projected as 'in T'. Read directly from *<name> via the appropriate
                    // marshaller — DO NOT zero or write back.
                    string raw = p.Parameter.Name ?? "param";
                    string ptr = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
                    AsmResolver.DotNet.Signatures.TypeSignature uRef = StripByRefAndCustomModifiers(p.Type);
                    if (uRef.IsString())
                    {
                        writer.Write("HStringMarshaller.ConvertToManaged(*");
                        writer.Write(ptr);
                        writer.Write(")");
                    }
                    else if (uRef.IsObject())
                    {
                        writer.Write("WindowsRuntimeObjectMarshaller.ConvertToManaged(*");
                        writer.Write(ptr);
                        writer.Write(")");
                    }
                    else if (IsRuntimeClassOrInterface(context.Cache, uRef))
                    {
                        writer.Write(GetMarshallerFullName(writer, context, uRef));
                        writer.Write(".ConvertToManaged(*");
                        writer.Write(ptr);
                        writer.Write(")");
                    }
                    else if (IsMappedAbiValueType(uRef))
                    {
                        writer.Write(GetMappedMarshallerName(uRef));
                        writer.Write(".ConvertToManaged(*");
                        writer.Write(ptr);
                        writer.Write(")");
                    }
                    else if (uRef.IsHResultException())
                    {
                        writer.Write("global::ABI.System.ExceptionMarshaller.ConvertToManaged(*");
                        writer.Write(ptr);
                        writer.Write(")");
                    }
                    else if (IsComplexStruct(context.Cache, uRef))
                    {
                        writer.Write(GetMarshallerFullName(writer, context, uRef));
                        writer.Write(".ConvertToManaged(*");
                        writer.Write(ptr);
                        writer.Write(")");
                    }
                    else if (IsAnyStruct(context.Cache, uRef) || IsBlittablePrimitive(context.Cache, uRef) || IsEnumType(context.Cache, uRef))
                    {
                        // Blittable/almost-blittable: ABI layout matches projected layout.
                        writer.Write("*");
                        writer.Write(ptr);
                    }
                    else
                    {
                        writer.Write("*");
                        writer.Write(ptr);
                    }
                }
                else if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
                {
                    string raw = p.Parameter.Name ?? "param";
                    writer.Write("__");
                    writer.Write(raw);
                }
                else if (cat == ParamCategory.ReceiveArray)
                {
                    string raw = p.Parameter.Name ?? "param";
                    writer.Write("out __");
                    writer.Write(raw);
                }
                else
                {
                    EmitDoAbiParamArgConversion(writer, context, p);
                }
            }
            writer.Write(");\n");
        }
        // After call: write back out params to caller's pointer.
        // NOTE: Ref params (WinRT 'in T') are read-only inputs — never written back.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.Out) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string ptr = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            AsmResolver.DotNet.Signatures.TypeSignature underlying = StripByRefAndCustomModifiers(p.Type);
            writer.Write("        *");
            writer.Write(ptr);
            writer.Write(" = ");
            // String: HStringMarshaller.ConvertToUnmanaged
            if (underlying.IsString())
            {
                writer.Write("HStringMarshaller.ConvertToUnmanaged(__");
                writer.Write(raw);
                writer.Write(")");
            }
            // Object/runtime class: <Marshaller>.ConvertToUnmanaged(...).DetachThisPtrUnsafe()
            else if (underlying.IsObject())
            {
                writer.Write("WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(__");
                writer.Write(raw);
                writer.Write(").DetachThisPtrUnsafe()");
            }
            else if (IsRuntimeClassOrInterface(context.Cache, underlying))
            {
                writer.Write(GetMarshallerFullName(writer, context, underlying));
                writer.Write(".ConvertToUnmanaged(__");
                writer.Write(raw);
                writer.Write(").DetachThisPtrUnsafe()");
            }
            // Generic instance (e.g. IEnumerable<string>): use the hoisted UnsafeAccessor
            // 'ConvertToUnmanaged_<name>' declared at the top of the method body.
            else if (underlying.IsGenericInstance())
            {
                writer.Write("ConvertToUnmanaged_");
                writer.Write(raw);
                writer.Write("(null, __");
                writer.Write(raw);
                writer.Write(").DetachThisPtrUnsafe()");
            }
            // For enums, function pointer signature uses the projected enum type, no cast needed.
            // For bool, cast to byte. For char, cast to ushort.
            else if (IsEnumType(context.Cache, underlying))
            {
                writer.Write("__");
                writer.Write(raw);
            }
            else if (underlying is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibBool &&
                     corlibBool.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
            {
                writer.Write("__");
                writer.Write(raw);
            }
            else if (underlying is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibChar &&
                     corlibChar.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
            {
                writer.Write("__");
                writer.Write(raw);
            }
            // Non-blittable struct (e.g. authored BasicStruct with string fields): marshal
            // the local managed value through <Type>Marshaller.ConvertToUnmanaged before
            // writing it into the *out ABI struct slot. Mirrors C++ marshaler.write_marshal_from_managed
            //: "Marshaller.ConvertToUnmanaged(local)".
            else if (IsComplexStruct(context.Cache, underlying))
            {
                writer.Write(GetMarshallerFullName(writer, context, underlying));
                writer.Write(".ConvertToUnmanaged(__");
                writer.Write(raw);
                writer.Write(")");
            }
            else
            {
                writer.Write("__");
                writer.Write(raw);
            }
            writer.Write(";\n");
        }
        // After call: for ReceiveArray params, emit ConvertToUnmanaged_<name> call (the
        // [UnsafeAccessor] declaration was hoisted to the top of the method body).
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.ReceiveArray) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string ptr = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            writer.Write("        ConvertToUnmanaged_");
            writer.Write(raw);
            writer.Write("(null, __");
            writer.Write(raw);
            writer.Write(", out *__");
            writer.Write(raw);
            writer.Write("Size, out *");
            writer.Write(ptr);
            writer.Write(");\n");
        }
        // After call: for non-blittable FillArray params (Span<T> where T is string/runtime
        // class/object/non-blittable struct), copy the managed delegate's writes back into the
        // native ABI buffer. Mirrors C++ write_marshal_from_managed
        // which emits 'CopyToUnmanaged_<name>(null, __<name>, __<name>Size, (T*)<name>)'.
        // Blittable element types don't need this — the Span wraps the native buffer directly.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.FillArray) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szFA) { continue; }
            // Blittable element types: Span wraps the native buffer; no copy-back needed.
            if (IsBlittablePrimitive(context.Cache, szFA.BaseType) || IsAnyStruct(context.Cache, szFA.BaseType)) { continue; }
            string raw = p.Parameter.Name ?? "param";
            string ptr = CSharpKeywords.IsKeyword(raw) ? "@" + raw : raw;
            IndentedTextWriter __scratchElementProjected = new();
            WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(szFA.BaseType));
            string elementProjected = __scratchElementProjected.ToString();
            string elementInteropArg = EncodeInteropTypeName(szFA.BaseType, TypedefNameType.Projected);

            _ = elementInteropArg;
            // Determine the ABI element type for the data pointer cast.
            // - Strings / runtime classes / objects: void**
            // - HResult exception: global::ABI.System.Exception*
            // - Mapped value types (DateTime/TimeSpan): global::ABI.System.{DateTimeOffset/TimeSpan}*
            // - Complex structs: <ABI struct>*
            string dataParamType;
            string dataCastType;
            if (szFA.BaseType.IsString() || IsRuntimeClassOrInterface(context.Cache, szFA.BaseType) || szFA.BaseType.IsObject())
            {
                dataParamType = "void** data";
                dataCastType = "(void**)";
            }
            else if (szFA.BaseType.IsHResultException())
            {
                dataParamType = "global::ABI.System.Exception* data";
                dataCastType = "(global::ABI.System.Exception*)";
            }
            else if (IsMappedAbiValueType(szFA.BaseType))
            {
                string abiName = GetMappedAbiTypeName(szFA.BaseType);
                dataParamType = abiName + "* data";
                dataCastType = "(" + abiName + "*)";
            }
            else
            {
                string abiStructName = GetAbiStructTypeName(writer, context, szFA.BaseType);
                dataParamType = abiStructName + "* data";
                dataCastType = "(" + abiStructName + "*)";
            }
            writer.Write("        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"CopyToUnmanaged\")]\n");
            writer.Write("        static extern void CopyToUnmanaged_");
            writer.Write(raw);
            writer.Write("([UnsafeAccessorType(\"");
            writer.Write(GetArrayMarshallerInteropPath(szFA.BaseType));
            writer.Write("\")] object _, ReadOnlySpan<");
            writer.Write(elementProjected);
            writer.Write("> span, uint length, ");
            writer.Write(dataParamType);
            writer.Write(");\n");
            writer.Write("        CopyToUnmanaged_");
            writer.Write(raw);
            writer.Write("(null, __");
            writer.Write(raw);
            writer.Write(", __");
            writer.Write(raw);
            writer.Write("Size, ");
            writer.Write(dataCastType);
            writer.Write(ptr);
            writer.Write(");\n");
        }
        if (rt is not null)
        {
            if (returnIsHResultExceptionDoAbi)
            {
                writer.Write("        *");
                writer.Write(retParamName);
                writer.Write(" = global::ABI.System.ExceptionMarshaller.ConvertToUnmanaged(");
                writer.Write(retLocalName);
                writer.Write(");\n");
            }
            else if (returnIsString)
            {
                writer.Write("        *");
                writer.Write(retParamName);
                writer.Write(" = HStringMarshaller.ConvertToUnmanaged(");
                writer.Write(retLocalName);
                writer.Write(");\n");
            }
            else if (returnIsRefType)
            {
                if (rt is not null && rt.IsNullableT())
                {
                    // Nullable<T> return (server-side): use <T>Marshaller.BoxToUnmanaged.
                    AsmResolver.DotNet.Signatures.TypeSignature inner = rt.GetNullableInnerType()!;
                    string innerMarshaller = GetNullableInnerMarshallerName(writer, context, inner);
                    writer.Write("        *");
                    writer.Write(retParamName);
                    writer.Write(" = ");
                    writer.Write(innerMarshaller);
                    writer.Write(".BoxToUnmanaged(");
                    writer.Write(retLocalName);
                    writer.Write(").DetachThisPtrUnsafe();\n");
                }
                else if (returnIsGenericInstance)
                {
                    // Generic instance return: use the UnsafeAccessor static local function declared at
                    // the top of the method body via the M12 hoisting pass; just emit the call here.
                    writer.Write("        *");
                    writer.Write(retParamName);
                    writer.Write(" = ConvertToUnmanaged_");
                    writer.Write(retParamName);
                    writer.Write("(null, ");
                    writer.Write(retLocalName);
                    writer.Write(").DetachThisPtrUnsafe();\n");
                }
                else
                {
                    writer.Write("        *");
                    writer.Write(retParamName);
                    writer.Write(" = ");
                    EmitMarshallerConvertToUnmanaged(writer, context, rt!, retLocalName);
                    writer.Write(".DetachThisPtrUnsafe();\n");
                }
            }
            else if (returnIsReceiveArrayDoAbi)
            {
                // Return-receive-array: emit ConvertToUnmanaged_<retParam> call (declaration
                // was hoisted to the top of the method body).
                writer.Write("        ConvertToUnmanaged_");
                writer.Write(retParamName);
                writer.Write("(null, ");
                writer.Write(retLocalName);
                writer.Write(", out *");
                writer.Write(retSizeParamName);
                writer.Write(", out *");
                writer.Write(retParamName);
                writer.Write(");\n");
            }
            else if (IsMappedAbiValueType(rt))
            {
                // Mapped value type return (DateTime/TimeSpan): convert via marshaller.
                writer.Write("        *");
                writer.Write(retParamName);
                writer.Write(" = ");
                writer.Write(GetMappedMarshallerName(rt));
                writer.Write(".ConvertToUnmanaged(");
                writer.Write(retLocalName);
                writer.Write(");\n");
            }
            else if (rt.IsSystemType())
            {
                // System.Type return (server-side): convert managed System.Type to ABI Type struct.
                writer.Write("        *");
                writer.Write(retParamName);
                writer.Write(" = global::ABI.System.TypeMarshaller.ConvertToUnmanaged(");
                writer.Write(retLocalName);
                writer.Write(");\n");
            }
            else if (IsComplexStruct(context.Cache, rt))
            {
                // Complex struct return (server-side): convert managed struct to ABI struct via marshaller.
                writer.Write("        *");
                writer.Write(retParamName);
                writer.Write(" = ");
                writer.Write(GetMarshallerFullName(writer, context, rt));
                writer.Write(".ConvertToUnmanaged(");
                writer.Write(retLocalName);
                writer.Write(");\n");
            }
            else if (returnIsBlittableStruct)
            {
                writer.Write("        *");
                writer.Write(retParamName);
                writer.Write(" = ");
                writer.Write(retLocalName);
                writer.Write(";\n");
            }
            else
            {
                string abiType = GetAbiPrimitiveType(context.Cache, rt);
                writer.Write("        *");
                writer.Write(retParamName);
                writer.Write(" = ");
                if (rt is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib &&
                    corlib.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
                {
                    writer.Write(retLocalName);
                    writer.Write(";\n");
                }
                else if (rt is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib2 &&
                         corlib2.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
                {
                    writer.Write(retLocalName);
                    writer.Write(";\n");
                }
                else if (IsEnumType(context.Cache, rt))
                {
                    // Enum: function pointer signature uses the projected enum type, no cast needed.
                    writer.Write(retLocalName);
                    writer.Write(";\n");
                }
                else
                {
                    writer.Write(retLocalName);
                    writer.Write(";\n");
                }
            }
        }
        writer.Write("        return 0;\n    }\n");
        writer.Write("    catch (Exception __exception__)\n    {\n");
        writer.Write("        return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(__exception__);\n    }\n");

        // For non-blittable PassArray params, emit finally block with ArrayPool<T>.Shared.Return.
        bool hasNonBlittableArrayDoAbi = false;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
            if (IsBlittablePrimitive(context.Cache, szArr.BaseType) || IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
            hasNonBlittableArrayDoAbi = true;
            break;
        }
        if (hasNonBlittableArrayDoAbi)
        {
            writer.Write("    finally\n    {\n");
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
                if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
                if (IsBlittablePrimitive(context.Cache, szArr.BaseType) || IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
                string raw = p.Parameter.Name ?? "param";
                IndentedTextWriter __scratchElementProjected = new();
                WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(szArr.BaseType));
                string elementProjected = __scratchElementProjected.ToString();
                writer.Write("\n        if (__");
                writer.Write(raw);
                writer.Write("_arrayFromPool is not null)\n        {\n");
                writer.Write("            global::System.Buffers.ArrayPool<");
                writer.Write(elementProjected);
                writer.Write(">.Shared.Return(__");
                writer.Write(raw);
                writer.Write("_arrayFromPool);\n        }\n");
            }
            writer.Write("    }\n");
        }

        writer.Write("}\n\n");
        _ = hasStringParams;
    }

    /// <summary>Converts an ABI parameter to its projected (managed) form for the Do_Abi call.</summary>
    internal static void EmitDoAbiParamArgConversion(IndentedTextWriter writer, ProjectionEmitContext context, ParamInfo p)
    {
        string rawName = p.Parameter.Name ?? "param";
        string pname = CSharpKeywords.IsKeyword(rawName) ? "@" + rawName : rawName;
        if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib &&
            corlib.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
        {
            writer.Write(pname);
        }
        else if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib2 &&
                 corlib2.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
        {
            writer.Write(pname);
        }
        else if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibStr &&
                 corlibStr.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.String)
        {
            writer.Write("HStringMarshaller.ConvertToManaged(");
            writer.Write(pname);
            writer.Write(")");
        }
        else if (p.Type.IsGenericInstance())
        {
            // Generic instance ABI parameter: caller already declared a local UnsafeAccessor +
            // local var __arg_<name> that holds the converted value.
            writer.Write("__arg_");
            writer.Write(rawName);
        }
        else if (IsRuntimeClassOrInterface(context.Cache, p.Type) || p.Type.IsObject())
        {
            EmitMarshallerConvertToManaged(writer, context, p.Type, pname);
        }
        else if (IsMappedAbiValueType(p.Type))
        {
            // Mapped value type input (DateTime/TimeSpan): the parameter is the ABI type;
            // convert to the projected managed type via the marshaller.
            writer.Write(GetMappedMarshallerName(p.Type));
            writer.Write(".ConvertToManaged(");
            writer.Write(pname);
            writer.Write(")");
        }
        else if (p.Type.IsSystemType())
        {
            // System.Type input (server-side): convert ABI Type struct to System.Type.
            writer.Write("global::ABI.System.TypeMarshaller.ConvertToManaged(");
            writer.Write(pname);
            writer.Write(")");
        }
        else if (IsComplexStruct(context.Cache, p.Type))
        {
            // Complex struct input (server-side): convert ABI struct to managed via marshaller.
            writer.Write(GetMarshallerFullName(writer, context, p.Type));
            writer.Write(".ConvertToManaged(");
            writer.Write(pname);
            writer.Write(")");
        }
        else if (IsAnyStruct(context.Cache, p.Type))
        {
            // Blittable / almost-blittable struct: pass directly (projected type == ABI type).
            writer.Write(pname);
        }
        else if (IsEnumType(context.Cache, p.Type))
        {
            // Enum: param signature is already the projected enum type, no cast needed.
            writer.Write(pname);
        }
        else
        {
            writer.Write(pname);
        }
    }

    /// <summary>Mirrors C++ <c>write_iid_guid</c> for use by ABI helpers.</summary>
    public static void WriteIidGuidReference(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (type.GenericParameters.Count != 0)
        {
            // Generic interface IID - call the unsafe accessor
            WriteIidGuidPropertyName(writer, context, type);
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
        WriteIidGuidPropertyName(writer, context, type);
    }

    /// <summary>
    /// Writes a marshaller class for a struct or enum (mirrors C++ write_struct_and_enum_marshaller_class).
    /// </summary>
    internal static void WriteStructEnumMarshallerClass(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = IdentifierEscaping.StripBackticks(name);
        TypeCategory cat = TypeCategorization.GetCategory(type);
        bool blittable = IsTypeBlittable(context.Cache, type);
        // "Almost-blittable" includes blittable + bool/char fields. Excludes string/object fields.
        // Use the same predicate as IsAnyStruct (which is now scoped to almost-blittable).
        AsmResolver.DotNet.Signatures.TypeDefOrRefSignature sig = type.ToTypeSignature(false) is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td2 ? td2 : null!;
        bool almostBlittable = cat == TypeCategory.Struct && (sig is null || IsAnyStruct(context.Cache, sig));
        bool isEnum = cat == TypeCategory.Enum;
        // Complex structs are non-almost-blittable structs with reference fields (string, object, etc.).
        bool isComplexStruct = cat == TypeCategory.Struct && !almostBlittable;
        // Detect Nullable<T> reference fields to determine whether the struct's BoxToUnmanaged
        // call needs CreateComInterfaceFlags.TrackerSupport (mirrors C++ use_tracker_object_support
        // which returns true for IReference`1 generic instances).
        bool hasReferenceFields = false;
        if (isComplexStruct)
        {
            foreach (FieldDefinition field in type.Fields)
            {
                if (field.IsStatic || field.Signature is null) { continue; }
                AsmResolver.DotNet.Signatures.TypeSignature ft = field.Signature.FieldType;
                if (TryGetNullablePrimitiveMarshallerName(ft, out _)) { hasReferenceFields = true; }
            }
        }

        // For structs that are mapped (e.g. Duration, KeyTime, RepeatBehavior — they have
        // EmitAbi=true and an addition file that completely replaces the public struct), skip
        // the per-field ConvertToUnmanaged/ConvertToManaged because the projected struct's
        // public fields don't match the WinMD field layout. The truth marshaller for these
        // contains only BoxToUnmanaged/UnboxToManaged.
        (string typeNs, string typeNm) = type.Names();
        bool isMappedStruct = isComplexStruct && MappedTypes.Get(typeNs, typeNm) is not null;
        if (isMappedStruct) { isComplexStruct = false; }

        writer.Write("public static unsafe class ");
        writer.Write(nameStripped);
        writer.Write("Marshaller\n{\n");

        if (isComplexStruct)
        {
            // ConvertToUnmanaged: build ABI struct from projected struct via per-field marshalling.
            writer.Write("    public static ");
            WriteTypedefName(writer, context, type, TypedefNameType.ABI, false);
            writer.Write(" ConvertToUnmanaged(");
            WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
            writer.Write(" value)\n    {\n");
            writer.Write("        return new() {\n");
            bool first = true;
            foreach (FieldDefinition field in type.Fields)
            {
                if (field.IsStatic || field.Signature is null) { continue; }
                string fname = field.Name?.Value ?? "";
                AsmResolver.DotNet.Signatures.TypeSignature ft = field.Signature.FieldType;
                if (!first) { writer.Write(",\n"); }
                first = false;
                writer.Write("            ");
                writer.Write(fname);
                writer.Write(" = ");
                if (ft.IsString())
                {
                    writer.Write("HStringMarshaller.ConvertToUnmanaged(value.");
                    writer.Write(fname);
                    writer.Write(")");
                }
                else if (IsMappedAbiValueType(ft))
                {
                    writer.Write(GetMappedMarshallerName(ft));
                    writer.Write(".ConvertToUnmanaged(value.");
                    writer.Write(fname);
                    writer.Write(")");
                }
                else if (ft.IsHResultException())
                {
                    // Mapped value type 'HResult' (excluded from IsMappedAbiValueType because
                    // it's "treated specially in many places", but for nested struct fields the
                    // marshalling is identical: use ABI.System.ExceptionMarshaller).
                    writer.Write("global::ABI.System.ExceptionMarshaller.ConvertToUnmanaged(value.");
                    writer.Write(fname);
                    writer.Write(")");
                }
                else if (ft is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature ftd
                         && TryResolveStructTypeDef(context.Cache, ftd) is TypeDefinition fieldStructTd
                         && TypeCategorization.GetCategory(fieldStructTd) == TypeCategory.Struct
                         && !IsTypeBlittable(context.Cache, fieldStructTd))
                {
                    // Nested non-blittable struct: marshal via its <Name>Marshaller.
                    writer.Write(IdentifierEscaping.StripBackticks(fieldStructTd.Name?.Value ?? string.Empty));
                    writer.Write("Marshaller.ConvertToUnmanaged(value.");
                    writer.Write(fname);
                    writer.Write(")");
                }
                else if (TryGetNullablePrimitiveMarshallerName(ft, out string? nullableMarshaller))
                {
                    writer.Write(nullableMarshaller!);
                    writer.Write(".BoxToUnmanaged(value.");
                    writer.Write(fname);
                    writer.Write(").DetachThisPtrUnsafe()");
                }
                else
                {
                    writer.Write("value.");
                    writer.Write(fname);
                }
            }
            writer.Write("\n        };\n    }\n");

            // ConvertToManaged: construct projected struct via constructor accepting the marshalled fields.
            writer.Write("    public static ");
            WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
            writer.Write(" ConvertToManaged(");
            WriteTypedefName(writer, context, type, TypedefNameType.ABI, false);
            // - In component mode: emit object initializer with named field assignments
            //   (positional ctor not always available on authored types).
            // - In non-component mode: emit positional constructor (matches the auto-generated
            //   primary constructor on projected struct types).
            bool useObjectInitializer = context.Settings.Component;
            writer.Write(" value)\n    {\n");
            writer.Write("        return new ");
            WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
            writer.Write(useObjectInitializer ? "(){\n" : "(\n");
            first = true;
            foreach (FieldDefinition field in type.Fields)
            {
                if (field.IsStatic || field.Signature is null) { continue; }
                string fname = field.Name?.Value ?? "";
                AsmResolver.DotNet.Signatures.TypeSignature ft = field.Signature.FieldType;
                if (!first) { writer.Write(",\n"); }
                first = false;
                writer.Write("            ");
                if (useObjectInitializer)
                {
                    writer.Write(fname);
                    writer.Write(" = ");
                }
                if (ft.IsString())
                {
                    writer.Write("HStringMarshaller.ConvertToManaged(value.");
                    writer.Write(fname);
                    writer.Write(")");
                }
                else if (IsMappedAbiValueType(ft))
                {
                    writer.Write(GetMappedMarshallerName(ft));
                    writer.Write(".ConvertToManaged(value.");
                    writer.Write(fname);
                    writer.Write(")");
                }
                else if (ft.IsHResultException())
                {
                    // Mapped value type 'HResult' (excluded from IsMappedAbiValueType because
                    // it's "treated specially in many places", but for nested struct fields the
                    // marshalling is identical: use ABI.System.ExceptionMarshaller).
                    writer.Write("global::ABI.System.ExceptionMarshaller.ConvertToManaged(value.");
                    writer.Write(fname);
                    writer.Write(")");
                }
                else if (ft is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature ftd2
                         && TryResolveStructTypeDef(context.Cache, ftd2) is TypeDefinition fieldStructTd2
                         && TypeCategorization.GetCategory(fieldStructTd2) == TypeCategory.Struct
                         && !IsTypeBlittable(context.Cache, fieldStructTd2))
                {
                    // Nested non-blittable struct: convert via its <Name>Marshaller.
                    writer.Write(IdentifierEscaping.StripBackticks(fieldStructTd2.Name?.Value ?? string.Empty));
                    writer.Write("Marshaller.ConvertToManaged(value.");
                    writer.Write(fname);
                    writer.Write(")");
                }
                else if (TryGetNullablePrimitiveMarshallerName(ft, out string? nullableMarshaller))
                {
                    writer.Write(nullableMarshaller!);
                    writer.Write(".UnboxToManaged(value.");
                    writer.Write(fname);
                    writer.Write(")");
                }
                else
                {
                    writer.Write("value.");
                    writer.Write(fname);
                }
            }
            writer.Write(useObjectInitializer ? "\n        };\n    }\n" : "\n        );\n    }\n");

            // Dispose: free non-blittable fields.
            writer.Write("    public static void Dispose(");
            WriteTypedefName(writer, context, type, TypedefNameType.ABI, false);
            writer.Write(" value)\n    {\n");
            foreach (FieldDefinition field in type.Fields)
            {
                if (field.IsStatic || field.Signature is null) { continue; }
                string fname = field.Name?.Value ?? "";
                AsmResolver.DotNet.Signatures.TypeSignature ft = field.Signature.FieldType;
                if (ft.IsString())
                {
                    writer.Write("        HStringMarshaller.Free(value.");
                    writer.Write(fname);
                    writer.Write(");\n");
                }
                else if (ft.IsHResultException())
                {
                    // HResult/Exception field has no per-value resources to release
                    // (the ABI representation is just an int HRESULT). Skip Dispose entirely.
                    continue;
                }
                else if (IsMappedAbiValueType(ft))
                {
                    // Mapped value types (DateTime/TimeSpan) have no per-value resources to
                    // release — the ABI representation is just an int64. Mirror C++
                    // set_skip_disposer_if_needed which explicitly
                    // skips the disposer for global::ABI.System.{DateTimeOffset,TimeSpan,Exception}.
                    continue;
                }
                else if (ft is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature ftd3
                         && TryResolveStructTypeDef(context.Cache, ftd3) is TypeDefinition fieldStructTd3
                         && TypeCategorization.GetCategory(fieldStructTd3) == TypeCategory.Struct
                         && !IsTypeBlittable(context.Cache, fieldStructTd3))
                {
                    // Nested non-blittable struct: dispose via its <Name>Marshaller.
                    // Mirror C++: this site always uses the fully-qualified marshaller name.
                    string nestedNs = fieldStructTd3.Namespace?.Value ?? string.Empty;
                    string nestedNm = IdentifierEscaping.StripBackticks(fieldStructTd3.Name?.Value ?? string.Empty);
                    writer.Write("        global::ABI.");
                    writer.Write(nestedNs);
                    writer.Write(".");
                    writer.Write(nestedNm);
                    writer.Write("Marshaller.Dispose(value.");
                    writer.Write(fname);
                    writer.Write(");\n");
                }
                else if (TryGetNullablePrimitiveMarshallerName(ft, out _))
                {
                    writer.Write("        WindowsRuntimeUnknownMarshaller.Free(value.");
                    writer.Write(fname);
                    writer.Write(");\n");
                }
            }
            writer.Write("    }\n");
        }

        // BoxToUnmanaged: same pattern for all (enum, almost-blittable, complex).
        // Truth uses CreateComInterfaceFlags.TrackerSupport when the struct has reference type
        // fields (Nullable<T>, etc.) to avoid GC issues with the boxed managed object reference.
        writer.Write("    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(");
        WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
        if (isEnum || almostBlittable || isComplexStruct)
        {
            writer.Write("? value)\n    {\n");
            writer.Write("        return WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged(value, CreateComInterfaceFlags.");
            writer.Write(hasReferenceFields ? "TrackerSupport" : "None");
            writer.Write(", in ");
            WriteIidReferenceExpression(writer, type);
            writer.Write(");\n    }\n");
        }
        else
        {
            // Mapped struct (Duration/KeyTime/etc.): BoxToUnmanaged is still required because the
            // public projected type still routes through this marshaller (it just lacks per-field
            // ConvertToUnmanaged/ConvertToManaged because the field layout doesn't match).
            writer.Write("? value)\n    {\n");
            writer.Write("        return WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged(value, CreateComInterfaceFlags.None, in ");
            WriteIidReferenceExpression(writer, type);
            writer.Write(");\n    }\n");
        }

        // UnboxToManaged: simple for almost-blittable; for complex, unbox to ABI struct then ConvertToManaged.
        writer.Write("    public static ");
        WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
        if (isEnum || almostBlittable)
        {
            writer.Write("? UnboxToManaged(void* value)\n    {\n");
            writer.Write("        return WindowsRuntimeValueTypeMarshaller.UnboxToManaged<");
            WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
            writer.Write(">(value);\n    }\n");
        }
        else if (isComplexStruct)
        {
            writer.Write("? UnboxToManaged(void* value)\n    {\n");
            writer.Write("        ");
            WriteTypedefName(writer, context, type, TypedefNameType.ABI, false);
            writer.Write("? abi = WindowsRuntimeValueTypeMarshaller.UnboxToManaged<");
            WriteTypedefName(writer, context, type, TypedefNameType.ABI, false);
            writer.Write(">(value);\n");
            writer.Write("        return abi.HasValue ? ConvertToManaged(abi.GetValueOrDefault()) : null;\n    }\n");
        }
        else
        {
            // Mapped struct: unbox directly to projected type (no per-field ConvertToManaged needed
            // because the projected struct's field layout matches the WinMD struct layout).
            writer.Write("? UnboxToManaged(void* value)\n    {\n");
            writer.Write("        return WindowsRuntimeValueTypeMarshaller.UnboxToManaged<");
            WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
            writer.Write(">(value);\n    }\n");
        }

        writer.Write("}\n\n");

        // Emit the InterfaceEntriesImpl static class and the proper ComWrappersMarshallerAttribute
        // class derived from WindowsRuntimeComWrappersMarshallerAttribute (matches truth).
        // For enums and almost-blittable structs, GetOrCreateComInterfaceForObject uses None.
        // For complex structs (with reference fields), it uses TrackerSupport.
        // For complex structs, CreateObject converts via the *Marshaller.ConvertToManaged after
        // unboxing to the ABI struct.
        if (isEnum || almostBlittable || isComplexStruct)
        {
            IndentedTextWriter __scratchIidRefExpr = new();
            WriteIidReferenceExpression(__scratchIidRefExpr, type);
            string iidRefExpr = __scratchIidRefExpr.ToString();

            // InterfaceEntriesImpl
            writer.Write("file static class ");
            writer.Write(nameStripped);
            writer.Write("InterfaceEntriesImpl\n{\n");
            writer.Write("    [FixedAddressValueType]\n");
            writer.Write("    public static readonly ReferenceInterfaceEntries Entries;\n\n");
            writer.Write("    static ");
            writer.Write(nameStripped);
            writer.Write("InterfaceEntriesImpl()\n    {\n");
            writer.Write("        Entries.IReferenceValue.IID = ");
            writer.Write(iidRefExpr);
            writer.Write(";\n");
            writer.Write("        Entries.IReferenceValue.Vtable = ");
            writer.Write(nameStripped);
            writer.Write("ReferenceImpl.Vtable;\n");
            writer.Write("        Entries.IPropertyValue.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IPropertyValue;\n");
            writer.Write("        Entries.IPropertyValue.Vtable = global::WindowsRuntime.InteropServices.IPropertyValueImpl.OtherTypeVtable;\n");
            writer.Write("        Entries.IStringable.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IStringable;\n");
            writer.Write("        Entries.IStringable.Vtable = global::WindowsRuntime.InteropServices.IStringableImpl.Vtable;\n");
            writer.Write("        Entries.IWeakReferenceSource.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IWeakReferenceSource;\n");
            writer.Write("        Entries.IWeakReferenceSource.Vtable = global::WindowsRuntime.InteropServices.IWeakReferenceSourceImpl.Vtable;\n");
            writer.Write("        Entries.IMarshal.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IMarshal;\n");
            writer.Write("        Entries.IMarshal.Vtable = global::WindowsRuntime.InteropServices.IMarshalImpl.Vtable;\n");
            writer.Write("        Entries.IAgileObject.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IAgileObject;\n");
            writer.Write("        Entries.IAgileObject.Vtable = global::WindowsRuntime.InteropServices.IAgileObjectImpl.Vtable;\n");
            writer.Write("        Entries.IInspectable.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IInspectable;\n");
            writer.Write("        Entries.IInspectable.Vtable = global::WindowsRuntime.InteropServices.IInspectableImpl.Vtable;\n");
            writer.Write("        Entries.IUnknown.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IUnknown;\n");
            writer.Write("        Entries.IUnknown.Vtable = global::WindowsRuntime.InteropServices.IUnknownImpl.Vtable;\n");
            writer.Write("    }\n}\n\n");
            // is NOT emitted for STRUCTS (the attribute is supplied by cswinrtgen instead). Enums
            // and other types still emit it from write_abi_enum/etc.
            if (context.Settings.Component && cat == TypeCategory.Struct) { return; }

            // ComWrappersMarshallerAttribute (full body)
            writer.Write("internal sealed unsafe class ");
            writer.Write(nameStripped);
            writer.Write("ComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute\n{\n");
            writer.Write("    public override void* GetOrCreateComInterfaceForObject(object value)\n    {\n");
            writer.Write("        return WindowsRuntimeComWrappersMarshal.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.");
            writer.Write(hasReferenceFields ? "TrackerSupport" : "None");
            writer.Write(");\n    }\n\n");
            writer.Write("    public override ComInterfaceEntry* ComputeVtables(out int count)\n    {\n");
            writer.Write("        count = sizeof(ReferenceInterfaceEntries) / sizeof(ComInterfaceEntry);\n");
            writer.Write("        return (ComInterfaceEntry*)Unsafe.AsPointer(in ");
            writer.Write(nameStripped);
            writer.Write("InterfaceEntriesImpl.Entries);\n    }\n\n");
            writer.Write("    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)\n    {\n");
            writer.Write("        wrapperFlags = CreatedWrapperFlags.NonWrapping;\n");
            if (isComplexStruct)
            {
                writer.Write("        return ");
                writer.Write(nameStripped);
                writer.Write("Marshaller.ConvertToManaged(WindowsRuntimeValueTypeMarshaller.UnboxToManagedUnsafe<");
                WriteTypedefName(writer, context, type, TypedefNameType.ABI, true);
                writer.Write(">(value, in ");
                writer.Write(iidRefExpr);
                writer.Write("));\n");
            }
            else
            {
                writer.Write("        return WindowsRuntimeValueTypeMarshaller.UnboxToManagedUnsafe<");
                WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
                writer.Write(">(value, in ");
                writer.Write(iidRefExpr);
                writer.Write(");\n");
            }
            writer.Write("    }\n}\n");
        }
        else
        {
            // Fallback: keep the placeholder class so consumer attribute references resolve.
            writer.Write("internal sealed class ");
            writer.Write(nameStripped);
            writer.Write("ComWrappersMarshallerAttribute : global::System.Attribute\n{\n}\n");
        }
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

    /// <summary>
    /// Emits the [UnsafeAccessor] declaration for the default interface IID inside a file-scoped
    /// ComWrappers class. Only emits if the default interface is a generic instantiation.
    /// behavior of inserting <c>write_unsafe_accessor_for_iid</c> at the top of the class body.
    /// </summary>
    internal static void EmitUnsafeAccessorForDefaultIfaceIfGeneric(IndentedTextWriter writer, ProjectionEmitContext context, ITypeDefOrRef? defaultIface)
    {
        if (defaultIface is TypeSpecification ts && ts.Signature is GenericInstanceTypeSignature gi)
        {
            EmitUnsafeAccessorForIid(writer, context, gi);
        }
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

    /// <summary>
    /// Emits the per-interface members (methods, properties, events) into an already-open Methods
    /// static class. Used both for the standalone case and for the fast-abi merged emission.
    /// </summary>
    internal static void EmitMethodsClassMembersFor(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type, int startSlot, bool skipExclusiveEvents)
    {
        // Build a map from each MethodDefinition to its WinMD vtable slot.
        // In AsmResolver, type.Methods is iterated in MethodDef row order, so the position of each
        // method in type.Methods (relative to the first method of the type) gives us the same value.
        Dictionary<MethodDefinition, int> methodSlot = new();
        {
            int idx = 0;
            foreach (MethodDefinition m in type.Methods)
            {
                methodSlot[m] = idx + startSlot;
                idx++;
            }
        }

        // Emit non-special methods first (output order is unchanged from before; only the slot lookup changes).
        foreach (MethodDefinition method in type.Methods)
        {
            if (method.IsSpecial()) { continue; }
            string mname = method.Name?.Value ?? string.Empty;
            MethodSig sig = new(method);

            writer.Write("    [MethodImpl(MethodImplOptions.NoInlining)]\n");
            writer.Write("    public static unsafe ");
            WriteProjectionReturnType(writer, context, sig);
            writer.Write(" ");
            writer.Write(mname);
            writer.Write("(WindowsRuntimeObjectReference thisReference");
            if (sig.Params.Count > 0) { writer.Write(", "); }
            WriteParameterList(writer, context, sig);
            writer.Write(")");

            // Emit the body if we can handle this case. Slot comes from the method's WinMD index.
            EmitAbiMethodBodyIfSimple(writer, context, sig, methodSlot[method], isNoExcept: method.IsNoExcept());
        }

        // Emit property accessors. Each getter / setter consumes one vtable slot — looked up from the underlying method.
        foreach (PropertyDefinition prop in type.Properties)
        {
            string pname = prop.Name?.Value ?? string.Empty;
            (MethodDefinition? getter, MethodDefinition? setter) = prop.GetPropertyMethods();
            string propType = WritePropType(context, prop);
            (MethodDefinition? gMethod, MethodDefinition? sMethod) = (getter, setter);
            // accessors of the property (the attribute is on the property itself, not on the
            // individual accessors).
            bool propIsNoExcept = prop.IsNoExcept();
            if (gMethod is not null)
            {
                MethodSig getSig = new(gMethod);
                writer.Write("    [MethodImpl(MethodImplOptions.NoInlining)]\n");
                writer.Write("    public static unsafe ");
                writer.Write(propType);
                writer.Write(" ");
                writer.Write(pname);
                writer.Write("(WindowsRuntimeObjectReference thisReference)");
                EmitAbiMethodBodyIfSimple(writer, context, getSig, methodSlot[gMethod], isNoExcept: propIsNoExcept);
            }
            if (sMethod is not null)
            {
                MethodSig setSig = new(sMethod);
                writer.Write("    [MethodImpl(MethodImplOptions.NoInlining)]\n");
                writer.Write("    public static unsafe void ");
                writer.Write(pname);
                writer.Write("(WindowsRuntimeObjectReference thisReference, ");
                // form of write_prop_type, which for SZ array types emits ReadOnlySpan<T> instead
                // of T[] (the getter's return-type form).
                writer.Write(WritePropType(context, prop, isSetProperty: true));
                writer.Write(" value)");
                EmitAbiMethodBodyIfSimple(writer, context, setSig, methodSlot[sMethod], paramNameOverride: "value", isNoExcept: propIsNoExcept);
            }
        }

        // Emit event member methods (returns an event source, takes thisObject + thisReference).
        // Skip events on exclusive interfaces used by their class — they're inlined directly in
        // the RCW class. (Mirrors C++ skip_exclusive_events.)
        foreach (EventDefinition evt in type.Events)
        {
            if (skipExclusiveEvents) { continue; }
            string evtName = evt.Name?.Value ?? string.Empty;
            AsmResolver.DotNet.Signatures.TypeSignature evtSig = evt.EventType!.ToTypeSignature(false);
            bool isGenericEvent = evtSig is AsmResolver.DotNet.Signatures.GenericInstanceTypeSignature;

            // Use the add method's WinMD slot. Mirrors C++: events use the add_X method's vmethod_index.
            (MethodDefinition? addMethod, MethodDefinition? _) = evt.GetEventMethods();
            int eventSlot = addMethod is not null && methodSlot.TryGetValue(addMethod, out int es) ? es : 0;

            // Build the projected event source type name. For non-generic delegate handlers, the
            // EventSource subclass lives in the ABI namespace alongside this Methods class, so
            // we need to use the ABI-qualified name. For generic handlers (Windows.Foundation.*EventHandler),
            // it's mapped to global::WindowsRuntime.InteropServices.EventHandlerEventSource<...>.
            string eventSourceProjectedFull;
            if (isGenericEvent)
            {
                IndentedTextWriter __scratchEvSrcGeneric = new();
                WriteTypeName(__scratchEvSrcGeneric, context, TypeSemanticsFactory.Get(evtSig), TypedefNameType.EventSource, true);
                eventSourceProjectedFull = __scratchEvSrcGeneric.ToString();
                if (!eventSourceProjectedFull.StartsWith("global::", System.StringComparison.Ordinal))
                {
                    eventSourceProjectedFull = "global::" + eventSourceProjectedFull;
                }
            }
            else
            {
                // Non-generic delegate handler: the EventSource lives in the same ABI namespace
                // as this Methods class, so we use just the short name (matches truth output).
                string delegateName = string.Empty;
                if (evtSig is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td)
                {
                    delegateName = td.Type?.Name?.Value ?? string.Empty;
                    delegateName = IdentifierEscaping.StripBackticks(delegateName);
                }
                eventSourceProjectedFull = delegateName + "EventSource";
            }
            string eventSourceInteropType = isGenericEvent
                ? EncodeInteropTypeName(evtSig, TypedefNameType.EventSource) + ", WinRT.Interop"
                : string.Empty;

            // Emit the per-event ConditionalWeakTable static field.
            writer.Write("\n    private static ConditionalWeakTable<object, ");
            writer.Write(eventSourceProjectedFull);
            writer.Write("> _");
            writer.Write(evtName);
            writer.Write("\n    {\n");
            writer.Write("        [MethodImpl(MethodImplOptions.AggressiveInlining)]\n");
            writer.Write("        get\n        {\n");
            writer.Write("            [MethodImpl(MethodImplOptions.NoInlining)]\n");
            writer.Write("            static ConditionalWeakTable<object, ");
            writer.Write(eventSourceProjectedFull);
            writer.Write("> MakeTable()\n            {\n");
            writer.Write("                _ = global::System.Threading.Interlocked.CompareExchange(ref field, [], null);\n\n");
            writer.Write("                return global::System.Threading.Volatile.Read(in field);\n");
            writer.Write("            }\n\n");
            writer.Write("            return global::System.Threading.Volatile.Read(in field) ?? MakeTable();\n        }\n    }\n");

            // Emit the static method that returns the per-instance event source.
            writer.Write("\n    public static ");
            writer.Write(eventSourceProjectedFull);
            writer.Write(" ");
            writer.Write(evtName);
            writer.Write("(object thisObject, WindowsRuntimeObjectReference thisReference)\n    {\n");
            if (isGenericEvent && !string.IsNullOrEmpty(eventSourceInteropType))
            {
                writer.Write("        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]\n");
                writer.Write("        [return: UnsafeAccessorType(\"");
                writer.Write(eventSourceInteropType);
                writer.Write("\")]\n");
                writer.Write("        static extern object ctor(WindowsRuntimeObjectReference nativeObjectReference, int index);\n\n");
                writer.Write("        return _");
                writer.Write(evtName);
                writer.Write(".GetOrAdd(\n");
                writer.Write("            key: thisObject,\n");
                writer.Write("            valueFactory: static (_, thisReference) => Unsafe.As<");
                writer.Write(eventSourceProjectedFull);
                writer.Write(">(ctor(thisReference, ");
                writer.Write(eventSlot.ToString(System.Globalization.CultureInfo.InvariantCulture));
                writer.Write(")),\n");
                writer.Write("            factoryArgument: thisReference);\n");
            }
            else
            {
                // Non-generic delegate: directly construct.
                writer.Write("        return _");
                writer.Write(evtName);
                writer.Write(".GetOrAdd(\n");
                writer.Write("            key: thisObject,\n");
                writer.Write("            valueFactory: static (_, thisReference) => new ");
                writer.Write(eventSourceProjectedFull);
                writer.Write("(thisReference, ");
                writer.Write(eventSlot.ToString(System.Globalization.CultureInfo.InvariantCulture));
                writer.Write("),\n");
                writer.Write("            factoryArgument: thisReference);\n");
            }
            writer.Write("    }\n");
        }
    }

    /// <summary>
    /// Emits a real method body for the cases we can fully marshal, otherwise emits
    /// the 'throw null!' stub. Trailing newline is included.
    /// </summary>
    /// <param name="isNoExcept">When true, the vtable call is emitted WITHOUT the
    /// <c>RestrictedErrorInfo.ThrowExceptionForHR(...)</c> wrap. Mirrors C++
    /// <c>code_writers.h:6725</c> which checks <c>has_noexcept_attr</c>
    /// (<c>is_noexcept(MethodDef)</c> / <c>is_noexcept(Property)</c> in <c>helpers.h:41-49</c>):
    /// methods/properties annotated with <c>[Windows.Foundation.Metadata.NoExceptionAttribute]</c>
    /// (or remove-overload methods) contractually return <c>S_OK</c>, so the wrap is omitted.</param>
    internal static void EmitAbiMethodBodyIfSimple(IndentedTextWriter writer, ProjectionEmitContext context, MethodSig sig, int slot, string? paramNameOverride = null, bool isNoExcept = false)
    {
        AsmResolver.DotNet.Signatures.TypeSignature? rt = sig.ReturnType;

        bool returnIsString = rt is not null && rt.IsString();
        bool returnIsRefType = rt is not null && (IsRuntimeClassOrInterface(context.Cache, rt) || rt.IsObject() || rt.IsGenericInstance());
        bool returnIsAnyStruct = rt is not null && IsAnyStruct(context.Cache, rt);
        bool returnIsComplexStruct = rt is not null && IsComplexStruct(context.Cache, rt);
        bool returnIsReceiveArray = rt is AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSzCheck
            && (IsBlittablePrimitive(context.Cache, retSzCheck.BaseType) || IsAnyStruct(context.Cache, retSzCheck.BaseType)
                || retSzCheck.BaseType.IsString() || IsRuntimeClassOrInterface(context.Cache, retSzCheck.BaseType) || retSzCheck.BaseType.IsObject()
                || IsComplexStruct(context.Cache, retSzCheck.BaseType)
                || retSzCheck.BaseType.IsHResultException()
                || IsMappedAbiValueType(retSzCheck.BaseType));
        bool returnIsHResultException = rt is not null && rt.IsHResultException();

        // Build the function pointer signature: void*, [paramAbiType...,] [retAbiType*,] int
        System.Text.StringBuilder fp = new();
        fp.Append("void*");
        foreach (ParamInfo p in sig.Params)
        {
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
            {
                fp.Append(", uint, void*");
                continue;
            }
            if (cat == ParamCategory.Out)
            {
                AsmResolver.DotNet.Signatures.TypeSignature uOut = StripByRefAndCustomModifiers(p.Type);
                fp.Append(", ");
                if (uOut.IsString() || IsRuntimeClassOrInterface(context.Cache, uOut) || uOut.IsObject() || uOut.IsGenericInstance()) { fp.Append("void**"); }
                else if (uOut.IsSystemType()) { fp.Append("global::ABI.System.Type*"); }
                else if (IsComplexStruct(context.Cache, uOut)) { fp.Append(GetAbiStructTypeName(writer, context, uOut)); fp.Append('*'); }
                else if (IsAnyStruct(context.Cache, uOut)) { fp.Append(GetBlittableStructAbiType(writer, context, uOut)); fp.Append('*'); }
                else { fp.Append(GetAbiPrimitiveType(context.Cache, uOut)); fp.Append('*'); }
                continue;
            }
            if (cat == ParamCategory.Ref)
            {
                AsmResolver.DotNet.Signatures.TypeSignature uRef = StripByRefAndCustomModifiers(p.Type);
                fp.Append(", ");
                if (IsComplexStruct(context.Cache, uRef)) { fp.Append(GetAbiStructTypeName(writer, context, uRef)); fp.Append('*'); }
                else if (IsAnyStruct(context.Cache, uRef)) { fp.Append(GetBlittableStructAbiType(writer, context, uRef)); fp.Append('*'); }
                else { fp.Append(GetAbiPrimitiveType(context.Cache, uRef)); fp.Append('*'); }
                continue;
            }
            if (cat == ParamCategory.ReceiveArray)
            {
                AsmResolver.DotNet.Signatures.SzArrayTypeSignature sza = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)StripByRefAndCustomModifiers(p.Type);
                fp.Append(", uint*, ");
                if (sza.BaseType.IsString() || IsRuntimeClassOrInterface(context.Cache, sza.BaseType) || sza.BaseType.IsObject())
                {
                    fp.Append("void*");
                }
                else if (sza.BaseType.IsHResultException())
                {
                    fp.Append("global::ABI.System.Exception");
                }
                else if (IsMappedAbiValueType(sza.BaseType))
                {
                    fp.Append(GetMappedAbiTypeName(sza.BaseType));
                }
                else if (IsComplexStruct(context.Cache, sza.BaseType)) { fp.Append(GetAbiStructTypeName(writer, context, sza.BaseType)); }
                else if (IsAnyStruct(context.Cache, sza.BaseType)) { fp.Append(GetBlittableStructAbiType(writer, context, sza.BaseType)); }
                else { fp.Append(GetAbiPrimitiveType(context.Cache, sza.BaseType)); }
                fp.Append("**");
                continue;
            }
            fp.Append(", ");
            if (p.Type.IsHResultException()) { fp.Append("global::ABI.System.Exception"); }
            else if (p.Type.IsString() || IsRuntimeClassOrInterface(context.Cache, p.Type) || p.Type.IsObject() || p.Type.IsGenericInstance()) { fp.Append("void*"); }
            else if (p.Type.IsSystemType()) { fp.Append("global::ABI.System.Type"); }
            else if (IsAnyStruct(context.Cache, p.Type)) { fp.Append(GetBlittableStructAbiType(writer, context, p.Type)); }
            else if (IsMappedAbiValueType(p.Type)) { fp.Append(GetMappedAbiTypeName(p.Type)); }
            else if (IsComplexStruct(context.Cache, p.Type)) { fp.Append(GetAbiStructTypeName(writer, context, p.Type)); }
            else { fp.Append(GetAbiPrimitiveType(context.Cache, p.Type)); }
        }
        if (rt is not null)
        {
            if (returnIsReceiveArray)
            {
                AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSz = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)rt;
                fp.Append(", uint*, ");
                if (retSz.BaseType.IsString() || IsRuntimeClassOrInterface(context.Cache, retSz.BaseType) || retSz.BaseType.IsObject())
                {
                    fp.Append("void*");
                }
                else if (IsComplexStruct(context.Cache, retSz.BaseType))
                {
                    fp.Append(GetAbiStructTypeName(writer, context, retSz.BaseType));
                }
                else if (retSz.BaseType.IsHResultException())
                {
                    fp.Append("global::ABI.System.Exception");
                }
                else if (IsMappedAbiValueType(retSz.BaseType))
                {
                    fp.Append(GetMappedAbiTypeName(retSz.BaseType));
                }
                else if (IsAnyStruct(context.Cache, retSz.BaseType))
                {
                    fp.Append(GetBlittableStructAbiType(writer, context, retSz.BaseType));
                }
                else
                {
                    fp.Append(GetAbiPrimitiveType(context.Cache, retSz.BaseType));
                }
                fp.Append("**");
            }
            else if (returnIsHResultException)
            {
                fp.Append(", global::ABI.System.Exception*");
            }
            else
            {
                fp.Append(", ");
                if (returnIsString || returnIsRefType) { fp.Append("void**"); }
                else if (rt is not null && rt.IsSystemType()) { fp.Append("global::ABI.System.Type*"); }
                else if (returnIsAnyStruct) { fp.Append(GetBlittableStructAbiType(writer, context, rt!)); fp.Append('*'); }
                else if (returnIsComplexStruct) { fp.Append(GetAbiStructTypeName(writer, context, rt!)); fp.Append('*'); }
                else if (rt is not null && IsMappedAbiValueType(rt)) { fp.Append(GetMappedAbiTypeName(rt)); fp.Append('*'); }
                else { fp.Append(GetAbiPrimitiveType(context.Cache, rt!)); fp.Append('*'); }
            }
        }
        fp.Append(", int");

        writer.Write("\n    {\n");
        writer.Write("        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();\n");
        writer.Write("        void* ThisPtr = thisValue.GetThisPtrUnsafe();\n");

        // Declare 'using' marshaller values for ref-type parameters (these need disposing).
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            if (IsRuntimeClassOrInterface(context.Cache, p.Type) || p.Type.IsObject())
            {
                string localName = GetParamLocalName(p, paramNameOverride);
                string callName = GetParamName(p, paramNameOverride);
                writer.Write("        using WindowsRuntimeObjectReferenceValue __");
                writer.Write(localName);
                writer.Write(" = ");
                EmitMarshallerConvertToUnmanaged(writer, context, p.Type, callName);
                writer.Write(";\n");
            }
            else if (p.Type.IsNullableT())
            {
                // Nullable<T> param: use <T>Marshaller.BoxToUnmanaged. Mirrors truth pattern.
                string localName = GetParamLocalName(p, paramNameOverride);
                string callName = GetParamName(p, paramNameOverride);
                AsmResolver.DotNet.Signatures.TypeSignature inner = p.Type.GetNullableInnerType()!;
                string innerMarshaller = GetNullableInnerMarshallerName(writer, context, inner);
                writer.Write("        using WindowsRuntimeObjectReferenceValue __");
                writer.Write(localName);
                writer.Write(" = ");
                writer.Write(innerMarshaller);
                writer.Write(".BoxToUnmanaged(");
                writer.Write(callName);
                writer.Write(");\n");
            }
            else if (p.Type.IsGenericInstance())
            {
                // Generic instance param: emit a local UnsafeAccessor delegate to get the marshaller method.
                string localName = GetParamLocalName(p, paramNameOverride);
                string callName = GetParamName(p, paramNameOverride);
                string interopTypeName = EncodeInteropTypeName(p.Type, TypedefNameType.ABI) + ", WinRT.Interop";
                IndentedTextWriter __scratchProjectedTypeName = new();
                WriteProjectedSignature(__scratchProjectedTypeName, context, p.Type, false);
                string projectedTypeName = __scratchProjectedTypeName.ToString();
                writer.Write("        [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToUnmanaged\")]\n");
                writer.Write("        static extern WindowsRuntimeObjectReferenceValue ConvertToUnmanaged_");
                writer.Write(localName);
                writer.Write("([UnsafeAccessorType(\"");
                writer.Write(interopTypeName);
                writer.Write("\")] object _, ");
                writer.Write(projectedTypeName);
                writer.Write(" value);\n");
                writer.Write("        using WindowsRuntimeObjectReferenceValue __");
                writer.Write(localName);
                writer.Write(" = ConvertToUnmanaged_");
                writer.Write(localName);
                writer.Write("(null, ");
                writer.Write(callName);
                writer.Write(");\n");
            }
        }
        // (String input params are now stack-allocated via the fast-path pinning pattern below;
        //  no separate void* local declaration or up-front allocation is needed.)
        // Declare locals for HResult/Exception input parameters (converted up-front).
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            if (ParamHelpers.GetParamCategory(p) != ParamCategory.In) { continue; }
            if (!p.Type.IsHResultException()) { continue; }
            string localName = GetParamLocalName(p, paramNameOverride);
            string callName = GetParamName(p, paramNameOverride);
            writer.Write("        global::ABI.System.Exception __");
            writer.Write(localName);
            writer.Write(" = global::ABI.System.ExceptionMarshaller.ConvertToUnmanaged(");
            writer.Write(callName);
            writer.Write(");\n");
        }
        // Declare locals for mapped value-type input parameters (DateTime/TimeSpan): convert via marshaller up-front.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            if (ParamHelpers.GetParamCategory(p) != ParamCategory.In) { continue; }
            if (!IsMappedAbiValueType(p.Type)) { continue; }
            string localName = GetParamLocalName(p, paramNameOverride);
            string callName = GetParamName(p, paramNameOverride);
            writer.Write("        ");
            writer.Write(GetMappedAbiTypeName(p.Type));
            writer.Write(" __");
            writer.Write(localName);
            writer.Write(" = ");
            writer.Write(GetMappedMarshallerName(p.Type));
            writer.Write(".ConvertToUnmanaged(");
            writer.Write(callName);
            writer.Write(");\n");
        }
        // Declare locals for complex-struct input parameters (e.g. ProfileUsage with nested
        // string/Nullable fields): default-initialize OUTSIDE try, assign inside try via marshaller,
        // dispose in finally. Mirrors C++ behavior for non-blittable struct input params.
        // Includes both 'in' (ParamCategory.In) and 'in T' (ParamCategory.Ref) forms.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.In && cat != ParamCategory.Ref) { continue; }
            AsmResolver.DotNet.Signatures.TypeSignature pType = StripByRefAndCustomModifiers(p.Type);
            if (!IsComplexStruct(context.Cache, pType)) { continue; }
            string localName = GetParamLocalName(p, paramNameOverride);
            writer.Write("        ");
            writer.Write(GetAbiStructTypeName(writer, context, pType));
            writer.Write(" __");
            writer.Write(localName);
            writer.Write(" = default;\n");
        }
        // Declare locals for Out parameters (need to be passed as &__<name> to the call).
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.Out) { continue; }
            string localName = GetParamLocalName(p, paramNameOverride);
            AsmResolver.DotNet.Signatures.TypeSignature uOut = StripByRefAndCustomModifiers(p.Type);
            writer.Write("        ");
            if (uOut.IsString() || IsRuntimeClassOrInterface(context.Cache, uOut) || uOut.IsObject() || uOut.IsGenericInstance()) { writer.Write("void*"); }
            else if (uOut.IsSystemType()) { writer.Write("global::ABI.System.Type"); }
            else if (IsComplexStruct(context.Cache, uOut)) { writer.Write(GetAbiStructTypeName(writer, context, uOut)); }
            else if (IsAnyStruct(context.Cache, uOut)) { writer.Write(GetBlittableStructAbiType(writer, context, uOut)); }
            else { writer.Write(GetAbiPrimitiveType(context.Cache, uOut)); }
            writer.Write(" __");
            writer.Write(localName);
            writer.Write(" = default;\n");
        }
        // Declare locals for ReceiveArray params (uint length + element pointer).
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.ReceiveArray) { continue; }
            string localName = GetParamLocalName(p, paramNameOverride);
            AsmResolver.DotNet.Signatures.SzArrayTypeSignature sza = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)StripByRefAndCustomModifiers(p.Type);
            writer.Write("        uint __");
            writer.Write(localName);
            writer.Write("_length = default;\n");
            writer.Write("        ");
            // Element ABI type: void* for ref types; ABI struct for complex/blittable structs;
            // primitive ABI otherwise.
            if (sza.BaseType.IsString() || IsRuntimeClassOrInterface(context.Cache, sza.BaseType) || sza.BaseType.IsObject())
            {
                writer.Write("void*");
            }
            else if (IsComplexStruct(context.Cache, sza.BaseType))
            {
                writer.Write(GetAbiStructTypeName(writer, context, sza.BaseType));
            }
            else if (IsAnyStruct(context.Cache, sza.BaseType))
            {
                writer.Write(GetBlittableStructAbiType(writer, context, sza.BaseType));
            }
            else
            {
                writer.Write(GetAbiPrimitiveType(context.Cache, sza.BaseType));
            }
            writer.Write("* __");
            writer.Write(localName);
            writer.Write("_data = default;\n");
        }
        // Declare InlineArray16 + ArrayPool fallback for non-blittable PassArray params
        // (runtime classes, objects, strings). Runtime class/object: just one InlineArray16<nint>.
        // String: also needs InlineArray16<HStringHeader> + InlineArray16<nint> for pinned handles.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
            if (IsBlittablePrimitive(context.Cache, szArr.BaseType) || IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
            // Non-blittable element type: emit InlineArray16<storageT> + ArrayPool<storageT>.
            // For mapped value types (DateTime/TimeSpan), use the ABI struct type.
            // For complex structs (e.g. authored BasicStruct with reference fields), use the ABI
            // struct type. For everything else (runtime classes, objects, strings), use nint.
            string localName = GetParamLocalName(p, paramNameOverride);
            string callName = GetParamName(p, paramNameOverride);
            string storageT = IsMappedAbiValueType(szArr.BaseType)
                ? GetMappedAbiTypeName(szArr.BaseType)
                : IsComplexStruct(context.Cache, szArr.BaseType)
                    ? GetAbiStructTypeName(writer, context, szArr.BaseType)
                    : szArr.BaseType.IsHResultException()
                        ? "global::ABI.System.Exception"
                        : "nint";
            writer.Write("\n        Unsafe.SkipInit(out InlineArray16<");
            writer.Write(storageT);
            writer.Write("> __");
            writer.Write(localName);
            writer.Write("_inlineArray);\n");
            writer.Write("        ");
            writer.Write(storageT);
            writer.Write("[] __");
            writer.Write(localName);
            writer.Write("_arrayFromPool = null;\n");
            writer.Write("        Span<");
            writer.Write(storageT);
            writer.Write("> __");
            writer.Write(localName);
            writer.Write("_span = ");
            writer.Write(callName);
            writer.Write(".Length <= 16\n            ? __");
            writer.Write(localName);
            writer.Write("_inlineArray[..");
            writer.Write(callName);
            writer.Write(".Length]\n            : (__");
            writer.Write(localName);
            writer.Write("_arrayFromPool = global::System.Buffers.ArrayPool<");
            writer.Write(storageT);
            writer.Write(">.Shared.Rent(");
            writer.Write(callName);
            writer.Write(".Length));\n");

            if (szArr.BaseType.IsString() && cat == ParamCategory.PassArray)
            {
                // Strings need an additional InlineArray16<HStringHeader> + InlineArray16<nint> (pinned handles).
                // Only required for PassArray (managed -> HSTRING conversion); FillArray's native side
                // fills HSTRING handles directly into the nint storage.
                writer.Write("\n        Unsafe.SkipInit(out InlineArray16<HStringHeader> __");
                writer.Write(localName);
                writer.Write("_inlineHeaderArray);\n");
                writer.Write("        HStringHeader[] __");
                writer.Write(localName);
                writer.Write("_headerArrayFromPool = null;\n");
                writer.Write("        Span<HStringHeader> __");
                writer.Write(localName);
                writer.Write("_headerSpan = ");
                writer.Write(callName);
                writer.Write(".Length <= 16\n            ? __");
                writer.Write(localName);
                writer.Write("_inlineHeaderArray[..");
                writer.Write(callName);
                writer.Write(".Length]\n            : (__");
                writer.Write(localName);
                writer.Write("_headerArrayFromPool = global::System.Buffers.ArrayPool<HStringHeader>.Shared.Rent(");
                writer.Write(callName);
                writer.Write(".Length));\n");

                writer.Write("\n        Unsafe.SkipInit(out InlineArray16<nint> __");
                writer.Write(localName);
                writer.Write("_inlinePinnedHandleArray);\n");
                writer.Write("        nint[] __");
                writer.Write(localName);
                writer.Write("_pinnedHandleArrayFromPool = null;\n");
                writer.Write("        Span<nint> __");
                writer.Write(localName);
                writer.Write("_pinnedHandleSpan = ");
                writer.Write(callName);
                writer.Write(".Length <= 16\n            ? __");
                writer.Write(localName);
                writer.Write("_inlinePinnedHandleArray[..");
                writer.Write(callName);
                writer.Write(".Length]\n            : (__");
                writer.Write(localName);
                writer.Write("_pinnedHandleArrayFromPool = global::System.Buffers.ArrayPool<nint>.Shared.Rent(");
                writer.Write(callName);
                writer.Write(".Length));\n");
            }
        }
        if (returnIsReceiveArray)
        {
            AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSz = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)rt!;
            writer.Write("        uint __retval_length = default;\n");
            writer.Write("        ");
            if (retSz.BaseType.IsString() || IsRuntimeClassOrInterface(context.Cache, retSz.BaseType) || retSz.BaseType.IsObject())
            {
                writer.Write("void*");
            }
            else if (IsComplexStruct(context.Cache, retSz.BaseType))
            {
                writer.Write(GetAbiStructTypeName(writer, context, retSz.BaseType));
            }
            else if (retSz.BaseType.IsHResultException())
            {
                writer.Write("global::ABI.System.Exception");
            }
            else if (IsMappedAbiValueType(retSz.BaseType))
            {
                writer.Write(GetMappedAbiTypeName(retSz.BaseType));
            }
            else if (IsAnyStruct(context.Cache, retSz.BaseType))
            {
                writer.Write(GetBlittableStructAbiType(writer, context, retSz.BaseType));
            }
            else
            {
                writer.Write(GetAbiPrimitiveType(context.Cache, retSz.BaseType));
            }
            writer.Write("* __retval_data = default;\n");
        }
        else if (returnIsHResultException)
        {
            writer.Write("        global::ABI.System.Exception __retval = default;\n");
        }
        else if (returnIsString || returnIsRefType)
        {
            writer.Write("        void* __retval = default;\n");
        }
        else if (returnIsAnyStruct)
        {
            writer.Write("        ");
            writer.Write(GetBlittableStructAbiType(writer, context, rt!));
            writer.Write(" __retval = default;\n");
        }
        else if (returnIsComplexStruct)
        {
            writer.Write("        ");
            writer.Write(GetAbiStructTypeName(writer, context, rt!));
            writer.Write(" __retval = default;\n");
        }
        else if (rt is not null && IsMappedAbiValueType(rt))
        {
            // Mapped value type return (e.g. DateTime/TimeSpan): use the ABI struct as __retval.
            writer.Write("        ");
            writer.Write(GetMappedAbiTypeName(rt));
            writer.Write(" __retval = default;\n");
        }
        else if (rt is not null && rt.IsSystemType())
        {
            // System.Type return: use ABI Type struct as __retval.
            writer.Write("        global::ABI.System.Type __retval = default;\n");
        }
        else if (rt is not null)
        {
            writer.Write("        ");
            writer.Write(GetAbiPrimitiveType(context.Cache, rt));
            writer.Write(" __retval = default;\n");
        }

        // Determine if we need a try/finally (for cleanup of string/refType return or receive array
        // return or Out runtime class params). Input string params no longer need try/finally —
        // they use the HString fast-path (stack-allocated HStringReference, no free needed).
        bool hasOutNeedsCleanup = false;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.Out) { continue; }
            AsmResolver.DotNet.Signatures.TypeSignature uOut = StripByRefAndCustomModifiers(p.Type);
            if (uOut.IsString() || IsRuntimeClassOrInterface(context.Cache, uOut) || uOut.IsObject() || uOut.IsSystemType() || IsComplexStruct(context.Cache, uOut) || uOut.IsGenericInstance()) { hasOutNeedsCleanup = true; break; }
        }
        bool hasReceiveArray = false;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            if (ParamHelpers.GetParamCategory(sig.Params[i]) == ParamCategory.ReceiveArray) { hasReceiveArray = true; break; }
        }
        bool hasNonBlittablePassArray = false;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if ((cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
                && p.Type is AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArrCheck
                && !IsBlittablePrimitive(context.Cache, szArrCheck.BaseType) && !IsAnyStruct(context.Cache, szArrCheck.BaseType)
                && !IsMappedAbiValueType(szArrCheck.BaseType))
            {
                hasNonBlittablePassArray = true; break;
            }
        }
        bool hasComplexStructInput = false;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if ((cat == ParamCategory.In || cat == ParamCategory.Ref) && IsComplexStruct(context.Cache, StripByRefAndCustomModifiers(p.Type))) { hasComplexStructInput = true; break; }
        }
        // System.Type return: ABI.System.Type contains an HSTRING that must be disposed
        // after marshalling to managed System.Type, otherwise the HSTRING leaks. Mirrors
        // C++ abi_marshaler::write_dispose path for is_out + non-empty marshaler_type.
        bool returnIsSystemTypeForCleanup = rt is not null && rt.IsSystemType();
        bool needsTryFinally = returnIsString || returnIsRefType || returnIsReceiveArray || hasOutNeedsCleanup || hasReceiveArray || returnIsComplexStruct || hasNonBlittablePassArray || hasComplexStructInput || returnIsSystemTypeForCleanup;
        if (needsTryFinally) { writer.Write("        try\n        {\n"); }

        string indent = needsTryFinally ? "            " : "        ";

        // Inside try (if applicable): assign complex-struct input locals via marshaller.
        // Mirrors truth pattern: '__value = ProfileUsageMarshaller.ConvertToUnmanaged(value);'
        // Includes both 'in' (ParamCategory.In) and 'in T' (ParamCategory.Ref) forms.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.In && cat != ParamCategory.Ref) { continue; }
            AsmResolver.DotNet.Signatures.TypeSignature pType = StripByRefAndCustomModifiers(p.Type);
            if (!IsComplexStruct(context.Cache, pType)) { continue; }
            string localName = GetParamLocalName(p, paramNameOverride);
            string callName = GetParamName(p, paramNameOverride);
            writer.Write(indent);
            writer.Write("__");
            writer.Write(localName);
            writer.Write(" = ");
            writer.Write(GetMarshallerFullName(writer, context, pType));
            writer.Write(".ConvertToUnmanaged(");
            writer.Write(callName);
            writer.Write(");\n");
        }
        // Type input params: set up TypeReference locals before the fixed block. Mirrors truth:
        //   global::ABI.System.TypeMarshaller.ConvertToUnmanagedUnsafe(forType, out TypeReference __forType);
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            if (ParamHelpers.GetParamCategory(p) != ParamCategory.In) { continue; }
            if (!p.Type.IsSystemType()) { continue; }
            string localName = GetParamLocalName(p, paramNameOverride);
            string callName = GetParamName(p, paramNameOverride);
            writer.Write(indent);
            writer.Write("global::ABI.System.TypeMarshaller.ConvertToUnmanagedUnsafe(");
            writer.Write(callName);
            writer.Write(", out TypeReference __");
            writer.Write(localName);
            writer.Write(");\n");
        }
        // Open a SINGLE fixed-block for ALL pinnable inputs (mirrors C++ write_abi_invoke):
        //   1. Ref params (typed ptr, separate "fixed(T* _x = &x)\n" lines, no braces)
        //   2. Complex-struct PassArrays (typed ptr, separate fixed line)
        //   3. All other "void*"-style pinnables (strings, Type[], blittable PassArrays,
        //      reference-type PassArrays via inline-pool span) merged into ONE
        //      "fixed(void* _a = ..., _b = ..., ...) {\n" block.
        //
        // C# allows multiple chained "fixed(...)" without braces to share the next braced
        // body, which is what the C++ tool emits. This avoids the deep nesting mine had
        // when emitting a separate fixed block per PassArray.
        int fixedNesting = 0;

        // Step 1: Emit typed-pointer fixed lines for Ref params and complex-struct PassArrays
        // (no braces - they share the body of the upcoming combined fixed-void* block, OR
        // each other if no void* block is needed).
        bool hasAnyVoidStarPinnable = false;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (p.Type.IsString() || p.Type.IsSystemType()) { hasAnyVoidStarPinnable = true; continue; }
            if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
            {
                // All PassArrays (including complex structs) go in the void* combined block,
                // matching truth's pattern. Complex structs use a (T*) cast at the call site.
                hasAnyVoidStarPinnable = true;
            }
        }
        // Emit typed fixed lines for Ref params.
        // Skip Ref+ComplexStruct: those are marshalled via __local (no fixed needed) and
        // passed as &__local at the call site, mirroring C++ tool's is_value_type_in path.
        int typedFixedCount = 0;
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat == ParamCategory.Ref)
            {
                AsmResolver.DotNet.Signatures.TypeSignature uRefSkip = StripByRefAndCustomModifiers(p.Type);
                if (IsComplexStruct(context.Cache, uRefSkip)) { continue; }
                string callName = GetParamName(p, paramNameOverride);
                string localName = GetParamLocalName(p, paramNameOverride);
                AsmResolver.DotNet.Signatures.TypeSignature uRef = uRefSkip;
                string abiType = IsAnyStruct(context.Cache, uRef) ? GetBlittableStructAbiType(writer, context, uRef) : GetAbiPrimitiveType(context.Cache, uRef);
                writer.Write(indent);
                writer.Write(new string(' ', fixedNesting * 4));
                writer.Write("fixed(");
                writer.Write(abiType);
                writer.Write("* _");
                writer.Write(localName);
                writer.Write(" = &");
                writer.Write(callName);
                writer.Write(")\n");
                typedFixedCount++;
            }
        }

        // Step 2: Emit ONE combined fixed-void* block for all pinnables that share the
        // same scope. Each variable is "_localName = rhsExpr". Strings get an extra
        // "_localName_inlineHeaderArray = __localName_headerSpan" entry.
        bool stringPinnablesEmitted = false;
        if (hasAnyVoidStarPinnable)
        {
            writer.Write(indent);
            writer.Write(new string(' ', fixedNesting * 4));
            writer.Write("fixed(void* ");
            bool first = true;
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                bool isString = p.Type.IsString();
                bool isType = p.Type.IsSystemType();
                bool isPassArray = cat == ParamCategory.PassArray || cat == ParamCategory.FillArray;
                if (!isString && !isType && !isPassArray) { continue; }
                string callName = GetParamName(p, paramNameOverride);
                string localName = GetParamLocalName(p, paramNameOverride);
                if (!first) { writer.Write(", "); }
                first = false;
                writer.Write("_");
                writer.Write(localName);
                writer.Write(" = ");
                if (isType)
                {
                    writer.Write("__");
                    writer.Write(localName);
                }
                else if (isPassArray)
                {
                    AsmResolver.DotNet.Signatures.TypeSignature elemT = ((AsmResolver.DotNet.Signatures.SzArrayTypeSignature)p.Type).BaseType;
                    bool isBlittableElem = IsBlittablePrimitive(context.Cache, elemT) || IsAnyStruct(context.Cache, elemT);
                    bool isStringElem = elemT.IsString();
                    if (isBlittableElem)
                    {
                        writer.Write(callName);
                    }
                    else
                    {
                        writer.Write("__");
                        writer.Write(localName);
                        writer.Write("_span");
                    }
                    // For string elements: only PassArray needs the additional inlineHeaderArray
                    // pinned alongside the data span. FillArray fills HSTRINGs into the nint
                    // storage directly (no header conversion needed).
                    if (isStringElem && cat == ParamCategory.PassArray)
                    {
                        writer.Write(", _");
                        writer.Write(localName);
                        writer.Write("_inlineHeaderArray = __");
                        writer.Write(localName);
                        writer.Write("_headerSpan");
                    }
                }
                else
                {
                    // string param
                    writer.Write(callName);
                }
            }
            writer.Write(")\n");
            writer.Write(indent);
            writer.Write(new string(' ', fixedNesting * 4));
            writer.Write("{\n");
            fixedNesting++;
            // Inside the body: emit HStringMarshaller calls for input string params.
            for (int i = 0; i < sig.Params.Count; i++)
            {
                if (!sig.Params[i].Type.IsString()) { continue; }
                string callName = GetParamName(sig.Params[i], paramNameOverride);
                string localName = GetParamLocalName(sig.Params[i], paramNameOverride);
                writer.Write(indent);
                writer.Write(new string(' ', fixedNesting * 4));
                writer.Write("HStringMarshaller.ConvertToUnmanagedUnsafe((char*)_");
                writer.Write(localName);
                writer.Write(", ");
                writer.Write(callName);
                writer.Write("?.Length, out HStringReference __");
                writer.Write(localName);
                writer.Write(");\n");
            }
            stringPinnablesEmitted = true;
        }
        else if (typedFixedCount > 0)
        {
            // Typed fixed lines exist but no void* combined block - we need a body block
            // to host them. Open a brace block after the last typed fixed line.
            writer.Write(indent);
            writer.Write(new string(' ', fixedNesting * 4));
            writer.Write("{\n");
            fixedNesting++;
        }
        // Suppress unused variable warning when block above doesn't fire.
        _ = stringPinnablesEmitted;

        string callIndent = indent + new string(' ', fixedNesting * 4);

        // For non-blittable PassArray params, emit CopyToUnmanaged_<name> (UnsafeAccessor) and call
        // it to populate the inline/pooled storage from the user-supplied span. For string arrays,
        // use HStringArrayMarshaller.ConvertToUnmanagedUnsafe instead.
        // FillArray of strings is the exception: the native side fills the HSTRING handles, so
        // there's nothing to convert pre-call (the post-call CopyToManaged_<name> handles writeback).
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
            if (IsBlittablePrimitive(context.Cache, szArr.BaseType) || IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
            string callName = GetParamName(p, paramNameOverride);
            string localName = GetParamLocalName(p, paramNameOverride);
            if (szArr.BaseType.IsString())
            {
                // Skip pre-call ConvertToUnmanagedUnsafe for FillArray of strings — there's
                // nothing to convert (native fills the handles). Mirrors C++ truth pattern.
                if (cat == ParamCategory.FillArray) { continue; }
                writer.Write(callIndent);
                writer.Write("HStringArrayMarshaller.ConvertToUnmanagedUnsafe(\n");
                writer.Write(callIndent);
                writer.Write("    source: ");
                writer.Write(callName);
                writer.Write(",\n");
                writer.Write(callIndent);
                writer.Write("    hstringHeaders: (HStringHeader*) _");
                writer.Write(localName);
                writer.Write("_inlineHeaderArray,\n");
                writer.Write(callIndent);
                writer.Write("    hstrings: __");
                writer.Write(localName);
                writer.Write("_span,\n");
                writer.Write(callIndent);
                writer.Write("    pinnedGCHandles: __");
                writer.Write(localName);
                writer.Write("_pinnedHandleSpan);\n");
            }
            else
            {
                // FillArray (Span<T>) of non-blittable element types: skip pre-call
                // CopyToUnmanaged. The buffer the native side gets (_<name>) is uninitialized
                // ABI-format storage; the native callee fills it. The post-call writeback loop
                // emits CopyToManaged_<name> to propagate the native fills into the user's
                // managed Span<T>. (Mirrors C++ marshaler.write_marshal_to_abi which only emits
                // CopyToUnmanaged for PassArray, not FillArray.)
                if (cat == ParamCategory.FillArray) { continue; }
                IndentedTextWriter __scratchElementProjected = new();
                WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(szArr.BaseType));
                string elementProjected = __scratchElementProjected.ToString();
                string elementInteropArg = EncodeInteropTypeName(szArr.BaseType, TypedefNameType.Projected);

                _ = elementInteropArg;
                // For mapped value types (DateTime/TimeSpan) and complex structs, the storage
                // element is the ABI struct type; the data pointer parameter type uses that
                // ABI struct. The fixed() opens with void* (per truth's pattern), so a cast
                // is required at the call site. For runtime classes/objects, use void**.
                string dataParamType;
                string dataCastType;
                if (IsMappedAbiValueType(szArr.BaseType))
                {
                    dataParamType = GetMappedAbiTypeName(szArr.BaseType) + "*";
                    dataCastType = "(" + GetMappedAbiTypeName(szArr.BaseType) + "*)";
                }
                else if (szArr.BaseType.IsHResultException())
                {
                    dataParamType = "global::ABI.System.Exception*";
                    dataCastType = "(global::ABI.System.Exception*)";
                }
                else if (IsComplexStruct(context.Cache, szArr.BaseType))
                {
                    string abiStructName = GetAbiStructTypeName(writer, context, szArr.BaseType);
                    dataParamType = abiStructName + "*";
                    dataCastType = "(" + abiStructName + "*)";
                }
                else
                {
                    dataParamType = "void**";
                    dataCastType = "(void**)";
                }
                writer.Write(callIndent);
                writer.Write("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"CopyToUnmanaged\")]\n");
                writer.Write(callIndent);
                writer.Write("static extern void CopyToUnmanaged_");
                writer.Write(localName);
                writer.Write("([UnsafeAccessorType(\"");
                writer.Write(GetArrayMarshallerInteropPath(szArr.BaseType));
                writer.Write("\")] object _, ReadOnlySpan<");
                writer.Write(elementProjected);
                writer.Write("> span, uint length, ");
                writer.Write(dataParamType);
                writer.Write(" data);\n");
                writer.Write(callIndent);
                writer.Write("CopyToUnmanaged_");
                writer.Write(localName);
                writer.Write("(null, ");
                writer.Write(callName);
                writer.Write(", (uint)");
                writer.Write(callName);
                writer.Write(".Length, ");
                writer.Write(dataCastType);
                writer.Write("_");
                writer.Write(localName);
                writer.Write(");\n");
            }
        }

        writer.Write(callIndent);
        // method/property is [NoException] (its HRESULT is contractually S_OK).
        if (!isNoExcept)
        {
            writer.Write("RestrictedErrorInfo.ThrowExceptionForHR((*(delegate* unmanaged[MemberFunction]<");
        }
        else
        {
            writer.Write("(*(delegate* unmanaged[MemberFunction]<");
        }
        writer.Write(fp.ToString());
        writer.Write(">**)ThisPtr)[");
        writer.Write(slot.ToString(System.Globalization.CultureInfo.InvariantCulture));
        writer.Write("](ThisPtr");
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat == ParamCategory.PassArray || cat == ParamCategory.FillArray)
            {
                string callName = GetParamName(p, paramNameOverride);
                string localName = GetParamLocalName(p, paramNameOverride);
                writer.Write(",\n  (uint)");
                writer.Write(callName);
                writer.Write(".Length, _");
                writer.Write(localName);
                continue;
            }
            if (cat == ParamCategory.Out)
            {
                string localName = GetParamLocalName(p, paramNameOverride);
                writer.Write(",\n  &__");
                writer.Write(localName);
                continue;
            }
            if (cat == ParamCategory.ReceiveArray)
            {
                string localName = GetParamLocalName(p, paramNameOverride);
                writer.Write(",\n  &__");
                writer.Write(localName);
                writer.Write("_length, &__");
                writer.Write(localName);
                writer.Write("_data");
                continue;
            }
            if (cat == ParamCategory.Ref)
            {
                string localName = GetParamLocalName(p, paramNameOverride);
                AsmResolver.DotNet.Signatures.TypeSignature uRefArg = StripByRefAndCustomModifiers(p.Type);
                if (IsComplexStruct(context.Cache, uRefArg))
                {
                    // Complex struct 'in' (Ref) param: pass &__local (the marshaled ABI struct).
                    writer.Write(",\n  &__");
                    writer.Write(localName);
                }
                else
                {
                    // 'in T' projected param: pass the pinned pointer.
                    writer.Write(",\n  _");
                    writer.Write(localName);
                }
                continue;
            }
            writer.Write(",\n  ");
            if (p.Type.IsHResultException())
            {
                writer.Write("__");
                writer.Write(GetParamLocalName(p, paramNameOverride));
            }
            else if (p.Type.IsString())
            {
                writer.Write("__");
                writer.Write(GetParamLocalName(p, paramNameOverride));
                writer.Write(".HString");
            }
            else if (IsRuntimeClassOrInterface(context.Cache, p.Type) || p.Type.IsObject() || p.Type.IsGenericInstance())
            {
                writer.Write("__");
                writer.Write(GetParamLocalName(p, paramNameOverride));
                writer.Write(".GetThisPtrUnsafe()");
            }
            else if (p.Type.IsSystemType())
            {
                // System.Type input: pass the pre-converted ABI Type struct (via the local set up before the call).
                writer.Write("__");
                writer.Write(GetParamLocalName(p, paramNameOverride));
                writer.Write(".ConvertToUnmanagedUnsafe()");
            }
            else if (IsMappedAbiValueType(p.Type))
            {
                // Mapped value-type input: pass the pre-converted ABI local.
                writer.Write("__");
                writer.Write(GetParamLocalName(p, paramNameOverride));
            }
            else if (IsComplexStruct(context.Cache, p.Type))
            {
                // Complex struct input: pass the pre-converted ABI struct local.
                writer.Write("__");
                writer.Write(GetParamLocalName(p, paramNameOverride));
            }
            else if (IsAnyStruct(context.Cache, p.Type))
            {
                writer.Write(GetParamName(p, paramNameOverride));
            }
            else
            {
                EmitParamArgConversion(writer, context, p, paramNameOverride);
            }
        }
        if (returnIsReceiveArray)
        {
            writer.Write(",\n  &__retval_length, &__retval_data");
        }
        else if (rt is not null)
        {
            writer.Write(",\n  &__retval");
        }
        // Close the vtable call. One less ')' when noexcept (no ThrowExceptionForHR wrap).
        writer.Write(isNoExcept ? ");\n" : "));\n");

        // After call: copy native-filled values back into the user's managed Span<T> for
        // FillArray of non-blittable element types. The native callee wrote into our
        // ABI-format buffer (_<name>) which is separate from the user's Span<T>; we need to
        // CopyToManaged_<name> to convert each ABI element back to the projected form and
        // store it in the user's Span. Mirrors C++ marshaler.write_marshal_from_abi
        //.
        // Blittable element types (primitives and almost-blittable structs) don't need this
        // because the user's Span wraps the same memory the native side wrote to.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.FillArray) { continue; }
            if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szFA) { continue; }
            if (IsBlittablePrimitive(context.Cache, szFA.BaseType) || IsAnyStruct(context.Cache, szFA.BaseType)) { continue; }
            string callName = GetParamName(p, paramNameOverride);
            string localName = GetParamLocalName(p, paramNameOverride);
            IndentedTextWriter __scratchElementProjected = new();
            WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(szFA.BaseType));
            string elementProjected = __scratchElementProjected.ToString();
            string elementInteropArg = EncodeInteropTypeName(szFA.BaseType, TypedefNameType.Projected);

            _ = elementInteropArg;
            // Determine the ABI element type for the data pointer parameter.
            // - Strings / runtime classes / objects: void**
            // - HResult exception: global::ABI.System.Exception*
            // - Mapped value types: global::ABI.System.{DateTimeOffset|TimeSpan}*
            // - Complex structs: <ABI struct>*
            string dataParamType;
            string dataCastType;
            if (szFA.BaseType.IsString() || IsRuntimeClassOrInterface(context.Cache, szFA.BaseType) || szFA.BaseType.IsObject())
            {
                dataParamType = "void** data";
                dataCastType = "(void**)";
            }
            else if (szFA.BaseType.IsHResultException())
            {
                dataParamType = "global::ABI.System.Exception* data";
                dataCastType = "(global::ABI.System.Exception*)";
            }
            else if (IsMappedAbiValueType(szFA.BaseType))
            {
                string abiName = GetMappedAbiTypeName(szFA.BaseType);
                dataParamType = abiName + "* data";
                dataCastType = "(" + abiName + "*)";
            }
            else
            {
                string abiStructName = GetAbiStructTypeName(writer, context, szFA.BaseType);
                dataParamType = abiStructName + "* data";
                dataCastType = "(" + abiStructName + "*)";
            }
            writer.Write(callIndent);
            writer.Write("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"CopyToManaged\")]\n");
            writer.Write(callIndent);
            writer.Write("static extern void CopyToManaged_");
            writer.Write(localName);
            writer.Write("([UnsafeAccessorType(\"");
            writer.Write(GetArrayMarshallerInteropPath(szFA.BaseType));
            writer.Write("\")] object _, uint length, ");
            writer.Write(dataParamType);
            writer.Write(", Span<");
            writer.Write(elementProjected);
            writer.Write("> span);\n");
            writer.Write(callIndent);
            writer.Write("CopyToManaged_");
            writer.Write(localName);
            writer.Write("(null, (uint)__");
            writer.Write(localName);
            writer.Write("_span.Length, ");
            writer.Write(dataCastType);
            writer.Write("_");
            writer.Write(localName);
            writer.Write(", ");
            writer.Write(callName);
            writer.Write(");\n");
        }

        // After call: write back Out params to caller's 'out' var.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.Out) { continue; }
            string callName = GetParamName(p, paramNameOverride);
            string localName = GetParamLocalName(p, paramNameOverride);
            AsmResolver.DotNet.Signatures.TypeSignature uOut = StripByRefAndCustomModifiers(p.Type);

            // For Out generic instance: emit inline UnsafeAccessor to ConvertToManaged_<name>
            // before the writeback. Mirrors the truth pattern (e.g. Collection1HandlerInvoke
            // emits the accessor inside try, right before the assignment).
            if (uOut.IsGenericInstance())
            {
                string interopTypeName = EncodeInteropTypeName(uOut, TypedefNameType.ABI) + ", WinRT.Interop";
                IndentedTextWriter __scratchProjectedTypeName = new();
                WriteProjectedSignature(__scratchProjectedTypeName, context, uOut, false);
                string projectedTypeName = __scratchProjectedTypeName.ToString();
                writer.Write(callIndent);
                writer.Write("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToManaged\")]\n");
                writer.Write(callIndent);
                writer.Write("static extern ");
                writer.Write(projectedTypeName);
                writer.Write(" ConvertToManaged_");
                writer.Write(localName);
                writer.Write("([UnsafeAccessorType(\"");
                writer.Write(interopTypeName);
                writer.Write("\")] object _, void* value);\n");
                writer.Write(callIndent);
                writer.Write(callName);
                writer.Write(" = ConvertToManaged_");
                writer.Write(localName);
                writer.Write("(null, __");
                writer.Write(localName);
                writer.Write(");\n");
                continue;
            }

            writer.Write(callIndent);
            writer.Write(callName);
            writer.Write(" = ");
            if (uOut.IsString())
            {
                writer.Write("HStringMarshaller.ConvertToManaged(__");
                writer.Write(localName);
                writer.Write(")");
            }
            else if (uOut.IsObject())
            {
                writer.Write("WindowsRuntimeObjectMarshaller.ConvertToManaged(__");
                writer.Write(localName);
                writer.Write(")");
            }
            else if (IsRuntimeClassOrInterface(context.Cache, uOut))
            {
                writer.Write(GetMarshallerFullName(writer, context, uOut));
                writer.Write(".ConvertToManaged(__");
                writer.Write(localName);
                writer.Write(")");
            }
            else if (uOut.IsSystemType())
            {
                writer.Write("global::ABI.System.TypeMarshaller.ConvertToManaged(__");
                writer.Write(localName);
                writer.Write(")");
            }
            else if (IsComplexStruct(context.Cache, uOut))
            {
                writer.Write(GetMarshallerFullName(writer, context, uOut));
                writer.Write(".ConvertToManaged(__");
                writer.Write(localName);
                writer.Write(")");
            }
            else if (IsAnyStruct(context.Cache, uOut))
            {
                writer.Write("__");
                writer.Write(localName);
            }
            else if (uOut is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibBool && corlibBool.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
            {
                writer.Write("__");
                writer.Write(localName);
            }
            else if (uOut is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlibChar && corlibChar.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
            {
                writer.Write("__");
                writer.Write(localName);
            }
            else if (IsEnumType(context.Cache, uOut))
            {
                // Enum out param: __<name> local is already the projected enum type (since the
                // function pointer signature uses the projected type). No cast needed.
                writer.Write("__");
                writer.Write(localName);
            }
            else
            {
                writer.Write("__");
                writer.Write(localName);
            }
            writer.Write(";\n");
        }

        // Writeback for ReceiveArray params: emit a UnsafeAccessor + assign to the out param.
        for (int i = 0; i < sig.Params.Count; i++)
        {
            ParamInfo p = sig.Params[i];
            ParamCategory cat = ParamHelpers.GetParamCategory(p);
            if (cat != ParamCategory.ReceiveArray) { continue; }
            string callName = GetParamName(p, paramNameOverride);
            string localName = GetParamLocalName(p, paramNameOverride);
            AsmResolver.DotNet.Signatures.SzArrayTypeSignature sza = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)StripByRefAndCustomModifiers(p.Type);
            IndentedTextWriter __scratchElementProjected = new();
            WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(sza.BaseType));
            string elementProjected = __scratchElementProjected.ToString();
            // Element ABI type: void* for ref types (string/runtime class/object); ABI struct
            // type for complex structs (e.g. authored BasicStruct); blittable struct ABI for
            // blittable structs; primitive ABI otherwise.
            string elementAbi = sza.BaseType.IsString() || IsRuntimeClassOrInterface(context.Cache, sza.BaseType) || sza.BaseType.IsObject()
                ? "void*"
                : IsComplexStruct(context.Cache, sza.BaseType)
                    ? GetAbiStructTypeName(writer, context, sza.BaseType)
                    : IsAnyStruct(context.Cache, sza.BaseType)
                        ? GetBlittableStructAbiType(writer, context, sza.BaseType)
                        : GetAbiPrimitiveType(context.Cache, sza.BaseType);
            string elementInteropArg = EncodeInteropTypeName(sza.BaseType, TypedefNameType.Projected);

            _ = elementInteropArg;
            string marshallerPath = GetArrayMarshallerInteropPath(sza.BaseType);
            writer.Write(callIndent);
            writer.Write("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToManaged\")]\n");
            writer.Write(callIndent);
            writer.Write("static extern ");
            writer.Write(elementProjected);
            writer.Write("[] ConvertToManaged_");
            writer.Write(localName);
            writer.Write("([UnsafeAccessorType(\"");
            writer.Write(marshallerPath);
            writer.Write("\")] object _, uint length, ");
            writer.Write(elementAbi);
            writer.Write("* data);\n");
            writer.Write(callIndent);
            writer.Write(callName);
            writer.Write(" = ConvertToManaged_");
            writer.Write(localName);
            writer.Write("(null, __");
            writer.Write(localName);
            writer.Write("_length, __");
            writer.Write(localName);
            writer.Write("_data);\n");
        }
        if (rt is not null)
        {
            if (returnIsReceiveArray)
            {
                AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSz = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)rt;
                IndentedTextWriter __scratchElementProjected = new();
                WriteProjectionType(__scratchElementProjected, context, TypeSemanticsFactory.Get(retSz.BaseType));
                string elementProjected = __scratchElementProjected.ToString();
                string elementAbi = retSz.BaseType.IsString() || IsRuntimeClassOrInterface(context.Cache, retSz.BaseType) || retSz.BaseType.IsObject()
                    ? "void*"
                    : IsComplexStruct(context.Cache, retSz.BaseType)
                        ? GetAbiStructTypeName(writer, context, retSz.BaseType)
                        : retSz.BaseType.IsHResultException()
                            ? "global::ABI.System.Exception"
                            : IsMappedAbiValueType(retSz.BaseType)
                                ? GetMappedAbiTypeName(retSz.BaseType)
                                : IsAnyStruct(context.Cache, retSz.BaseType)
                                    ? GetBlittableStructAbiType(writer, context, retSz.BaseType)
                                    : GetAbiPrimitiveType(context.Cache, retSz.BaseType);
                string elementInteropArg = EncodeInteropTypeName(retSz.BaseType, TypedefNameType.Projected);

                _ = elementInteropArg;
                writer.Write(callIndent);
                writer.Write("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToManaged\")]\n");
                writer.Write(callIndent);
                writer.Write("static extern ");
                writer.Write(elementProjected);
                writer.Write("[] ConvertToManaged_retval([UnsafeAccessorType(\"");
                writer.Write(GetArrayMarshallerInteropPath(retSz.BaseType));
                writer.Write("\")] object _, uint length, ");
                writer.Write(elementAbi);
                writer.Write("* data);\n");
                writer.Write(callIndent);
                writer.Write("return ConvertToManaged_retval(null, __retval_length, __retval_data);\n");
            }
            else if (returnIsHResultException)
            {
                writer.Write(callIndent);
                writer.Write("return global::ABI.System.ExceptionMarshaller.ConvertToManaged(__retval);\n");
            }
            else if (returnIsString)
            {
                writer.Write(callIndent);
                writer.Write("return HStringMarshaller.ConvertToManaged(__retval);\n");
            }
            else if (returnIsRefType)
            {
                if (rt.IsNullableT())
                {
                    // Nullable<T> return: use <T>Marshaller.UnboxToManaged. Mirrors truth pattern;
                    // there is no Nullable<T>Marshaller, the inner-T marshaller has UnboxToManaged.
                    AsmResolver.DotNet.Signatures.TypeSignature inner = rt.GetNullableInnerType()!;
                    string innerMarshaller = GetNullableInnerMarshallerName(writer, context, inner);
                    writer.Write(callIndent);
                    writer.Write("return ");
                    writer.Write(innerMarshaller);
                    writer.Write(".UnboxToManaged(__retval);\n");
                }
                else if (rt.IsGenericInstance())
                {
                    string interopTypeName = EncodeInteropTypeName(rt, TypedefNameType.ABI) + ", WinRT.Interop";
                    IndentedTextWriter __scratchProjectedTypeName = new();
                    WriteProjectedSignature(__scratchProjectedTypeName, context, rt, false);
                    string projectedTypeName = __scratchProjectedTypeName.ToString();
                    writer.Write(callIndent);
                    writer.Write("[UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"ConvertToManaged\")]\n");
                    writer.Write(callIndent);
                    writer.Write("static extern ");
                    writer.Write(projectedTypeName);
                    writer.Write(" ConvertToManaged_retval([UnsafeAccessorType(\"");
                    writer.Write(interopTypeName);
                    writer.Write("\")] object _, void* value);\n");
                    writer.Write(callIndent);
                    writer.Write("return ConvertToManaged_retval(null, __retval);\n");
                }
                else
                {
                    writer.Write(callIndent);
                    writer.Write("return ");
                    EmitMarshallerConvertToManaged(writer, context, rt, "__retval");
                    writer.Write(";\n");
                }
            }
            else if (rt is not null && IsMappedAbiValueType(rt))
            {
                // Mapped value type return (e.g. DateTime/TimeSpan): convert ABI struct back via marshaller.
                writer.Write(callIndent);
                writer.Write("return ");
                writer.Write(GetMappedMarshallerName(rt));
                writer.Write(".ConvertToManaged(__retval);\n");
            }
            else if (rt is not null && rt.IsSystemType())
            {
                // System.Type return: convert ABI Type struct back to System.Type via TypeMarshaller.
                writer.Write(callIndent);
                writer.Write("return global::ABI.System.TypeMarshaller.ConvertToManaged(__retval);\n");
            }
            else if (returnIsAnyStruct)
            {
                writer.Write(callIndent);
                if (rt is not null && IsMappedAbiValueType(rt))
                {
                    // Mapped value type return: convert ABI struct back to projected via marshaller.
                    writer.Write("return ");
                    writer.Write(GetMappedMarshallerName(rt));
                    writer.Write(".ConvertToManaged(__retval);\n");
                }
                else
                {
                    writer.Write("return __retval;\n");
                }
            }
            else if (returnIsComplexStruct)
            {
                writer.Write(callIndent);
                writer.Write("return ");
                writer.Write(GetMarshallerFullName(writer, context, rt!));
                writer.Write(".ConvertToManaged(__retval);\n");
            }
            else
            {
                writer.Write(callIndent);
                writer.Write("return ");
                IndentedTextWriter __scratchProjected = new();
                WriteProjectedSignature(__scratchProjected, context, rt!, false);
                string projected = __scratchProjected.ToString();
                string abiType = GetAbiPrimitiveType(context.Cache, rt!);
                if (projected == abiType) { writer.Write("__retval;\n"); }
                else
                {
                    writer.Write("(");
                    writer.Write(projected);
                    writer.Write(")__retval;\n");
                }
            }
        }

        // Close fixed blocks (innermost first).
        for (int i = fixedNesting - 1; i >= 0; i--)
        {
            writer.Write(indent);
            writer.Write(new string(' ', i * 4));
            writer.Write("}\n");
        }

        if (needsTryFinally)
        {
            writer.Write("        }\n        finally\n        {\n");

            // Order matches truth (mirrors C++ disposer iteration order):
            // 0. Complex-struct input param Dispose (e.g. ProfileUsageMarshaller.Dispose(__value))
            // 1. Non-blittable PassArray/FillArray cleanup (Dispose + ArrayPools)
            // 2. Out param frees (HString / object / runtime class)
            // 3. ReceiveArray param frees (Free_<name> via UnsafeAccessor)
            // 4. Return free (__retval) — last

            // 0. Dispose complex-struct input params via marshaller (both 'in' and 'in T' forms).
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                if (cat != ParamCategory.In && cat != ParamCategory.Ref) { continue; }
                AsmResolver.DotNet.Signatures.TypeSignature pType = StripByRefAndCustomModifiers(p.Type);
                if (!IsComplexStruct(context.Cache, pType)) { continue; }
                string localName = GetParamLocalName(p, paramNameOverride);
                writer.Write("            ");
                writer.Write(GetMarshallerFullName(writer, context, pType));
                writer.Write(".Dispose(__");
                writer.Write(localName);
                writer.Write(");\n");
            }
            // 1. Cleanup non-blittable PassArray/FillArray params:
            // For strings: HStringArrayMarshaller.Dispose + return ArrayPools (3 of them).
            // For runtime classes/objects: Dispose_<name> (UnsafeAccessor) + return ArrayPool.
            // For mapped value types (DateTime/TimeSpan): no per-element disposal needed and truth
            // doesn't return the ArrayPool either, so skip entirely.
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                if (cat != ParamCategory.PassArray && cat != ParamCategory.FillArray) { continue; }
                if (p.Type is not AsmResolver.DotNet.Signatures.SzArrayTypeSignature szArr) { continue; }
                if (IsBlittablePrimitive(context.Cache, szArr.BaseType) || IsAnyStruct(context.Cache, szArr.BaseType)) { continue; }
                if (IsMappedAbiValueType(szArr.BaseType)) { continue; }
                if (szArr.BaseType.IsHResultException())
                {
                    // HResultException ABI is just an int; per-element Dispose is a no-op (mirror
                    // the truth: no Dispose_<name> emitted). Just return the inline-array's pool
                    // using the correct element type (ABI.System.Exception, not nint).
                    string localNameH = GetParamLocalName(p, paramNameOverride);
                    writer.Write("\n            if (__");
                    writer.Write(localNameH);
                    writer.Write("_arrayFromPool is not null)\n            {\n");
                    writer.Write("                global::System.Buffers.ArrayPool<global::ABI.System.Exception>.Shared.Return(__");
                    writer.Write(localNameH);
                    writer.Write("_arrayFromPool);\n            }\n");
                    continue;
                }
                string localName = GetParamLocalName(p, paramNameOverride);
                if (szArr.BaseType.IsString())
                {
                    // The HStringArrayMarshaller.Dispose + ArrayPool returns for strings only
                    // apply to PassArray (where we set up the pinned handles + headers in the
                    // first place). FillArray writes back HSTRING handles into the nint storage
                    // array directly, with no per-element pinned handle / header to release.
                    if (cat == ParamCategory.PassArray)
                    {
                        writer.Write("            HStringArrayMarshaller.Dispose(__");
                        writer.Write(localName);
                        writer.Write("_pinnedHandleSpan);\n\n");
                        writer.Write("            if (__");
                        writer.Write(localName);
                        writer.Write("_pinnedHandleArrayFromPool is not null)\n            {\n");
                        writer.Write("                global::System.Buffers.ArrayPool<nint>.Shared.Return(__");
                        writer.Write(localName);
                        writer.Write("_pinnedHandleArrayFromPool);\n            }\n\n");
                        writer.Write("            if (__");
                        writer.Write(localName);
                        writer.Write("_headerArrayFromPool is not null)\n            {\n");
                        writer.Write("                global::System.Buffers.ArrayPool<HStringHeader>.Shared.Return(__");
                        writer.Write(localName);
                        writer.Write("_headerArrayFromPool);\n            }\n");
                    }
                    // Both PassArray and FillArray need the inline-array's nint pool returned.
                    writer.Write("\n            if (__");
                    writer.Write(localName);
                    writer.Write("_arrayFromPool is not null)\n            {\n");
                    writer.Write("                global::System.Buffers.ArrayPool<nint>.Shared.Return(__");
                    writer.Write(localName);
                    writer.Write("_arrayFromPool);\n            }\n");
                }
                else
                {
                    // For complex structs, both the Dispose_<name> data param and the fixed()
                    // pointer must be typed as <ABI struct>*; the cast can be omitted. For
                    // runtime classes / objects / strings the data is void** and the fixed()
                    // remains void* with a (void**) cast.
                    string disposeDataParamType;
                    string fixedPtrType;
                    string disposeCastType;
                    if (IsComplexStruct(context.Cache, szArr.BaseType))
                    {
                        string abiStructName = GetAbiStructTypeName(writer, context, szArr.BaseType);
                        disposeDataParamType = abiStructName + "*";
                        fixedPtrType = abiStructName + "*";
                        disposeCastType = string.Empty;
                    }
                    else
                    {
                        disposeDataParamType = "void** data";
                        fixedPtrType = "void*";
                        disposeCastType = "(void**)";
                    }
                    string elementInteropArg = EncodeInteropTypeName(szArr.BaseType, TypedefNameType.Projected);

                    _ = elementInteropArg;
                    writer.Write("            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"Dispose\")]\n");
                    writer.Write("            static extern void Dispose_");
                    writer.Write(localName);
                    writer.Write("([UnsafeAccessorType(\"");
                    writer.Write(GetArrayMarshallerInteropPath(szArr.BaseType));
                    writer.Write("\")] object _, uint length, ");
                    writer.Write(disposeDataParamType);
                    if (!disposeDataParamType.EndsWith("data", System.StringComparison.Ordinal)) { writer.Write(" data"); }
                    writer.Write(");\n\n");
                    writer.Write("            fixed(");
                    writer.Write(fixedPtrType);
                    writer.Write(" _");
                    writer.Write(localName);
                    writer.Write(" = __");
                    writer.Write(localName);
                    writer.Write("_span)\n            {\n");
                    writer.Write("                Dispose_");
                    writer.Write(localName);
                    writer.Write("(null, (uint) __");
                    writer.Write(localName);
                    writer.Write("_span.Length, ");
                    writer.Write(disposeCastType);
                    writer.Write("_");
                    writer.Write(localName);
                    writer.Write(");\n            }\n");
                }
                // ArrayPool storage type matches the InlineArray storage (mapped ABI value type
                // for DateTime/TimeSpan; ABI struct for complex structs; nint otherwise).
                string poolStorageT = IsMappedAbiValueType(szArr.BaseType)
                    ? GetMappedAbiTypeName(szArr.BaseType)
                    : IsComplexStruct(context.Cache, szArr.BaseType)
                        ? GetAbiStructTypeName(writer, context, szArr.BaseType)
                        : "nint";
                writer.Write("\n            if (__");
                writer.Write(localName);
                writer.Write("_arrayFromPool is not null)\n            {\n");
                writer.Write("                global::System.Buffers.ArrayPool<");
                writer.Write(poolStorageT);
                writer.Write(">.Shared.Return(__");
                writer.Write(localName);
                writer.Write("_arrayFromPool);\n            }\n");
            }

            // 2. Free Out string/object/runtime-class params.
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                if (cat != ParamCategory.Out) { continue; }
                AsmResolver.DotNet.Signatures.TypeSignature uOut = StripByRefAndCustomModifiers(p.Type);
                string localName = GetParamLocalName(p, paramNameOverride);
                if (uOut.IsString())
                {
                    writer.Write("            HStringMarshaller.Free(__");
                    writer.Write(localName);
                    writer.Write(");\n");
                }
                else if (uOut.IsObject() || IsRuntimeClassOrInterface(context.Cache, uOut) || uOut.IsGenericInstance())
                {
                    writer.Write("            WindowsRuntimeUnknownMarshaller.Free(__");
                    writer.Write(localName);
                    writer.Write(");\n");
                }
                else if (uOut.IsSystemType())
                {
                    writer.Write("            global::ABI.System.TypeMarshaller.Dispose(__");
                    writer.Write(localName);
                    writer.Write(");\n");
                }
                else if (IsComplexStruct(context.Cache, uOut))
                {
                    writer.Write("            ");
                    writer.Write(GetMarshallerFullName(writer, context, uOut));
                    writer.Write(".Dispose(__");
                    writer.Write(localName);
                    writer.Write(");\n");
                }
            }

            // 3. Free ReceiveArray params via UnsafeAccessor.
            for (int i = 0; i < sig.Params.Count; i++)
            {
                ParamInfo p = sig.Params[i];
                ParamCategory cat = ParamHelpers.GetParamCategory(p);
                if (cat != ParamCategory.ReceiveArray) { continue; }
                string localName = GetParamLocalName(p, paramNameOverride);
                AsmResolver.DotNet.Signatures.SzArrayTypeSignature sza = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)StripByRefAndCustomModifiers(p.Type);
                // Element ABI type: void* for ref types; ABI struct for complex/blittable structs;
                // primitive ABI otherwise. (Same categorization as the ConvertToManaged_<name> path.)
                string elementAbi = sza.BaseType.IsString() || IsRuntimeClassOrInterface(context.Cache, sza.BaseType) || sza.BaseType.IsObject()
                    ? "void*"
                    : IsComplexStruct(context.Cache, sza.BaseType)
                        ? GetAbiStructTypeName(writer, context, sza.BaseType)
                        : IsAnyStruct(context.Cache, sza.BaseType)
                            ? GetBlittableStructAbiType(writer, context, sza.BaseType)
                            : GetAbiPrimitiveType(context.Cache, sza.BaseType);
                string elementInteropArg = EncodeInteropTypeName(sza.BaseType, TypedefNameType.Projected);

                _ = elementInteropArg;
                string marshallerPath = GetArrayMarshallerInteropPath(sza.BaseType);
                writer.Write("            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"Free\")]\n");
                writer.Write("            static extern void Free_");
                writer.Write(localName);
                writer.Write("([UnsafeAccessorType(\"");
                writer.Write(marshallerPath);
                writer.Write("\")] object _, uint length, ");
                writer.Write(elementAbi);
                writer.Write("* data);\n\n");
                writer.Write("            Free_");
                writer.Write(localName);
                writer.Write("(null, __");
                writer.Write(localName);
                writer.Write("_length, __");
                writer.Write(localName);
                writer.Write("_data);\n");
            }

            // 4. Free return value (__retval) — emitted last to match truth ordering.
            if (returnIsString)
            {
                writer.Write("            HStringMarshaller.Free(__retval);\n");
            }
            else if (returnIsRefType)
            {
                writer.Write("            WindowsRuntimeUnknownMarshaller.Free(__retval);\n");
            }
            else if (returnIsComplexStruct)
            {
                writer.Write("            ");
                writer.Write(GetMarshallerFullName(writer, context, rt!));
                writer.Write(".Dispose(__retval);\n");
            }
            else if (returnIsSystemTypeForCleanup)
            {
                // System.Type return: dispose the ABI.System.Type's HSTRING fields.
                writer.Write("            global::ABI.System.TypeMarshaller.Dispose(__retval);\n");
            }
            else if (returnIsReceiveArray)
            {
                AsmResolver.DotNet.Signatures.SzArrayTypeSignature retSz = (AsmResolver.DotNet.Signatures.SzArrayTypeSignature)rt!;
                string elementAbi = retSz.BaseType.IsString() || IsRuntimeClassOrInterface(context.Cache, retSz.BaseType) || retSz.BaseType.IsObject()
                    ? "void*"
                    : IsComplexStruct(context.Cache, retSz.BaseType)
                        ? GetAbiStructTypeName(writer, context, retSz.BaseType)
                        : retSz.BaseType.IsHResultException()
                            ? "global::ABI.System.Exception"
                            : IsMappedAbiValueType(retSz.BaseType)
                                ? GetMappedAbiTypeName(retSz.BaseType)
                                : IsAnyStruct(context.Cache, retSz.BaseType)
                                    ? GetBlittableStructAbiType(writer, context, retSz.BaseType)
                                    : GetAbiPrimitiveType(context.Cache, retSz.BaseType);
                string elementInteropArg = EncodeInteropTypeName(retSz.BaseType, TypedefNameType.Projected);

                _ = elementInteropArg;
                writer.Write("            [UnsafeAccessor(UnsafeAccessorKind.StaticMethod, Name = \"Free\")]\n");
                writer.Write("            static extern void Free_retval([UnsafeAccessorType(\"");
                writer.Write(GetArrayMarshallerInteropPath(retSz.BaseType));
                writer.Write("\")] object _, uint length, ");
                writer.Write(elementAbi);
                writer.Write("* data);\n");
                writer.Write("            Free_retval(null, __retval_length, __retval_data);\n");
            }

            writer.Write("        }\n");
        }

        writer.Write("    }\n");
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
    private static string GetMappedMarshallerName(AsmResolver.DotNet.Signatures.TypeSignature sig)
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
    private static AsmResolver.DotNet.Signatures.TypeSignature StripByRefAndCustomModifiers(AsmResolver.DotNet.Signatures.TypeSignature sig)
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
            // mirrors how the C++ tool's abi_marshaler abstraction handles unknown types — it
            // dispatches based on the metadata semantics, not on resolution.
            return !td.IsValueType;
        }
        return false;
    }

    /// <summary>Emits the call to the appropriate marshaller's ConvertToUnmanaged for a runtime class / object input parameter.</summary>
    internal static void EmitMarshallerConvertToUnmanaged(IndentedTextWriter writer, ProjectionEmitContext context, AsmResolver.DotNet.Signatures.TypeSignature sig, string argName)
    {
        if (sig.IsObject())
        {
            writer.Write("WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(");
            writer.Write(argName);
            writer.Write(")");
            return;
        }
        // Runtime class / interface: use ABI.<NS>.<Name>Marshaller
        writer.Write(GetMarshallerFullName(writer, context, sig));
        writer.Write(".ConvertToUnmanaged(");
        writer.Write(argName);
        writer.Write(")");
    }

    /// <summary>Emits the call to the appropriate marshaller's ConvertToManaged for a runtime class / object return value.</summary>
    internal static void EmitMarshallerConvertToManaged(IndentedTextWriter writer, ProjectionEmitContext context, AsmResolver.DotNet.Signatures.TypeSignature sig, string argName)
    {
        if (sig.IsObject())
        {
            writer.Write("WindowsRuntimeObjectMarshaller.ConvertToManaged(");
            writer.Write(argName);
            writer.Write(")");
            return;
        }
        writer.Write(GetMarshallerFullName(writer, context, sig));
        writer.Write(".ConvertToManaged(");
        writer.Write(argName);
        writer.Write(")");
    }

    /// <summary>Returns the full marshaller name (e.g. <c>global::ABI.Windows.Foundation.UriMarshaller</c>).
    /// When the marshaller would land in the writer's current ABI namespace, returns just the
    /// short marshaller class name (e.g. <c>BasicStructMarshaller</c>) — mirrors C++ which
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

    private static string GetParamName(ParamInfo p, string? paramNameOverride)
    {
        string name = paramNameOverride ?? p.Parameter.Name ?? "param";
        return CSharpKeywords.IsKeyword(name) ? "@" + name : name;
    }

    private static string GetParamLocalName(ParamInfo p, string? paramNameOverride)
    {
        // For local helper variables (e.g. __<name>), strip the @ escape since `__event` is valid.
        return paramNameOverride ?? p.Parameter.Name ?? "param";
    }

    /// <summary>Emits the conversion of a parameter from its projected (managed) form to the ABI argument form.</summary>
    internal static void EmitParamArgConversion(IndentedTextWriter writer, ProjectionEmitContext context, ParamInfo p, string? paramNameOverride = null)
    {
        string pname = paramNameOverride ?? p.Parameter.Name ?? "param";
        // bool: ABI is 'bool' directly; pass as-is.
        if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib &&
            corlib.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Boolean)
        {
            writer.Write(pname);
        }
        // char: ABI is 'char' directly; pass as-is.
        else if (p.Type is AsmResolver.DotNet.Signatures.CorLibTypeSignature corlib2 &&
                 corlib2.ElementType == AsmResolver.PE.DotNet.Metadata.Tables.ElementType.Char)
        {
            writer.Write(pname);
        }
        // Enums: function pointer signature uses the projected enum type, so pass directly.
        else if (IsEnumType(context.Cache, p.Type))
        {
            writer.Write(pname);
        }
        else
        {
            writer.Write(pname);
        }
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
        WriteProjectedSignature(__scratchProj, context, sig, false);
        return __scratchProj.ToString();
    }

    /// <summary>Returns the ABI struct type name for a complex struct (e.g. global::ABI.Windows.Web.Http.HttpProgress).
    /// When the writer is currently in the matching ABI namespace, returns just the
    /// short type name (e.g. <c>HttpProgress</c>) to mirror the C++ tool which uses the
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

    /// <summary>Mirrors C++ <c>write_abi_type</c>: writes the ABI type for a type semantics.</summary>
    public static void WriteAbiType(IndentedTextWriter writer, ProjectionEmitContext context, TypeSemantics semantics)
    {
        switch (semantics)
        {
            case TypeSemantics.Fundamental f:
                writer.Write(GetAbiFundamentalType(f.Type));
                break;
            case TypeSemantics.Object_:
                writer.Write("void*");
                break;
            case TypeSemantics.Guid_:
                writer.Write("Guid");
                break;
            case TypeSemantics.Type_:
                writer.Write("global::WindowsRuntime.InteropServices.WindowsRuntimeTypeName");
                break;
            case TypeSemantics.Definition d:
                if (TypeCategorization.GetCategory(d.Type) is TypeCategory.Enum)
                {
                    // Enums in WinRT ABI use the projected enum type directly (since their C#
                    // layout matches their underlying integer ABI representation 1:1).
                    WriteTypedefName(writer, context, d.Type, TypedefNameType.Projected, true);
                }
                else if (TypeCategorization.GetCategory(d.Type) is TypeCategory.Struct)
                {
                    (string dNs, string dName) = d.Type.Names();
                    // Special case: mapped value types that require ABI marshalling
                    // (DateTime/TimeSpan -> ABI.System.DateTimeOffset/TimeSpan).
                    if (dNs == "Windows.Foundation" && dName == "DateTime")
                    {
                        writer.Write("global::ABI.System.DateTimeOffset");
                        break;
                    }
                    if (dNs == "Windows.Foundation" && dName == "TimeSpan")
                    {
                        writer.Write("global::ABI.System.TimeSpan");
                        break;
                    }
                    if (dNs == "Windows.Foundation" && dName == "HResult")
                    {
                        writer.Write("global::ABI.System.Exception");
                        break;
                    }
                    if (dNs == "Windows.UI.Xaml.Interop" && dName == "TypeName")
                    {
                        // System.Type ABI struct: maps to global::ABI.System.Type, not the
                        // ABI.Windows.UI.Xaml.Interop.TypeName form.
                        writer.Write("global::ABI.System.Type");
                        break;
                    }
                    AsmResolver.DotNet.Signatures.TypeSignature dts = d.Type.ToTypeSignature();
                    // "Almost-blittable" structs (with bool/char fields but no reference-type
                    // fields) can pass through using the projected type since the C# layout
                    // matches the WinRT ABI directly. Truly complex structs (with string/object/
                    // Nullable<T> fields) need the ABI struct.
                    if (IsAnyStruct(context.Cache, dts))
                    {
                        WriteTypedefName(writer, context, d.Type, TypedefNameType.Projected, true);
                    }
                    else
                    {
                        WriteTypedefName(writer, context, d.Type, TypedefNameType.ABI, true);
                    }
                }
                else
                {
                    writer.Write("void*");
                }
                break;
            case TypeSemantics.Reference r:
                // Cross-module typeref: try resolving the type, applying mapped-type translation
                // for the field/parameter type after resolution.
                if (context.Cache is not null)
                {
                    (string rns, string rname) = r.Reference_.Names();
                    // Special case: mapped value types that require ABI marshalling.
                    if (rns == "Windows.Foundation" && rname == "DateTime")
                    {
                        writer.Write("global::ABI.System.DateTimeOffset");
                        break;
                    }
                    if (rns == "Windows.Foundation" && rname == "TimeSpan")
                    {
                        writer.Write("global::ABI.System.TimeSpan");
                        break;
                    }
                    if (rns == "Windows.Foundation" && rname == "HResult")
                    {
                        writer.Write("global::ABI.System.Exception");
                        break;
                    }
                    // Look up the type by its ORIGINAL (unmapped) name in the cache.
                    TypeDefinition? rd = context.Cache.Find(rns + "." + rname);
                    // If not found, try the mapped name (for cases where the mapping target is in the cache).
                    if (rd is null)
                    {
                        MappedType? rmapped = MappedTypes.Get(rns, rname);
                        if (rmapped is not null)
                        {
                            rd = context.Cache.Find(rmapped.MappedNamespace + "." + rmapped.MappedName);
                        }
                    }
                    if (rd is not null)
                    {
                        TypeCategory cat = TypeCategorization.GetCategory(rd);
                        if (cat == TypeCategory.Enum)
                        {
                            // Enums use the projected enum type directly (C# layout == ABI layout).
                            WriteTypedefName(writer, context, rd, TypedefNameType.Projected, true);
                            break;
                        }
                        if (cat == TypeCategory.Struct)
                        {
                            // Special case: HResult is mapped to System.Exception (a reference type)
                            // but its ABI representation is the global::ABI.System.Exception struct
                            // (which wraps the underlying HRESULT int).
                            (string rdNs, string rdName) = rd.Names();
                            if (rdNs == "Windows.Foundation" && rdName == "HResult")
                            {
                                writer.Write("global::ABI.System.Exception");
                                break;
                            }
                            if (IsAnyStruct(context.Cache, rd.ToTypeSignature()))
                            {
                                WriteTypedefName(writer, context, rd, TypedefNameType.Projected, true);
                            }
                            else
                            {
                                WriteTypedefName(writer, context, rd, TypedefNameType.ABI, true);
                            }
                            break;
                        }
                    }
                }
                // Unresolved cross-assembly TypeRef. If the signature was encoded as a value type
                // (e.g. WindowId from Microsoft.UI.winmd when that winmd isn't loaded), assume it's
                // a blittable struct and emit the projected type name — the consumer's compiler
                // will resolve it via their own references. Otherwise (encoded as Class) emit
                // void* (it's a runtime class/interface/delegate).
                if (r.IsValueType)
                {
                    (string rns, string rname) = r.Reference_.Names();
                    writer.Write("global::");
                    if (!string.IsNullOrEmpty(rns)) { writer.Write(rns); writer.Write("."); }
                    writer.Write(IdentifierEscaping.StripBackticks(rname));
                    break;
                }
                writer.Write("void*");
                break;
            case TypeSemantics.GenericInstance:
                writer.Write("void*");
                break;
            default:
                writer.Write("void*");
                break;
        }
    }

    internal static string GetAbiFundamentalType(FundamentalType t) => t switch
    {
        FundamentalType.Boolean => "bool",
        FundamentalType.Char => "char",
        FundamentalType.String => "void*",
        _ => FundamentalTypes.ToCSharpType(t)
    };
}
