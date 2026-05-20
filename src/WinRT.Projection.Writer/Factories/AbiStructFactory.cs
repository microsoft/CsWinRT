// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Factories.Callbacks;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Factories;

/// <summary>
/// Emits the ABI struct layout (when needed), the ABI marshaller class, and the
/// IReference&lt;T&gt; impl for a projected struct type.
/// </summary>
internal static class AbiStructFactory
{
    /// <summary>
    /// Writes the ABI struct, marshaller class, and IReference impl for a struct type.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="type">The struct type definition.</param>
    public static void WriteAbiStruct(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        // Emit the underlying ABI struct only when not blittable AND not a mapped struct
        // (mapped structs like Duration/KeyTime/RepeatBehavior have addition files that
        // replace the public struct's field layout, so a per-field ABI struct can't be
        // built directly from the projected type).
        bool blittable = AbiTypeHelpers.IsTypeBlittable(context.Cache, type);
        (string typeNs, string typeNm) = type.Names();
        bool isMappedStruct = MappedTypes.Get(typeNs, typeNm) is not null;

        if (!blittable && !isMappedStruct)
        {
            // In component mode emit the [WindowsRuntimeMetadataTypeName]/[WindowsRuntimeMappedType]
            // attribute pair; otherwise emit the [ComWrappersMarshaller] attribute. Both branches
            // then emit [WindowsRuntimeClassName] + the struct definition with public ABI fields.
            string nameStripped = type.GetStrippedName();
            string marshallerOrTypeAttrs = context.Settings.Component
                ? $"{MetadataAttributeFactory.WriteWinRTMetadataTypeNameAttribute(context, type).Format()}\n{MetadataAttributeFactory.WriteWinRTMappedTypeAttribute(context, type).Format()}"
                : MetadataAttributeFactory.WriteComWrapperMarshallerAttribute(context, type).Format();
            WriteValueTypeWinRTClassNameAttributeCallback valueTypeAttr = MetadataAttributeFactory.WriteValueTypeWinRTClassNameAttribute(context, type);
            writer.WriteLine(isMultiline: true, $$"""
                {{marshallerOrTypeAttrs}}
                {{valueTypeAttr}}
                public unsafe struct {{nameStripped}}
                """);
            using (writer.WriteBlock())
            {
                foreach (FieldDefinition field in type.Fields)
                {
                    if (field.IsStatic || field.Signature is null)
                    {
                        continue;
                    }

                    TypeSignature ft = field.Signature.FieldType;
                    string fieldType = GetAbiFieldType(context, ft);
                    writer.WriteLine($"public {fieldType} {field.Name?.Value};");
                }
            }
            writer.WriteLine();
        }
        else if (blittable && context.Settings.Component)
        {
            // For blittable component structs, emit the authoring metadata wrapper
            // (a 'file static class T {}' with the WinRT metadata attributes).
            AbiClassFactory.WriteAuthoringMetadataType(writer, context, type);
        }

        StructEnumMarshallerFactory.WriteStructEnumMarshallerClass(writer, context, type);
        ReferenceImplFactory.WriteReferenceImpl(writer, context, type);
    }

    /// <summary>
    /// Returns the projected ABI field type for an unblittable / unmapped struct field.
    /// Truth uses <c>void*</c> for string and <c>Nullable&lt;T&gt;</c> fields, the mapped ABI
    /// type for mapped value types (DateTime/TimeSpan), the ABI typedef for nested non-blittable
    /// structs, and the projected C# type for everything else (including enums and bool — their
    /// C# layout matches the WinRT ABI directly).
    /// </summary>
    private static string GetAbiFieldType(ProjectionEmitContext context, TypeSignature ft)
    {
        if (ft.IsString() || AbiTypeHelpers.TryGetNullablePrimitiveMarshallerName(ft, out _))
        {
            return "void*";
        }

        if (context.AbiTypeKindResolver.IsMappedAbiValueType(ft))
        {
            return AbiTypeHelpers.GetMappedAbiTypeName(ft);
        }

        if (ft is TypeDefOrRefSignature tdr
            && AbiTypeHelpers.TryResolveStructTypeDef(context.Cache, tdr) is TypeDefinition fieldTd
            && TypeCategorization.GetCategory(fieldTd) == TypeCategory.Struct
            && !AbiTypeHelpers.IsTypeBlittable(context.Cache, fieldTd))
        {
            return TypedefNameWriter.WriteTypedefName(context, fieldTd, TypedefNameType.ABI, false).Format();
        }

        // Default: emit the projected C# type via the signature writer.
        using IndentedTextWriterOwner owner = IndentedTextWriterPool.GetOrCreate();
        MethodFactory.WriteProjectedSignature(owner.Writer, context, ft, false);
        return owner.Writer.ToString();
    }
}
