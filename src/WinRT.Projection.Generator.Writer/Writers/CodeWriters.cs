// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionGenerator.Writer;

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

    /// <summary>Emits an ABI class wrapper. Stub.</summary>
    public static void WriteAbiClass(TypeWriter w, TypeDefinition type)
    {
        // Stub: full implementation will be in a later commit.
    }

    /// <summary>Emits an ABI delegate. Stub.</summary>
    public static void WriteAbiDelegate(TypeWriter w, TypeDefinition type)
    {
        // Stub
    }

    /// <summary>Emits a temporary delegate event source subclass. Stub.</summary>
    public static void WriteTempDelegateEventSourceSubclass(TypeWriter w, TypeDefinition type)
    {
        // Stub
    }

    /// <summary>Emits an ABI enum (stub).</summary>
    public static void WriteAbiEnum(TypeWriter w, TypeDefinition type)
    {
        // Stub: full implementation will be in a later commit.
    }

    /// <summary>Emits an ABI interface (stub).</summary>
    public static void WriteAbiInterface(TypeWriter w, TypeDefinition type)
    {
        // Stub
    }

    /// <summary>Emits an ABI struct (stub).</summary>
    public static void WriteAbiStruct(TypeWriter w, TypeDefinition type)
    {
        // Stub
    }

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
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I4 => System.BitConverter.ToInt32(data, 0).ToString(System.Globalization.CultureInfo.InvariantCulture),
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U4 => System.BitConverter.ToUInt32(data, 0).ToString(System.Globalization.CultureInfo.InvariantCulture),
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.I8 => System.BitConverter.ToInt64(data, 0).ToString(System.Globalization.CultureInfo.InvariantCulture),
            AsmResolver.PE.DotNet.Metadata.Tables.ElementType.U8 => System.BitConverter.ToUInt64(data, 0).ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => "0"
        };
    }

    /// <summary>
    /// Mirrors C++ <c>write_struct</c>. Emits a struct projection.
    /// </summary>
    public static void WriteStruct(TypeWriter w, TypeDefinition type)
    {
        // Simple stub
        string name = type.Name?.Value ?? string.Empty;
        w.Write("public partial struct ");
        w.Write(name);
        w.Write(" { /* TODO: struct fields */ }\n\n");
    }

    /// <summary>
    /// Mirrors C++ <c>write_contract</c>. Emits a static class for an API contract.
    /// </summary>
    public static void WriteContract(TypeWriter w, TypeDefinition type)
    {
        string name = type.Name?.Value ?? string.Empty;
        w.Write("public static class ");
        w.Write(name);
        w.Write(" { /* TODO: contract version */ }\n\n");
    }

    /// <summary>
    /// Mirrors C++ <c>write_delegate</c>. Emits a delegate projection.
    /// </summary>
    public static void WriteDelegate(TypeWriter w, TypeDefinition type)
    {
        string name = type.Name?.Value ?? string.Empty;
        w.Write("public delegate void ");
        w.Write(name);
        w.Write("(); // TODO: delegate signature\n\n");
    }

    /// <summary>
    /// Mirrors C++ <c>write_interface</c>. Emits an interface projection.
    /// </summary>
    public static void WriteInterface(TypeWriter w, TypeDefinition type)
    {
        string name = type.Name?.Value ?? string.Empty;
        w.Write("public partial interface ");
        w.Write(name);
        w.Write(" { /* TODO: interface members */ }\n\n");
    }

    /// <summary>
    /// Mirrors C++ <c>write_class</c>. Emits a class projection.
    /// </summary>
    public static void WriteClass(TypeWriter w, TypeDefinition type)
    {
        string name = type.Name?.Value ?? string.Empty;
        w.Write("public partial class ");
        w.Write(name);
        w.Write(" { /* TODO: class members */ }\n\n");
    }

    /// <summary>
    /// Mirrors C++ <c>write_attribute</c>. Emits an attribute projection.
    /// </summary>
    public static void WriteAttribute(TypeWriter w, TypeDefinition type)
    {
        string name = type.Name?.Value ?? string.Empty;
        w.Write("public sealed class ");
        w.Write(name);
        w.Write(" : System.Attribute { /* TODO: attribute members */ }\n\n");
    }
}
