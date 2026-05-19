// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using WindowsRuntime.ProjectionWriter.Errors;
using WindowsRuntime.ProjectionWriter.Factories;
using WindowsRuntime.ProjectionWriter.Generation;
using WindowsRuntime.ProjectionWriter.Metadata;
using WindowsRuntime.ProjectionWriter.Writers;
using static WindowsRuntime.ProjectionWriter.References.ProjectionNames;

namespace WindowsRuntime.ProjectionWriter.Helpers;

/// <summary>
/// Helpers for emitting WinRT GUID / IID expressions: signature characters for the GUID
/// hash algorithm, the canonical hyphenated string form of a type's <c>[Guid]</c>, and the
/// <c>byte</c>-list form used when initializing native IID storage.
/// </summary>
internal static partial class IidExpressionGenerator
{
    /// <summary>
    /// Returns the GUID-signature character code for a fundamental WinRT type.
    /// </summary>
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
        _ => throw WellKnownProjectionWriterExceptions.UnknownFundamentalType()
    };

    [GeneratedRegex(@"[ :<>`,.]")]
    private static partial Regex TypeNameEscapeRegex();

    /// <summary>
    /// Escapes a type name into a C# identifier-safe form.
    /// </summary>
    public static string EscapeTypeNameForIdentifier(string typeName, bool stripGlobal = false, bool stripGlobalABI = false)
    {
        // Escape special chars first, then strip ONLY the prefix (not all occurrences).
        string result = TypeNameEscapeRegex().Replace(typeName, "_");

        if (stripGlobalABI && typeName.StartsWith(GlobalAbiPrefix, StringComparison.Ordinal))
        {
            result = result[12..]; // Remove GlobalAbiPrefix (with ":" and "." already replaced)
        }
        else if (stripGlobal && typeName.StartsWith(GlobalPrefix, StringComparison.Ordinal))
        {
            result = result[8..]; // Remove GlobalPrefix
        }

        return result;
    }

    /// <summary>
    /// Reads the GUID values from the [GuidAttribute] of the type and returns them as a tuple.
    /// </summary>
    public static (uint Data1, ushort Data2, ushort Data3, byte[] Data4)? GetGuidFields(TypeDefinition type)
    {
        CustomAttribute? attr = type.GetWindowsFoundationMetadataAttribute("GuidAttribute");

        if (attr is null || attr.Signature is null)
        {
            return null;
        }

        IList<CustomAttributeArgument> args = attr.Signature.FixedArguments;

        if (args.Count < 11)
        {
            return null;
        }

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

    /// <summary>
    /// Writes the GUID for <paramref name="type"/> in canonical hyphenated string form.
    /// </summary>
    /// <summary>
    /// Writes the GUID for <paramref name="type"/> in canonical hyphenated string form.
    /// </summary>
    public static void WriteGuid(IndentedTextWriter writer, TypeDefinition type, bool lowerCase)
    {
        (uint data1, ushort data2, ushort data3, byte[] data4) = GetGuidFields(type) ?? throw WellKnownProjectionWriterExceptions.MissingGuidAttribute($"{type.Namespace}.{type.Name}");
        string fmt = lowerCase ? "x" : "X";
        // Format: %08x-%04x-%04x-%02x%02x-%02x%02x%02x%02x%02x%02x
        writer.Write($"{data1.ToString(fmt + "8", CultureInfo.InvariantCulture)}-{data2.ToString(fmt + "4", CultureInfo.InvariantCulture)}-{data3.ToString(fmt + "4", CultureInfo.InvariantCulture)}-");

        for (int i = 0; i < 2; i++)
        {
            writer.Write(data4[i].ToString(fmt + "2", CultureInfo.InvariantCulture));
        }

        writer.Write("-");

        for (int i = 2; i < 8; i++)
        {
            writer.Write(data4[i].ToString(fmt + "2", CultureInfo.InvariantCulture));
        }
    }

    /// <inheritdoc cref="WriteGuid(IndentedTextWriter, TypeDefinition, bool)"/>
    /// <returns>The formatted GUID as a string.</returns>
    public static string FormatGuid(TypeDefinition type, bool lowerCase)
    {
        using IndentedTextWriterOwner owner = IndentedTextWriterPool.GetOrCreate();
        WriteGuid(owner.Writer, type, lowerCase);
        return owner.Writer.ToString();
    }

    /// <summary>
    /// Writes the GUID bytes for <paramref name="type"/> as a hex byte list.
    /// </summary>
    public static void WriteGuidBytes(IndentedTextWriter writer, TypeDefinition type)
    {
        (uint data1, ushort data2, ushort data3, byte[] data4) = GetGuidFields(type) ?? throw WellKnownProjectionWriterExceptions.MissingGuidAttribute($"{type.Namespace}.{type.Name}");
        WriteByte(writer, (data1 >> 0) & 0xFF, true);
        WriteByte(writer, (data1 >> 8) & 0xFF, false);
        WriteByte(writer, (data1 >> 16) & 0xFF, false);
        WriteByte(writer, (data1 >> 24) & 0xFF, false);
        WriteByte(writer, (uint)((data2 >> 0) & 0xFF), false);
        WriteByte(writer, (uint)((data2 >> 8) & 0xFF), false);
        WriteByte(writer, (uint)((data3 >> 0) & 0xFF), false);
        WriteByte(writer, (uint)((data3 >> 8) & 0xFF), false);

        for (int i = 0; i < 8; i++)
        {
            WriteByte(writer, data4[i], false);
        }
    }
    private static void WriteByte(IndentedTextWriter writer, uint b, bool first)
    {
        writer.WriteIf(!first, ", ");

        writer.Write($"0x{(b & 0xFF).ToString("X", CultureInfo.InvariantCulture)}");
    }

    /// <summary>
    /// Writes the property name <c>IID_X</c> for the IID property of <paramref name="type"/>.
    /// </summary>
    public static void WriteIidGuidPropertyName(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string name = EscapeTypeNameForIdentifier(TypedefNameWriter.WriteTypedefNameWithTypeParams(context, type, TypedefNameType.ABI, true).Format(), true, true);
        writer.Write($"IID_{name}");
    }

    /// <summary>
    /// Writes the property name <c>IID_XReference</c> for the reference IID property.
    /// </summary>
    public static void WriteIidReferenceGuidPropertyName(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string name = EscapeTypeNameForIdentifier(TypedefNameWriter.WriteTypedefNameWithTypeParams(context, type, TypedefNameType.ABI, true).Format(), true, true);
        writer.Write($"IID_{name}Reference");
    }

    /// <summary>
    /// Writes a static IID property whose body is built from the [Guid] attribute bytes.
    /// </summary>
    public static void WriteIidGuidPropertyFromType(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        writer.Write("public static ref readonly Guid ");
        WriteIidGuidPropertyName(writer, context, type);
        writer.WriteLine();
        writer.Write(isMultiline: true, """
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    ReadOnlySpan<byte> data =
                    [
                        
            """);
        WriteGuidBytes(writer, type);
        writer.WriteLine();
        writer.WriteLine(isMultiline: true, """
                    ];
                    return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
                }
            }
            """);
        writer.WriteLine();
    }

    /// <summary>
    /// Writes the WinRT GUID parametric signature string for a type semantics.
    /// </summary>
    public static void WriteGuidSignature(IndentedTextWriter writer, ProjectionEmitContext context, TypeSemantics semantics)
    {
        switch (semantics)
        {
            case TypeSemantics.GuidType:
                writer.Write("g16");
                break;
            case TypeSemantics.ObjectType:
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
                    (string ns, string name) = r.Type.Names();
                    TypeDefinition? resolved = null;

                    if (context.Cache is not null)
                    {
                        resolved = r.Type.TryResolve(context.Cache.RuntimeContext)
                            ?? context.Cache.Find(ns, name);
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
                    writer.WriteIf(i > 0, ";");

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
                        resolved = gir.GenericType.TryResolve(context.Cache.RuntimeContext)
                            ?? context.Cache.Find(ns, name);
                    }

                    if (resolved is not null)
                    {
                        writer.Write("pinterface({");
                        WriteGuid(writer, resolved, true);
                        writer.Write("};");
                        for (int i = 0; i < gir.GenericArgs.Count; i++)
                        {
                            writer.WriteIf(i > 0, ";");

                            WriteGuidSignature(writer, context, gir.GenericArgs[i]);
                        }
                        writer.Write(")");
                    }
                }
                break;
        }
    }

    /// <summary>
    /// Convenience overload of <see cref="WriteGuidSignature(IndentedTextWriter, ProjectionEmitContext, TypeSemantics)"/>
    /// that leases an <see cref="IndentedTextWriter"/> from <see cref="IndentedTextWriterPool"/>,
    /// emits the GUID signature into it, and returns the resulting string.
    /// </summary>
    /// <param name="context">The active emit context.</param>
    /// <param name="semantics">The type semantics whose GUID signature is emitted.</param>
    /// <returns>The emitted GUID signature.</returns>
    public static string WriteGuidSignature(ProjectionEmitContext context, TypeSemantics semantics)
    {
        using IndentedTextWriterOwner writerOwner = IndentedTextWriterPool.GetOrCreate();
        IndentedTextWriter writer = writerOwner.Writer;
        WriteGuidSignature(writer, context, semantics);
        return writer.ToString();
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
                    if (field.IsStatic)
                    {
                        continue;
                    }

                    if (field.Signature is null)
                    {
                        continue;
                    }

                    writer.WriteIf(!first, ";");

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

    /// <summary>
    /// Writes a static IID property whose body is built from the parametric GUID signature.
    /// </summary>
    public static void WriteIidGuidPropertyFromSignature(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string guidSig = WriteGuidSignature(context, new TypeSemantics.Definition(type));
        string ireferenceGuidSig = "pinterface({61c17706-2d65-11e0-9ae8-d48564015472};" + guidSig + ")";
        Guid guidValue = GuidGenerator.Generate(ireferenceGuidSig);
        byte[] bytes = guidValue.ToByteArray();

        writer.Write("public static ref readonly Guid ");
        WriteIidReferenceGuidPropertyName(writer, context, type);
        writer.WriteLine();
        writer.Write(isMultiline: true, """
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    ReadOnlySpan<byte> data =
                    [
                        
            """);
        for (int i = 0; i < 16; i++)
        {
            writer.WriteIf(i > 0, ", ");

            writer.Write($"0x{bytes[i].ToString("X", CultureInfo.InvariantCulture)}");
        }
        writer.WriteLine();
        writer.WriteLine(isMultiline: true, """
                    ];
                    return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
                }
            }
            """);
        writer.WriteLine();
    }

    /// <summary>
    /// Emits IID properties for any not-included interfaces transitively implemented by a class.
    /// </summary>
    public static void WriteIidGuidPropertyForClassInterfaces(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type, System.Collections.Generic.HashSet<TypeDefinition> interfacesEmitted)
    {
        foreach (InterfaceImplementation impl in type.Interfaces)
        {
            if (impl.Interface is null)
            {
                continue;
            }

            // Resolve TypeRef -> TypeDefinition via metadata cache (so we pick up cross-module
            // inherited interfaces, e.g. Windows.UI.Composition.IAnimationObject from a XAML class).
            TypeDefinition? ifaceType = impl.Interface as TypeDefinition;

            if (ifaceType is null && impl.Interface is TypeReference tr)
            {
                (string trNs, string trNm) = tr.Names();
                ifaceType = ResolveCrossModuleType(context.Cache, trNs, trNm);
            }

            if (ifaceType is null)
            {
                continue;
            }

            (string ns, string nm) = ifaceType.Names();
            // Skip mapped types
            if (MappedTypes.Get(ns, nm) is not null)
            {
                continue;
            }

            // Skip generic interfaces
            if (ifaceType.GenericParameters.Count != 0)
            {
                continue;
            }

            // Skip already-emitted
            if (interfacesEmitted.Contains(ifaceType))
            {
                continue;
            }

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
        return cache.Find(ns, name);
    }

    /// <summary>
    /// Writes the InterfaceIIDs file header.
    /// </summary>
    public static void WriteInterfaceIidsBegin(IndentedTextWriter writer)
    {
        writer.WriteLine();
        writer.WriteLine(isMultiline: true, $$"""
            //------------------------------------------------------------------------------
            // <auto-generated>
            //     This file was generated by cswinrt.exe version {{MetadataAttributeFactory.GetVersionString()}}
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
            """);
    }

    /// <summary>
    /// Writes the InterfaceIIDs file footer.
    /// </summary>
    public static void WriteInterfaceIidsEnd(IndentedTextWriter writer)
    {
        writer.DecreaseIndent();
        writer.WriteLine("}");
        writer.WriteLine();
    }
}
