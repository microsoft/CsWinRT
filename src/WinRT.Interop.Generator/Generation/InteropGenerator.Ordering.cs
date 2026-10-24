// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AsmResolver.DotNet;
using WindowsRuntime.InteropGenerator.Helpers;
using WindowsRuntime.InteropGenerator.References;
using WindowsRuntime.InteropGenerator.Writers;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <inheritdoc cref="InteropGenerator"/>
internal static partial class InteropGenerator
{
    /// <summary>The minimum useful collection or attribute batch size for parallel ordering.</summary>
    private const int MinimumOrderingBatchSize = 256;

    /// <summary>The maximum number of attribute batches per worker, allowing uneven payload costs to balance.</summary>
    private const int OrderingBatchesPerWorker = 4;

    /// <summary>
    /// Orders independently emitted metadata before the binary builder assigns tokens.
    /// </summary>
    /// <param name="args">The invocation's parallelism and cancellation settings.</param>
    /// <param name="module">The completed interop module.</param>
    /// <param name="definitions">The shared member holders.</param>
    /// <param name="payloads">The assembly attribute payloads to reuse for this module's write.</param>
    private static void OrderGeneratedMetadata(
        InteropGeneratorArgs args,
        ModuleDefinition module,
        InteropDefinitions definitions,
        CustomAttributePayloadCache payloads)
    {
        CustomAttribute[] attributes = [.. module.Assembly!.CustomAttributes];
        Dictionary<ICustomAttributeType, string> constructors = GetAttributeConstructorOrderingKeys(attributes, args.Token);
        (CustomAttribute Attribute, string Constructor, ReadOnlyMemory<byte> Blob)[] entries =
            new (CustomAttribute, string, ReadOnlyMemory<byte>)[attributes.Length];
        MetadataCollectionOrdering attributeOrdering = MetadataCollectionOrdering.Create(module.Assembly.CustomAttributes, _ => entries
            .OrderBy(static entry => entry.Constructor, StringComparer.Ordinal)
            .ThenBy(static entry => entry.Blob, ByteSequenceComparer.Instance)
            .Select(static entry => entry.Attribute));

        // These holders, unlike ordinary generated types, receive members from multiple workers
        MetadataCollectionOrdering[] orderings =
        [
            MetadataCollectionOrdering.Create(module.TopLevelTypes, static types => types
                .OrderBy(static type => type.Name != "<Module>")
                .ThenBy(static type => type.FullName, StringComparer.Ordinal)),
            MetadataCollectionOrdering.Create(definitions.RvaFields.Fields, static fields =>
                fields.OrderBy(static field => field.Name?.Value, StringComparer.Ordinal)),
            MetadataCollectionOrdering.Create(definitions.InterfaceIIDs.Methods, static methods =>
                methods.OrderBy(static method => method.Name?.Value, StringComparer.Ordinal)),
            MetadataCollectionOrdering.Create(definitions.InterfaceIIDs.Properties, static properties =>
                properties.OrderBy(static property => property.Name?.Value, StringComparer.Ordinal)),
            MetadataCollectionOrdering.Create(definitions.KeyValuePairMethods?.Methods ?? [], static methods =>
                methods.OrderBy(static method => method.Name?.Value, StringComparer.Ordinal)),
            attributeOrdering
        ];

        int workerCount = args.MaxDegreesOfParallelism == -1 ? Environment.ProcessorCount : args.MaxDegreesOfParallelism;

        if (workerCount == 1 || orderings.All(static ordering => ordering.Count < MinimumOrderingBatchSize))
        {
            PrepareAttributeKeys(0, attributes.Length, args.Token);

            foreach (MetadataCollectionOrdering ordering in orderings)
            {
                args.Token.ThrowIfCancellationRequested();
                ordering.Compute();
            }
        }
        else
        {
            int batchCount = GetAttributeKeyBatchCount(attributes.Length, workerCount);
            int[] batches = [.. Enumerable.Range(orderings.Length, batchCount)];
            InteropGenerationWorkQueue<int, CancellationToken> queue = new(
            [
                .. orderings.Select((ordering, index) => new InteropGenerationWorkQueue<int, CancellationToken>.WorkItem(
                    Key: index,
                    Body: _ => ordering.Compute(),
                    Prerequisites: ReferenceEquals(ordering, attributeOrdering) ? batches : [])),
                .. batches.Select((key, batchIndex) => new InteropGenerationWorkQueue<int, CancellationToken>.WorkItem(
                    Key: key,
                    Body: token => PrepareAttributeKeys(
                        start: (int)((long)attributes.Length * batchIndex / batchCount),
                        end: (int)((long)attributes.Length * (batchIndex + 1) / batchCount),
                        token: token)))
            ]);

            queue.Run(args.Token, args.MaxDegreesOfParallelism, args.Token);
        }

        args.Token.ThrowIfCancellationRequested();

        // Clearing owned collections temporarily detaches types from their module. Every name/payload
        // reader must finish before any collection is changed, including on the serial small-input path.
        foreach (MetadataCollectionOrdering ordering in orderings)
        {
            ordering.Apply();
        }

        void PrepareAttributeKeys(int start, int end, CancellationToken token)
        {
            for (int index = start; index < end; index++)
            {
                token.ThrowIfCancellationRequested();

                CustomAttribute attribute = attributes[index];

                entries[index] = (attribute, constructors[attribute.Constructor!], payloads.GetOrAdd(attribute));
            }
        }
    }

    /// <summary>
    /// Bounds key preparation by both the available workers and the amount of useful work.
    /// </summary>
    /// <param name="attributeCount">The number of attribute payloads to prepare.</param>
    /// <param name="workerCount">The configured worker limit, with the default already resolved.</param>
    /// <returns>The number of non-overlapping ranges to schedule.</returns>
    private static int GetAttributeKeyBatchCount(int attributeCount, int workerCount)
    {
        return (int)Math.Min(Math.Max(1, attributeCount / MinimumOrderingBatchSize), (long)workerCount * OrderingBatchesPerWorker);
    }

    /// <summary>
    /// Formats each constructor descriptor once while the completed metadata graph is stable.
    /// </summary>
    /// <param name="attributes">The attributes whose constructors are used as ordering keys.</param>
    /// <param name="token">The invocation's cancellation token.</param>
    /// <returns>The constructor identity keys, frozen for the remainder of this ordering pass.</returns>
    private static Dictionary<ICustomAttributeType, string> GetAttributeConstructorOrderingKeys(IEnumerable<CustomAttribute> attributes, CancellationToken token)
    {
        Dictionary<ICustomAttributeType, string> constructors = new(ReferenceEqualityComparer.Instance);

        foreach (CustomAttribute attribute in attributes)
        {
            token.ThrowIfCancellationRequested();

            ICustomAttributeType constructor = attribute.Constructor!;

            if (!constructors.ContainsKey(constructor))
            {
                constructors.Add(constructor, TypeDescriptorComparer.Default.GetOrderKey(constructor.DeclaringType!) + constructor.Signature);
            }
        }

        return constructors;
    }
}
