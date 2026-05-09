// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Text.RegularExpressions;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Extensions;
using WindowsRuntime.ProjectionWriter.Writers;

namespace WindowsRuntime.ProjectionWriter;

/// <summary>
/// GUID/IID-related code writers.
/// </summary>
internal static class IIDExpressionWriter
{
    /// <summary>Returns the GUID-signature character code for a fundamental WinRT type.</summary>
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

    /// <summary>Escapes a type name into a C# identifier-safe form.</summary>
    public static string EscapeTypeNameForIdentifier(string typeName, bool stripGlobal = false, bool stripGlobalABI = false)
    {
        // Escape special chars first, then strip ONLY the prefix (not all occurrences).
        string result = s_typeNameEscapeRe.Replace(typeName, "_");
        if (stripGlobalABI && typeName.StartsWith("global::ABI.", StringComparison.Ordinal))
        {
            result = result[12..]; // Remove "global::ABI." (with ":" and "." already replaced)
        }
        else if (stripGlobal && typeName.StartsWith("global::", StringComparison.Ordinal))
        {
            result = result[8..]; // Remove "global::"
        }
        return result;
    }

    /// <summary>
    /// Reads the GUID values from the [GuidAttribute] of the type and returns them as a tuple.
    /// </summary>
    public static (uint Data1, ushort Data2, ushort Data3, byte[] Data4)? GetGuidFields(TypeDefinition type)
    {
        CustomAttribute? attr = type.GetAttribute("Windows.Foundation.Metadata", "GuidAttribute");
        if (attr is null || attr.Signature is null) { return null; }
        System.Collections.Generic.IList<AsmResolver.DotNet.Signatures.CustomAttributeArgument> args = attr.Signature.FixedArguments;
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
            _ => 0
        };
        static byte ToByte(object? v) => v switch
        {
            byte b => b,
            sbyte sb => (byte)sb,
            int i => (byte)i,
            _ => 0
        };
    }

    /// <summary>Writes the GUID for <paramref name="type"/> in canonical hyphenated string form.</summary>
    public static void WriteGuid(IndentedTextWriter writer, TypeDefinition type, bool lowerCase)
    {
        (uint data1, ushort data2, ushort data3, byte[] data4) = GetGuidFields(type) ?? throw new InvalidOperationException(
            $"'Windows.Foundation.Metadata.GuidAttribute' attribute for type '{type.Namespace}.{type.Name}' not found");
        string fmt = lowerCase ? "x" : "X";
        // Format: %08x-%04x-%04x-%02x%02x-%02x%02x%02x%02x%02x%02x
        writer.Write(data1.ToString(fmt + "8", CultureInfo.InvariantCulture));
        writer.Write("-");
        writer.Write(data2.ToString(fmt + "4", CultureInfo.InvariantCulture));
        writer.Write("-");
        writer.Write(data3.ToString(fmt + "4", CultureInfo.InvariantCulture));
        writer.Write("-");
        for (int i = 0; i < 2; i++) { writer.Write(data4[i].ToString(fmt + "2", CultureInfo.InvariantCulture)); }
        writer.Write("-");
        for (int i = 2; i < 8; i++) { writer.Write(data4[i].ToString(fmt + "2", CultureInfo.InvariantCulture)); }
    }
    /// <summary>Writes the GUID bytes for <paramref name="type"/> as a hex byte list.</summary>
    public static void WriteGuidBytes(IndentedTextWriter writer, TypeDefinition type)
    {
        (uint data1, ushort data2, ushort data3, byte[] data4) = GetGuidFields(type) ?? throw new InvalidOperationException(
            $"'Windows.Foundation.Metadata.GuidAttribute' attribute for type '{type.Namespace}.{type.Name}' not found");
        WriteByte(writer, (data1 >> 0) & 0xFF, true);
        WriteByte(writer, (data1 >> 8) & 0xFF, false);
        WriteByte(writer, (data1 >> 16) & 0xFF, false);
        WriteByte(writer, (data1 >> 24) & 0xFF, false);
        WriteByte(writer, (uint)((data2 >> 0) & 0xFF), false);
        WriteByte(writer, (uint)((data2 >> 8) & 0xFF), false);
        WriteByte(writer, (uint)((data3 >> 0) & 0xFF), false);
        WriteByte(writer, (uint)((data3 >> 8) & 0xFF), false);
        for (int i = 0; i < 8; i++) { WriteByte(writer, data4[i], false); }
    }
    private static void WriteByte(IndentedTextWriter writer, uint b, bool first)
    {
        if (!first) { writer.Write(", "); }
        writer.Write("0x");
        writer.Write((b & 0xFF).ToString("X", CultureInfo.InvariantCulture));
    }

    /// <summary>Writes the property name <c>IID_X</c> for the IID property of <paramref name="type"/>.</summary>
    public static void WriteIidGuidPropertyName(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        IndentedTextWriter scratch = new();
        TypedefNameWriter.WriteTypedefName(scratch, context, type, TypedefNameType.ABI, true);
        TypedefNameWriter.WriteTypeParams(scratch, type);
        string name = EscapeTypeNameForIdentifier(scratch.ToString(), true, true);
        writer.Write("IID_");
        writer.Write(name);
    }
    /// <summary>Writes the property name <c>IID_XReference</c> for the reference IID property.</summary>
    public static void WriteIidReferenceGuidPropertyName(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        IndentedTextWriter scratch = new();
        TypedefNameWriter.WriteTypedefName(scratch, context, type, TypedefNameType.ABI, true);
        TypedefNameWriter.WriteTypeParams(scratch, type);
        string name = EscapeTypeNameForIdentifier(scratch.ToString(), true, true);
        writer.Write("IID_");
        writer.Write(name);
        writer.Write("Reference");
    }
    /// <summary>Writes a static IID property whose body is built from the [Guid] attribute bytes.</summary>
    public static void WriteIidGuidPropertyFromType(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        writer.Write("public static ref readonly Guid ");
        WriteIidGuidPropertyName(writer, context, type);
        writer.Write("\n{\n    [MethodImpl(MethodImplOptions.AggressiveInlining)]\n    get\n    {\n        ReadOnlySpan<byte> data =\n        [\n            ");
        WriteGuidBytes(writer, type);
        writer.Write("\n        ];\n        return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));\n    }\n}\n\n");
    }
    /// <summary>Writes the WinRT GUID parametric signature string for a type semantics.</summary>
    public static void WriteGuidSignature(IndentedTextWriter writer, ProjectionEmitContext context, TypeSemantics semantics)
    {
        switch (semantics)
        {
            case TypeSemantics.Guid_:
                writer.Write("g16");
                break;
            case TypeSemantics.Object_:
                writer.Write("cinterface(IInspectable)");
                break;
            case TypeSemantics.Fundamental f:
                writer.Write(GetFundamentalTypeGuidSignature(f.Type));
                break;
            case TypeSemantics.Definition d:
                WriteGuidSignatureForType(writer, context, d.Type);
                break;
            case TypeSemantics.Reference r:
                {
                    // Resolve the reference to a TypeDefinition (cross-module struct field, etc.).
                    (string ns, string name) = r.Reference_.Names();
                    TypeDefinition? resolved = null;
                    if (context.Cache is not null)
                    {
                        try { resolved = r.Reference_.Resolve(context.Cache.RuntimeContext); }
                        catch { resolved = null; }
                        resolved ??= context.Cache.Find(string.IsNullOrEmpty(ns) ? name : (ns + "." + name));
                    }
                    if (resolved is not null)
                    {
                        WriteGuidSignatureForType(writer, context, resolved);
                    }
                }
                break;
            case TypeSemantics.GenericInstance gi:
                writer.Write("pinterface({");
                WriteGuid(writer, gi.GenericType, true);
                writer.Write("};");
                for (int i = 0; i < gi.GenericArgs.Count; i++)
                {
                    if (i > 0) { writer.Write(";"); }
                    WriteGuidSignature(writer, context, gi.GenericArgs[i]);
                }
                writer.Write(")");
                break;
            case TypeSemantics.GenericInstanceRef gir:
                {
                    // Cross-module generic instance (e.g. Windows.Foundation.IReference<UInt64>
                    // appearing as a struct field). Resolve the generic type to a TypeDefinition
                    // so we can extract its [Guid]; recurse on each type argument.
                    (string ns, string name) = gir.GenericType.Names();
                    TypeDefinition? resolved = null;
                    if (context.Cache is not null)
                    {
                        try { resolved = gir.GenericType.Resolve(context.Cache.RuntimeContext); }
                        catch { resolved = null; }
                        resolved ??= context.Cache.Find(string.IsNullOrEmpty(ns) ? name : (ns + "." + name));
                    }
                    if (resolved is not null)
                    {
                        writer.Write("pinterface({");
                        WriteGuid(writer, resolved, true);
                        writer.Write("};");
                        for (int i = 0; i < gir.GenericArgs.Count; i++)
                        {
                            if (i > 0) { writer.Write(";"); }
                            WriteGuidSignature(writer, context, gir.GenericArgs[i]);
                        }
                        writer.Write(")");
                    }
                }
                break;
        }
    }
    private static void WriteGuidSignatureForType(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        TypeCategory cat = TypeCategorization.GetCategory(type);
        switch (cat)
        {
            case TypeCategory.Enum:
                writer.Write("enum(");
                TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.NonProjected, true);
                TypedefNameWriter.WriteTypeParams(writer, type);
                writer.Write(";");
                writer.Write(TypeCategorization.IsFlagsEnum(type) ? "u4" : "i4");
                writer.Write(")");
                break;
            case TypeCategory.Struct:
                writer.Write("struct(");
                TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.NonProjected, true);
                TypedefNameWriter.WriteTypeParams(writer, type);
                writer.Write(";");
                bool first = true;
                foreach (FieldDefinition field in type.Fields)
                {
                    if (field.IsStatic) { continue; }
                    if (field.Signature is null) { continue; }
                    if (!first) { writer.Write(";"); }
                    first = false;
                    WriteGuidSignature(writer, context, TypeSemanticsFactory.Get(field.Signature.FieldType));
                }
                writer.Write(")");
                break;
            case TypeCategory.Delegate:
                writer.Write("delegate({");
                WriteGuid(writer, type, true);
                writer.Write("})");
                break;
            case TypeCategory.Interface:
                writer.Write("{");
                WriteGuid(writer, type, true);
                writer.Write("}");
                break;
            case TypeCategory.Class:
                ITypeDefOrRef? defaultIface = type.GetDefaultInterface();
                if (defaultIface is TypeDefinition di)
                {
                    writer.Write("rc(");
                    TypedefNameWriter.WriteTypedefName(writer, context, type, TypedefNameType.NonProjected, true);
                    TypedefNameWriter.WriteTypeParams(writer, type);
                    writer.Write(";");
                    WriteGuidSignature(writer, context, new TypeSemantics.Definition(di));
                    writer.Write(")");
                }
                else
                {
                    writer.Write("{");
                    WriteGuid(writer, type, true);
                    writer.Write("}");
                }
                break;
        }
    }

    /// <summary>Writes a static IID property whose body is built from the parametric GUID signature.</summary>
    public static void WriteIidGuidPropertyFromSignature(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        IndentedTextWriter scratch = new();
        WriteGuidSignature(scratch, context, new TypeSemantics.Definition(type));
        string guidSig = scratch.ToString();
        string ireferenceGuidSig = "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};" + guidSig + ")";
        Guid guidValue = GuidGenerator.Generate(ireferenceGuidSig);
        byte[] bytes = guidValue.ToByteArray();

        writer.Write("public static ref readonly Guid ");
        WriteIidReferenceGuidPropertyName(writer, context, type);
        writer.Write("\n{\n    [MethodImpl(MethodImplOptions.AggressiveInlining)]\n    get\n    {\n        ReadOnlySpan<byte> data =\n        [\n            ");
        for (int i = 0; i < 16; i++)
        {
            if (i > 0) { writer.Write(", "); }
            writer.Write("0x");
            writer.Write(bytes[i].ToString("X", CultureInfo.InvariantCulture));
        }
        writer.Write("\n        ];\n        return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));\n    }\n}\n\n");
    }
    /// <summary>Emits IID properties for any not-included interfaces transitively implemented by a class.</summary>
    public static void WriteIidGuidPropertyForClassInterfaces(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type, System.Collections.Generic.HashSet<TypeDefinition> interfacesEmitted)
    {
        foreach (InterfaceImplementation impl in type.Interfaces)
        {
            if (impl.Interface is null) { continue; }
            // Resolve TypeRef -> TypeDefinition via metadata cache (so we pick up cross-module
            // inherited interfaces, e.g. Windows.UI.Composition.IAnimationObject from a XAML class).
            TypeDefinition? ifaceType = impl.Interface as TypeDefinition;
            if (ifaceType is null && impl.Interface is TypeReference tr)
            {
                (string trNs, string trNm) = tr.Names();
                ifaceType = ResolveCrossModuleType(context.Cache, trNs, trNm);
            }
            if (ifaceType is null) { continue; }

            (string ns, string nm) = ifaceType.Names();
            // Skip mapped types
            if (MappedTypes.Get(ns, nm) is not null) { continue; }
            // Skip generic interfaces
            if (ifaceType.GenericParameters.Count != 0) { continue; }
            // Skip already-emitted
            if (interfacesEmitted.Contains(ifaceType)) { continue; }
            // Only emit if the interface is not in the projection (otherwise it'll be emitted naturally)
            if (!context.Settings.Filter.Includes(ifaceType))
            {
                WriteIidGuidPropertyFromType(writer, context, ifaceType);
                _ = interfacesEmitted.Add(ifaceType);
            }
        }
    }
    private static TypeDefinition? ResolveCrossModuleType(MetadataCache cache, string ns, string name)
    {
        return cache.Find(string.IsNullOrEmpty(ns) ? name : (ns + "." + name));
    }

    /// <summary>Writes the InterfaceIIDs file header.</summary>
    public static void WriteInterfaceIidsBegin(IndentedTextWriter writer)
    {
        writer.Write("\n");
        writer.Write("//------------------------------------------------------------------------------\n");
        writer.Write("// <auto-generated>\n");
        writer.Write("//     This file was generated by cswinrt.exe version ");
        writer.Write(CodeWriters.GetVersionString());
        writer.Write("\n");
        writer.Write("//\n");
        writer.Write("//     Changes to this file may cause incorrect behavior and will be lost if\n");
        writer.Write("//     the code is regenerated.\n");
        writer.Write("// </auto-generated>\n");
        writer.Write("//------------------------------------------------------------------------------\n");
        writer.Write(@"
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime.ProjectionWriter.Extensions;

namespace ABI;

internal static class InterfaceIIDs
{
");
    }

    /// <summary>Writes the InterfaceIIDs file footer.</summary>
    public static void WriteInterfaceIidsEnd(IndentedTextWriter writer)
    {
        writer.Write("}\n\n");
    }
}
