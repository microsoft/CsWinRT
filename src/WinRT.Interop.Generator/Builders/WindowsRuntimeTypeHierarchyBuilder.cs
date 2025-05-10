// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using AsmResolver;
using AsmResolver.DotNet;
using AsmResolver.DotNet.Code.Cil;
using AsmResolver.DotNet.Collections;
using AsmResolver.DotNet.Signatures;
using AsmResolver.PE.DotNet.Cil;
using AsmResolver.PE.DotNet.Metadata.Tables;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics.Tensors;
using System.Runtime.InteropServices;
using WindowsRuntime.InteropGenerator.Errors;
using WindowsRuntime.InteropGenerator.Factories;
using WindowsRuntime.InteropGenerator.References;
using static AsmResolver.PE.DotNet.Cil.CilOpCodes;

namespace WindowsRuntime.InteropGenerator.Builders;

/// <summary>
/// A builder for the <c>WindowsRuntimeTypeHierarchy</c> type.
/// </summary>
internal static partial class WindowsRuntimeTypeHierarchyBuilder
{
    /// <summary>
    /// Creates a new type definition for the <c>WindowsRuntimeTypeHierarchy</c> type.
    /// </summary>
    /// <param name="typeHierarchyEntries">The type hierarchy entries for the application.</param>
    /// <param name="wellKnownInteropDefinitions">The <see cref="WellKnownInteropDefinitions"/> instance to use.</param>
    /// <param name="wellKnownInteropReferences">The <see cref="WellKnownInteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    /// <param name="lookupType">The resulting <see cref="TypeDefinition"/>.</param>
    public static unsafe void Lookup(
        SortedDictionary<string, string> typeHierarchyEntries,
        WellKnownInteropDefinitions wellKnownInteropDefinitions,
        WellKnownInteropReferences wellKnownInteropReferences,
        ModuleDefinition module,
        out TypeDefinition lookupType)
    {
        ValuesRva(
            typeHierarchyEntries,
            wellKnownInteropDefinitions,
            module,
            out SortedDictionary<string, ValueInfo> typeHierarchyValues,
            out FieldDefinition valuesRvaField);

        KeysRva(
            typeHierarchyEntries,
            typeHierarchyValues,
            wellKnownInteropDefinitions,
            module,
            out int bucketSize,
            out Dictionary<int, int> chainOffsets,
            out FieldDefinition keysRvaField);

        BucketsRva(
            bucketSize,
            chainOffsets,
            wellKnownInteropDefinitions,
            module,
            out FieldDefinition bucketsRvaField);

        // We're declaring a 'public static class' type
        lookupType = new TypeDefinition(
            ns: "WindowsRuntime.Interop"u8,
            name: "WindowsRuntimeTypeHierarchy"u8,
            attributes: TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
            baseType: module.CorLibTypeFactory.Object.ToTypeDefOrRef());

        module.TopLevelTypes.Add(lookupType);

        // Emit the actual lookup method, using the RVA fields just declared
        TryGetBaseRuntimeClassName(
            typeHierarchyEntries,
            bucketSize,
            wellKnownInteropDefinitions,
            wellKnownInteropReferences,
            module,
            bucketsRvaField,
            keysRvaField,
            valuesRvaField,
            out MethodDefinition tryGetBaseRuntimeClassNameMethod);

        lookupType.Methods.Add(tryGetBaseRuntimeClassNameMethod);

        // Emit the fast lookup method for following base types
        TryGetNextBaseRuntimeClassName(
            wellKnownInteropReferences,
            module,
            valuesRvaField,
            out MethodDefinition tryGetNextBaseRuntimeClassNameMethod);

        lookupType.Methods.Add(tryGetNextBaseRuntimeClassNameMethod);
    }

    /// <summary>
    /// Creates the 'Values' RVA field for the type hierarchy.
    /// </summary>
    /// <param name="typeHierarchyEntries">The type hierarchy entries for the application.</param>
    /// <param name="wellKnownInteropDefinitions">The <see cref="WellKnownInteropDefinitions"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    /// <param name="typeHierarchyValues">The mapping of infos of all type hierarchy values.</param>
    /// <param name="valuesRvaField">The resulting 'Values' RVA field.</param>
    private static void ValuesRva(
        SortedDictionary<string, string> typeHierarchyEntries,
        WellKnownInteropDefinitions wellKnownInteropDefinitions,
        ModuleDefinition module,
        out SortedDictionary<string, ValueInfo> typeHierarchyValues,
        out FieldDefinition valuesRvaField)
    {
        typeHierarchyValues = [];

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
            if (value.Length > ushort.MaxValue)
            {
                throw WellKnownInteropExceptions.RuntimeClassNameTooLong(value);
            }

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

        if (valuesRvaBuffer.WrittenCount >= ushort.MaxValue)
        {
            throw WellKnownInteropExceptions.RuntimeClassNameLookupSizeLimitExceeded();
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
        valuesRvaField = new(
            name: "TypeHierarchyLookupValues"u8,
            attributes: FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly | FieldAttributes.HasFieldRva,
            fieldType: valuesRvaDataType.ToTypeSignature())
        {
            FieldRva = new DataSegment(valuesRvaBuffer.WrittenSpan.ToArray())
        };

        // Add the RVA field to the parent type
        wellKnownInteropDefinitions.RvaFields.Fields.Add(valuesRvaField);
    }

    /// <summary>
    /// Creates the 'Keys' RVA field for the type hierarchy.
    /// </summary>
    /// <param name="typeHierarchyEntries">The type hierarchy entries for the application.</param>
    /// <param name="typeHierarchyValues">The mapping of infos of all type hierarchy values.</param>
    /// <param name="wellKnownInteropDefinitions">The <see cref="WellKnownInteropDefinitions"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    /// <param name="bucketSize">The resulting bucket size.</param>
    /// <param name="chainOffsets">The mapping of offsets of each chain.</param>
    /// <param name="keysRvaField">The resulting 'Keys' RVA field.</param>
    private static void KeysRva(
        SortedDictionary<string, string> typeHierarchyEntries,
        SortedDictionary<string, ValueInfo> typeHierarchyValues,
        WellKnownInteropDefinitions wellKnownInteropDefinitions,
        ModuleDefinition module,
        out int bucketSize,
        out Dictionary<int, int> chainOffsets,
        out FieldDefinition keysRvaField)
    {
        // Set of known prime numbers, in ascending order
        scoped ReadOnlySpan<int> primeNumbers =
        [
            3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
            1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
            17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
            187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
            1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
        ];

        int startingBucketSize = 0;

        // Find the smallest prime number greater than the number of values
        for (int i = 0; i < primeNumbers.Length && startingBucketSize < typeHierarchyEntries.Count; i++)
        {
            startingBucketSize = primeNumbers[i];
        }

        // Hash all keys to avoid wasting time re-hashing them multiple times
        Dictionary<string, int> typeHierarchyKeyHashes = typeHierarchyEntries
            .Keys
            .ToDictionary(keySelector: static key => key, elementSelector: static key => ComputeReadOnlySpanHash(key));

        int numberOfKeysPerChain = int.MaxValue;

        bucketSize = 0;

        // Search up to some maximum size for the best bucket size we can use for the application
        for (int i = startingBucketSize; i < 2333; i++)
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
                bucketSize = i;
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
            List<string> chain = CollectionsMarshal.GetValueRefOrAddDefault(keyChains, (int)((uint)hash % (uint)bucketSize), out _) ??= [];

            // Add the current key to the chain
            chain.Add(key);
        }

        using ArrayPoolBufferWriter<byte> keysRvaBuffer = new();

        // We also need to track the offset of the start of each chain, to reference it from the bucket entries
        chainOffsets = [];

        // We need to go through indices in order to ensure the results are deterministic
        foreach ((int bucketIndex, List<string> chain) in keyChains.OrderBy(static pair => pair.Key))
        {
            // Track the RVA offset of the current chain
            chainOffsets[bucketIndex] = keysRvaBuffer.WrittenCount;

            // Process all keys in the chain (they might all point to different values).
            // That is, they are in the same chain just because the hash collided.
            foreach (string key in chain)
            {
                if (key.Length > ushort.MaxValue)
                {
                    throw WellKnownInteropExceptions.RuntimeClassNameTooLong(key);
                }

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
        keysRvaField = new(
            name: "TypeHierarchyLookupKeys"u8,
            attributes: FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly | FieldAttributes.HasFieldRva,
            fieldType: keysRvaDataType.ToTypeSignature())
        {
            FieldRva = new DataSegment(keysRvaBuffer.WrittenSpan.ToArray())
        };

        // Add the RVA field to the parent type
        wellKnownInteropDefinitions.RvaFields.Fields.Add(keysRvaField);
    }

    /// <summary>
    /// Creates the 'Buckets' RVA field for the type hierarchy.
    /// </summary>
    /// <param name="bucketSize">The resulting bucket size.</param>
    /// <param name="chainOffsets">The mapping of offsets of each chain.</param>
    /// <param name="wellKnownInteropDefinitions">The <see cref="WellKnownInteropDefinitions"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    /// <param name="bucketsRvaField">The resulting 'Buckets' RVA field.</param>
    private static void BucketsRva(
        int bucketSize,
        Dictionary<int, int> chainOffsets,
        WellKnownInteropDefinitions wellKnownInteropDefinitions,
        ModuleDefinition module,
        out FieldDefinition bucketsRvaField)
    {
        using ArrayPoolBufferWriter<byte> bucketsRvaBuffer = new(initialCapacity: bucketSize);

        // Fill the buckets RVA data with the right offsets (or '-1' for no matches)
        for (int i = 0; i < bucketSize; i++)
        {
            bucketsRvaBuffer.Write(chainOffsets.TryGetValue(i, out int offset) ? offset : -1);
        }

        // Define the data type for 'Buckets' data
        TypeDefinition bucketsRvaDataType = new(
            ns: null,
            name: $"TypeHierarchyLookupBucketsRvaData(Size={bucketsRvaBuffer.WrittenCount}|Align=4)",
            attributes: TypeAttributes.NestedAssembly | TypeAttributes.ExplicitLayout | TypeAttributes.Sealed,
            baseType: module.DefaultImporter.ImportType(typeof(ValueType)))
        {
            ClassLayout = new ClassLayout(packingSize: 4, classSize: (uint)bucketsRvaBuffer.WrittenCount)
        };

        // Nest the type under the '<RvaFields>' type
        wellKnownInteropDefinitions.RvaFields.NestedTypes.Add(bucketsRvaDataType);

        // Create the RVA field for the 'Buckets' data
        bucketsRvaField = new(
            name: "TypeHierarchyLookupBuckets"u8,
            attributes: FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.InitOnly | FieldAttributes.HasFieldRva,
            fieldType: bucketsRvaDataType.ToTypeSignature())
        {
            FieldRva = new DataSegment(bucketsRvaBuffer.WrittenSpan.ToArray())
        };

        // Add the RVA field to the parent type
        wellKnownInteropDefinitions.RvaFields.Fields.Add(bucketsRvaField);
    }

    /// <summary>
    /// Creates the 'TryGetBaseRuntimeClassName' method for the type hierarchy.
    /// </summary>
    /// <param name="typeHierarchyEntries">The type hierarchy entries for the application.</param>
    /// <param name="bucketSize">The resulting bucket size.</param>
    /// <param name="wellKnownInteropDefinitions">The <see cref="WellKnownInteropDefinitions"/> instance to use.</param>
    /// <param name="wellKnownInteropReferences">The <see cref="WellKnownInteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    /// <param name="bucketsRvaField">The 'Buckets' RVA field (created by <see cref="BucketsRva"/>).</param>
    /// <param name="keysRvaField">The 'Keys' RVA field (created by <see cref="KeysRva"/>).</param>
    /// <param name="valuesRvaField">The 'Values' RVA field (created by <see cref="ValuesRva"/>).</param>
    /// <param name="tryGetBaseRuntimeClassNameMethod">The resulting 'TryGetBaseRuntimeClassName' method.</param>
    private static void TryGetBaseRuntimeClassName(
        SortedDictionary<string, string> typeHierarchyEntries,
        int bucketSize,
        WellKnownInteropDefinitions wellKnownInteropDefinitions,
        WellKnownInteropReferences wellKnownInteropReferences,
        ModuleDefinition module,
        FieldDefinition bucketsRvaField,
        FieldDefinition keysRvaField,
        FieldDefinition valuesRvaField,
        out MethodDefinition tryGetBaseRuntimeClassNameMethod)
    {
        // Define the 'TryGetBaseRuntimeClassName' method as follows:
        //
        // public static bool TryGetBaseRuntimeClassName(
        //     scoped ReadOnlySpan<char> runtimeClassName,
        //     out ReadOnlySpan<char> baseRuntimeClassName,
        //     out int nextBaseRuntimeClassNameIndex)
        tryGetBaseRuntimeClassNameMethod = new MethodDefinition(
            name: "TryGetBaseRuntimeClassName"u8,
            attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
            signature: MethodSignature.CreateStatic(
                returnType: module.CorLibTypeFactory.Boolean,
                parameterTypes: [
                    wellKnownInteropReferences.ReadOnlySpanChar.ToTypeSignature(isValueType: true).Import(module),
                    wellKnownInteropReferences.ReadOnlySpanChar.ToTypeSignature(isValueType: true).Import(module).MakeByReferenceType(),
                    module.CorLibTypeFactory.Int32.MakeByReferenceType()]))
        {
            // Both 'baseRuntimeClassName' and 'nextBaseRuntimeClassNameIndex' are '[out]' parameters.
            // We also need to emit '[ScopedRef]' for the 'baseRuntimeClassName' parameter (for 'scoped').
            ParameterDefinitions =
            {
                new ParameterDefinition(sequence: 1, name: null, attributes: default)
                {
                    CustomAttributes = { InteropCustomAttributeFactory.ScopedRef(module) }
                },
                new ParameterDefinition(sequence: 2, name: null, attributes: ParameterAttributes.Out),
                new ParameterDefinition(sequence: 3, name: null, attributes: ParameterAttributes.Out)
            }
        };

        // Import 'MemoryMarshal.CreateReadOnlySpan<char>'
        MethodSpecification createReadOnlySpanMethod = module.DefaultImporter
            .ImportType(typeof(MemoryMarshal))
            .CreateMemberReference("CreateReadOnlySpan", MethodSignature.CreateStatic(
                returnType: module.DefaultImporter.ImportType(typeof(ReadOnlySpan<>)).MakeGenericInstanceType(new GenericParameterSignature(GenericParameterType.Method, 0)),
                genericParameterCount: 1,
                parameterTypes: [
                    new GenericParameterSignature(GenericParameterType.Method, 0).MakeByReferenceType(),
                    module.CorLibTypeFactory.Int32]))
            .MakeGenericInstanceMethod(module.CorLibTypeFactory.Char)
            .ImportWith(module.DefaultImporter);

        // Compute the range check arguments
        int minLength = typeHierarchyEntries.Keys.Min(static key => key.Length);
        int maxLength = typeHierarchyEntries.Keys.Max(static key => key.Length);

        // Labels for jumps
        CilInstruction ldc_I4_0_returnFalse = new(Ldc_I4_0);
        CilInstruction ldsflda_rangeCheckSuccess = new(Ldsflda, bucketsRvaField);
        CilInstruction ldloc_1_loopStart = new(Ldloc_1);

        // Extract the parameters:
        //   [0]: 'scoped ReadOnlySpan<char>' (the runtime class name)
        //   [1]: 'ReadOnlySpan<char>&' (the base runtime class name)
        //   [2]: 'int&' (the next base runtime class name index)
        Parameter arg_0_runtimeClassName = tryGetBaseRuntimeClassNameMethod.Parameters[0];
        Parameter arg_1_baseRuntimeClassName = tryGetBaseRuntimeClassNameMethod.Parameters[1];
        Parameter arg_2_nextBaseRuntimeClassNameIndex = tryGetBaseRuntimeClassNameMethod.Parameters[2];

        // Declare the following variables:
        //   [0]: 'int' (the bucket index)
        //   [1]: 'byte&' (the reference into the 'Keys' RVA field)
        //   [2]: 'int' (the length of the current key candidate)
        //   [3]: 'int' (the offset to the matching value in the 'Values' RVA field)
        //   [4]: 'ReadOnlySpan<char>' (the current key candidate)
        //   [5]: 'byte&' (the reference into the 'Values' RVA field)
        //   [6]: 'int' (the length of the matching value)
        CilLocalVariable loc_0_bucketIndex = new(module.CorLibTypeFactory.Int32);
        CilLocalVariable loc_1_keysRef = new(module.CorLibTypeFactory.Byte.MakeByReferenceType());
        CilLocalVariable loc_2_keyLength = new(module.CorLibTypeFactory.Int32);
        CilLocalVariable loc_3_valueOffset = new(module.CorLibTypeFactory.Int32);
        CilLocalVariable loc_4_keySpan = new(wellKnownInteropReferences.ReadOnlySpanChar.ToTypeSignature(isValueType: true).Import(module));
        CilLocalVariable loc_5_valuesRef = new(module.CorLibTypeFactory.Byte.MakeByReferenceType());
        CilLocalVariable loc_6_valueLength = new(module.CorLibTypeFactory.Int32);

        // Create a method body for the 'TryGetBaseRuntimeClassName' method
        tryGetBaseRuntimeClassNameMethod.CilMethodBody = new CilMethodBody(tryGetBaseRuntimeClassNameMethod)
        {
            LocalVariables =
            {
                loc_0_bucketIndex,
                loc_1_keysRef,
                loc_2_keyLength,
                loc_3_valueOffset,
                loc_4_keySpan,
                loc_5_valuesRef,
                loc_6_valueLength
            },
            Instructions =
            {
                // Set the 'out' parameters to default
                { Ldarg_1 },
                { Initobj, wellKnownInteropReferences.ReadOnlySpanChar.Import(module) },
                { Ldarg_2 },
                { Ldc_I4_0 },
                { Stind_I4 },

                // Emit the range checks
                { Ldarga_S, arg_0_runtimeClassName },
                { Call, wellKnownInteropReferences.ReadOnlySpanCharget_Length.Import(module) },
                { CilInstruction.CreateLdcI4(minLength) },
                { Blt, ldc_I4_0_returnFalse.CreateLabel() },
                { Ldarga_S, arg_0_runtimeClassName },
                { Call, wellKnownInteropReferences.ReadOnlySpanCharget_Length.Import(module) },
                { CilInstruction.CreateLdcI4(maxLength) },
                { Bgt_S, ldc_I4_0_returnFalse.CreateLabel() },

                // Compute the hash and get the bucket index
                { ldsflda_rangeCheckSuccess },
                { Ldarg_0 },
                { Call, wellKnownInteropDefinitions.InteropImplementationDetails.GetMethod("ComputeReadOnlySpanHash"u8) },
                { Ldc_I4, bucketSize },
                { Rem_Un },
                { Ldc_I4_4 },
                { Mul },
                { Add },
                { Ldind_I4 },
                { Stloc_0 },
                { Ldloc_0 },
                { Ldc_I4_0 },
                { Blt_S, ldc_I4_0_returnFalse.CreateLabel() },

                // Get the reference to the start of the keys RVA field data, for this bucket
                { Ldsflda, keysRvaField },
                { Ldloc_0 },
                { Add },
                { Stloc_1 },

                // Start looping through conflicting keys for this chain, stop if we reached the end
                { ldloc_1_loopStart },
                { Ldind_U2 },
                { Stloc_2 },
                { Ldloc_2 },
                { Brfalse_S, ldc_I4_0_returnFalse.CreateLabel() },
                { Ldloc_1 },
                { Ldc_I4_2 },
                { Add },
                { Stloc_1 },

                // Load the value offset for the current key, and save it for later
                { Ldloc_1 },
                { Ldind_U2 },
                { Stloc_3 },
                { Ldloc_1 },
                { Ldc_I4_2 },
                { Add },
                { Stloc_1 },

                // Create the span, advance past it, compare it, and repeat if we don't have a match
                { Ldloc_1 },
                { Ldloc_2 },
                { Call, createReadOnlySpanMethod },
                { Stloc_S, loc_4_keySpan },
                { Ldloc_1 },
                { Ldloc_2 },
                { Add },
                { Stloc_1 },
                { Ldarg_0 },
                { Ldloc_S, loc_4_keySpan },
                { Call, wellKnownInteropReferences.MemoryExtensionsSequenceEqualChar.Import(module) },
                { Brfalse_S, ldloc_1_loopStart.CreateLabel() },

                // Read the matching value and the index of the next parent from the 'Values' RVA field and set the arguments
                { Ldsflda, valuesRvaField },
                { Ldloc_3 },
                { Add },
                { Stloc_S, loc_5_valuesRef },
                { Ldloc_S, loc_5_valuesRef },
                { Ldind_U2 },
                { Stloc_S, loc_6_valueLength },
                { Ldloc_S, loc_5_valuesRef },
                { Ldc_I4_2 },
                { Add },
                { Stloc_S, loc_5_valuesRef },
                { Ldarg_2 },
                { Ldloc_S, loc_5_valuesRef },
                { Ldind_U2 },
                { Stind_I4 },
                { Ldloc_S, loc_5_valuesRef },
                { Ldc_I4_2 },
                { Add },
                { Stloc_S, loc_5_valuesRef },
                { Ldarg_1 },
                { Ldloc_S, loc_5_valuesRef },
                { Ldloc_S, loc_6_valueLength },
                { Call, createReadOnlySpanMethod },
                { Stobj, wellKnownInteropReferences.ReadOnlySpanChar.Import(module) },

                // Success epilogue
                { Ldc_I4_1 },
                { Ret },

                // Shared failure epilogue
                { ldc_I4_0_returnFalse },
                { Ret },
            }
        };
    }

    /// <summary>
    /// Creates the 'TryGetNextBaseRuntimeClassName' method for the type hierarchy.
    /// </summary>
    /// <param name="wellKnownInteropReferences">The <see cref="WellKnownInteropReferences"/> instance to use.</param>
    /// <param name="module">The interop module being built.</param>
    /// <param name="valuesRvaField">The 'Values' RVA field (created by <see cref="ValuesRva"/>).</param>
    /// <param name="tryGetNextBaseRuntimeClassNameMethod">The resulting 'TryGetNextBaseRuntimeClassName' method.</param>
    private static void TryGetNextBaseRuntimeClassName(
        WellKnownInteropReferences wellKnownInteropReferences,
        ModuleDefinition module,
        FieldDefinition valuesRvaField,
        out MethodDefinition tryGetNextBaseRuntimeClassNameMethod)
    {
        // Define the 'TryGetNextBaseRuntimeClassName' method as follows:
        //
        // public static bool TryGetNextBaseRuntimeClassName(
        //     int baseRuntimeClassNameIndex,
        //     out ReadOnlySpan<char> baseRuntimeClassName,
        //     out int nextBaseRuntimeClassNameIndex)
        tryGetNextBaseRuntimeClassNameMethod = new MethodDefinition(
            name: "TryGetNextBaseRuntimeClassName"u8,
            attributes: MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Static,
            signature: MethodSignature.CreateStatic(
                returnType: module.CorLibTypeFactory.Boolean,
                parameterTypes: [
                    module.CorLibTypeFactory.Int32,
                    wellKnownInteropReferences.ReadOnlySpanChar.ToTypeSignature(isValueType: true).Import(module).MakeByReferenceType(),
                    module.CorLibTypeFactory.Int32.MakeByReferenceType()]))
        {
            // Both 'baseRuntimeClassName' and 'nextBaseRuntimeClassNameIndex' are '[out]' parameters
            ParameterDefinitions =
            {
                new ParameterDefinition(sequence: 2, name: null, attributes: ParameterAttributes.Out),
                new ParameterDefinition(sequence: 3, name: null, attributes: ParameterAttributes.Out)
            }
        };

        // Import 'MemoryMarshal.CreateReadOnlySpan<char>'
        MethodSpecification createReadOnlySpanMethod = module.DefaultImporter
            .ImportType(typeof(MemoryMarshal))
            .CreateMemberReference("CreateReadOnlySpan", MethodSignature.CreateStatic(
                returnType: module.DefaultImporter.ImportType(typeof(ReadOnlySpan<>)).MakeGenericInstanceType(new GenericParameterSignature(GenericParameterType.Method, 0)),
                genericParameterCount: 1,
                parameterTypes: [
                    new GenericParameterSignature(GenericParameterType.Method, 0).MakeByReferenceType(),
                    module.CorLibTypeFactory.Int32]))
            .MakeGenericInstanceMethod(module.CorLibTypeFactory.Char)
            .ImportWith(module.DefaultImporter);

        // Labels for jumps
        CilInstruction ldc_I4_0_returnFalse = new(Ldc_I4_0);

        // Declare the following variables:
        //   [0]: 'byte&' (the reference into the 'Values' RVA field)
        //   [1]: 'int' (the length of the matching value)
        CilLocalVariable loc_0_valuesRef = new(module.CorLibTypeFactory.Byte.MakeByReferenceType());
        CilLocalVariable loc_1_valueLength = new(module.CorLibTypeFactory.Int32);

        // Create a method body for the 'TryGetNextBaseRuntimeClassName' method
        tryGetNextBaseRuntimeClassNameMethod.CilMethodBody = new CilMethodBody(tryGetNextBaseRuntimeClassNameMethod)
        {
            LocalVariables = { loc_0_valuesRef, loc_1_valueLength },
            Instructions =
            {
                // Set the 'out' parameters to default
                { Ldarg_1 },
                { Initobj, wellKnownInteropReferences.ReadOnlySpanChar.Import(module) },
                { Ldarg_2 },
                { Ldc_I4_0 },
                { Stind_I4 },

                // Emit the range check
                { Ldarg_0 },
                { Ldc_I4_0 },
                { Blt_S, ldc_I4_0_returnFalse.CreateLabel() },

                // Read the matching value and the index of the next parent from the 'Values' RVA field and set the arguments
                { Ldsflda, valuesRvaField },
                { Ldarg_0 },
                { Add },
                { Stloc_0 },
                { Ldloc_0 },
                { Ldind_U2 },
                { Stloc_1 },
                { Ldloc_0 },
                { Ldc_I4_2 },
                { Add },
                { Stloc_0 },
                { Ldarg_2 },
                { Ldloc_0 },
                { Ldind_U2 },
                { Stind_I4 },
                { Ldloc_0 },
                { Ldc_I4_2 },
                { Add },
                { Stloc_0 },
                { Ldarg_1 },
                { Ldloc_0 },
                { Ldloc_1 },
                { Call, createReadOnlySpanMethod },
                { Stobj, wellKnownInteropReferences.ReadOnlySpanChar.Import(module) },

                // Success epilogue
                { Ldc_I4_1 },
                { Ret },

                // Shared failure epilogue
                { ldc_I4_0_returnFalse },
                { Ret },
            }
        };
    }

    /// <summary>
    /// Computes a deterministic hash of an input span.
    /// </summary>
    /// <param name="span">The input span.</param>
    /// <returns>The hash of <paramref name="span"/>.</returns>
    /// <remarks>
    /// This implementation must be identical to the one emitted from <see cref="Factories.WellKnownMemberDefinitionFactory.ComputeReadOnlySpanHash"/>
    /// </remarks>
    private static int ComputeReadOnlySpanHash(ReadOnlySpan<char> span)
    {
        uint hash = 2166136261u;

        foreach (char c in span)
        {
            hash = (c ^ hash) * 16777619;
        }

        return (int)hash;
    }

    /// <summary>
    /// Info on a given type hierarchy value.
    /// </summary>
    private sealed class ValueInfo
    {
        /// <summary>
        /// The index of the value in the final set.
        /// </summary>
        public required int Index { get; init; }

        /// <summary>
        /// The starting offset of the value in its RVA field.
        /// </summary>
        public int RvaOffset { get; set; }
    }
}
