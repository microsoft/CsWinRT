// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Extensions;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Emits the ABI struct layout (when needed), the ABI marshaller class, and the
/// IReference&lt;T&gt; impl for a projected struct type.
/// </summary>
internal static class AbiStructFactory
{
    /// <summary>Writes the ABI struct, marshaller class, and IReference impl for a struct type.</summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="type">The struct type definition.</param>
    public static void Write(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        // Emit the underlying ABI struct only when not blittable AND not a mapped struct
        // (mapped structs like Duration/KeyTime/RepeatBehavior have addition files that
        // replace the public struct's field layout, so a per-field ABI struct can't be
        // built directly from the projected type).
        bool blittable = CodeWriters.IsTypeBlittable(context.Cache, type);
        (string typeNs, string typeNm) = type.Names();
        bool isMappedStruct = MappedTypes.Get(typeNs, typeNm) is not null;
        if (!blittable && !isMappedStruct)
        {
            // In component mode emit the [WindowsRuntimeMetadataTypeName]/[WindowsRuntimeMappedType]
            // attribute pair; otherwise emit the [ComWrappersMarshaller] attribute. Both branches
            // then emit [WindowsRuntimeClassName] + the struct definition with public ABI fields.
            if (context.Settings.Component)
            {
                CodeWriters.WriteWinRTMetadataTypeNameAttribute(writer, context, type);
                CodeWriters.WriteWinRTMappedTypeAttribute(writer, context, type);
            }
            else
            {
                CodeWriters.WriteComWrapperMarshallerAttribute(writer, context, type);
            }
            CodeWriters.WriteValueTypeWinRTClassNameAttribute(writer, context, type);
            writer.Write(AccessibilityHelper.InternalAccessibility(context.Settings));
            writer.Write(" unsafe struct ");
            CodeWriters.WriteTypedefName(writer, context, type, TypedefNameType.ABI, false);
            writer.Write("\n{\n");
            foreach (FieldDefinition field in type.Fields)
            {
                if (field.IsStatic || field.Signature is null) { continue; }
                AsmResolver.DotNet.Signatures.TypeSignature ft = field.Signature.FieldType;
                writer.Write("public ");
                // Truth uses void* for string and Nullable<T> fields, the ABI type for mapped value
                // types (DateTime/TimeSpan), and the projected type for everything else (including
                // enums and bool — their C# layout matches the WinRT ABI directly).
                if (ft.IsString() || CodeWriters.TryGetNullablePrimitiveMarshallerName(ft, out _))
                {
                    writer.Write("void*");
                }
                else if (CodeWriters.IsMappedAbiValueType(ft))
                {
                    writer.Write(CodeWriters.GetMappedAbiTypeName(ft));
                }
                else if (ft is AsmResolver.DotNet.Signatures.TypeDefOrRefSignature tdr
                         && CodeWriters.TryResolveStructTypeDef(context.Cache, tdr) is TypeDefinition fieldTd
                         && TypeCategorization.GetCategory(fieldTd) == TypeCategory.Struct
                         && !CodeWriters.IsTypeBlittable(context.Cache, fieldTd))
                {
                    CodeWriters.WriteTypedefName(writer, context, fieldTd, TypedefNameType.ABI, false);
                }
                else
                {
                    MethodFactory.WriteProjectedSignature(writer, context, ft, false);
                }
                writer.Write(" ");
                writer.Write(field.Name?.Value ?? string.Empty);
                writer.Write(";\n");
            }
            writer.Write("}\n\n");
        }
        else if (blittable && context.Settings.Component)
        {
            // For blittable component structs, emit the authoring metadata wrapper
            // (a 'file static class T {}' with the WinRT metadata attributes).
            AbiClassFactory.WriteAuthoringMetadataType(writer, context, type);
        }

        StructEnumMarshallerFactory.WriteStructEnumMarshallerClass(writer, context, type);
        ReferenceImplFactory.Write(writer, context, type);
    }
}
