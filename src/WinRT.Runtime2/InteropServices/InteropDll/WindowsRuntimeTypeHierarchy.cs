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
        // If the length can't possibly match any runtime class name we know of, we can stop here
        if (!WindowsRuntimeTypeHierarchyData.IsLengthInRange(null, runtimeClassName.Length))
        {
            goto Failure;
        }

        // Get the hashcode of the input runtime class name. This function is deterministic and is
        // an exact match for the one used to actually emit the data in the RVA fields for the lookup.
        int hashCode = ComputeReadOnlySpanHash(runtimeClassName);

        // Get the bucket index, which represents the chain of all colliding potential matches
        int bucketIndex = WindowsRuntimeTypeHierarchyData.get_Buckets(null)[(int)((uint)hashCode % (uint)WindowsRuntimeTypeHierarchyData.get_Buckets(null).Length)];

        // Bucket indices of '-1' indicate no possible matches
        if (bucketIndex < 0)
        {
            goto Failure;
        }

        RvaDataReader keysDataReader = new(WindowsRuntimeTypeHierarchyData.get_Keys(null));

        // Move to the start of the bucket chain where the possible match might be
        keysDataReader.Advance(bucketIndex);

        // Iterate all keys in the bucket chain until we either find a match or reach the end
        // of the chain. This is the same exact logic as the standard 'Dictionary<,>' lookup.
        while (true)
        {
            // The first value (2 bytes) is the length of the key (in characters)
            ushort keyLength = keysDataReader.ReadUInt16();

            // A key length of '0' indicates the end of a chain. If
            // we found this value, it means we didn't find a match.
            if (keyLength == 0)
            {
                goto Failure;
            }

            // The second value (2 bytes) is the offset to the value in the values RVA field data
            ushort valueOffset = keysDataReader.ReadUInt16();

            // Check if the current key is a match for the input runtime class name
            if (keysDataReader.ReadString(keyLength).SequenceEqual(runtimeClassName))
            {
                RvaDataReader valuesDataReader = new(WindowsRuntimeTypeHierarchyData.get_Values(null));

                // Move to the start of the value data for the current (matching) key
                valuesDataReader.Advance(valueOffset);

                // The first value (2 bytes) is the length of the base runtime class name (in characters)
                ushort valueLength = valuesDataReader.ReadUInt16();

                // Next, we have:
                //   - The index (2 bytes) of the next base runtime class name (0 if there is no next base class)
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
        throw null!;
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
        /// Reads a <see cref="ReadOnlySpan{T}"/> of <see cref="char"/> value and advances the reader.
        /// </summary>
        /// <param name="length">The length of the span to read.</param>
        /// <returns>The resulting span.</returns>
        public ReadOnlySpan<char> ReadString(int length)
        {
            if ((uint)length > (uint)_length)
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
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
    public static extern ReadOnlySpan<int> get_Buckets(
        /* [UnsafeAccessorType("WindowsRuntime.Interop.WindowsRuntimeTypeHierarchyData, WinRT.Interop.dll"))] */ object? _);

    /// <summary>
    /// Gets the <see cref="ReadOnlySpan{T}"/> for the runtime class name lookup keys.
    /// </summary>
    /// <returns>The keys <see cref="ReadOnlySpan{T}"/>.</returns>
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
    public static extern ReadOnlySpan<byte> get_Keys(
        /* [UnsafeAccessorType("WindowsRuntime.Interop.WindowsRuntimeTypeHierarchyData, WinRT.Interop.dll"))] */ object? _);

    /// <summary>
    /// Gets the <see cref="ReadOnlySpan{T}"/> for the runtime class name lookup values.
    /// </summary>
    /// <returns>The values <see cref="ReadOnlySpan{T}"/>.</returns>
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
    public static extern ReadOnlySpan<byte> get_Values(
        /* [UnsafeAccessorType("WindowsRuntime.Interop.WindowsRuntimeTypeHierarchyData, WinRT.Interop.dll"))] */ object? _);

    /// <summary>
    /// Gets the <see cref="ReadOnlySpan{T}"/> for the runtime class name lookup buckets.
    /// </summary>
    /// <returns>The buckets <see cref="ReadOnlySpan{T}"/>.</returns>
    [UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
    public static extern bool IsLengthInRange(
        /* [UnsafeAccessorType("WindowsRuntime.Interop.WindowsRuntimeTypeHierarchyData, WinRT.Interop.dll"))] */ object? _,
        int length);
}
