// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Writers;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Metadata;
using AsmResolver.DotNet.Signatures;

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
            if (context.Settings.Component)
            {
                MetadataAttributeFactory.WriteWinRTMetadataTypeNameAttribute(writer, context, type);
                MetadataAttributeFactory.WriteWinRTMappedTypeAttribute(writer, context, type);
            }
            else
            {
                MetadataAttributeFactory.WriteComWrapperMarshallerAttribute(writer, context, type);
            }
            MetadataAttributeFactory.WriteValueTypeWinRTClassNameAttribute(writer, context, type);
            writer.Write($"{context.Settings.InternalAccessibility} unsafe struct ");
            TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.ABI, false);
            writer.WriteLine();
            using (writer.WriteBlock())
            {
                foreach (FieldDefinition field in type.Fields)
                {
                    if (field.IsStatic || field.Signature is null) { continue; }
                    TypeSignature ft = field.Signature.FieldType;
                    writer.Write("public ");
                    // Truth uses void* for string and Nullable<T> fields, the ABI type for mapped value
                    // types (DateTime/TimeSpan), and the projected type for everything else (including
                    // enums and bool — their C# layout matches the WinRT ABI directly).
                    if (ft.IsString() || AbiTypeHelpers.TryGetNullablePrimitiveMarshallerName(ft, out _))
                    {
                        writer.Write("void*");
                    }
                    else if (context.AbiTypeShapeResolver.IsMappedAbiValueType(ft))
                    {
                        writer.Write(AbiTypeHelpers.GetMappedAbiTypeName(ft));
                    }
                    else if (ft is TypeDefOrRefSignature tdr
                             && AbiTypeHelpers.TryResolveStructTypeDef(context.Cache, tdr) is TypeDefinition fieldTd
                             && TypeCategorization.GetCategory(fieldTd) == TypeCategory.Struct
                             && !AbiTypeHelpers.IsTypeBlittable(context.Cache, fieldTd))
                    {
                        TypedefNameWriter.WriteTypedefName(writer, context, fieldTd, TypedefNameType.ABI, false);
                    }
                    else
                    {
                        MethodFactory.WriteProjectedSignature(writer, context, ft, false);
                    }
                    writer.WriteLine($" {field.Name?.Value ?? string.Empty};");
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
}
