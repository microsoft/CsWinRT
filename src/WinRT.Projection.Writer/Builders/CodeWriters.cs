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
internal static partial class CodeWriters
{
    /// <summary>Dispatches type emission based on the type category.</summary>
    public static void WriteType(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type, TypeCategory category)
    {
        // The Class family is being migrated incrementally. For not-yet-migrated entries, wrap
        // the writer+context in a transient TypeWriter so the underlying buffer + state are shared.
        TypeWriter w = new(writer, context);
        switch (category)
        {
            case TypeCategory.Class:
                if (TypeCategorization.IsAttributeType(type))
                {
                    WriteAttribute(writer, context, type);
                }
                else
                {
                    WriteClass(w, type);
                }
                break;
            case TypeCategory.Delegate:
                WriteDelegate(writer, context, type);
                break;
            case TypeCategory.Enum:
                WriteEnum(writer, context, type);
                break;
            case TypeCategory.Interface:
                WriteInterface(writer, context, type);
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

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static void WriteType(TypeWriter w, TypeDefinition type, TypeCategory category, Settings settings, MetadataCache cache)
        => WriteType(w.Writer, w.Context, type, category);

    /// <summary>Dispatches ABI emission based on the type category.</summary>
    public static void WriteAbiType(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type, TypeCategory category)
    {
        // The Abi family is being migrated incrementally (it's the largest single file). For
        // not-yet-migrated entries, wrap the writer+context in a transient TypeWriter so the
        // underlying buffer + state are shared.
        TypeWriter w = new(writer, context);
        switch (category)
        {
            case TypeCategory.Class:
                WriteAbiClass(w, type);
                break;
            case TypeCategory.Delegate:
                WriteAbiDelegate(w, type);
                WriteTempDelegateEventSourceSubclass(w, type);
                break;
            case TypeCategory.Enum:
                WriteAbiEnum(w, type);
                break;
            case TypeCategory.Interface:
                WriteAbiInterface(w, type);
                break;
            case TypeCategory.Struct:
                WriteAbiStruct(w, type);
                break;
        }
    }

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static void WriteAbiType(TypeWriter w, TypeDefinition type, TypeCategory category, Settings settings)
        => WriteAbiType(w.Writer, w.Context, type, category);

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
            writer.Write("\n[FlagsAttribute]\n");
        }
        else
        {
            writer.Write("\n");
        }
        WriteWinRTMetadataAttribute(writer, type, _cacheRef!);
        WriteValueTypeWinRTClassNameAttribute(writer, context, type);
        WriteTypeCustomAttributes(writer, context, type, true);
        WriteComWrapperMarshallerAttribute(writer, context, type);
        WriteWinRTReferenceTypeAttribute(writer, context, type);

        writer.Write(accessibility);
        writer.Write(" enum ");
        writer.Write(typeName);
        writer.Write(" : ");
        writer.Write(enumUnderlyingType);
        writer.Write("\n{\n");

        foreach (FieldDefinition field in type.Fields)
        {
            if (field.Constant is null)
            {
                continue;
            }
            string fieldName = field.Name?.Value ?? string.Empty;
            string constantValue = FormatConstant(field.Constant);
            // Emits per-enum-field [SupportedOSPlatform] when the field has a [ContractVersion].
            WritePlatformAttribute(writer, context, field);
            writer.Write(fieldName);
            writer.Write(" = unchecked((");
            writer.Write(enumUnderlyingType);
            writer.Write(")");
            writer.Write(constantValue);
            writer.Write("),\n");
        }
        writer.Write("}\n\n");
    }

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static void WriteEnum(TypeWriter w, TypeDefinition type)
        => WriteEnum(w.Writer, w.Context, type);

    /// <summary>Formats a metadata Constant value as a C# literal.</summary>
    private static string FormatConstant(AsmResolver.DotNet.Constant constant)
    {
        // The Constant.Value contains raw bytes representing the value
        AsmResolver.PE.DotNet.Metadata.Tables.ElementType type = constant.Type;
        byte[] data = constant.Value?.Data ?? System.Array.Empty<byte>();
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
        System.Collections.Generic.List<(string TypeStr, string Name, string ParamName, bool IsInterface)> fields = new();
        foreach (FieldDefinition field in type.Fields)
        {
            if (field.IsStatic || field.Signature is null) { continue; }
            TypeSemantics semantics = TypeSemanticsFactory.Get(field.Signature.FieldType);
            IndentedTextWriter scratch = new();
            WriteProjectionType(scratch, context, semantics);
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
        WriteWinRTMetadataAttribute(writer, type, _cacheRef!);
        WriteValueTypeWinRTClassNameAttribute(writer, context, type);
        WriteTypeCustomAttributes(writer, context, type, true);
        WriteComWrapperMarshallerAttribute(writer, context, type);
        WriteWinRTReferenceTypeAttribute(writer, context, type);
        writer.Write("public");
        if (hasAddition) { writer.Write(" partial"); }
        writer.Write(" struct ");
        writer.Write(projectionName);
        writer.Write(": IEquatable<");
        writer.Write(projectionName);
        writer.Write(">\n{\n");

        // ctor
        writer.Write("public ");
        writer.Write(projectionName);
        writer.Write("(");
        for (int i = 0; i < fields.Count; i++)
        {
            if (i > 0) { writer.Write(", "); }
            writer.Write(fields[i].TypeStr);
            writer.Write(" ");
            IdentifierEscaping.WriteEscapedIdentifier(writer, fields[i].ParamName);
        }
        writer.Write(")\n{\n");
        foreach (var f in fields)
        {
            // When the param name matches the field name (i.e. ToCamelCase couldn't change casing),
            // qualify with this. to disambiguate.
            if (f.Name == f.ParamName)
            {
                writer.Write("this.");
                writer.Write(f.Name);
                writer.Write(" = ");
                IdentifierEscaping.WriteEscapedIdentifier(writer, f.ParamName);
                writer.Write("; ");
            }
            else
            {
                writer.Write(f.Name);
                writer.Write(" = ");
                IdentifierEscaping.WriteEscapedIdentifier(writer, f.ParamName);
                writer.Write("; ");
            }
        }
        writer.Write("\n}\n");

        // properties
        foreach (var f in fields)
        {
            writer.Write("public ");
            writer.Write(f.TypeStr);
            writer.Write(" ");
            writer.Write(f.Name);
            writer.Write("\n{\nreadonly get; set;\n}\n");
        }

        // ==
        writer.Write("public static bool operator ==(");
        writer.Write(projectionName);
        writer.Write(" x, ");
        writer.Write(projectionName);
        writer.Write(" y) => ");
        if (fields.Count == 0)
        {
            writer.Write("true");
        }
        else
        {
            for (int i = 0; i < fields.Count; i++)
            {
                if (i > 0) { writer.Write(" && "); }
                writer.Write("x.");
                writer.Write(fields[i].Name);
                writer.Write(" == y.");
                writer.Write(fields[i].Name);
            }
        }
        writer.Write(";\n");

        // !=
        writer.Write("public static bool operator !=(");
        writer.Write(projectionName);
        writer.Write(" x, ");
        writer.Write(projectionName);
        writer.Write(" y) => !(x == y);\n");

        // equals
        writer.Write("public bool Equals(");
        writer.Write(projectionName);
        writer.Write(" other) => this == other;\n");

        writer.Write("public override bool Equals(object obj) => obj is ");
        writer.Write(projectionName);
        writer.Write(" that && this == that;\n");

        // hashcode
        writer.Write("public override int GetHashCode() => ");
        if (fields.Count == 0)
        {
            writer.Write("0");
        }
        else
        {
            for (int i = 0; i < fields.Count; i++)
            {
                if (i > 0) { writer.Write(" ^ "); }
                writer.Write(fields[i].Name);
                writer.Write(".GetHashCode()");
            }
        }
        writer.Write(";\n");
        writer.Write("}\n\n");
    }

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static void WriteStruct(TypeWriter w, TypeDefinition type)
        => WriteStruct(w.Writer, w.Context, type);

    /// <summary>Writes a projected API contract (an empty enum stand-in).</summary>
    public static void WriteContract(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (context.Settings.Component) { return; }

        string typeName = type.Name?.Value ?? string.Empty;
        WriteTypeCustomAttributes(writer, context, type, false);
        writer.Write(AccessibilityHelper.InternalAccessibility(context.Settings));
        writer.Write(" enum ");
        writer.Write(typeName);
        writer.Write("\n{\n}\n");
    }

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static void WriteContract(TypeWriter w, TypeDefinition type)
        => WriteContract(w.Writer, w.Context, type);

    /// <summary>Writes a projected delegate.</summary>
    public static void WriteDelegate(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (context.Settings.Component) { return; }

        MethodDefinition? invoke = type.GetDelegateInvoke();
        if (invoke is null) { return; }
        MethodSig sig = new(invoke);

        writer.Write("\n");
        WriteWinRTMetadataAttribute(writer, type, _cacheRef!);
        WriteTypeCustomAttributes(writer, context, type, false);
        WriteComWrapperMarshallerAttribute(writer, context, type);
        if (!context.Settings.ReferenceProjection)
        {
            // GUID attribute
            writer.Write("[Guid(\"");
            WriteGuid(writer, type, false);
            writer.Write("\")]\n");
        }
        writer.Write(AccessibilityHelper.InternalAccessibility(context.Settings));
        writer.Write(" delegate ");
        WriteProjectionReturnType(writer, context, sig);
        writer.Write(" ");
        WriteTypedefName(writer, context, type, TypedefNameType.Projected, false);
        WriteTypeParams(writer, type);
        writer.Write("(");
        WriteParameterList(writer, context, sig);
        writer.Write(");\n");
    }

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static void WriteDelegate(TypeWriter w, TypeDefinition type)
        => WriteDelegate(w.Writer, w.Context, type);

    /// <summary>Writes a projected attribute class.</summary>
    public static void WriteAttribute(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string typeName = type.Name?.Value ?? string.Empty;

        WriteWinRTMetadataAttribute(writer, type, _cacheRef!);
        WriteTypeCustomAttributes(writer, context, type, true);
        writer.Write(AccessibilityHelper.InternalAccessibility(context.Settings));
        writer.Write(" sealed class ");
        writer.Write(typeName);
        writer.Write(": Attribute\n{\n");

        // Constructors
        foreach (MethodDefinition method in type.Methods)
        {
            if (method.Name?.Value != ".ctor") { continue; }
            MethodSig sig = new(method);
            writer.Write("public ");
            writer.Write(typeName);
            writer.Write("(");
            WriteParameterList(writer, context, sig);
            writer.Write("){}\n");
        }
        // Fields
        foreach (FieldDefinition field in type.Fields)
        {
            if (field.IsStatic || field.Signature is null) { continue; }
            writer.Write("public ");
            WriteProjectionType(writer, context, TypeSemanticsFactory.Get(field.Signature.FieldType));
            writer.Write(" ");
            writer.Write(field.Name?.Value ?? string.Empty);
            writer.Write(";\n");
        }
        writer.Write("}\n");
    }

    /// <summary>Legacy <see cref="TypeWriter"/> overload that delegates to the primary one.</summary>
    public static void WriteAttribute(TypeWriter w, TypeDefinition type)
        => WriteAttribute(w.Writer, w.Context, type);

    private static MetadataCache? _cacheRef;

    /// <summary>Sets the cache reference used by writers that need source-file paths.</summary>
    public static void SetMetadataCache(MetadataCache cache)
    {
        _cacheRef = cache;
    }

    /// <summary>Gets the metadata cache previously set via <see cref="SetMetadataCache"/>.</summary>
    internal static MetadataCache? GetMetadataCache() => _cacheRef;

    /// <summary>Returns the camel-case form of <paramref name="name"/>.</summary>
    public static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name)) { return name; }
        char c = name[0];
        if (c >= 'A' && c <= 'Z')
        {
            return char.ToLowerInvariant(c) + name.Substring(1);
        }
        return name;
    }
}
