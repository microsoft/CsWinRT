// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

#pragma warning disable CA2208

namespace WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// A helper for marshaller types for Windows Runtime arrays.
/// </summary>
internal static class WindowsRuntimeArrayMarshallerHelpers
{
    /// <summary>
    /// Validates that the specified destination span has the required number of elements.
    /// </summary>
    /// <param name="sourceSize">The expected number of elements in the destination span.</param>
    /// <param name="destinationSize">The length of the destination span.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="destinationSize"/> does not equal <paramref name="sourceSize"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [StackTraceHidden]
    public static void ValidateDestinationSize(uint sourceSize, int destinationSize)
    {
        if ((uint)destinationSize != sourceSize)
        {
            [StackTraceHidden]
            static void ThrowArgumentException() => throw new ArgumentException("The destination array is too small.", "destination");

            ThrowArgumentException();
        }
    }

    /// <summary>
    /// Validates that the specified destination span has the required number of elements.
    /// </summary>
    /// <param name="sourceSize">The length of the span to validate.</param>
    /// <param name="destinationSize">The expected number of elements in the destination span.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="sourceSize"/> does not equal <paramref name="destinationSize"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [StackTraceHidden]
    public static void ValidateDestinationSize(int sourceSize, uint destinationSize)
    {
        if ((uint)sourceSize != destinationSize)
        {
            [StackTraceHidden]
            static void ThrowArgumentException() => throw new ArgumentException("The destination array is too small.", "destination");

            ThrowArgumentException();
        }
    }
}
#endif