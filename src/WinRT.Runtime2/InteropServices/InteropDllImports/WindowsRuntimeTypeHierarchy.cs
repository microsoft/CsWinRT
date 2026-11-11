// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CS1573

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides fast lookup for base runtime class names for derived Windows Runtime classes.
/// </summary>
internal static class WindowsRuntimeTypeHierarchy
{
    /// <summary>
    /// Tries to find the runtime class name of the base class of a given Windows Runtime class.
    /// </summary>
    /// <param name="runtimeClassName">The runtime class name of the input Windows Runtime type.</param>
    /// <param name="baseRuntimeClassName">The resulting runtime class name of the base type for <paramref name="baseRuntimeClassName"/>.</param>
    /// <param name="nextBaseRuntimeClassNameIndex">The index of the next base class, if applicable (use with <see cref="TryGetNextBaseRuntimeClassName"/>).</param>
    /// <returns>Whether <paramref name="baseRuntimeClassName"/> was successfully retrieved.</returns>
    public static bool TryGetBaseRuntimeClassName(
        scoped ReadOnlySpan<char> runtimeClassName,
        out ReadOnlySpan<char> baseRuntimeClassName,
        out int nextBaseRuntimeClassNameIndex)
    {
        int minimumKeyLength = WindowsRuntimeTypeHierarchyData.get_MinimumKeyLength(null);
        int maximumKeyLength = WindowsRuntimeTypeHierarchyData.get_MaximumKeyLength(null);

        // If the length can't possibly match any runtime class name we know of, we can stop here
        if ((uint)(runtimeClassName.Length - minimumKeyLength) > (uint)(maximumKeyLength - minimumKeyLength))
        {
            goto Failure;
        }

        // Get the hashcode of the input runtime class name. This function is deterministic and is
        // an exact match for the one used to actually emit the data in the RVA fields for the lookup.
        int hashCode = ComputeReadOnlySpanHash(runtimeClassName);

        // Get the offset of the chain of all colliding potential matches. Empty buckets just
        // point to an empty chain at the start of the keys RVA data, so we don't need to check for them.
        int chainOffset = GetChainOffset(hashCode);

        ReadOnlySpan<byte> keysRvaData = WindowsRuntimeTypeHierarchyData.get_Keys(null);
        RvaDataReader keysDataReader = new(keysRvaData);

        // Move to the start of the bucket chain where the possible match might be
        keysDataReader.AdvanceUnits(chainOffset);

        // Iterate all keys in the bucket chain until we either find a match or reach the end
        // of the chain. This is the same exact logic as the standard 'Dictionary<,>' lookup.
        while (true)
        {
            // The first value (2 bytes) is the length of the suffix of the key (in characters)
            ushort suffixLength = keysDataReader.ReadUInt16();

            // A suffix length of '0' indicates the end of a chain. If
            // we found this value, it means we didn't find a match.
            if (suffixLength == 0)
            {
                goto Failure;
            }

            // The second value (2 bytes) is the offset to the value in the values RVA field data
            ushort valueOffset = keysDataReader.ReadUInt16();

            // The third value (2 or 4 bytes, depending on the format) is the offset (in 16-bit units) to the prefix of the key
            int prefixOffset = WindowsRuntimeTypeHierarchyData.get_UsesNarrowOffsets(null)
                ? keysDataReader.ReadUInt16()
                : keysDataReader.ReadInt32();

            // Always read the suffix, so that the reader moves to the next key in the chain if this one doesn't match
            ReadOnlySpan<char> suffix = keysDataReader.ReadString(suffixLength);

            // Check the suffix first, so we can skip reading the prefix for all keys with a different suffix
            if (!runtimeClassName.EndsWith(suffix))
            {
                continue;
            }

            RvaDataReader prefixDataReader = new(keysRvaData);

            // Move to the prefix for the current key (it's stored as its length, followed by its characters)
            prefixDataReader.AdvanceUnits(prefixOffset);

            ushort prefixLength = prefixDataReader.ReadUInt16();
            ReadOnlySpan<char> prefix = prefixDataReader.ReadString(prefixLength);

            // Check if the current key (ie. its prefix followed by its suffix) is a match for the input runtime class name
            if (prefixLength + suffixLength == runtimeClassName.Length && runtimeClassName.StartsWith(prefix))
            {
                RvaDataReader valuesDataReader = new(WindowsRuntimeTypeHierarchyData.get_Values(null));

                // Move to the start of the value data for the current (matching) key
                valuesDataReader.Advance(valueOffset);

                // The first value (2 bytes) is the length of the base runtime class name (in characters)
                ushort valueLength = valuesDataReader.ReadUInt16();

                // Next, we have:
                //   - The index (2 bytes) of the next base runtime class name ('ushort.MaxValue' if there is no next base class)
                //   - The actual base runtime class name (with the previously retrieved length)
                nextBaseRuntimeClassNameIndex = valuesDataReader.ReadUInt16();
                baseRuntimeClassName = valuesDataReader.ReadString(valueLength);

                return true;
            }
        }

    Failure:
        baseRuntimeClassName = default;
        nextBaseRuntimeClassNameIndex = 0;

        return false;
    }

    /// <summary>
    /// Tries to find the next runtime class name following a previous lookup.
    /// </summary>
    /// <param name="baseRuntimeClassNameIndex">The index of the base class name to retrieve, if available.</param>
    /// <param name="baseRuntimeClassName">The resulting runtime class name of the base type for <paramref name="baseRuntimeClassName"/>.</param>
    /// <param name="nextBaseRuntimeClassNameIndex">The index of the next base class, if applicable (use with <see cref="TryGetNextBaseRuntimeClassName"/>).</param>
    /// <returns>Whether <paramref name="baseRuntimeClassName"/> was successfully retrieved.</returns>
    public static bool TryGetNextBaseRuntimeClassName(
        int baseRuntimeClassNameIndex,
        out ReadOnlySpan<char> baseRuntimeClassName,
        out int nextBaseRuntimeClassNameIndex)
    {
        // When there is no next base class, the index is set to '-1'. Because each value is
        // stored as an 'ushort' value, but here we take an 'int', we need to adjust the check.
        if ((uint)baseRuntimeClassNameIndex < ushort.MaxValue)
        {
            RvaDataReader valuesDataReader = new(WindowsRuntimeTypeHierarchyData.get_Values(null));

            // The input index should directly point to the next value to return
            valuesDataReader.Advance(baseRuntimeClassNameIndex);

            // Read the length of the value, same as above
            ushort valueLength = valuesDataReader.ReadUInt16();

            // Return the new value, and the next index (if available, it might be '0')
            nextBaseRuntimeClassNameIndex = valuesDataReader.ReadUInt16();
            baseRuntimeClassName = valuesDataReader.ReadString(valueLength);

            return true;
        }

        baseRuntimeClassName = [];
        nextBaseRuntimeClassNameIndex = 0;

        return false;
    }

    /// <summary>
    /// Gets the offset of the chain of keys for a given hash code.
    /// </summary>
    /// <param name="hashCode">The hash code of the input runtime class name.</param>
    /// <returns>The offset (in 16-bit units) of the chain of keys for <paramref name="hashCode"/> in the keys RVA field data.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int GetChainOffset(int hashCode)
    {
        ReadOnlySpan<byte> bucketsRvaData = WindowsRuntimeTypeHierarchyData.get_Buckets(null);

        // The format depends on whether chain and prefix offsets fit in a 'ushort' value. This is a constant
        // in the generated code, so after inlining only one of these two paths should actually be compiled.
        if (WindowsRuntimeTypeHierarchyData.get_UsesNarrowOffsets(null))
        {
            ReadOnlySpan<ushort> buckets = MemoryMarshal.Cast<byte, ushort>(bucketsRvaData);

            return buckets[(int)((uint)hashCode % (uint)buckets.Length)];
        }

        ReadOnlySpan<int> wideBuckets = MemoryMarshal.Cast<byte, int>(bucketsRvaData);

        return wideBuckets[(int)((uint)hashCode % (uint)wideBuckets.Length)];
    }

    /// <summary>
    /// Computes a deterministic hash of an input span.
    /// </summary>
    /// <param name="span">The input span.</param>
    /// <returns>The hash of <paramref name="span"/>.</returns>
    /// <remarks>This implementation must be identical to the one in runtime.</remarks>
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
    /// A helper type to read RVA data.
    /// </summary>
    /// <param name="rvaData">The RVA data to wrap.</param>
    private ref struct RvaDataReader(ReadOnlySpan<byte> rvaData)
    {
        /// <summary>
        /// The reference to the RVA data (for the current offset).
        /// </summary>
        private ref byte _dataRef = ref MemoryMarshal.GetReference(rvaData);

        /// <summary>
        /// The remaining length of the RVA data to read.
        /// </summary>
        private int _length = rvaData.Length;

        /// <summary>
        /// Advances the reader to the specified offset.
        /// </summary>
        /// <param name="offset">The offset to use to advance the reader.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int offset)
        {
            if ((uint)offset > (uint)_length)
            {
                ThrowBadImageFormatException();
            }

            _dataRef = ref Unsafe.Add(ref _dataRef, offset);
            _length -= offset;
        }

        /// <summary>
        /// Advances the reader to the specified offset, in 16-bit units.
        /// </summary>
        /// <param name="offset">The offset (in 16-bit units) to use to advance the reader.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AdvanceUnits(int offset)
        {
            // Validate the offset before scaling it, so that invalid offsets can't overflow into valid ones
            if ((uint)offset > (uint)_length / sizeof(char))
            {
                ThrowBadImageFormatException();
            }

            Advance(offset * sizeof(char));
        }

        /// <summary>
        /// Reads an <see cref="ushort"/> value and advances the reader.
        /// </summary>
        /// <returns>The resulting <see cref="ushort"/> value.</returns>
        public ushort ReadUInt16()
        {
            if (_length < sizeof(ushort))
            {
                ThrowBadImageFormatException();
            }

            ushort value = Unsafe.As<byte, ushort>(ref _dataRef);

            Advance(sizeof(ushort));

            return value;
        }

        /// <summary>
        /// Reads an <see cref="int"/> value and advances the reader.
        /// </summary>
        /// <returns>The resulting <see cref="int"/> value.</returns>
        public int ReadInt32()
        {
            if (_length < sizeof(int))
            {
                ThrowBadImageFormatException();
            }

            // The RVA data is only guaranteed to be 2-byte aligned, so this read might be unaligned
            int value = Unsafe.ReadUnaligned<int>(ref _dataRef);

            Advance(sizeof(int));

            return value;
        }

        /// <summary>
        /// Reads a <see cref="ReadOnlySpan{T}"/> of <see cref="char"/> value and advances the reader.
        /// </summary>
        /// <param name="length">The length of the span to read.</param>
        /// <returns>The resulting span.</returns>
        public ReadOnlySpan<char> ReadString(int length)
        {
            if ((uint)length > (uint)_length / sizeof(char))
            {
                ThrowBadImageFormatException();
            }

            ReadOnlySpan<char> value = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<byte, char>(ref _dataRef), length);

            Advance(length * sizeof(char));

            return value;
        }

        /// <summary>
        /// Throws a <see cref="BadImageFormatException"/> indicating that the RVA data is malformed.
        /// </summary>
        /// <exception cref="BadImageFormatException"></exception>
        private static void ThrowBadImageFormatException()
        {
            throw new BadImageFormatException("The RVA data for the type hierarchy lookup is malformed.", "WinRT.Interop.dll");
        }
    }
}

/// <summary>
/// Forwarded stubs from the <c>WindowsRuntimeTypeHierarchyData</c> type in <c>WinRT.Interop.dll</c>.
/// </summary>
file static class WindowsRuntimeTypeHierarchyData
{
    /// <summary>
    /// Gets the <see cref="ReadOnlySpan{T}"/> for the runtime class name lookup buckets.
    /// </summary>
    /// <returns>The buckets <see cref="ReadOnlySpan{T}"/>.</returns>
    /// <remarks>Each bucket is either a <see cref="ushort"/> or an <see cref="int"/> value, depending on <see cref="get_UsesNarrowOffsets"/>.</remarks>
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
    public static extern ReadOnlySpan<byte> get_Buckets(
        [UnsafeAccessorType("WindowsRuntime.Interop.WindowsRuntimeTypeHierarchyData, WinRT.Interop")] object? _);

    /// <summary>
    /// Gets the <see cref="ReadOnlySpan{T}"/> for the runtime class name lookup keys.
    /// </summary>
    /// <returns>The keys <see cref="ReadOnlySpan{T}"/>.</returns>
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
    public static extern ReadOnlySpan<byte> get_Keys(
        [UnsafeAccessorType("WindowsRuntime.Interop.WindowsRuntimeTypeHierarchyData, WinRT.Interop")] object? _);

    /// <summary>
    /// Gets the <see cref="ReadOnlySpan{T}"/> for the runtime class name lookup values.
    /// </summary>
    /// <returns>The values <see cref="ReadOnlySpan{T}"/>.</returns>
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
    public static extern ReadOnlySpan<byte> get_Values(
        [UnsafeAccessorType("WindowsRuntime.Interop.WindowsRuntimeTypeHierarchyData, WinRT.Interop")] object? _);

    /// <summary>
    /// Gets whether offsets into the runtime class name lookup keys are <see cref="ushort"/> values (rather than <see cref="int"/> values).
    /// </summary>
    /// <returns>Whether offsets into the runtime class name lookup keys are <see cref="ushort"/> values.</returns>
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
    public static extern bool get_UsesNarrowOffsets(
        [UnsafeAccessorType("WindowsRuntime.Interop.WindowsRuntimeTypeHierarchyData, WinRT.Interop")] object? _);

    /// <summary>
    /// Gets the length of the shortest runtime class name in the lookup.
    /// </summary>
    /// <returns>The length of the shortest runtime class name in the lookup.</returns>
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
    public static extern int get_MinimumKeyLength(
        [UnsafeAccessorType("WindowsRuntime.Interop.WindowsRuntimeTypeHierarchyData, WinRT.Interop")] object? _);

    /// <summary>
    /// Gets the length of the longest runtime class name in the lookup.
    /// </summary>
    /// <returns>The length of the longest runtime class name in the lookup.</returns>
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
    public static extern int get_MaximumKeyLength(
        [UnsafeAccessorType("WindowsRuntime.Interop.WindowsRuntimeTypeHierarchyData, WinRT.Interop")] object? _);
}