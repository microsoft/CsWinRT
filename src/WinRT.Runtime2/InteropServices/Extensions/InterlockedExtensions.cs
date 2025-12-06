// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Extensions for the <see cref="Interlocked"/> type.
/// </summary>
internal static class InterlockedExtensions
{
    extension(Interlocked)
    {
        /// <summary>
        /// Writes a value to a location using an interlocked operation.
        /// </summary>
        /// <typeparam name="T">The type to be used for <paramref name="location"/> and <paramref name="value"/>.</typeparam>
        /// <param name="location">The variable to set to the specified value.</param>
        /// <param name="value">The value to which the <paramref name="location"/> parameter is set.</param>
        /// <exception cref="System.NullReferenceException">The address of <paramref name="location"/> is <see langword="null"/>.</exception>
        /// <exception cref="System.NotSupportedException">An unsupported <typeparamref name="T"/> is specified.</exception>
        /// <remarks>
        /// Unlike <see cref="Volatile.Write{T}"/>, this method does a full memory fence and ensures immediate visibility for
        /// other threads. Volatile operations only do half memory fences, and only ensure ordering and eventual publishing.
        /// So when using just <see cref="Volatile.Write{T}"/>, other threads might keep reading stale data for some time, rather
        /// than the new value just written by the current thread (until some point in the future where coherency is reached).
        /// </remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>([NotNullIfNotNull(nameof(value))] ref T location, T value)
        {
            _ = Interlocked.Exchange(ref location, value);
        }
    }
}
