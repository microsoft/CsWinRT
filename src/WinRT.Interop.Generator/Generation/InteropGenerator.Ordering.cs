// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AsmResolver.DotNet;
using WindowsRuntime.InteropGenerator.Helpers;
using WindowsRuntime.InteropGenerator.References;
using WindowsRuntime.InteropGenerator.Writers;

namespace WindowsRuntime.InteropGenerator.Generation;

/// <inheritdoc cref="InteropGenerator"/>
internal static partial class InteropGenerator
{
    /// <summary>The minimum useful attribute key batch size for parallel ordering.</summary>
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
        AttributeOrderingEntry[] entries = new AttributeOrderingEntry[attributes.Length];

        // Unlike the independent orderings below, attributes must wait for all key-preparation batches. The last batch
        // computes their ordering, and the snapshot is applied separately after all metadata readers have finished.
        MetadataCollectionOrdering attributeOrdering = MetadataCollectionOrdering.Create(module.Assembly.CustomAttributes, _ => entries
            .OrderBy(static entry => entry.ConstructorKey, StringComparer.Ordinal)
            .ThenBy(static entry => entry.Payload, ByteSequenceComparer.Instance)
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
            MetadataCollectionOrdering.Create(definitions.KeyValuePairMethods.Methods, static methods =>
                methods.OrderBy(static method => method.Name?.Value, StringComparer.Ordinal))
        ];

        int workerCount = args.GetEffectiveMaxDegreesOfParallelism();
        int batchCount = GetAttributeKeyBatchCount(attributes.Length, workerCount);
        int itemCount = orderings.Length + batchCount;
        int remainingBatches = batchCount;

        // The index range represents one job per independent collection followed by one per attribute-key batch. Both job
        // kinds share the same workers, letting collection sorts overlap with key preparation without an extra barrier.
        _ = Parallel.ForEachUnbufferedUntilFirstFailure(
            source: _ => Enumerable.Range(0, itemCount),
            token: args.Token,
            maxDegreesOfParallelism: Math.Min(workerCount, itemCount),
            body: (index, token) => ComputeOrdering(index, orderings, attributes, constructors, payloads, entries, attributeOrdering, batchCount, ref remainingBatches, token));

        args.Token.ThrowIfCancellationRequested();

        // Clearing owned collections temporarily detaches types from their module. Every name/payload
        // reader must finish before any collection is changed, even when only one worker is used.
        foreach (MetadataCollectionOrdering ordering in orderings)
        {
            ordering.Apply();
        }

        attributeOrdering.Apply();
    }

    /// <summary>
    /// Computes one independent collection ordering or prepares one batch of attribute ordering keys.
    /// </summary>
    /// <param name="index">The work-item index in the combined collection and batch range.</param>
    /// <param name="orderings">The independently computed collection orderings.</param>
    /// <param name="attributes">The attributes whose ordering keys are prepared in batches.</param>
    /// <param name="constructors">The precomputed constructor identity keys.</param>
    /// <param name="payloads">The attribute payload cache shared with the writer.</param>
    /// <param name="entries">The non-overlapping output slots for attribute ordering keys.</param>
    /// <param name="attributeOrdering">The ordering computed after all attribute keys are ready.</param>
    /// <param name="batchCount">The total number of attribute-key preparation batches.</param>
    /// <param name="remainingBatches">The shared count of unfinished batches, decremented atomically.</param>
    /// <param name="token">The worker token observing invocation cancellation and sibling failures.</param>
    private static void ComputeOrdering(
        int index,
        MetadataCollectionOrdering[] orderings,
        CustomAttribute[] attributes,
        Dictionary<ICustomAttributeType, string> constructors,
        CustomAttributePayloadCache payloads,
        AttributeOrderingEntry[] entries,
        MetadataCollectionOrdering attributeOrdering,
        int batchCount,
        ref int remainingBatches,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        // Leading indices compute sorted snapshots; the rest prepare attribute ordering keys in batches
        if (index < orderings.Length)
        {
            orderings[index].Compute();

            return;
        }

        int batchIndex = index - orderings.Length;
        int start = (int)((long)attributes.Length * batchIndex / batchCount);
        int end = (int)((long)attributes.Length * (batchIndex + 1) / batchCount);

        // Prepare constructor and payload ordering keys for this batch's non-overlapping attribute range
        for (int attributeIndex = start; attributeIndex < end; attributeIndex++)
        {
            token.ThrowIfCancellationRequested();

            CustomAttribute attribute = attributes[attributeIndex];

            entries[attributeIndex] = new(attribute, constructors[attribute.Constructor!], payloads.GetOrAdd(attribute));
        }

        // The last batch can sort attributes immediately, while independent sorts may still be running
        if (Interlocked.Decrement(ref remainingBatches) == 0)
        {
            token.ThrowIfCancellationRequested();
            attributeOrdering.Compute();
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

        // Format each constructor instance's type/signature key once, then reuse it across attributes and key batches
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

    /// <summary>
    /// An assembly attribute and the precomputed keys used to order it.
    /// </summary>
    /// <param name="attribute">The attribute to order.</param>
    /// <param name="constructorKey">The constructor identity used as the primary ordering key.</param>
    /// <param name="payload">The serialized payload used to break constructor-key ties.</param>
    private readonly struct AttributeOrderingEntry(
        CustomAttribute attribute,
        string constructorKey,
        ReadOnlyMemory<byte> payload)
    {
        /// <summary>Gets the attribute to order.</summary>
        public CustomAttribute Attribute { get; } = attribute;

        /// <summary>Gets the constructor identity used as the primary ordering key.</summary>
        public string ConstructorKey { get; } = constructorKey;

        /// <summary>Gets the serialized payload used to break constructor-key ties.</summary>
        public ReadOnlyMemory<byte> Payload { get; } = payload;
    }
}
