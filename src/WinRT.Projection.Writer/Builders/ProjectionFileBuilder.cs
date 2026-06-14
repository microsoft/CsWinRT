// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Errors;
using WindowsRuntime.ProjectionWriter.Factories;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Helpers;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter.Builders;

/// <summary>
/// Top-level dispatchers and emission for projected enums, structs, contracts, delegates,
/// and attribute classes.
/// </summary>
internal static class ProjectionFileBuilder
{
    /// <summary>
    /// Dispatches type emission based on the type category.
    /// </summary>
    public static void WriteType(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type, TypeKind kind)
    {
        switch (kind)
        {
            case TypeKind.Class when type.IsAttributeType:
                WriteAttribute(writer, context, type);
                break;
            case TypeKind.Class:
                ClassFactory.WriteClass(writer, context, type);
                break;
            case TypeKind.Delegate:
                WriteDelegate(writer, context, type);
                break;
            case TypeKind.Enum:
                WriteEnum(writer, context, type);
                break;
            case TypeKind.Interface:
                InterfaceFactory.WriteInterface(writer, context, type);
                break;
            case TypeKind.Struct when type.IsApiContractType:
                WriteContract(writer, context, type);
                break;
            case TypeKind.Struct:
                WriteStruct(writer, context, type);
                break;
            default:
                throw WellKnownProjectionWriterExceptions.UnknownTypeKind(kind);
        }
    }

    /// <summary>
    /// Dispatches ABI emission based on the type category.
    /// </summary>
    public static void WriteAbiType(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type, TypeKind kind)
    {
        switch (kind)
        {
            case TypeKind.Class:
                AbiClassFactory.WriteAbiClass(writer, context, type);
                break;
            case TypeKind.Delegate:
                AbiDelegateFactory.WriteAbiDelegate(writer, context, type);
                AbiDelegateFactory.WriteDelegateEventSourceSubclass(writer, context, type);
                break;
            case TypeKind.Enum:
                AbiEnumFactory.WriteAbiEnum(writer, context, type);
                break;
            case TypeKind.Interface:
                AbiInterfaceFactory.WriteAbiInterface(writer, context, type);
                break;
            case TypeKind.Struct:
                AbiStructFactory.WriteAbiStruct(writer, context, type);
                break;
            default:
                throw WellKnownProjectionWriterExceptions.UnknownTypeKind(kind);
        }
    }

    /// <summary>
    /// Writes a projected enum (with [Flags] when applicable).
    /// </summary>
    private static void WriteEnum(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (context.Settings.Component)
        {
            return;
        }

        bool isFlags = type.IsFlagsEnum;
        string enumUnderlyingType = isFlags ? "uint" : "int";
        string typeName = type.GetRawName();

        IndentedTextWriterCallback metadataAttr = MetadataAttributeFactory.WriteWinRTMetadataAttribute(type, context.Cache);
        IndentedTextWriterCallback valueTypeAttr = MetadataAttributeFactory.WriteValueTypeWinRTClassNameAttribute(context, type);
        IndentedTextWriterCallback customAttrs = CustomAttributeFactory.WriteTypeCustomAttributes(context, type, true);
        IndentedTextWriterCallback comWrappersAttr = MetadataAttributeFactory.WriteComWrapperMarshallerAttribute(context, type);
        IndentedTextWriterCallback refTypeAttr = MetadataAttributeFactory.WriteWinRTReferenceTypeAttribute(context, type);

        writer.WriteLine();
        writer.WriteLineIf(isFlags, "[Flags]");
        writer.WriteLine(isMultiline: true, $$"""
            {{metadataAttr}}
            {{valueTypeAttr}}
            {{customAttrs}}
            {{comWrappersAttr}}
            {{refTypeAttr}}
            public enum {{typeName}} : {{enumUnderlyingType}}
            """);

        using (writer.WriteBlock())
        {
            foreach (FieldDefinition field in type.Fields)
            {
                if (field.Constant is null)
                {
                    continue;
                }

                string fieldName = field.GetRawName();
                string constantValue = field.Constant.FormatLiteral();

                // Emits per-enum-field '[SupportedOSPlatform]' when the field has a '[ContractVersion]'
                CustomAttributeFactory.WritePlatformAttribute(writer, context, field);

                writer.WriteLine($"{fieldName} = unchecked(({enumUnderlyingType}){constantValue}),");
            }
        }

        writer.WriteLine();
    }

    /// <summary>
    /// Writes a projected struct.
    /// </summary>
    private static void WriteStruct(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (context.Settings.Component)
        {
            return;
        }

        List<(string TypeStr, string Name, string ParamName, bool IsInterface)> fields = [];

        // Collect field info
        foreach (FieldDefinition field in type.Fields)
        {
            if (field.IsStatic || field.Signature is null)
            {
                continue;
            }

            TypeSemantics semantics = TypeSemanticsFactory.Get(field.Signature.FieldType);
            string fieldType = TypedefNameWriter.WriteProjectionType(context, semantics).Format();
            string fieldName = field.GetRawName();
            string paramName = IdentifierEscaping.ToCamelCase(fieldName);
            bool isInterface = false;

            if (semantics is TypeSemantics.Definition d)
            {
                isInterface = d.Type.IsInterface;
            }
            else if (semantics is TypeSemantics.GenericInstance gi)
            {
                isInterface = gi.GenericType.IsInterface;
            }

            fields.Add((fieldType, fieldName, paramName, isInterface));
        }

        string projectionName = type.GetRawName();

        // Header attributes + struct declaration as a single multiline template.
        IndentedTextWriterCallback metadataAttr = MetadataAttributeFactory.WriteWinRTMetadataAttribute(type, context.Cache);
        IndentedTextWriterCallback valueTypeAttr = MetadataAttributeFactory.WriteValueTypeWinRTClassNameAttribute(context, type);
        IndentedTextWriterCallback customAttrs = CustomAttributeFactory.WriteTypeCustomAttributes(context, type, true);
        IndentedTextWriterCallback comWrappersAttr = MetadataAttributeFactory.WriteComWrapperMarshallerAttribute(context, type);
        IndentedTextWriterCallback refTypeAttr = MetadataAttributeFactory.WriteWinRTReferenceTypeAttribute(context, type);

        // We are generating all types as 'partial' so that if they have any
        // custom additions, they can be added without issues. This is much
        // simpler than having logic to check that (the metadata is the same).
        writer.WriteLine(isMultiline: true, $$"""
            {{metadataAttr}}
            {{valueTypeAttr}}
            {{customAttrs}}
            {{comWrappersAttr}}
            {{refTypeAttr}}
            public partial struct {{projectionName}} : IEquatable<{{projectionName}}>
            """);

        using (writer.WriteBlock())
        {
            // Emit the constructor declaration
            writer.Write($"public {projectionName}(");
            for (int i = 0; i < fields.Count; i++)
            {
                writer.WriteIf(i > 0, ", ");

                IndentedTextWriterCallback name = IdentifierEscaping.WriteEscapedIdentifier(fields[i].ParamName);
                writer.Write($"{fields[i].TypeStr} {name}");
            }

            // Emit the constructor body (just assigning each field)
            writer.WriteLine(")");
            using (writer.WriteBlock())
            {
                foreach ((string _, string name, string paramName, bool _) in fields)
                {
                    // When the param name matches the field name (i.e. 'ToCamelCase' couldn't
                    // change casing), qualify with 'this.' to disambiguate.
                    IndentedTextWriterCallback paramRef = IdentifierEscaping.WriteEscapedIdentifier(paramName);
                    if (name == paramName)
                    {
                        writer.Write($"this.{name} = {paramRef}; ");
                    }
                    else
                    {
                        writer.Write($"{name} = {paramRef}; ");
                    }
                }

                writer.WriteLine();
            }

            // Properties (all getters are readonly)
            foreach ((string typeStr, string name, string _, bool _) in fields)
            {
                writer.WriteLine($$"""
                    public {{typeStr}} {{name}}
                    {
                        readonly get; set;
                    }
                    """);
            }

            // Overridden '==' operator
            writer.Write($"public static bool operator ==({projectionName} x, {projectionName} y) => ");

            // If we have any fields, we just emit a direct comparison for each of them
            if (fields.Count == 0)
            {
                writer.WriteLine("true;");
            }
            else
            {
                for (int i = 0; i < fields.Count; i++)
                {
                    writer.WriteIf(i > 0, " && ");
                    writer.Write($"x.{fields[i].Name} == y.{fields[i].Name}");
                }

                writer.WriteLine(";");
            }

            // Other equality operators
            writer.WriteLine($"""
                public static bool operator !=({projectionName} x, {projectionName} y) => !(x == y);
                public bool Equals({projectionName} other) => this == other;
                public override bool Equals(object obj) => obj is {projectionName} that && this == that;
                """);

            // Also override 'GetHashCode' (especially important for structs, as it avoids reflection)
            writer.Write("public override int GetHashCode() => ");

            // If we have aby fields, just combine the hashcode of all fields
            if (fields.Count == 0)
            {
                writer.WriteLine("0;");
            }
            else
            {
                for (int i = 0; i < fields.Count; i++)
                {
                    writer.WriteIf(i > 0, " ^ ");

                    writer.Write($"{fields[i].Name}.GetHashCode()");
                }

                writer.WriteLine(";");
            }
        }

        writer.WriteLine();
    }

    /// <summary>
    /// Writes a projected API contract (an empty enum stand-in).
    /// </summary>
    private static void WriteContract(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (context.Settings.Component)
        {
            return;
        }

        string typeName = type.GetRawName();

        CustomAttributeFactory.WriteTypeCustomAttributes(writer, context, type, false);

        writer.WriteLine($"public enum {typeName};");
    }

    /// <summary>
    /// Writes a projected delegate.
    /// </summary>
    private static void WriteDelegate(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (context.Settings.Component)
        {
            return;
        }

        if (type.GetDelegateInvoke() is not { } invoke)
        {
            return;
        }

        MethodSignatureInfo sig = new(invoke);

        IndentedTextWriterCallback metadataAttr = MetadataAttributeFactory.WriteWinRTMetadataAttribute(type, context.Cache);
        IndentedTextWriterCallback customAttrs = CustomAttributeFactory.WriteTypeCustomAttributes(context, type, false);
        IndentedTextWriterCallback comWrappersAttr = MetadataAttributeFactory.WriteComWrapperMarshallerAttribute(context, type);
        string guidAttr = context.Settings.ReferenceProjection
            ? string.Empty
            : $"[Guid(\"{IidExpressionGenerator.FormatGuid(type, lowerCase: false)}\")]";
        IndentedTextWriterCallback ret = MethodFactory.WriteProjectionReturnType(context, sig);
        IndentedTextWriterCallback typedefName = TypedefNameWriter.WriteTypedefName(context, type, TypedefNameType.Projected, false);
        IndentedTextWriterCallback typeParams = TypedefNameWriter.WriteTypeParams(type);
        IndentedTextWriterCallback parms = MethodFactory.WriteParameterList(context, sig);

        writer.WriteLine();
        writer.WriteLine(isMultiline: true, $$"""
            {{metadataAttr}}
            {{customAttrs}}
            {{comWrappersAttr}}
            {{guidAttr}}
            public delegate {{ret}} {{typedefName}}{{typeParams}}({{parms}});
            """);
    }

    /// <summary>
    /// Writes a projected attribute class.
    /// </summary>
    private static void WriteAttribute(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string typeName = type.GetRawName();

        IndentedTextWriterCallback metadataAttr = MetadataAttributeFactory.WriteWinRTMetadataAttribute(type, context.Cache);
        IndentedTextWriterCallback customAttrs = CustomAttributeFactory.WriteTypeCustomAttributes(context, type, true);

        writer.WriteLine();
        writer.WriteLine(isMultiline: true, $$"""
            {{metadataAttr}}
            {{customAttrs}}
            public sealed class {{typeName}} : Attribute
            """);

        using (writer.WriteBlock())
        {
            // Constructors
            foreach (MethodDefinition method in type.GetConstructors())
            {
                IndentedTextWriterCallback parameterList = MethodFactory.WriteParameterList(context, new MethodSignatureInfo(method));

                writer.Write($$"""public {{typeName}}({{parameterList}}) { }""");
            }

            // Fields
            foreach (FieldDefinition field in type.Fields)
            {
                if (field.IsStatic || field.Signature is null)
                {
                    continue;
                }

                IndentedTextWriterCallback fieldType = TypedefNameWriter.WriteProjectionType(context, TypeSemanticsFactory.Get(field.Signature.FieldType));

                writer.WriteLine($"public {fieldType} {field.Name?.Value};");
            }
        }
    }
}
