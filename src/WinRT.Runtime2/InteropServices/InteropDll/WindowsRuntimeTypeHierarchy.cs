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
            baseRuntimeClassName = default;
            nextBaseRuntimeClassNameIndex = 0;

            return false;
        }

        int hashCode = ComputeReadOnlySpanHash(runtimeClassName);
        P_1 = default(ReadOnlySpan<char>);
        P_2 = 0;
        if (P_0.Length >= 21 && P_0.Length <= 81)
        {
            int num = Unsafe.As << RvaFields >.TypeHierarchyLookupBucketsRvaData(Size = 4384 | Align = 4), int> (ref Unsafe.AddByteOffset(ref < RvaFields >.TypeHierarchyLookupBuckets, (uint)< InteropImplementationDetails >.ComputeReadOnlySpanHash(P_0) % 1096u * 4));
            if (num >= 0)
            {
                ref byte reference = ref Unsafe.As << RvaFields >.TypeHierarchyLookupKeysRvaData(Size = 72514 | Align = 2), byte> (ref Unsafe.AddByteOffset(ref < RvaFields >.TypeHierarchyLookupKeys, num));
                while (true)
                {
                    int num2 = Unsafe.As<byte, ushort>(ref reference);
                    if (num2 == 0)
                    {
                        break;
                    }
                    reference = ref Unsafe.Add(ref reference, 2);
                    int num3 = Unsafe.As<byte, ushort>(ref reference);
                    reference = ref Unsafe.Add(ref reference, 2);
                    ReadOnlySpan<char> readOnlySpan = MemoryMarshal.CreateReadOnlySpan<char>(ref Unsafe.As<byte, char>(ref reference), num2);
                    reference = ref Unsafe.Add(ref reference, num2);
                    if (MemoryExtensions.SequenceEqual<char>(P_0, readOnlySpan))
                    {
                        ref byte reference2 = ref Unsafe.As << RvaFields >.TypeHierarchyLookupValuesRvaData(Size = 13054 | Align = 2), byte> (ref Unsafe.AddByteOffset(ref < RvaFields >.TypeHierarchyLookupValues, num3));
                        int num4 = Unsafe.As<byte, ushort>(ref reference2);
                        reference2 = ref Unsafe.Add(ref reference2, 2);
                        P_2 = Unsafe.As<byte, ushort>(ref reference2);
                        P_1 = MemoryMarshal.CreateReadOnlySpan<char>(ref Unsafe.As<byte, char>(ref Unsafe.Add(ref reference2, 2)), num4);
                        return true;
                    }
                }
            }
        }
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
