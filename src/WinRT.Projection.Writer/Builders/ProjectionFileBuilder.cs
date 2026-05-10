// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Extensions;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Top-level dispatchers and emission for projected enums, structs, contracts, delegates,
/// and attribute classes.
/// </summary>
internal static class ProjectionFileBuilder
{
    /// <summary>Dispatches type emission based on the type category.</summary>
    public static void WriteType(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type, TypeCategory category)
    {
        switch (category)
        {
            case TypeCategory.Class:
                if (TypeCategorization.IsAttributeType(type))
                {
                    WriteAttribute(writer, context, type);
                }
                else
                {
                    ClassFactory.WriteClass(writer, context, type);
                }
                break;
            case TypeCategory.Delegate:
                WriteDelegate(writer, context, type);
                break;
            case TypeCategory.Enum:
                WriteEnum(writer, context, type);
                break;
            case TypeCategory.Interface:
                InterfaceFactory.WriteInterface(writer, context, type);
                break;
            case TypeCategory.Struct:
                if (TypeCategorization.IsApiContractType(type))
                {
                    WriteContract(writer, context, type);
                }
                else
                {
                    WriteStruct(writer, context, type);
                }
                break;
        }
    }

    /// <summary>Dispatches ABI emission based on the type category.</summary>
    public static void WriteAbiType(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type, TypeCategory category)
    {
        switch (category)
        {
            case TypeCategory.Class:
                AbiClassFactory.WriteAbiClass(writer, context, type);
                break;
            case TypeCategory.Delegate:
                AbiDelegateFactory.WriteAbiDelegate(writer, context, type);
                AbiDelegateFactory.WriteTempDelegateEventSourceSubclass(writer, context, type);
                break;
            case TypeCategory.Enum:
                AbiEnumFactory.Write(writer, context, type);
                break;
            case TypeCategory.Interface:
                AbiInterfaceFactory.WriteAbiInterface(writer, context, type);
                break;
            case TypeCategory.Struct:
                AbiStructFactory.Write(writer, context, type);
                break;
        }
    }

    /// <summary>Writes a projected enum (with [Flags] when applicable).</summary>
    public static void WriteEnum(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (context.Settings.Component)
        {
            return;
        }

        bool isFlags = TypeCategorization.IsFlagsEnum(type);
        string enumUnderlyingType = isFlags ? "uint" : "int";
        string accessibility = context.Settings.Internal ? "internal" : "public";
        string typeName = type.Name?.Value ?? string.Empty;

        if (isFlags)
        {
            writer.WriteLine("");
            writer.WriteLine("[FlagsAttribute]");
        }
        else
        {
            writer.WriteLine("");
        }
        MetadataAttributeFactory.WriteWinRTMetadataAttribute(writer, type, context.Cache);
        MetadataAttributeFactory.WriteValueTypeWinRTClassNameAttribute(writer, context, type);
        CustomAttributeFactory.WriteTypeCustomAttributes(writer, context, type, true);
        MetadataAttributeFactory.WriteComWrapperMarshallerAttribute(writer, context, type);
        MetadataAttributeFactory.WriteWinRTReferenceTypeAttribute(writer, context, type);

        writer.Write($"{accessibility} enum {typeName} : {enumUnderlyingType}\n{{\n");

        foreach (FieldDefinition field in type.Fields)
        {
            if (field.Constant is null)
            {
                continue;
            }
            string fieldName = field.Name?.Value ?? string.Empty;
            string constantValue = FormatConstant(field.Constant);
            // Emits per-enum-field [SupportedOSPlatform] when the field has a [ContractVersion].
            CustomAttributeFactory.WritePlatformAttribute(writer, context, field);
            writer.WriteLine($"{fieldName} = unchecked(({enumUnderlyingType}){constantValue}),");
        }
        writer.WriteLine("}");
        writer.WriteLine("");
    }
    /// <summary>Formats a metadata Constant value as a C# literal.</summary>
    internal static string FormatConstant(AsmResolver.DotNet.Constant constant)
    {
        // The Constant.Value contains raw bytes representing the value
        AsmResolver.PE.DotNet.Metadata.Tables.ElementType type = constant.Type;
        byte[] data = constant.Value?.Data ?? [];
        return type switch
        {
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I1 => ((sbyte)data[0]).ToString(System.Globalization.CultureInfo.InvariantCulture),
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U1 => data[0].ToString(System.Globalization.CultureInfo.InvariantCulture),
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I2 => System.BitConverter.ToInt16(data, 0).ToString(System.Globalization.CultureInfo.InvariantCulture),
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U2 => System.BitConverter.ToUInt16(data, 0).ToString(System.Globalization.CultureInfo.InvariantCulture),
            // I4/U4 use printf "%#0x" semantics: 0 -> "0", non-zero -> "0x<hex>"
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I4 => FormatHexAlternate((uint)System.BitConverter.ToInt32(data, 0)),
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U4 => FormatHexAlternate(System.BitConverter.ToUInt32(data, 0)),
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I8 => System.BitConverter.ToInt64(data, 0).ToString(System.Globalization.CultureInfo.InvariantCulture),
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U8 => System.BitConverter.ToUInt64(data, 0).ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => "0"
        };
    }

    private static string FormatHexAlternate(uint v)
    {
        // C++ printf "%#0x": for 0, outputs "0"; for non-zero, outputs "0x<hex>" with no padding.
        if (v == 0) { return "0"; }
        return "0x" + v.ToString("x", System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Writes a projected struct.</summary>
    public static void WriteStruct(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (context.Settings.Component) { return; }

        // Collect field info
        System.Collections.Generic.List<(string TypeStr, string Name, string ParamName, bool IsInterface)> fields = [];
        foreach (FieldDefinition field in type.Fields)
        {
            if (field.IsStatic || field.Signature is null) { continue; }
            TypeSemantics semantics = TypeSemanticsFactory.Get(field.Signature.FieldType);
            IndentedTextWriter scratch = new();
            TypedefNameWriter.WriteProjectionType(scratch, context, semantics);
            string fieldType = scratch.ToString();
            string fieldName = field.Name?.Value ?? string.Empty;
            string paramName = ToCamelCase(fieldName);
            bool isInterface = false;
            if (semantics is TypeSemantics.Definition d)
            {
                isInterface = TypeCategorization.GetCategory(d.Type) == TypeCategory.Interface;
            }
            else if (semantics is TypeSemantics.GenericInstance gi)
            {
                isInterface = TypeCategorization.GetCategory(gi.GenericType) == TypeCategory.Interface;
            }
            fields.Add((fieldType, fieldName, paramName, isInterface));
        }

        string projectionName = type.Name?.Value ?? string.Empty;
        bool hasAddition = AdditionTypes.HasAdditionToType(type.Namespace?.Value ?? string.Empty, projectionName);

        // Header attributes
        MetadataAttributeFactory.WriteWinRTMetadataAttribute(writer, type, context.Cache);
        MetadataAttributeFactory.WriteValueTypeWinRTClassNameAttribute(writer, context, type);
        CustomAttributeFactory.WriteTypeCustomAttributes(writer, context, type, true);
        MetadataAttributeFactory.WriteComWrapperMarshallerAttribute(writer, context, type);
        MetadataAttributeFactory.WriteWinRTReferenceTypeAttribute(writer, context, type);
        writer.Write("public");
        if (hasAddition) { writer.Write(" partial"); }
        writer.Write($" struct {projectionName}: IEquatable<{projectionName}>\n{{\npublic {projectionName}(");
        for (int i = 0; i < fields.Count; i++)
        {
            if (i > 0) { writer.Write(", "); }
            writer.Write($"{fields[i].TypeStr} ");
            IdentifierEscaping.WriteEscapedIdentifier(writer, fields[i].ParamName);
        }
        writer.WriteLine(")");
        writer.WriteLine("{");
        foreach ((string _, string name, string paramName, bool _) in fields)
        {
            // When the param name matches the field name (i.e. ToCamelCase couldn't change casing),
            // qualify with this. to disambiguate.
            if (name == paramName)
            {
                writer.Write($"this.{name} = ");
                IdentifierEscaping.WriteEscapedIdentifier(writer, paramName);
                writer.Write("; ");
            }
            else
            {
                writer.Write($"{name} = ");
                IdentifierEscaping.WriteEscapedIdentifier(writer, paramName);
                writer.Write("; ");
            }
        }
        writer.WriteLine("");
        writer.WriteLine("}");

        // properties
        foreach ((string typeStr, string name, string _, bool _) in fields)
        {
            writer.Write($"public {typeStr} {name}\n{{\nreadonly get; set;\n}}\n");
        }

        // ==
        writer.Write($"public static bool operator ==({projectionName} x, {projectionName} y) => ");
        if (fields.Count == 0)
        {
            writer.Write("true");
        }
        else
        {
            for (int i = 0; i < fields.Count; i++)
            {
                if (i > 0) { writer.Write(" && "); }
                writer.Write($"x.{fields[i].Name} == y.{fields[i].Name}");
            }
        }
        writer.Write("""
            ;
            public static bool operator !=(
            """, isMultiline: true);
        writer.Write(projectionName);
        writer.Write(" x, ");
        writer.Write(projectionName);
        writer.Write("""
             y) => !(x == y);
            public bool Equals(
            """, isMultiline: true);
        writer.Write(projectionName);
        writer.Write("""
             other) => this == other;
            public override bool Equals(object obj) => obj is 
            """, isMultiline: true);
        writer.Write(projectionName);
        writer.Write("""
             that && this == that;
            public override int GetHashCode() => 
            """, isMultiline: true);
        if (fields.Count == 0)
        {
            writer.Write("0");
        }
        else
        {
            for (int i = 0; i < fields.Count; i++)
            {
                if (i > 0) { writer.Write(" ^ "); }
                writer.Write($"{fields[i].Name}.GetHashCode()");
            }
        }
        writer.WriteLine(";");
        writer.WriteLine("}");
        writer.WriteLine("");
    }
    /// <summary>Writes a projected API contract (an empty enum stand-in).</summary>
    public static void WriteContract(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (context.Settings.Component) { return; }

        string typeName = type.Name?.Value ?? string.Empty;
        CustomAttributeFactory.WriteTypeCustomAttributes(writer, context, type, false);
        writer.Write($"{AccessibilityHelper.InternalAccessibility(context.Settings)} enum {typeName}\n{{\n}}\n");
    }
    /// <summary>Writes a projected delegate.</summary>
    public static void WriteDelegate(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (context.Settings.Component) { return; }

        MethodDefinition? invoke = type.GetDelegateInvoke();
        if (invoke is null) { return; }
        MethodSig sig = new(invoke);

        writer.WriteLine("");
        MetadataAttributeFactory.WriteWinRTMetadataAttribute(writer, type, context.Cache);
        CustomAttributeFactory.WriteTypeCustomAttributes(writer, context, type, false);
        MetadataAttributeFactory.WriteComWrapperMarshallerAttribute(writer, context, type);
        if (!context.Settings.ReferenceProjection)
        {
            // GUID attribute
            writer.Write("[Guid(\"");
            IIDExpressionWriter.WriteGuid(writer, type, false);
            writer.WriteLine("\")]");
        }
        writer.Write($"{AccessibilityHelper.InternalAccessibility(context.Settings)} delegate ");
        MethodFactory.WriteProjectionReturnType(writer, context, sig);
        writer.Write(" ");
        TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.Projected, false);
        TypedefNameWriter.WriteTypeParams(writer, type);
        writer.Write("(");
        MethodFactory.WriteParameterList(writer, context, sig);
        writer.WriteLine(");");
    }
    /// <summary>Writes a projected attribute class.</summary>
    public static void WriteAttribute(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string typeName = type.Name?.Value ?? string.Empty;

        MetadataAttributeFactory.WriteWinRTMetadataAttribute(writer, type, context.Cache);
        CustomAttributeFactory.WriteTypeCustomAttributes(writer, context, type, true);
        writer.Write($"{AccessibilityHelper.InternalAccessibility(context.Settings)} sealed class {typeName}: Attribute\n{{\n");

        // Constructors
        foreach (MethodDefinition method in type.Methods)
        {
            if (method.Name?.Value != ".ctor") { continue; }
            MethodSig sig = new(method);
            writer.Write($"public {typeName}(");
            MethodFactory.WriteParameterList(writer, context, sig);
            writer.WriteLine("){}");
        }
        // Fields
        foreach (FieldDefinition field in type.Fields)
        {
            if (field.IsStatic || field.Signature is null) { continue; }
            writer.Write("public ");
            TypedefNameWriter.WriteProjectionType(writer, context, TypeSemanticsFactory.Get(field.Signature.FieldType));
            writer.WriteLine($" {field.Name?.Value ?? string.Empty};");
        }
        writer.WriteLine("}");
    }

    /// <summary>Returns the camel-case form of <paramref name="name"/>.</summary>
    public static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) { return name; }
        char c = name[0];
        if (c is >= 'A' and <= 'Z')
        {
            return char.ToLowerInvariant(c) + name[1..];
        }
        return name;
    }
}