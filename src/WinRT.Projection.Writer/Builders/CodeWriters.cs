// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Models;
using WindowsRuntime.ProjectionWriter.Extensions;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// Mirrors the C++ <c>code_writers.h</c>. Emits projected and ABI types.
/// <para>
/// **STATUS**: This is a partial 1:1 port. The C++ <c>code_writers.h</c> file is ~11K lines containing
/// 295 functions. This C# port is a work-in-progress. The current implementation produces minimal
/// stub output for each type category to validate the architecture end-to-end.
/// </para>
/// </summary>
internal static partial class CodeWriters
{
    /// <summary>
    /// Dispatches type emission based on the type category.
    /// </summary>
    public static void WriteType(TypeWriter w, TypeDefinition type, TypeCategory category, Settings settings, MetadataCache cache)
    {
        switch (category)
        {
            case TypeCategory.Class:
                if (TypeCategorization.IsAttributeType(type))
                {
                    WriteAttribute(w, type);
                }
                else
                {
                    WriteClass(w, type);
                }
                break;
            case TypeCategory.Delegate:
                WriteDelegate(w, type);
                break;
            case TypeCategory.Enum:
                WriteEnum(w, type);
                break;
            case TypeCategory.Interface:
                WriteInterface(w, type);
                break;
            case TypeCategory.Struct:
                if (TypeCategorization.IsApiContractType(type))
                {
                    WriteContract(w, type);
                }
                else
                {
                    WriteStruct(w, type);
                }
                break;
        }
    }

    /// <summary>
    /// Dispatches ABI emission based on the type category.
    /// </summary>
    public static void WriteAbiType(TypeWriter w, TypeDefinition type, TypeCategory category, Settings settings)
    {
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

    // ABI emission methods are implemented in CodeWriters.Abi.cs

    /// <summary>
    /// Mirrors C++ <c>write_enum</c>. Emits an enum projection.
    /// </summary>
    public static void WriteEnum(TypeWriter w, TypeDefinition type)
    {
        if (w.Settings.Component)
        {
            return;
        }

        bool isFlags = TypeCategorization.IsFlagsEnum(type);
        string enumUnderlyingType = isFlags ? "uint" : "int";
        string accessibility = w.Settings.Internal ? "internal" : "public";
        string typeName = type.Name?.Value ?? string.Empty;

        if (isFlags)
        {
            w.Write("\n[FlagsAttribute]\n");
        }
        else
        {
            w.Write("\n");
        }
        WriteWinRTMetadataAttribute(w, type, _cacheRef!);
        WriteValueTypeWinRTClassNameAttribute(w, type);
        WriteTypeCustomAttributes(w, type, true);
        WriteComWrapperMarshallerAttribute(w, type);
        WriteWinRTReferenceTypeAttribute(w, type);

        w.Write(accessibility);
        w.Write(" enum ");
        w.Write(typeName);
        w.Write(" : ");
        w.Write(enumUnderlyingType);
        w.Write("\n{\n");

        foreach (FieldDefinition field in type.Fields)
        {
            if (field.Constant is null)
            {
                continue;
            }
            string fieldName = field.Name?.Value ?? string.Empty;
            string constantValue = FormatConstant(field.Constant);

            // Mirror C++ code_writers.h:10106 write_platform_attribute(field.CustomAttribute()):
            // emits per-enum-field [SupportedOSPlatform] when the field has a [ContractVersion].
            WritePlatformAttribute(w, field);
            w.Write(fieldName);
            w.Write(" = unchecked((");
            w.Write(enumUnderlyingType);
            w.Write(")");
            w.Write(constantValue);
            w.Write("),\n");
        }
        w.Write("}\n\n");
    }

    /// <summary>
    /// Formats a metadata Constant value as a C# literal.
    /// Mirrors C++ <c>write_constant</c>: I4/U4 are formatted as hex (e.g. <c>0x1</c>) to match
    /// the truth output. Other types fall back to decimal.
    /// </summary>
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
            // I4/U4 use printf "%#0x" semantics: 0 -> "0", non-zero -> "0x<hex>" (alternate hex form omits prefix when value is zero).
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

    /// <summary>
    /// Mirrors C++ <c>write_struct</c>. Emits a struct projection.
    /// </summary>
    public static void WriteStruct(TypeWriter w, TypeDefinition type)
    {
        if (w.Settings.Component) { return; }

        // Collect field info
        System.Collections.Generic.List<(string TypeStr, string Name, string ParamName, bool IsInterface)> fields = new();
        foreach (FieldDefinition field in type.Fields)
        {
            if (field.IsStatic || field.Signature is null) { continue; }
            TypeSemantics semantics = TypeSemanticsFactory.Get(field.Signature.FieldType);
            string fieldType = w.WriteTemp("%", new System.Action<TextWriter>(_ => WriteProjectionType(w, semantics)));
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
        WriteWinRTMetadataAttribute(w, type, _cacheRef!);
        WriteValueTypeWinRTClassNameAttribute(w, type);
        WriteTypeCustomAttributes(w, type, true);
        WriteComWrapperMarshallerAttribute(w, type);
        WriteWinRTReferenceTypeAttribute(w, type);
        w.Write("public");
        if (hasAddition) { w.Write(" partial"); }
        w.Write(" struct ");
        w.Write(projectionName);
        w.Write(": IEquatable<");
        w.Write(projectionName);
        w.Write(">\n{\n");

        // ctor
        w.Write("public ");
        w.Write(projectionName);
        w.Write("(");
        for (int i = 0; i < fields.Count; i++)
        {
            if (i > 0) { w.Write(", "); }
            w.Write(fields[i].TypeStr);
            w.Write(" ");
            Helpers.WriteEscapedIdentifier(w, fields[i].ParamName);
        }
        w.Write(")\n{\n");
        foreach (var f in fields)
        {
            // When the param name matches the field name (i.e. ToCamelCase couldn't change casing),
            // qualify with this. to disambiguate.
            if (f.Name == f.ParamName)
            {
                w.Write("this.");
                w.Write(f.Name);
                w.Write(" = ");
                Helpers.WriteEscapedIdentifier(w, f.ParamName);
                w.Write("; ");
            }
            else
            {
                w.Write(f.Name);
                w.Write(" = ");
                Helpers.WriteEscapedIdentifier(w, f.ParamName);
                w.Write("; ");
            }
        }
        w.Write("\n}\n");

        // properties
        foreach (var f in fields)
        {
            w.Write("public ");
            w.Write(f.TypeStr);
            w.Write(" ");
            w.Write(f.Name);
            w.Write("\n{\nreadonly get; set;\n}\n");
        }

        // ==
        w.Write("public static bool operator ==(");
        w.Write(projectionName);
        w.Write(" x, ");
        w.Write(projectionName);
        w.Write(" y) => ");
        if (fields.Count == 0)
        {
            w.Write("true");
        }
        else
        {
            for (int i = 0; i < fields.Count; i++)
            {
                if (i > 0) { w.Write(" && "); }
                w.Write("x.");
                w.Write(fields[i].Name);
                w.Write(" == y.");
                w.Write(fields[i].Name);
            }
        }
        w.Write(";\n");

        // !=
        w.Write("public static bool operator !=(");
        w.Write(projectionName);
        w.Write(" x, ");
        w.Write(projectionName);
        w.Write(" y) => !(x == y);\n");

        // equals
        w.Write("public bool Equals(");
        w.Write(projectionName);
        w.Write(" other) => this == other;\n");

        w.Write("public override bool Equals(object obj) => obj is ");
        w.Write(projectionName);
        w.Write(" that && this == that;\n");

        // hashcode
        w.Write("public override int GetHashCode() => ");
        if (fields.Count == 0)
        {
            w.Write("0");
        }
        else
        {
            for (int i = 0; i < fields.Count; i++)
            {
                if (i > 0) { w.Write(" ^ "); }
                w.Write(fields[i].Name);
                w.Write(".GetHashCode()");
            }
        }
        w.Write(";\n");
        w.Write("}\n\n");
    }

    /// <summary>
    /// Mirrors C++ <c>write_contract</c>. Emits a static class for an API contract.
    /// </summary>
    public static void WriteContract(TypeWriter w, TypeDefinition type)
    {
        if (w.Settings.Component) { return; }

        string typeName = type.Name?.Value ?? string.Empty;
        WriteTypeCustomAttributes(w, type, false);
        w.Write(Helpers.InternalAccessibility(w.Settings));
        w.Write(" enum ");
        w.Write(typeName);
        w.Write("\n{\n}\n");
    }

    /// <summary>
    /// Mirrors C++ <c>write_delegate</c>. Emits a delegate projection.
    /// </summary>
    public static void WriteDelegate(TypeWriter w, TypeDefinition type)
    {
        if (w.Settings.Component) { return; }

        MethodDefinition? invoke = type.GetDelegateInvoke();
        if (invoke is null) { return; }
        MethodSig sig = new(invoke);

        w.Write("\n");
        WriteWinRTMetadataAttribute(w, type, _cacheRef!);
        WriteTypeCustomAttributes(w, type, false);
        WriteComWrapperMarshallerAttribute(w, type);
        if (!w.Settings.ReferenceProjection)
        {
            // GUID attribute
            w.Write("[Guid(\"");
            WriteGuid(w, type, false);
            w.Write("\")]\n");
        }
        w.Write(Helpers.InternalAccessibility(w.Settings));
        w.Write(" delegate ");
        WriteProjectionReturnType(w, sig);
        w.Write(" ");
        WriteTypedefName(w, type, TypedefNameType.Projected, false);
        WriteTypeParams(w, type);
        w.Write("(");
        WriteParameterList(w, sig);
        w.Write(");\n");
    }

    /// <summary>
    /// Mirrors C++ <c>write_attribute</c>. Emits an attribute projection.
    /// </summary>
    public static void WriteAttribute(TypeWriter w, TypeDefinition type)
    {
        string typeName = type.Name?.Value ?? string.Empty;

        WriteWinRTMetadataAttribute(w, type, _cacheRef!);
        WriteTypeCustomAttributes(w, type, true);
        w.Write(Helpers.InternalAccessibility(w.Settings));
        w.Write(" sealed class ");
        w.Write(typeName);
        w.Write(": Attribute\n{\n");

        // Constructors
        foreach (MethodDefinition method in type.Methods)
        {
            if (method.Name?.Value != ".ctor") { continue; }
            MethodSig sig = new(method);
            w.Write("public ");
            w.Write(typeName);
            w.Write("(");
            WriteParameterList(w, sig);
            w.Write("){}\n");
        }
        // Fields
        foreach (FieldDefinition field in type.Fields)
        {
            if (field.IsStatic || field.Signature is null) { continue; }
            w.Write("public ");
            WriteProjectionType(w, TypeSemanticsFactory.Get(field.Signature.FieldType));
            w.Write(" ");
            w.Write(field.Name?.Value ?? string.Empty);
            w.Write(";\n");
        }
        w.Write("}\n");
    }

    private static MetadataCache? _cacheRef;

    /// <summary>Sets the cache reference used by writers that need source-file paths.</summary>
    public static void SetMetadataCache(MetadataCache cache)
    {
        _cacheRef = cache;
    }

    /// <summary>Gets the metadata cache previously set via <see cref="SetMetadataCache"/>.</summary>
    internal static MetadataCache? GetMetadataCache() => _cacheRef;

    /// <summary>Mirrors C++ <c>to_camel_case</c>.</summary>
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
