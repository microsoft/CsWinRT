// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
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
/// Helpers for emitting WinRT GUID / IID expressions: the canonical hyphenated string form of a
/// type's <c>[Guid]</c>, the <c>byte</c>-list form used when initializing native IID storage, and
/// the <c>IID_X</c> / <c>IID_XReference</c> property emissions on the generated <c>InterfaceIIDs</c>
/// static class.
/// </summary>
/// <remarks>
/// The WinRT parametric signature generation that drives the IID hash for generic instances
/// lives in <see cref="SignatureGenerator"/>; <see cref="WriteIidGuidPropertyFromSignature"/>
/// is the only consumer of it from this file.
/// </remarks>
internal static partial class IidExpressionGenerator
{
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
    /// Writes the GUID for <paramref name="type"/> in canonical hyphenated string form
    /// (e.g. <c>00000000-0000-0000-0000-000000000000</c>).
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="type">The type whose <c>[Guid]</c> attribute is read.</param>
    /// <param name="lowerCase">When <see langword="true"/>, hex digits are lower-case; otherwise upper-case.</param>
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
    /// Writes the GUID bytes for <paramref name="type"/> as a hex byte list (e.g.
    /// <c>0x00, 0x00, ..., 0x00</c>), formatted for embedding inside a C# collection
    /// expression that initializes a <c>ReadOnlySpan&lt;byte&gt;</c>.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="type">The type whose <c>[Guid]</c> attribute is read.</param>
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

    /// <summary>
    /// Writes a single byte as <c>0xXX</c> (or <c>, 0xXX</c> when not the first byte in a list).
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="b">The byte value (only the low 8 bits are used).</param>
    /// <param name="first">When <see langword="true"/>, omits the leading <c>", "</c> separator.</param>
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
    /// Writes a static IID property whose body is built from the <c>[Guid]</c> attribute bytes
    /// of <paramref name="type"/>. The property is named <c>IID_X</c> (see
    /// <see cref="WriteIidGuidPropertyName"/>) and returns a <c>ref readonly Guid</c> backed by
    /// a stack-allocated <c>ReadOnlySpan&lt;byte&gt;</c>.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="type">The type whose <c>[Guid]</c> attribute is read.</param>
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
    /// Writes a static IID property whose body is built from the parametric GUID signature
    /// (computed via <see cref="SignatureGenerator.GetSignature(ProjectionEmitContext, TypeSemantics)"/>
    /// for the type, then wrapped in the <c>Windows.Foundation.IReference&lt;T&gt;</c>
    /// pinterface to produce the <c>IID_XReference</c> hashed GUID).
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="type">The type whose <c>IReference&lt;T&gt;</c> IID is emitted.</param>
    public static void WriteIidGuidPropertyFromSignature(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string signature = SignatureGenerator.GetSignature(context, new TypeSemantics.Definition(type));
        Guid guid;

        // We can use an interpolated handler here to avoid allocating the 'string' for the full signature.
        // The additional scope here is to ensure that the rest of the method never reuses this handler.
        {
            DefaultInterpolatedStringHandler handler = $$"""pinterface({61c17706-2d65-11e0-9ae8-d48564015472};{{signature}})""";

            guid = GuidGenerator.Generate(handler.Text);

            // Don't forget to clear the handler after we used it, so its rented buffer can be returned
            handler.Clear();
        }

        byte[] bytes = guid.ToByteArray();

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
