// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using AsmResolver.DotNet;

namespace WindowsRuntime.ProjectionGenerator.Writer;

/// <summary>
/// GUID/IID-related code writers, mirroring functions in <c>code_writers.h</c>.
/// </summary>
internal static partial class CodeWriters
{
    /// <summary>Mirrors C++ <c>get_fundamental_type_guid_signature</c>.</summary>
    public static string GetFundamentalTypeGuidSignature(FundamentalType t) => t switch
    {
        FundamentalType.Boolean => "b1",
        FundamentalType.Char => "c2",
        FundamentalType.Int8 => "i1",
        FundamentalType.UInt8 => "u1",
        FundamentalType.Int16 => "i2",
        FundamentalType.UInt16 => "u2",
        FundamentalType.Int32 => "i4",
        FundamentalType.UInt32 => "u4",
        FundamentalType.Int64 => "i8",
        FundamentalType.UInt64 => "u8",
        FundamentalType.Float => "f4",
        FundamentalType.Double => "f8",
        FundamentalType.String => "string",
        _ => throw new InvalidOperationException("Unknown fundamental type")
    };

    private static readonly Regex s_typeNameEscapeRe = new(@"[ :<>`,.]", RegexOptions.Compiled);

    /// <summary>Mirrors C++ <c>escape_type_name_for_identifier</c>.</summary>
    public static string EscapeTypeNameForIdentifier(string typeName, bool stripGlobal = false, bool stripGlobalABI = false)
    {
        string result = s_typeNameEscapeRe.Replace(typeName, "_");
        if (stripGlobalABI && typeName.StartsWith("global::ABI.", StringComparison.Ordinal))
        {
            result = result.Substring(12); // Remove "global::ABI." (with "::" and "." already replaced)
        }
        else if (stripGlobal && typeName.StartsWith("global::", StringComparison.Ordinal))
        {
            result = result.Substring(8); // Remove "global::"
        }
        return result;
    }

    /// <summary>
    /// Reads the GUID values from the [GuidAttribute] of the type and returns them as a tuple.
    /// </summary>
    public static (uint Data1, ushort Data2, ushort Data3, byte[] Data4)? GetGuidFields(TypeDefinition type)
    {
        CustomAttribute? attr = TypeCategorization.GetAttribute(type, "Windows.Foundation.Metadata", "GuidAttribute");
        if (attr is null || attr.Signature is null) { return null; }
        var args = attr.Signature.FixedArguments;
        if (args.Count < 11) { return null; }

        uint data1 = ToUInt32(args[0].Element);
        ushort data2 = ToUInt16(args[1].Element);
        ushort data3 = ToUInt16(args[2].Element);
        byte[] data4 = new byte[8];
        for (int i = 0; i < 8; i++)
        {
            data4[i] = ToByte(args[3 + i].Element);
        }
        return (data1, data2, data3, data4);

        static uint ToUInt32(object? v) => v switch
        {
            uint u => u,
            int i => (uint)i,
            _ => 0u
        };
        static ushort ToUInt16(object? v) => v switch
        {
            ushort u => u,
            short s => (ushort)s,
            int i => (ushort)i,
            _ => (ushort)0
        };
        static byte ToByte(object? v) => v switch
        {
            byte b => b,
            sbyte sb => (byte)sb,
            int i => (byte)i,
            _ => (byte)0
        };
    }

    /// <summary>Mirrors C++ <c>write_guid</c>.</summary>
    public static void WriteGuid(TextWriter w, TypeDefinition type, bool lowerCase)
    {
        var fields = GetGuidFields(type) ?? throw new InvalidOperationException(
            $"'Windows.Foundation.Metadata.GuidAttribute' attribute for type '{type.Namespace}.{type.Name}' not found");
        string fmt = lowerCase ? "x" : "X";
        // Format: %08x-%04x-%04x-%02x%02x-%02x%02x%02x%02x%02x%02x
        w.Write(fields.Data1.ToString(fmt + "8", CultureInfo.InvariantCulture));
        w.Write("-");
        w.Write(fields.Data2.ToString(fmt + "4", CultureInfo.InvariantCulture));
        w.Write("-");
        w.Write(fields.Data3.ToString(fmt + "4", CultureInfo.InvariantCulture));
        w.Write("-");
        for (int i = 0; i < 2; i++) { w.Write(fields.Data4[i].ToString(fmt + "2", CultureInfo.InvariantCulture)); }
        w.Write("-");
        for (int i = 2; i < 8; i++) { w.Write(fields.Data4[i].ToString(fmt + "2", CultureInfo.InvariantCulture)); }
    }

    /// <summary>Mirrors C++ <c>write_guid_bytes</c>.</summary>
    public static void WriteGuidBytes(TextWriter w, TypeDefinition type)
    {
        var fields = GetGuidFields(type) ?? throw new InvalidOperationException(
            $"'Windows.Foundation.Metadata.GuidAttribute' attribute for type '{type.Namespace}.{type.Name}' not found");
        WriteByte(w, (fields.Data1 >> 0) & 0xFF, true);
        WriteByte(w, (fields.Data1 >> 8) & 0xFF, false);
        WriteByte(w, (fields.Data1 >> 16) & 0xFF, false);
        WriteByte(w, (fields.Data1 >> 24) & 0xFF, false);
        WriteByte(w, (uint)((fields.Data2 >> 0) & 0xFF), false);
        WriteByte(w, (uint)((fields.Data2 >> 8) & 0xFF), false);
        WriteByte(w, (uint)((fields.Data3 >> 0) & 0xFF), false);
        WriteByte(w, (uint)((fields.Data3 >> 8) & 0xFF), false);
        for (int i = 0; i < 8; i++) { WriteByte(w, fields.Data4[i], false); }
    }

    private static void WriteByte(TextWriter w, uint b, bool first)
    {
        if (!first) { w.Write(", "); }
        w.Write("0x");
        w.Write((b & 0xFF).ToString("X", CultureInfo.InvariantCulture));
    }

    /// <summary>Mirrors C++ <c>write_iid_guid_property_name</c>.</summary>
    public static void WriteIidGuidPropertyName(TypeWriter w, TypeDefinition type)
    {
        string name = w.WriteTemp("%", new Action<TextWriter>(_ =>
        {
            WriteTypedefName(w, type, TypedefNameType.ABI, true);
            WriteTypeParams(w, type);
        }));
        name = EscapeTypeNameForIdentifier(name, true, true);
        w.Write("IID_");
        w.Write(name);
    }

    /// <summary>Mirrors C++ <c>write_iid_reference_guid_property_name</c>.</summary>
    public static void WriteIidReferenceGuidPropertyName(TypeWriter w, TypeDefinition type)
    {
        string name = w.WriteTemp("%", new Action<TextWriter>(_ =>
        {
            WriteTypedefName(w, type, TypedefNameType.ABI, true);
            WriteTypeParams(w, type);
        }));
        name = EscapeTypeNameForIdentifier(name, true, true);
        w.Write("IID_");
        w.Write(name);
        w.Write("Reference");
    }

    /// <summary>Mirrors C++ <c>write_iid_guid_property_from_type</c>.</summary>
    public static void WriteIidGuidPropertyFromType(TypeWriter w, TypeDefinition type)
    {
        w.Write("public static ref readonly Guid ");
        WriteIidGuidPropertyName(w, type);
        w.Write("\n{\n    [MethodImpl(MethodImplOptions.AggressiveInlining)]\n    get\n    {\n        ReadOnlySpan<byte> data =\n        [\n            ");
        WriteGuidBytes(w, type);
        w.Write("\n        ];\n        return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));\n    }\n}\n\n");
    }

    /// <summary>
    /// Mirrors C++ <c>write_guid_signature</c>.
    /// </summary>
    public static void WriteGuidSignature(TypeWriter w, TypeSemantics semantics)
    {
        switch (semantics)
        {
            case TypeSemantics.Guid_:
                w.Write("g16");
                break;
            case TypeSemantics.Object_:
                w.Write("cinterface(IInspectable)");
                break;
            case TypeSemantics.Fundamental f:
                w.Write(GetFundamentalTypeGuidSignature(f.Type));
                break;
            case TypeSemantics.Definition d:
                WriteGuidSignatureForType(w, d.Type);
                break;
            case TypeSemantics.GenericInstance gi:
                w.Write("pinterface({");
                WriteGuid(w, gi.GenericType, true);
                w.Write("};");
                for (int i = 0; i < gi.GenericArgs.Count; i++)
                {
                    if (i > 0) { w.Write(";"); }
                    WriteGuidSignature(w, gi.GenericArgs[i]);
                }
                w.Write(")");
                break;
        }
    }

    private static void WriteGuidSignatureForType(TypeWriter w, TypeDefinition type)
    {
        TypeCategory cat = TypeCategorization.GetCategory(type);
        switch (cat)
        {
            case TypeCategory.Enum:
                w.Write("enum(");
                WriteTypedefName(w, type, TypedefNameType.NonProjected, true);
                WriteTypeParams(w, type);
                w.Write(";");
                w.Write(TypeCategorization.IsFlagsEnum(type) ? "u4" : "i4");
                w.Write(")");
                break;
            case TypeCategory.Struct:
                w.Write("struct(");
                WriteTypedefName(w, type, TypedefNameType.NonProjected, true);
                WriteTypeParams(w, type);
                w.Write(";");
                bool first = true;
                foreach (FieldDefinition field in type.Fields)
                {
                    if (field.IsStatic) { continue; }
                    if (field.Signature is null) { continue; }
                    if (!first) { w.Write(";"); }
                    first = false;
                    WriteGuidSignature(w, TypeSemanticsFactory.Get(field.Signature.FieldType));
                }
                w.Write(")");
                break;
            case TypeCategory.Delegate:
                w.Write("delegate({");
                WriteGuid(w, type, true);
                w.Write("})");
                break;
            case TypeCategory.Interface:
                w.Write("{");
                WriteGuid(w, type, true);
                w.Write("}");
                break;
            case TypeCategory.Class:
                ITypeDefOrRef? defaultIface = Helpers.GetDefaultInterface(type);
                if (defaultIface is TypeDefinition di)
                {
                    w.Write("rc(");
                    WriteTypedefName(w, type, TypedefNameType.NonProjected, true);
                    WriteTypeParams(w, type);
                    w.Write(";");
                    WriteGuidSignature(w, new TypeSemantics.Definition(di));
                    w.Write(")");
                }
                else
                {
                    w.Write("{");
                    WriteGuid(w, type, true);
                    w.Write("}");
                }
                break;
        }
    }

    /// <summary>Mirrors C++ <c>write_iid_guid_property_from_signature</c>.</summary>
    public static void WriteIidGuidPropertyFromSignature(TypeWriter w, TypeDefinition type)
    {
        string guidSig = w.WriteTemp("%", new Action<TextWriter>(_ =>
        {
            WriteGuidSignature(w, new TypeSemantics.Definition(type));
        }));
        string ireferenceGuidSig = "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};" + guidSig + ")";
        Guid guidValue = GuidGenerator.Generate(ireferenceGuidSig);
        byte[] bytes = guidValue.ToByteArray();

        w.Write("public static ref readonly Guid ");
        WriteIidReferenceGuidPropertyName(w, type);
        w.Write("\n{\n    [MethodImpl(MethodImplOptions.AggressiveInlining)]\n    get\n    {\n        ReadOnlySpan<byte> data =\n        [\n            ");
        for (int i = 0; i < 16; i++)
        {
            if (i > 0) { w.Write(", "); }
            w.Write("0x");
            w.Write(bytes[i].ToString("X", CultureInfo.InvariantCulture));
        }
        w.Write("\n        ];\n        return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));\n    }\n}\n\n");
    }

    /// <summary>Mirrors C++ <c>write_iid_guid_property_for_class_interfaces</c>.</summary>
    public static void WriteIidGuidPropertyForClassInterfaces(TypeWriter w, TypeDefinition type, System.Collections.Generic.HashSet<TypeDefinition> interfacesEmitted)
    {
        foreach (InterfaceImplementation impl in type.Interfaces)
        {
            if (impl.Interface is null) { continue; }
            TypeDefinition? ifaceType = impl.Interface as TypeDefinition;
            if (ifaceType is null) { continue; }

            string ns = ifaceType.Namespace?.Value ?? string.Empty;
            string nm = ifaceType.Name?.Value ?? string.Empty;
            // Skip mapped types
            if (MappedTypes.Get(ns, nm) is not null) { continue; }
            // Skip generic interfaces
            if (ifaceType.GenericParameters.Count != 0) { continue; }
            // Skip already-emitted
            if (interfacesEmitted.Contains(ifaceType)) { continue; }
            // Only emit if the interface is not in the projection (otherwise it'll be emitted naturally)
            if (!w.Settings.Filter.Includes(ifaceType))
            {
                WriteIidGuidPropertyFromType(w, ifaceType);
                _ = interfacesEmitted.Add(ifaceType);
            }
        }
    }

    /// <summary>Writes the InterfaceIIDs file header (mirrors C++ <c>write_begin_interface_iids</c> in type_writers.h).</summary>
    public static void WriteInterfaceIidsBegin(TextWriter w)
    {
        w.Write(@"
//------------------------------------------------------------------------------
// <auto-generated>
//     This file was generated by cswinrt.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ABI;

internal static class InterfaceIIDs
{
");
    }

    /// <summary>Writes the InterfaceIIDs file footer.</summary>
    public static void WriteInterfaceIidsEnd(TextWriter w)
    {
        w.Write("}\n\n");
    }
}
