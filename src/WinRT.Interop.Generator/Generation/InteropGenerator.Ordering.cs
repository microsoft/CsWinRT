// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Signatures;
using AsmResolver.IO;
using WindowsRuntime.InteropGenerator.Helpers;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <inheritdoc cref="InteropGenerator"/>
internal static partial class InteropGenerator
{
    /// <summary>
    /// Orders independently emitted metadata before the binary builder assigns tokens.
    /// </summary>
    /// <param name="module">The completed interop module.</param>
    /// <param name="definitions">The shared member holders.</param>
    private static void OrderGeneratedMetadata(ModuleDefinition module, InteropDefinitions definitions)
    {
        Reorder(module.TopLevelTypes, module.TopLevelTypes
            .OrderBy(static type => type.Name != "<Module>")
            .ThenBy(static type => type.FullName, StringComparer.Ordinal));

        // These holders, unlike ordinary generated types, receive members from multiple workers.
        Reorder(definitions.RvaFields.Fields, definitions.RvaFields.Fields.OrderBy(static field => field.Name?.Value, StringComparer.Ordinal));
        Reorder(definitions.InterfaceIIDs.Methods, definitions.InterfaceIIDs.Methods.OrderBy(static method => method.Name?.Value, StringComparer.Ordinal));
        Reorder(definitions.InterfaceIIDs.Properties, definitions.InterfaceIIDs.Properties.OrderBy(static property => property.Name?.Value, StringComparer.Ordinal));

        if (definitions.KeyValuePairMethods is { } pairMethods)
        {
            Reorder(pairMethods.Methods, pairMethods.Methods.OrderBy(static method => method.Name?.Value, StringComparer.Ordinal));
        }

        IList<CustomAttribute> attributes = module.Assembly!.CustomAttributes;
        Reorder(attributes, attributes
            .Select(attribute => (
                Attribute: attribute,
                Constructor: TypeDescriptorComparer.Default.GetOrderKey(attribute.Constructor!.DeclaringType!) + attribute.Constructor.Signature,
                Blob: GetAttributeOrderingKey(attribute, module)))
            .OrderBy(static entry => entry.Constructor, StringComparer.Ordinal)
            .ThenBy(static entry => entry.Blob, StringComparer.Ordinal)
            .Select(static entry => entry.Attribute));
    }

    /// <summary>
    /// Replaces an owned metadata collection without attaching an element to two owners.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="collection">The collection to reorder.</param>
    /// <param name="ordered">The desired order.</param>
    private static void Reorder<T>(IList<T> collection, IEnumerable<T> ordered)
    {
        T[] items = [.. ordered];
        collection.Clear();

        foreach (T item in items)
        {
            collection.Add(item);
        }
    }

    /// <summary>
    /// Serializes the complete attribute payload, including named arguments and type scopes.
    /// </summary>
    /// <param name="attribute">The attribute to order.</param>
    /// <param name="module">The output module used to format type-valued arguments.</param>
    /// <returns>A lossless, ordinal ordering key.</returns>
    private static string GetAttributeOrderingKey(CustomAttribute attribute, ModuleDefinition module)
    {
        using MemoryStream stream = new();
        BlobSerializationContext context = new(
            writer: new BinaryStreamWriter(stream),
            contextModule: module,
            indexProvider: AttributeOrderingIndexProvider.Instance,
            errorListener: ThrowErrorListener.Instance,
            diagnosticSource: attribute);

        attribute.Signature!.Write(context);

        return Convert.ToHexString(stream.GetBuffer().AsSpan(0, (int)stream.Length));
    }

    /// <summary>
    /// Rejects token-based signatures: custom attribute payloads encode types by name, not metadata index.
    /// </summary>
    private sealed class AttributeOrderingIndexProvider : ITypeCodedIndexProvider
    {
        /// <summary>Gets the shared stateless provider.</summary>
        public static AttributeOrderingIndexProvider Instance { get; } = new();

        /// <inheritdoc/>
        public uint GetTypeDefOrRefIndex(ITypeDefOrRef type, object? diagnosticSource)
        {
            throw new InvalidOperationException("Custom attribute ordering must not allocate metadata tokens.");
        }
    }
}
