// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.IO;
using WindowsRuntime;
using WindowsRuntime.InteropServices;

namespace Windows.Storage.Streams;

/// <summary>
/// Provides extension methods for converting between Windows Runtime streams and managed <see cref="Stream"/> objects.
/// </summary>
public static class WindowsRuntimeStreamExtensions
{
    /// <summary>
    /// Converts a Windows Runtime <see cref="IInputStream"/> to a managed <see cref="Stream"/>.
    /// </summary>
    /// <param name="windowsRuntimeStream">The Windows Runtime input stream to convert.</param>
    /// <returns>A managed <see cref="Stream"/> wrapping the specified Windows Runtime stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="windowsRuntimeStream"/> is <see langword="null"/>.</exception>
    public static Stream AsStreamForRead(this IInputStream windowsRuntimeStream)
    {
        return WindowsRuntimeStreamMapping.AsManagedStream(windowsRuntimeStream, WindowsRuntimeIOHelpers.DefaultBufferSize, nameof(AsStreamForRead), forceBufferSize: false);
    }

    /// <summary>
    /// Converts a Windows Runtime <see cref="IInputStream"/> to a managed <see cref="Stream"/> with the specified buffer size.
    /// </summary>
    /// <param name="windowsRuntimeStream">The Windows Runtime input stream to convert.</param>
    /// <param name="bufferSize">The size of the buffer to use for the adapter. Use <c>0</c> to disable buffering.</param>
    /// <returns>A managed <see cref="Stream"/> wrapping the specified Windows Runtime stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="windowsRuntimeStream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="bufferSize"/> is negative.</exception>
    public static Stream AsStreamForRead(this IInputStream windowsRuntimeStream, int bufferSize)
    {
        return WindowsRuntimeStreamMapping.AsManagedStream(windowsRuntimeStream, bufferSize, nameof(AsStreamForRead), forceBufferSize: true);
    }

    /// <summary>
    /// Converts a Windows Runtime <see cref="IOutputStream"/> to a managed <see cref="Stream"/>.
    /// </summary>
    /// <param name="windowsRuntimeStream">The Windows Runtime output stream to convert.</param>
    /// <returns>A managed <see cref="Stream"/> wrapping the specified Windows Runtime stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="windowsRuntimeStream"/> is <see langword="null"/>.</exception>
    public static Stream AsStreamForWrite(this IOutputStream windowsRuntimeStream)
    {
        return WindowsRuntimeStreamMapping.AsManagedStream(windowsRuntimeStream, WindowsRuntimeIOHelpers.DefaultBufferSize, nameof(AsStreamForWrite), forceBufferSize: false);
    }

    /// <summary>
    /// Converts a Windows Runtime <see cref="IOutputStream"/> to a managed <see cref="Stream"/> with the specified buffer size.
    /// </summary>
    /// <param name="windowsRuntimeStream">The Windows Runtime output stream to convert.</param>
    /// <param name="bufferSize">The size of the buffer to use for the adapter. Use <c>0</c> to disable buffering.</param>
    /// <returns>A managed <see cref="Stream"/> wrapping the specified Windows Runtime stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="windowsRuntimeStream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="bufferSize"/> is negative.</exception>
    public static Stream AsStreamForWrite(this IOutputStream windowsRuntimeStream, int bufferSize)
    {
        return WindowsRuntimeStreamMapping.AsManagedStream(windowsRuntimeStream, bufferSize, nameof(AsStreamForWrite), forceBufferSize: true);
    }

    /// <summary>
    /// Converts a Windows Runtime <see cref="IRandomAccessStream"/> to a managed <see cref="Stream"/>.
    /// </summary>
    /// <param name="windowsRuntimeStream">The Windows Runtime random access stream to convert.</param>
    /// <returns>A managed <see cref="Stream"/> wrapping the specified Windows Runtime stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="windowsRuntimeStream"/> is <see langword="null"/>.</exception>
    public static Stream AsStream(this IRandomAccessStream windowsRuntimeStream)
    {
        return WindowsRuntimeStreamMapping.AsManagedStream(windowsRuntimeStream, WindowsRuntimeIOHelpers.DefaultBufferSize, nameof(AsStream), forceBufferSize: false);
    }

    /// <summary>
    /// Converts a Windows Runtime <see cref="IRandomAccessStream"/> to a managed <see cref="Stream"/> with the specified buffer size.
    /// </summary>
    /// <param name="windowsRuntimeStream">The Windows Runtime random access stream to convert.</param>
    /// <param name="bufferSize">The size of the buffer to use for the adapter. Use <c>0</c> to disable buffering.</param>
    /// <returns>A managed <see cref="Stream"/> wrapping the specified Windows Runtime stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="windowsRuntimeStream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="bufferSize"/> is negative.</exception>
    public static Stream AsStream(this IRandomAccessStream windowsRuntimeStream, int bufferSize)
    {
        return WindowsRuntimeStreamMapping.AsManagedStream(windowsRuntimeStream, bufferSize, nameof(AsStream), forceBufferSize: true);
    }

    /// <summary>
    /// Converts a managed <see cref="Stream"/> to a Windows Runtime <see cref="IInputStream"/>.
    /// </summary>
    /// <param name="stream">The managed stream to convert.</param>
    /// <returns>A Windows Runtime <see cref="IInputStream"/> wrapping the specified managed stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException">Thrown if <paramref name="stream"/> does not support reading.</exception>
    public static IInputStream AsInputStream(this Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        NotSupportedException.ThrowIfStreamCannotConvertToInputStream(stream.CanRead);

        return (IInputStream)WindowsRuntimeStreamMapping.AsNativeStream(stream);
    }

    /// <summary>
    /// Converts a managed <see cref="Stream"/> to a Windows Runtime <see cref="IOutputStream"/>.
    /// </summary>
    /// <param name="stream">The managed stream to convert.</param>
    /// <returns>A Windows Runtime <see cref="IOutputStream"/> wrapping the specified managed stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException">Thrown if <paramref name="stream"/> does not support writing.</exception>
    public static IOutputStream AsOutputStream(this Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        NotSupportedException.ThrowIfStreamCannotConvertToOutputStream(stream.CanWrite);

        return (IOutputStream)WindowsRuntimeStreamMapping.AsNativeStream(stream);
    }

    /// <summary>
    /// Converts a managed <see cref="Stream"/> to a Windows Runtime <see cref="IRandomAccessStream"/>.
    /// </summary>
    /// <param name="stream">The managed stream to convert.</param>
    /// <returns>A Windows Runtime <see cref="IRandomAccessStream"/> wrapping the specified managed stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException">Thrown if <paramref name="stream"/> does not support seeking.</exception>
    public static IRandomAccessStream AsRandomAccessStream(this Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        NotSupportedException.ThrowIfStreamCannotConvertToRandomAccessStream(stream.CanSeek);

        return (IRandomAccessStream)WindowsRuntimeStreamMapping.AsNativeStream(stream);
    }
}
#endif
