// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.PE.DotNet.Metadata.Tables;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics.Tensors;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropGenerator.References;

namespace WindowsRuntime.InteropGenerator.Builders;

/// <summary>
/// A builder for the <c>WindowsRuntimeTypeHierarchy</c> type.
/// </summary>
internal static partial class TypeHierarchyBuilder
{
    /// <summary>
    /// Set of known prime numbers, in ascending order.
    /// </summary>
    private static ReadOnlySpan<int> PrimeNumbers =>
    [
        3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
        1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
        17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
        187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
        1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
    ];

    /// <summary>
    /// Creates a new type definition for the implementation of the COM interface entries for a managed type.
    /// </summary>
    /// <param name="ns">The namespace for the type.</param>
    /// <param name="name">The type name.</param>
    /// <param name="entriesFieldType">The <see cref="TypeDefinition"/> for the type of entries field.</param>
    /// <param name="module">The module that will contain the type being created.</param>
    /// <param name="implType">The resulting implementation type.</param>
    /// <param name="implTypes">The set of vtable accessors to use for each entry.</param>
    public static unsafe void Lookup(
        SortedDictionary<string, string> typeHierarchyEntries,
        WellKnownInteropDefinitions wellKnownInteropDefinitions,
        ModuleDefinition module,
        out TypeDefinition lookupType)
    {
        // We're declaring an 'internal static class' type
        lookupType = new TypeDefinition(
            ns: "WindowsRuntime.Interop"u8,
            name: "WindowsRuntimeTypeHierarchy"u8,
            attributes: TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

        module.TopLevelTypes.Add(lookupType);

        SortedDictionary<string, ValueInfo> typeHierarchyValues = [];
        int valueIndex = 0;

        // Add all values with a unique, progressive index to each of them
        foreach (string value in typeHierarchyEntries.Values)
        {
            if (typeHierarchyValues.TryAdd(value, new ValueInfo { Index = valueIndex }))
            {
                valueIndex++;
            }
        }

        using ArrayPoolBufferWriter<byte> valuesRvaBuffer = new();

        // Prepare the buffer for the 'Values' RVA field
        foreach ((string value, ValueInfo info) in typeHierarchyValues)
        {
            // Update the RVA offset for this value, for later
            info.RvaOffset = valuesRvaBuffer.WrittenCount;

            // Write the value length (in characters)
            valuesRvaBuffer.Write((ushort)value.Length);

            // Get the index of the parent, if available
            int parentIndex = typeHierarchyEntries.TryGetValue(value, out string? parentValue)
                ? typeHierarchyValues[parentValue].Index
                : -1;

            // Write the parent index
            valuesRvaBuffer.Write((ushort)parentIndex);

            // Write the value right after that
            valuesRvaBuffer.Write(MemoryMarshal.AsBytes(value.AsSpan()));
        }

        // Round up the size to a multiple of 2
        while (valuesRvaBuffer.WrittenCount % 2 != 0)
        {
            valuesRvaBuffer.Write((byte)0);
        }

        // Define the data type for 'Values' data
        TypeDefinition valuesRvaDataType = new(
            ns: null,
            name: $"TypeHierarchyLookupValuesRvaData(Size={valuesRvaBuffer.WrittenCount}|Align=2)",
            attributes: TypeAttributes.NestedAssembly | TypeAttributes.ExplicitLayout | TypeAttributes.Sealed,
            baseType: module.DefaultImporter.ImportType(typeof(ValueType)))
        {
            ClassLayout = new ClassLayout(packingSize: 2, classSize: (uint)valuesRvaBuffer.WrittenCount)
        };

        // Nest the type under the '<RvaFields>' type
        wellKnownInteropDefinitions.RvaFields.NestedTypes.Add(valuesRvaDataType);

        // Create the RVA field for the 'Values' data
        FieldDefinition valuesRvaField = new(
            name: "TypeHierarchyLookupValues"u8,
            attributes: FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly | FieldAttributes.HasFieldRva,
            fieldType: valuesRvaDataType.ToTypeSignature())
        {
            FieldRva = new DataSegment(valuesRvaBuffer.WrittenSpan.ToArray())
        };

        // Add the RVA field to the parent type
        wellKnownInteropDefinitions.RvaFields.Fields.Add(valuesRvaField);

        int bucketSize = 0;

        // Find the smallest prime number greater than the number of values
        for (int i = 0; i < PrimeNumbers.Length && bucketSize < typeHierarchyEntries.Count; i++)
        {
            bucketSize = PrimeNumbers[i];
        }

        // Hash all keys to avoid wasting time re-hashing them multiple times
        Dictionary<string, int> typeHierarchyKeyHashes = typeHierarchyEntries
            .Keys
            .ToDictionary(keySelector: static key => key, elementSelector: static key => ComputeReadOnlySpanHash(key));

        int numberOfKeysPerChain = int.MaxValue;
        int bestBucketSize = 0;

        // Search up to some maximum size for the best bucket size we can use for the application
        for (int i = bucketSize; i < 2333; i++)
        {
            using SpanOwner<int> buckets = SpanOwner<int>.Allocate(i, AllocationMode.Clear);

            // Increment the number of hits for each key
            foreach ((_, int hash) in typeHierarchyKeyHashes)
            {
                int currentNumberOfKeysPerChain = ++buckets.Span[(int)((uint)hash % (uint)i)];

                // If for sure this bucket can't be better than the current best, stop right away
                if (currentNumberOfKeysPerChain >= numberOfKeysPerChain)
                {
                    break;
                }
            }

            // Find the maximum number of keys that would go in a single chain
            int maxNumberOfKeysPerChain = TensorPrimitives.Max(buckets.Span);

            // If we found a new best bucket size, track it
            if (maxNumberOfKeysPerChain < numberOfKeysPerChain)
            {
                numberOfKeysPerChain = maxNumberOfKeysPerChain;
                bestBucketSize = i;
            }

            // Stop if we reached 3 keys per chain, as that's good enough and allows us to
            // minimize the size. Collisions scale very poorly with size (ie. a size of ~1000
            // is enough for 2 collisions, but you need ~30000 to go down to no collisions),
            // so limiting to 3 keys per chain is a good speed/size compromise.
            if (numberOfKeysPerChain <= 3)
            {
                break;
            }
        }

        Dictionary<int, List<string>> keyChains = [];

        // Precompute the keys in each chain, so we can figure out the contents of the other RVA fields
        foreach ((string key, int hash) in typeHierarchyKeyHashes)
        {
            List<string> chain = CollectionsMarshal.GetValueRefOrAddDefault(keyChains, (int)((uint)hash % (uint)bestBucketSize), out _) ??= [];

            // Add the current key to the chain
            chain.Add(key);
        }

        using ArrayPoolBufferWriter<byte> keysRvaBuffer = new();

        // We also need to track the offset of the start of each chain, to reference it from the bucket entries
        Dictionary<int, int> chainOffsets = [];

        // We need to go through indices in order to ensure the results are deterministic
        foreach ((int bucketIndex, List<string> chain) in keyChains.OrderBy(static pair => pair.Key))
        {
            // Track the RVA offset of the current chain
            chainOffsets[bucketIndex] = keysRvaBuffer.WrittenCount;

            // Process all keys in the chain (they might all point to different values).
            // That is, they are in the same chain just because the hash collided.
            foreach (string key in chain)
            {
                // The format is as follows:
                //   - (2 bytes) length of the key
                //   - (2 bytes) RVA offset of the value
                //   - Key data (UTF-16LE encoded)
                keysRvaBuffer.Write((ushort)key.Length);
                keysRvaBuffer.Write((ushort)typeHierarchyValues[typeHierarchyEntries[key]].RvaOffset);
                keysRvaBuffer.Write(key.AsSpan());
            }

            // Append a '\0' character to indicate the end of a chain, so callers know when to stop
            keysRvaBuffer.Write('\0');
        }

        // Round up the size to a multiple of 2
        while (keysRvaBuffer.WrittenCount % 2 != 0)
        {
            keysRvaBuffer.Write((byte)0);
        }

        // Define the data type for 'Keys' data
        TypeDefinition keysRvaDataType = new(
            ns: null,
            name: $"TypeHierarchyLookupKeysRvaData(Size={keysRvaBuffer.WrittenCount}|Align=2)",
            attributes: TypeAttributes.NestedAssembly | TypeAttributes.ExplicitLayout | TypeAttributes.Sealed,
            baseType: module.DefaultImporter.ImportType(typeof(ValueType)))
        {
            ClassLayout = new ClassLayout(packingSize: 2, classSize: (uint)keysRvaBuffer.WrittenCount)
        };

        // Nest the type under the '<RvaFields>' type
        wellKnownInteropDefinitions.RvaFields.NestedTypes.Add(keysRvaDataType);

        // Create the RVA field for the 'Keys' data
        FieldDefinition keysRvaField = new(
            name: "TypeHierarchyLookupKeys"u8,
            attributes: FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly | FieldAttributes.HasFieldRva,
            fieldType: keysRvaDataType.ToTypeSignature())
        {
            FieldRva = new DataSegment(keysRvaBuffer.WrittenSpan.ToArray())
        };

        // Add the RVA field to the parent type
        wellKnownInteropDefinitions.RvaFields.Fields.Add(keysRvaField);

        using ArrayPoolBufferWriter<byte> bucketsRvaBuffer = new(initialCapacity: bestBucketSize);

        // Fill the buckets RVA data with the right offsets (or '-1' for no matches)
        for (int i = 0; i < bestBucketSize; i++)
        {
            bucketsRvaBuffer.Write((short)(chainOffsets.TryGetValue(i, out int offset) ? offset : -1));
        }

        // Define the data type for 'Buckets' data
        TypeDefinition bucketsRvaDataType = new(
            ns: null,
            name: $"TypeHierarchyLookupBucketsRvaData(Size={bucketsRvaBuffer.WrittenCount}|Align=2)",
            attributes: TypeAttributes.NestedAssembly | TypeAttributes.ExplicitLayout | TypeAttributes.Sealed,
            baseType: module.DefaultImporter.ImportType(typeof(ValueType)))
        {
            ClassLayout = new ClassLayout(packingSize: 2, classSize: (uint)bucketsRvaBuffer.WrittenCount)
        };

        // Nest the type under the '<RvaFields>' type
        wellKnownInteropDefinitions.RvaFields.NestedTypes.Add(bucketsRvaDataType);

        // Create the RVA field for the 'Buckets' data
        FieldDefinition bucketsRvaField = new(
            name: "TypeHierarchyLookupBuckets"u8,
            attributes: FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly | FieldAttributes.HasFieldRva,
            fieldType: bucketsRvaDataType.ToTypeSignature())
        {
            FieldRva = new DataSegment(bucketsRvaBuffer.WrittenSpan.ToArray())
        };

        // Add the RVA field to the parent type
        wellKnownInteropDefinitions.RvaFields.Fields.Add(bucketsRvaField);
    }

    static int ComputeReadOnlySpanHash(ReadOnlySpan<char> s)
    {
        uint num = 2166136261u;
        int num2 = 0;
        while (num2 < s.Length)
        {
            num = (s[num2] ^ num) * 16777619;
            num2++;
        }
        return (int)num;
    }
}

file sealed class ValueInfo
{
    public required int Index { get; init; }

    public int RvaOffset { get; set; }
}
