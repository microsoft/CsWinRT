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
            // mirrors how the C++ tool's abi_marshaler abstraction handles unknown types — it
            // dispatches based on the metadata semantics, not on resolution.
            return !td.IsValueType;
        }
        return false;
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
}
