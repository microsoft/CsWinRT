// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AsmResolver.DotNet;
using WindowsRuntime.ProjectionWriter.Errors;
using WindowsRuntime.ProjectionWriter.Factories;
using WindowsRuntime.ProjectionWriter.Factories.Callbacks;
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
    private static partial Regex TypeNameEscapeRegex { get; }

    /// <summary>
    /// Escapes a type name into a C# identifier-safe form.
    /// </summary>
    public static string EscapeTypeNameForIdentifier(string typeName, bool stripGlobal = false, bool stripGlobalABI = false)
    {
        // Escape special chars first, then strip ONLY the prefix (not all occurrences).
        string result = TypeNameEscapeRegex.Replace(typeName, "_");

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
    /// Writes <paramref name="guid"/> in canonical hyphenated string form
    /// (e.g. <c>00000000-0000-0000-0000-000000000000</c>).
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="guid">The GUID value.</param>
    /// <param name="lowerCase">When <see langword="true"/>, hex digits are lower-case; otherwise upper-case.</param>
    private static void WriteGuid(IndentedTextWriter writer, Guid guid, bool lowerCase)
    {
        Span<byte> bytes = stackalloc byte[16];
        _ = guid.TryWriteBytes(bytes);

        uint data1 = bytes[0] | ((uint)bytes[1] << 8) | ((uint)bytes[2] << 16) | ((uint)bytes[3] << 24);
        ushort data2 = (ushort)(bytes[4] | (bytes[5] << 8));
        ushort data3 = (ushort)(bytes[6] | (bytes[7] << 8));

        string fmt = lowerCase ? "x" : "X";

        // Format: %08x-%04x-%04x-%02x%02x-%02x%02x%02x%02x%02x%02x
        writer.Write($"{data1.ToString(fmt + "8", CultureInfo.InvariantCulture)}-{data2.ToString(fmt + "4", CultureInfo.InvariantCulture)}-{data3.ToString(fmt + "4", CultureInfo.InvariantCulture)}-");

        for (int i = 8; i < 10; i++)
        {
            writer.Write(bytes[i].ToString(fmt + "2", CultureInfo.InvariantCulture));
        }

        writer.Write("-");

        for (int i = 10; i < 16; i++)
        {
            writer.Write(bytes[i].ToString(fmt + "2", CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// Writes the GUID for <paramref name="type"/> in canonical hyphenated string form. Reads
    /// the <c>[Guid]</c> attribute on the type via
    /// <see cref="TypeDefinitionExtensions"/>'s <c>GetWindowsRuntimeGuid</c> and forwards to
    /// <see cref="WriteGuid(IndentedTextWriter, Guid, bool)"/>.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="type">The type whose <c>[Guid]</c> attribute is read.</param>
    /// <param name="lowerCase">When <see langword="true"/>, hex digits are lower-case; otherwise upper-case.</param>
    /// <exception cref="WellKnownProjectionWriterException">Thrown when the type has no <c>[Guid]</c> attribute.</exception>
    public static void WriteGuid(IndentedTextWriter writer, TypeDefinition type, bool lowerCase)
    {
        if (!type.TryGetWindowsRuntimeGuid(out Guid guid))
        {
            throw WellKnownProjectionWriterExceptions.MissingGuidAttribute($"{type.Namespace}.{type.Name}");
        }

        WriteGuid(writer, guid, lowerCase);
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
    /// Writes the 16 bytes of <paramref name="guid"/> as a hex byte list (e.g.
    /// <c>0x00, 0x00, ..., 0x00</c>), formatted for embedding inside a C# collection
    /// expression that initializes a <c>ReadOnlySpan&lt;byte&gt;</c>.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="guid">The GUID whose bytes are emitted.</param>
    public static void WriteGuidBytes(IndentedTextWriter writer, Guid guid)
    {
        Span<byte> bytes = stackalloc byte[16];
        _ = guid.TryWriteBytes(bytes);

        for (int i = 0; i < 16; i++)
        {
            WriteByte(writer, bytes[i], first: i == 0);
        }
    }

    /// <inheritdoc cref="WriteGuidBytes(IndentedTextWriter, Guid)"/>
    /// <returns>A callback that writes the bytes when invoked.</returns>
    public static WriteGuidBytesCallback WriteGuidBytes(Guid guid)
    {
        return new(guid);
    }

    /// <summary>
    /// Writes a single byte as <c>0xXX</c> (or <c>, 0xXX</c> when not the first byte in a list).
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="b">The byte value.</param>
    /// <param name="first">When <see langword="true"/>, omits the leading <c>", "</c> separator.</param>
    private static void WriteByte(IndentedTextWriter writer, byte b, bool first)
    {
        writer.WriteIf(!first, ", ");

        writer.Write($"0x{b.ToString("X", CultureInfo.InvariantCulture)}");
    }

    /// <summary>
    /// Writes the property name <c>IID_X</c> for the IID property of <paramref name="type"/>.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="type">The type whose IID property name is emitted.</param>
    public static void WriteIidGuidPropertyName(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string name = EscapeTypeNameForIdentifier(TypedefNameWriter.WriteTypedefNameWithTypeParams(context, type, TypedefNameType.ABI, true).Format(), true, true);
        writer.Write($"IID_{name}");
    }

    /// <inheritdoc cref="WriteIidGuidPropertyName(IndentedTextWriter, ProjectionEmitContext, TypeDefinition)"/>
    /// <returns>A callback that writes the property name when invoked.</returns>
    public static WriteIidGuidPropertyNameCallback WriteIidGuidPropertyName(ProjectionEmitContext context, TypeDefinition type)
    {
        return new(context, type);
    }

    /// <summary>
    /// Writes the property name <c>IID_XReference</c> for the <c>IReference&lt;T&gt;</c> IID
    /// property of <paramref name="type"/>.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="type">The type whose reference IID property name is emitted.</param>
    public static void WriteIidReferenceGuidPropertyName(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        string name = EscapeTypeNameForIdentifier(TypedefNameWriter.WriteTypedefNameWithTypeParams(context, type, TypedefNameType.ABI, true).Format(), true, true);
        writer.Write($"IID_{name}Reference");
    }

    /// <inheritdoc cref="WriteIidReferenceGuidPropertyName(IndentedTextWriter, ProjectionEmitContext, TypeDefinition)"/>
    /// <returns>A callback that writes the property name when invoked.</returns>
    public static WriteIidReferenceGuidPropertyNameCallback WriteIidReferenceGuidPropertyName(ProjectionEmitContext context, TypeDefinition type)
    {
        return new(context, type);
    }

    /// <summary>
    /// Writes a static IID property whose body is built from the <c>[Guid]</c> attribute bytes
    /// of <paramref name="type"/>. The property is named <c>IID_X</c> (see
    /// <see cref="WriteIidGuidPropertyName(ProjectionEmitContext, TypeDefinition)"/>) and returns
    /// a <c>ref readonly Guid</c> backed by a stack-allocated <c>ReadOnlySpan&lt;byte&gt;</c>.
    /// </summary>
    /// <param name="writer">The writer to emit to.</param>
    /// <param name="context">The active emit context.</param>
    /// <param name="type">The type whose <c>[Guid]</c> attribute is read.</param>
    /// <exception cref="WellKnownProjectionWriterException">Thrown when the type has no <c>[Guid]</c> attribute.</exception>
    public static void WriteIidGuidPropertyFromType(IndentedTextWriter writer, ProjectionEmitContext context, TypeDefinition type)
    {
        if (!type.TryGetWindowsRuntimeGuid(out Guid guid))
        {
            throw WellKnownProjectionWriterExceptions.MissingGuidAttribute($"{type.Namespace}.{type.Name}");
        }

        WriteIidGuidPropertyNameCallback name = WriteIidGuidPropertyName(context, type);
        WriteGuidBytesCallback bytes = WriteGuidBytes(guid);

        writer.WriteLine(isMultiline: true, $$"""
            public static ref readonly Guid {{name}}
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    ReadOnlySpan<byte> data = [{{bytes}}];

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

        WriteIidReferenceGuidPropertyNameCallback name = WriteIidReferenceGuidPropertyName(context, type);
        WriteGuidBytesCallback bytes = WriteGuidBytes(guid);

        writer.WriteLine(isMultiline: true, $$"""
            public static ref readonly Guid {{name}}
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    ReadOnlySpan<byte> data = [{{bytes}}];

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
            if (!context.Settings.Filter.Includes(ifaceType.FullName))
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
