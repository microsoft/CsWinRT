// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using AsmResolver.DotNet;
using WindowsRuntime.InteropGenerator.Helpers;
using WindowsRuntime.InteropGenerator.References;
using WindowsRuntime.InteropGenerator.Writers;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <inheritdoc cref="InteropGenerator"/>
internal static partial class InteropGenerator
{
    /// <summary>
    /// Orders independently emitted metadata before the binary builder assigns tokens.
    /// </summary>
    /// <param name="module">The completed interop module.</param>
    /// <param name="definitions">The shared member holders.</param>
    /// <param name="payloads">The assembly attribute payloads to reuse for this module's write.</param>
    private static void OrderGeneratedMetadata(ModuleDefinition module, InteropDefinitions definitions, CustomAttributePayloadCache payloads)
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
        Dictionary<ICustomAttributeType, string> constructors = GetAttributeConstructorOrderingKeys(attributes);

        Reorder(attributes, attributes
            .Select(attribute => (
                Attribute: attribute,
                Constructor: constructors[attribute.Constructor!],
                Blob: payloads.GetOrAdd(attribute)))
            .OrderBy(static entry => entry.Constructor, StringComparer.Ordinal)
            .ThenBy(static entry => entry.Blob, ByteSequenceComparer.Instance)
            .Select(static entry => entry.Attribute));
    }

    /// <summary>
    /// Formats each constructor descriptor once while the completed metadata graph is stable.
    /// </summary>
    /// <param name="attributes">The attributes whose constructors are used as ordering keys.</param>
    /// <returns>The constructor identity keys, frozen for the remainder of this ordering pass.</returns>
    private static Dictionary<ICustomAttributeType, string> GetAttributeConstructorOrderingKeys(IEnumerable<CustomAttribute> attributes)
    {
        Dictionary<ICustomAttributeType, string> constructors = new(ReferenceEqualityComparer.Instance);

        foreach (CustomAttribute attribute in attributes)
        {
            ICustomAttributeType constructor = attribute.Constructor!;

            if (!constructors.ContainsKey(constructor))
            {
                constructors.Add(constructor, TypeDescriptorComparer.Default.GetOrderKey(constructor.DeclaringType!) + constructor.Signature);
            }
        }

        return constructors;
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
}
