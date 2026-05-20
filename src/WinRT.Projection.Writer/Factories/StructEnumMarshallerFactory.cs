// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Factories.Callbacks;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories;

/// <summary>
/// Emits the static marshaller class for a complex struct or enum type (managed-to-ABI/ABI-to-managed conversion, blittable detection, etc.).
/// </summary>
internal static class StructEnumMarshallerFactory
{
    /// <summary>
    /// Returns the instance fields of <paramref name="type"/> (skipping static fields and fields
    /// without a signature). Used by the 4 places in this file that loop over a struct's fields.
    /// </summary>
    private static IEnumerable<FieldDefinition> GetInstanceFields(TypeDefinition type)
    {
        foreach (FieldDefinition field in type.Fields)
        {
            if (field.IsStatic || field.Signature is null)
            {
                continue;
            }

            yield return field;
        }
    }

    /// <summary>
    /// Writes a marshaller class for a struct or enum.
    /// </summary>
    internal static void WriteStructEnumMarshallerClass(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string nameStripped = type.GetStrippedName();
        TypeCategory cat = TypeCategorization.GetCategory(type);

        // "Almost-blittable" includes blittable + bool/char fields. Excludes string/object fields.
        // Use the same predicate as IsAnyStruct (which is now scoped to almost-blittable).
        TypeDefOrRefSignature sig = type.ToTypeSignature(false) is TypeDefOrRefSignature td2 ? td2 : null!;
        bool almostBlittable = cat == TypeCategory.Struct && (sig is null || context.AbiTypeKindResolver.IsBlittableStruct(sig));
        bool isEnum = cat == TypeCategory.Enum;

        // Complex structs are non-almost-blittable structs with reference fields (string, object, etc.).
        bool isComplexStruct = cat == TypeCategory.Struct && !almostBlittable;

        // Detect Nullable<T> reference fields to determine whether the struct's BoxToUnmanaged
        // call needs CreateComInterfaceFlags.TrackerSupport .
        bool hasReferenceFields = false;

        if (isComplexStruct)
        {
            foreach (FieldDefinition field in GetInstanceFields(type))
            {
                TypeSignature ft = field.Signature!.FieldType;

                if (AbiTypeHelpers.TryGetNullablePrimitiveMarshallerName(ft, out _))
                {
                    hasReferenceFields = true;
                }
            }
        }

        // For structs that are mapped (e.g. Duration, KeyTime, RepeatBehavior — they have
        // EmitAbi=true and an addition file that completely replaces the public struct), skip
        // the per-field ConvertToUnmanaged/ConvertToManaged because the projected struct's
        // public fields don't match the WinMD field layout. The truth marshaller for these
        // contains only BoxToUnmanaged/UnboxToManaged.
        (string typeNs, string typeNm) = type.Names();
        bool isMappedStruct = isComplexStruct && MappedTypes.Get(typeNs, typeNm) is not null;

        if (isMappedStruct)
        {
            isComplexStruct = false;
        }

        WriteTypedefNameCallback abi = TypedefNameWriter.WriteTypedefName(context, type, TypedefNameType.ABI, false);
        WriteTypedefNameCallback projected = TypedefNameWriter.WriteTypedefName(context, type, TypedefNameType.Projected, true);

        writer.WriteLine(isMultiline: true, $$"""
            public static unsafe class {{nameStripped}}Marshaller
            {
            """);
        writer.IncreaseIndent();

        if (isComplexStruct)
        {
            // ConvertToUnmanaged: build ABI struct from projected struct via per-field marshalling.
            writer.WriteLine(isMultiline: true, $$"""
                public static {{abi}} ConvertToUnmanaged({{projected}} value)
                {
                    return new() {
                """);
            writer.IncreaseIndent();
            writer.IncreaseIndent();
            bool first = true;
            foreach (FieldDefinition field in GetInstanceFields(type))
            {
                string fname = field.Name?.Value ?? "";
                TypeSignature ft = field.Signature!.FieldType;

                writer.WriteLineIf(!first, ",");

                first = false;
                writer.Write($"{fname} = ");

                if (ft.IsString())
                {
                    writer.Write($"HStringMarshaller.ConvertToUnmanaged(value.{fname})");
                }
                else if (context.AbiTypeKindResolver.IsMappedAbiValueType(ft))
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
                else if (ft is TypeDefOrRefSignature ftd
                         && AbiTypeHelpers.TryResolveStructTypeDef(context.Cache, ftd) is TypeDefinition fieldStructTd
                         && TypeCategorization.GetCategory(fieldStructTd) == TypeCategory.Struct
                         && !AbiTypeHelpers.IsTypeBlittable(context.Cache, fieldStructTd))
                {
                    // Nested non-blittable struct: marshal via its <Name>Marshaller.
                    writer.Write($"{fieldStructTd.GetStrippedName()}Marshaller.ConvertToUnmanaged(value.{fname})");
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

            // - In component mode: emit object initializer with named field assignments
            //   (positional ctor not always available on authored types).
            // - In non-component mode: emit positional constructor (matches the auto-generated
            //   primary constructor on projected struct types).
            bool useObjectInitializer = context.Settings.Component;
            writer.WriteLine();
            writer.DecreaseIndent();
            writer.WriteLine("};");
            writer.DecreaseIndent();
            writer.WriteLine("}");
            writer.WriteLine(isMultiline: true, $$"""
                public static {{projected}} ConvertToManaged({{abi}} value)
                {
                    return new {{projected}}{{(useObjectInitializer ? "(){" : "(")}}
                """);
            writer.IncreaseIndent();
            writer.IncreaseIndent();
            first = true;
            foreach (FieldDefinition field in GetInstanceFields(type))
            {
                string fname = field.Name?.Value ?? "";
                TypeSignature ft = field.Signature!.FieldType;

                writer.WriteLineIf(!first, ",");

                first = false;
                writer.Write(useObjectInitializer ? $"{fname} = " : "");

                if (ft.IsString())
                {
                    writer.Write($"HStringMarshaller.ConvertToManaged(value.{fname})");
                }
                else if (context.AbiTypeKindResolver.IsMappedAbiValueType(ft))
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
                else if (ft is TypeDefOrRefSignature ftd2
                         && AbiTypeHelpers.TryResolveStructTypeDef(context.Cache, ftd2) is TypeDefinition fieldStructTd2
                         && TypeCategorization.GetCategory(fieldStructTd2) == TypeCategory.Struct
                         && !AbiTypeHelpers.IsTypeBlittable(context.Cache, fieldStructTd2))
                {
                    // Nested non-blittable struct: convert via its <Name>Marshaller.
                    writer.Write($"{fieldStructTd2.GetStrippedName()}Marshaller.ConvertToManaged(value.{fname})");
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
            writer.WriteLine();
            writer.DecreaseIndent();
            writer.WriteLine(useObjectInitializer ? "};" : ");");
            writer.DecreaseIndent();
            writer.WriteLine("}");
            writer.WriteLine(isMultiline: true, $$"""
                public static void Dispose({{abi}} value)
                {
                """);
            writer.IncreaseIndent();
            foreach (FieldDefinition field in GetInstanceFields(type))
            {
                string fname = field.Name?.Value ?? "";
                TypeSignature ft = field.Signature!.FieldType;

                if (ft.IsString())
                {
                    writer.WriteLine($"HStringMarshaller.Free(value.{fname});");
                }
                else if (ft.IsHResultException())
                {
                    // HResult/Exception field has no per-value resources to release
                    // (the ABI representation is just an int HRESULT). Skip Dispose entirely.
                    continue;
                }
                else if (context.AbiTypeKindResolver.IsMappedAbiValueType(ft))
                {
                    // Mapped value types (DateTime/TimeSpan) have no per-value resources to
                    // release — the ABI representation is just an int64
                    continue;
                }
                else if (ft is TypeDefOrRefSignature ftd3
                         && AbiTypeHelpers.TryResolveStructTypeDef(context.Cache, ftd3) is TypeDefinition fieldStructTd3
                         && TypeCategorization.GetCategory(fieldStructTd3) == TypeCategory.Struct
                         && !AbiTypeHelpers.IsTypeBlittable(context.Cache, fieldStructTd3))
                {
                    // Nested non-blittable struct: dispose via its <Name>Marshaller.
                    string nestedNs = fieldStructTd3.Namespace?.Value ?? string.Empty;
                    string nestedNm = fieldStructTd3.GetStrippedName();
                    writer.WriteLine($"global::ABI.{nestedNs}.{nestedNm}Marshaller.Dispose(value.{fname});");
                }
                else if (AbiTypeHelpers.TryGetNullablePrimitiveMarshallerName(ft, out _))
                {
                    writer.WriteLine($"WindowsRuntimeUnknownMarshaller.Free(value.{fname});");
                }
            }
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }

        // BoxToUnmanaged: same pattern for all (enum, almost-blittable, complex, mapped struct).
        // Truth uses CreateComInterfaceFlags.TrackerSupport when the struct has reference type
        // fields (Nullable<T>, etc.) to avoid GC issues with the boxed managed object reference.
        // Mapped struct (Duration/KeyTime/etc.) variants always use None — the public projected
        // type still routes through this marshaller (it just lacks per-field
        // ConvertToUnmanaged/ConvertToManaged because the field layout doesn't match).
        string boxFlags = (isEnum || almostBlittable || isComplexStruct) && hasReferenceFields ? "TrackerSupport" : "None";
        WriteIidReferenceExpressionCallback boxIidRef = ObjRefNameGenerator.WriteIidReferenceExpression(type);

        writer.WriteLine(isMultiline: true, $$"""
            public static WindowsRuntimeObjectReferenceValue BoxToUnmanaged({{projected}}? value)
            {
                return WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged(value, CreateComInterfaceFlags.{{boxFlags}}, in {{boxIidRef}});
            }
            """);

        // UnboxToManaged: simple for almost-blittable and mapped structs; for complex, unbox to
        // ABI struct then ConvertToManaged. Mapped struct unboxes directly to projected type (no
        // per-field ConvertToManaged needed because the projected struct's field layout matches
        // the WinMD struct layout).
        if (isComplexStruct)
        {
            writer.WriteLine(isMultiline: true, $$"""
                public static {{projected}}? UnboxToManaged(void* value)
                {
                    {{abi}}? abi = WindowsRuntimeValueTypeMarshaller.UnboxToManaged<{{abi}}>(value);
                    return abi.HasValue ? ConvertToManaged(abi.GetValueOrDefault()) : null;
                }
                """);
        }
        else
        {
            writer.WriteLine(isMultiline: true, $$"""
                public static {{projected}}? UnboxToManaged(void* value)
                {
                    return WindowsRuntimeValueTypeMarshaller.UnboxToManaged<{{projected}}>(value);
                }
                """);
        }

        writer.DecreaseIndent();
        writer.WriteLine("}");
        writer.WriteLine();

        // Emit the InterfaceEntriesImpl static class and the proper ComWrappersMarshallerAttribute
        // class derived from WindowsRuntimeComWrappersMarshallerAttribute (matches truth).
        // For enums and almost-blittable structs, GetOrCreateComInterfaceForObject uses None.
        // For complex structs (with reference fields), it uses TrackerSupport.
        // For complex structs, CreateObject converts via the *Marshaller.ConvertToManaged after
        // unboxing to the ABI struct.
        if (isEnum || almostBlittable || isComplexStruct)
        {
            WriteIidReferenceExpressionCallback iidRefExpr = ObjRefNameGenerator.WriteIidReferenceExpression(type);

            // InterfaceEntriesImpl
            writer.WriteLine(isMultiline: true, $$"""
                file static class {{nameStripped}}InterfaceEntriesImpl
                {
                    [FixedAddressValueType]
                    public static readonly ReferenceInterfaceEntries Entries;
                
                    static {{nameStripped}}InterfaceEntriesImpl()
                    {
                        Entries.IReferenceValue.IID = {{iidRefExpr}};
                        Entries.IReferenceValue.Vtable = {{nameStripped}}ReferenceImpl.Vtable;
                        Entries.IPropertyValue.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IPropertyValue;
                        Entries.IPropertyValue.Vtable = global::WindowsRuntime.InteropServices.IPropertyValueImpl.OtherTypeVtable;
                        Entries.IStringable.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IStringable;
                        Entries.IStringable.Vtable = global::WindowsRuntime.InteropServices.IStringableImpl.Vtable;
                        Entries.IWeakReferenceSource.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IWeakReferenceSource;
                        Entries.IWeakReferenceSource.Vtable = global::WindowsRuntime.InteropServices.IWeakReferenceSourceImpl.Vtable;
                        Entries.IMarshal.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IMarshal;
                        Entries.IMarshal.Vtable = global::WindowsRuntime.InteropServices.IMarshalImpl.Vtable;
                        Entries.IAgileObject.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IAgileObject;
                        Entries.IAgileObject.Vtable = global::WindowsRuntime.InteropServices.IAgileObjectImpl.Vtable;
                        Entries.IInspectable.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IInspectable;
                        Entries.IInspectable.Vtable = global::WindowsRuntime.InteropServices.IInspectableImpl.Vtable;
                        Entries.IUnknown.IID = global::WindowsRuntime.InteropServices.WellKnownInterfaceIIDs.IID_IUnknown;
                        Entries.IUnknown.Vtable = global::WindowsRuntime.InteropServices.IUnknownImpl.Vtable;
                    }
                }
                """);
            writer.WriteLine();

            // is NOT emitted for STRUCTS (the attribute is supplied by cswinrtgen instead). Enums
            // and other types still emit it from write_abi_enum/etc.
            if (context.Settings.Component && cat == TypeCategory.Struct)
            {
                return;
            }

            // ComWrappersMarshallerAttribute (full body)
            writer.WriteLine(isMultiline: true, $$"""
                internal sealed unsafe class {{nameStripped}}ComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
                {
                    public override void* GetOrCreateComInterfaceForObject(object value)
                    {
                        return WindowsRuntimeComWrappersMarshal.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.{{(hasReferenceFields ? "TrackerSupport" : "None")}});
                    }
                
                    public override ComInterfaceEntry* ComputeVtables(out int count)
                    {
                        count = sizeof(ReferenceInterfaceEntries) / sizeof(ComInterfaceEntry);
                        return (ComInterfaceEntry*)Unsafe.AsPointer(in {{nameStripped}}InterfaceEntriesImpl.Entries);
                    }
                
                    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
                    {
                        wrapperFlags = CreatedWrapperFlags.NonWrapping;
                """);
            writer.IncreaseIndent();
            writer.IncreaseIndent();
            if (isComplexStruct)
            {
                WriteTypedefNameCallback abiFq = TypedefNameWriter.WriteTypedefName(context, type, TypedefNameType.ABI, true);
                writer.WriteLine($"return {nameStripped}Marshaller.ConvertToManaged(WindowsRuntimeValueTypeMarshaller.UnboxToManagedUnsafe<{abiFq}>(value, in {iidRefExpr}));");
            }
            else
            {
                writer.WriteLine($"return WindowsRuntimeValueTypeMarshaller.UnboxToManagedUnsafe<{projected}>(value, in {iidRefExpr});");
            }
            writer.DecreaseIndent();
            writer.WriteLine("}");
            writer.DecreaseIndent();
            writer.WriteLine("}");
        }
        else
        {
            // Fallback: keep the placeholder class so consumer attribute references resolve.
            writer.WriteLine(isMultiline: true, $$"""
                internal sealed class {{nameStripped}}ComWrappersMarshallerAttribute : global::System.Attribute
                {
                }
                """);
        }
    }
}
