// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Extensions;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Emits the static marshaller class for a complex struct or enum type (managed-to-ABI/ABI-to-managed conversion, blittable detection, etc.).
/// </summary>
internal static class StructEnumMarshallerFactory
{
    /// <summary>
    /// Writes a marshaller class for a struct or enum.
    /// </summary>
    internal static void WriteStructEnumMarshallerClass(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string name = type.Name?.Value ?? string.Empty;
        string nameStripped = IdentifierEscaping.StripBackticks(name);
        TypeCategory cat = TypeCategorization.GetCategory(type);
        // "Almost-blittable" includes blittable + bool/char fields. Excludes string/object fields.
        // Use the same predicate as IsAnyStruct (which is now scoped to almost-blittable).
        AsmResolver.DotNet.Signatures.TypeDefOrRefSignature sig = type.ToTypeSignature(false) is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature td2 ? td2 : null!;
        bool almostBlittable = cat == TypeCategory.Struct && (sig is null || AbiTypeHelpers.IsAnyStruct(context.Cache, sig));
        bool isEnum = cat == TypeCategory.Enum;
        // Complex structs are non-almost-blittable structs with reference fields (string, object, etc.).
        bool isComplexStruct = cat == TypeCategory.Struct && !almostBlittable;
        // Detect Nullable<T> reference fields to determine whether the struct's BoxToUnmanaged
        // call needs CreateComInterfaceFlags.TrackerSupport .
        bool hasReferenceFields = false;
        if (isComplexStruct)
        {
            foreach (FieldDefinition field in type.Fields)
            {
                if (field.IsStatic || field.Signature is null) { continue; }
                AsmResolver.DotNet.Signatures.TypeSignature ft = field.Signature.FieldType;
                if (AbiTypeHelpers.TryGetNullablePrimitiveMarshallerName(ft, out _)) { hasReferenceFields = true; }
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

        writer.Write($"public static unsafe class {nameStripped}Marshaller\n{{\n");

        if (isComplexStruct)
        {
            // ConvertToUnmanaged: build ABI struct from projected struct via per-field marshalling.
            writer.Write("    public static ");
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.ABI, false);
            writer.Write(" ConvertToUnmanaged(");
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
            writer.WriteLine(" value)");
            writer.WriteLine("    {");
            writer.WriteLine("        return new() {");
            bool first = true;
            foreach (FieldDefinition field in type.Fields)
            {
                if (field.IsStatic || field.Signature is null) { continue; }
                string fname = field.Name?.Value ?? "";
                AsmResolver.DotNet.Signatures.TypeSignature ft = field.Signature.FieldType;
                if (!first) { writer.WriteLine(","); }
                first = false;
                writer.Write($"            {fname} = ");
                if (ft.IsString())
                {
                    writer.Write($"HStringMarshaller.ConvertToUnmanaged(value.{fname})");
                }
                else if (AbiTypeHelpers.IsMappedAbiValueType(ft))
                {
                    writer.Write($"{AbiTypeHelpers.GetMappedMarshallerName(ft)}.ConvertToUnmanaged(value.{fname})");
                }
                else if (ft.IsHResultException())
                {
                    // Mapped value type 'HResult' (excluded from IsMappedAbiValueType because
                    // it's "treated specially in many places", but for nested struct fields the
                    // marshalling is identical: use ABI.System.ExceptionMarshaller).
                    writer.Write($"global::ABI.System.ExceptionMarshaller.ConvertToUnmanaged(value.{fname})");
                }
                else if (ft is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature ftd
                         && AbiTypeHelpers.TryResolveStructTypeDef(context.Cache, ftd) is TypeDefinition fieldStructTd
                         && TypeCategorization.GetCategory(fieldStructTd) == TypeCategory.Struct
                         && !AbiTypeHelpers.IsTypeBlittable(context.Cache, fieldStructTd))
                {
                    // Nested non-blittable struct: marshal via its <Name>Marshaller.
                    writer.Write($"{IdentifierEscaping.StripBackticks(fieldStructTd.Name?.Value ?? string.Empty)}Marshaller.ConvertToUnmanaged(value.{fname})");
                }
                else if (AbiTypeHelpers.TryGetNullablePrimitiveMarshallerName(ft, out string? nullableMarshaller))
                {
                    writer.Write($"{nullableMarshaller!}.BoxToUnmanaged(value.{fname}).DetachThisPtrUnsafe()");
                }
                else
                {
                    writer.Write($"value.{fname}");
                }
            }
            writer.WriteLine("");
            writer.WriteLine("        };");
            writer.WriteLine("    }");
            writer.Write("    public static ");
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
            writer.Write(" ConvertToManaged(");
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.ABI, false);
            // - In component mode: emit object initializer with named field assignments
            //   (positional ctor not always available on authored types).
            // - In non-component mode: emit positional constructor (matches the auto-generated
            //   primary constructor on projected struct types).
            bool useObjectInitializer = context.Settings.Component;
            writer.WriteLine(" value)");
            writer.WriteLine("    {");
            writer.Write("        return new ");
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
            writer.Write(useObjectInitializer ? "(){\n" : "(\n");
            first = true;
            foreach (FieldDefinition field in type.Fields)
            {
                if (field.IsStatic || field.Signature is null) { continue; }
                string fname = field.Name?.Value ?? "";
                AsmResolver.DotNet.Signatures.TypeSignature ft = field.Signature.FieldType;
                if (!first) { writer.WriteLine(","); }
                first = false;
                writer.Write("            ");
                if (useObjectInitializer)
                {
                    writer.Write($"{fname} = ");
                }
                if (ft.IsString())
                {
                    writer.Write($"HStringMarshaller.ConvertToManaged(value.{fname})");
                }
                else if (AbiTypeHelpers.IsMappedAbiValueType(ft))
                {
                    writer.Write($"{AbiTypeHelpers.GetMappedMarshallerName(ft)}.ConvertToManaged(value.{fname})");
                }
                else if (ft.IsHResultException())
                {
                    // Mapped value type 'HResult' (excluded from IsMappedAbiValueType because
                    // it's "treated specially in many places", but for nested struct fields the
                    // marshalling is identical: use ABI.System.ExceptionMarshaller).
                    writer.Write($"global::ABI.System.ExceptionMarshaller.ConvertToManaged(value.{fname})");
                }
                else if (ft is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature ftd2
                         && AbiTypeHelpers.TryResolveStructTypeDef(context.Cache, ftd2) is TypeDefinition fieldStructTd2
                         && TypeCategorization.GetCategory(fieldStructTd2) == TypeCategory.Struct
                         && !AbiTypeHelpers.IsTypeBlittable(context.Cache, fieldStructTd2))
                {
                    // Nested non-blittable struct: convert via its <Name>Marshaller.
                    writer.Write($"{IdentifierEscaping.StripBackticks(fieldStructTd2.Name?.Value ?? string.Empty)}Marshaller.ConvertToManaged(value.{fname})");
                }
                else if (AbiTypeHelpers.TryGetNullablePrimitiveMarshallerName(ft, out string? nullableMarshaller))
                {
                    writer.Write($"{nullableMarshaller!}.UnboxToManaged(value.{fname})");
                }
                else
                {
                    writer.Write($"value.{fname}");
                }
            }
            writer.Write($"{(useObjectInitializer ? "\n        };\n    }\n" : "\n        );\n    }\n")}    public static void Dispose(");
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.ABI, false);
            writer.WriteLine(" value)");
            writer.WriteLine("    {");
            foreach (FieldDefinition field in type.Fields)
            {
                if (field.IsStatic || field.Signature is null) { continue; }
                string fname = field.Name?.Value ?? "";
                AsmResolver.DotNet.Signatures.TypeSignature ft = field.Signature.FieldType;
                if (ft.IsString())
                {
                    writer.WriteLine($"        HStringMarshaller.Free(value.{fname});");
                }
                else if (ft.IsHResultException())
                {
                    // HResult/Exception field has no per-value resources to release
                    // (the ABI representation is just an int HRESULT). Skip Dispose entirely.
                    continue;
                }
                else if (AbiTypeHelpers.IsMappedAbiValueType(ft))
                {
                    // Mapped value types (DateTime/TimeSpan) have no per-value resources to
                    // release — the ABI representation is just an int64
                    continue;
                }
                else if (ft is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature ftd3
                         && AbiTypeHelpers.TryResolveStructTypeDef(context.Cache, ftd3) is TypeDefinition fieldStructTd3
                         && TypeCategorization.GetCategory(fieldStructTd3) == TypeCategory.Struct
                         && !AbiTypeHelpers.IsTypeBlittable(context.Cache, fieldStructTd3))
                {
                    // Nested non-blittable struct: dispose via its <Name>Marshaller.
                    string nestedNs = fieldStructTd3.Namespace?.Value ?? string.Empty;
                    string nestedNm = IdentifierEscaping.StripBackticks(fieldStructTd3.Name?.Value ?? string.Empty);
                    writer.WriteLine($"        global::ABI.{nestedNs}.{nestedNm}Marshaller.Dispose(value.{fname});");
                }
                else if (AbiTypeHelpers.TryGetNullablePrimitiveMarshallerName(ft, out _))
                {
                    writer.WriteLine($"        WindowsRuntimeUnknownMarshaller.Free(value.{fname});");
                }
            }
            writer.WriteLine("    }");
        }

        // BoxToUnmanaged: same pattern for all (enum, almost-blittable, complex).
        // Truth uses CreateComInterfaceFlags.TrackerSupport when the struct has reference type
        // fields (Nullable<T>, etc.) to avoid GC issues with the boxed managed object reference.
        writer.Write("    public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged(");
        TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
        if (isEnum || almostBlittable || isComplexStruct)
        {
            writer.Write($"? value)\n    {{\n        return WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged(value, CreateComInterfaceFlags.{(hasReferenceFields ? "TrackerSupport" : "None")}, in ");
            ObjRefNameGenerator.WriteIidReferenceExpression(writer, type);
            writer.WriteLine(");");
            writer.WriteLine("    }");
        }
        else
        {
            // Mapped struct (Duration/KeyTime/etc.): BoxToUnmanaged is still required because the
            // public projected type still routes through this marshaller (it just lacks per-field
            // ConvertToUnmanaged/ConvertToManaged because the field layout doesn't match).
            writer.WriteLine("? value)");
            writer.WriteLine("    {");
            writer.Write("        return WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged(value, CreateComInterfaceFlags.None, in ");
            ObjRefNameGenerator.WriteIidReferenceExpression(writer, type);
            writer.WriteLine(");");
            writer.WriteLine("    }");
        }

        // UnboxToManaged: simple for almost-blittable; for complex, unbox to ABI struct then ConvertToManaged.
        writer.Write("    public static ");
        TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
        if (isEnum || almostBlittable)
        {
            writer.WriteLine("? UnboxToManaged(void* value)");
            writer.WriteLine("    {");
            writer.Write("        return WindowsRuntimeValueTypeMarshaller.UnboxToManaged<");
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
            writer.WriteLine(">(value);");
            writer.WriteLine("    }");
        }
        else if (isComplexStruct)
        {
            writer.WriteLine("? UnboxToManaged(void* value)");
            writer.WriteLine("    {");
            writer.Write("        ");
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.ABI, false);
            writer.Write("? abi = WindowsRuntimeValueTypeMarshaller.UnboxToManaged<");
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.ABI, false);
            writer.WriteLine(">(value);");
            writer.WriteLine("        return abi.HasValue ? ConvertToManaged(abi.GetValueOrDefault()) : null;");
            writer.WriteLine("    }");
        }
        else
        {
            // Mapped struct: unbox directly to projected type (no per-field ConvertToManaged needed
            // because the projected struct's field layout matches the WinMD struct layout).
            writer.WriteLine("? UnboxToManaged(void* value)");
            writer.WriteLine("    {");
            writer.Write("        return WindowsRuntimeValueTypeMarshaller.UnboxToManaged<");
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
            writer.WriteLine(">(value);");
            writer.WriteLine("    }");
        }

        writer.WriteLine("}");
        writer.WriteLine("");

        // Emit the InterfaceEntriesImpl static class and the proper ComWrappersMarshallerAttribute
        // class derived from WindowsRuntimeComWrappersMarshallerAttribute (matches truth).
        // For enums and almost-blittable structs, GetOrCreateComInterfaceForObject uses None.
        // For complex structs (with reference fields), it uses TrackerSupport.
        // For complex structs, CreateObject converts via the *Marshaller.ConvertToManaged after
        // unboxing to the ABI struct.
        if (isEnum || almostBlittable || isComplexStruct)
        {
            IndentedTextWriter __scratchIidRefExpr = new();
            ObjRefNameGenerator.WriteIidReferenceExpression(__scratchIidRefExpr, type);
            string iidRefExpr = __scratchIidRefExpr.ToString();

            // InterfaceEntriesImpl
            writer.Write("file static class ");
            writer.Write(nameStripped);
            writer.WriteLine("InterfaceEntriesImpl");
            writer.WriteLine("{");
            writer.WriteLine("    [FixedAddressValueType]");
            writer.WriteLine("    public static readonly ReferenceInterfaceEntries Entries;");
            writer.WriteLine("");
            writer.Write("    static ");
            writer.Write(nameStripped);
            writer.WriteLine("InterfaceEntriesImpl()");
            writer.WriteLine("    {");
            writer.Write("        Entries.IReferenceValue.IID = ");
            writer.Write(iidRefExpr);
            writer.WriteLine(";");
            writer.Write("        Entries.IReferenceValue.Vtable = ");
            writer.Write(nameStripped);
            writer.WriteLine("ReferenceImpl.Vtable;");
            writer.WriteLine("        Entries.IPropertyValue.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IPropertyValue;");
            writer.WriteLine("        Entries.IPropertyValue.Vtable = global::WindowsRuntime.InteropServices.IPropertyValueImpl.OtherTypeVtable;");
            writer.WriteLine("        Entries.IStringable.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IStringable;");
            writer.WriteLine("        Entries.IStringable.Vtable = global::WindowsRuntime.InteropServices.IStringableImpl.Vtable;");
            writer.WriteLine("        Entries.IWeakReferenceSource.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IWeakReferenceSource;");
            writer.WriteLine("        Entries.IWeakReferenceSource.Vtable = global::WindowsRuntime.InteropServices.IWeakReferenceSourceImpl.Vtable;");
            writer.WriteLine("        Entries.IMarshal.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IMarshal;");
            writer.WriteLine("        Entries.IMarshal.Vtable = global::WindowsRuntime.InteropServices.IMarshalImpl.Vtable;");
            writer.WriteLine("        Entries.IAgileObject.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IAgileObject;");
            writer.WriteLine("        Entries.IAgileObject.Vtable = global::WindowsRuntime.InteropServices.IAgileObjectImpl.Vtable;");
            writer.WriteLine("        Entries.IInspectable.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IInspectable;");
            writer.WriteLine("        Entries.IInspectable.Vtable = global::WindowsRuntime.InteropServices.IInspectableImpl.Vtable;");
            writer.WriteLine("        Entries.IUnknown.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IUnknown;");
            writer.WriteLine("        Entries.IUnknown.Vtable = global::WindowsRuntime.InteropServices.IUnknownImpl.Vtable;");
            writer.WriteLine("    }");
            writer.WriteLine("}");
            writer.WriteLine("");
            // is NOT emitted for STRUCTS (the attribute is supplied by cswinrtgen instead). Enums
            // and other types still emit it from write_abi_enum/etc.
            if (context.Settings.Component && cat == TypeCategory.Struct) { return; }

            // ComWrappersMarshallerAttribute (full body)
            writer.Write("internal sealed unsafe class ");
            writer.Write(nameStripped);
            writer.WriteLine("ComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute");
            writer.WriteLine("{");
            writer.WriteLine("    public override void* GetOrCreateComInterfaceForObject(object value)");
            writer.WriteLine("    {");
            writer.Write("        return WindowsRuntimeComWrappersMarshal.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.");
            writer.Write(hasReferenceFields ? "TrackerSupport" : "None");
            writer.WriteLine(");");
            writer.WriteLine("    }");
            writer.WriteLine("");
            writer.WriteLine("    public override ComInterfaceEntry* ComputeVtables(out int count)");
            writer.WriteLine("    {");
            writer.WriteLine("        count = sizeof(ReferenceInterfaceEntries) / sizeof(ComInterfaceEntry);");
            writer.WriteLine($"        return (ComInterfaceEntry*)Unsafe.AsPointer(in {nameStripped}InterfaceEntriesImpl.Entries);\n    }}\n\n    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)\n    {{\n        wrapperFlags = CreatedWrapperFlags.NonWrapping;");
            if (isComplexStruct)
            {
                writer.Write($"        return {nameStripped}Marshaller.ConvertToManaged(WindowsRuntimeValueTypeMarshaller.UnboxToManagedUnsafe<");
                TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.ABI, true);
                writer.WriteLine($">(value, in {iidRefExpr}));");
            }
            else
            {
                writer.Write("        return WindowsRuntimeValueTypeMarshaller.UnboxToManagedUnsafe<");
                TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, true);
                writer.WriteLine($">(value, in {iidRefExpr});");
            }
            writer.WriteLine("    }");
            writer.WriteLine("}");
        }
        else
        {
            // Fallback: keep the placeholder class so consumer attribute references resolve.
            writer.Write($"internal sealed class {nameStripped}ComWrappersMarshallerAttribute : global::System.Attribute\n{{\n}}\n");
        }
    }
}