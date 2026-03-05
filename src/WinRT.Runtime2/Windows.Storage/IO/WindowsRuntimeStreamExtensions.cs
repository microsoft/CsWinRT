// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using Windows.Storage.Streams;
using WindowsRuntime;
using WindowsRuntime.InteropServices;

namespace Windows.Storage.IO;

/// <summary>
/// Provides extension methods for converting between Windows Runtime streams and managed <see cref="Stream"/> objects.
/// </summary>
public static class WindowsRuntimeStreamExtensions
{
    /// <summary>
    /// Maps Windows Runtime stream objects to their corresponding managed <see cref="Stream"/> adapters.
    /// </summary>
    private static readonly ConditionalWeakTable<object, Stream> windowsRuntimeToManagedAdapterMap = [];

    /// <summary>
    /// Maps managed <see cref="Stream"/> objects to their corresponding <see cref="WindowsRuntimeNativeStreamAdapter"/> adapters.
    /// </summary>
    private static readonly ConditionalWeakTable<Stream, WindowsRuntimeNativeStreamAdapter> managedToWindowsRuntimeAdapterMap = [];

    /// <summary>
    /// Converts a Windows Runtime <see cref="IInputStream"/> to a managed <see cref="Stream"/>.
    /// </summary>
    /// <param name="windowsRuntimeStream">The Windows Runtime input stream to convert.</param>
    /// <returns>A managed <see cref="Stream"/> wrapping the specified Windows Runtime stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="windowsRuntimeStream"/> is <see langword="null"/>.</exception>
    public static Stream AsStreamForRead(this IInputStream windowsRuntimeStream)
    {
        return AsStreamInternal(windowsRuntimeStream, WindowsRuntimeIOHelpers.DefaultBufferSize, nameof(AsStreamForRead), forceBufferSize: false);
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
        return AsStreamInternal(windowsRuntimeStream, bufferSize, nameof(AsStreamForRead), forceBufferSize: true);
    }

    /// <summary>
    /// Converts a Windows Runtime <see cref="IOutputStream"/> to a managed <see cref="Stream"/>.
    /// </summary>
    /// <param name="windowsRuntimeStream">The Windows Runtime output stream to convert.</param>
    /// <returns>A managed <see cref="Stream"/> wrapping the specified Windows Runtime stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="windowsRuntimeStream"/> is <see langword="null"/>.</exception>
    public static Stream AsStreamForWrite(this IOutputStream windowsRuntimeStream)
    {
        return AsStreamInternal(windowsRuntimeStream, WindowsRuntimeIOHelpers.DefaultBufferSize, nameof(AsStreamForWrite), forceBufferSize: false);
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
        return AsStreamInternal(windowsRuntimeStream, bufferSize, nameof(AsStreamForWrite), forceBufferSize: true);
    }

    /// <summary>
    /// Converts a Windows Runtime <see cref="IRandomAccessStream"/> to a managed <see cref="Stream"/>.
    /// </summary>
    /// <param name="windowsRuntimeStream">The Windows Runtime random access stream to convert.</param>
    /// <returns>A managed <see cref="Stream"/> wrapping the specified Windows Runtime stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="windowsRuntimeStream"/> is <see langword="null"/>.</exception>
    public static Stream AsStream(this IRandomAccessStream windowsRuntimeStream)
    {
        return AsStreamInternal(windowsRuntimeStream, WindowsRuntimeIOHelpers.DefaultBufferSize, nameof(AsStream), forceBufferSize: false);
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
        return AsStreamInternal(windowsRuntimeStream, bufferSize, nameof(AsStream), forceBufferSize: true);
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

        return (IInputStream)AsWindowsRuntimeStreamInternal(stream);
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

        return (IOutputStream)AsWindowsRuntimeStreamInternal(stream);
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

        return (IRandomAccessStream)AsWindowsRuntimeStreamInternal(stream);
    }

    /// <summary>
    /// Converts a Windows Runtime stream to a managed <see cref="Stream"/> adapter, handling unwrapping and caching.
    /// </summary>
    /// <param name="windowsRuntimeStream">The Windows Runtime stream object to convert.</param>
    /// <param name="bufferSize">The size of the buffer to use.</param>
    /// <param name="invokedMethodName">The name of the public method that initiated the conversion.</param>
    /// <param name="forceBufferSize">Whether to enforce the specified buffer size on an existing adapter.</param>
    /// <returns>A managed <see cref="Stream"/> wrapping the specified Windows Runtime stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="windowsRuntimeStream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="bufferSize"/> is negative.</exception>
    private static Stream AsStreamInternal(object windowsRuntimeStream, int bufferSize, string invokedMethodName, bool forceBufferSize)
    {
        ArgumentNullException.ThrowIfNull(windowsRuntimeStream);
        ArgumentOutOfRangeException.ThrowIfNegative(bufferSize);

        Debug.Assert(!string.IsNullOrWhiteSpace(invokedMethodName));

        // If the Windows Runtime stream is actually a wrapped managed stream, unwrap it and return the original.
        // In that case we do not need to put the wrapper into the map. We currently do capability-based adapter
        // selection for Windows Runtime to .NET, but not vice versa (due to time constraints). Once we added the
        // reverse direction, we will be able replce this entire section with just a few lines.
        if (windowsRuntimeStream is WindowsRuntimeNativeStreamAdapter nativeAdapter)
        {
            Stream? wrappedManagedStream = nativeAdapter.GetManagedStream();

            ObjectDisposedException.ThrowIfStreamIsDisposed(wrappedManagedStream);

            return wrappedManagedStream;
        }

        // If we got here, we have a real Windows Runtime stream, so check if we already have an adapter for it
        if (windowsRuntimeToManagedAdapterMap.TryGetValue(windowsRuntimeStream, out Stream? adapter))
        {
            Debug.Assert(adapter is WindowsRuntimeManagedStreamAdapter or BufferedStream { UnderlyingStream: WindowsRuntimeManagedStreamAdapter });

            if (forceBufferSize)
            {
                EnsureAdapterBufferSize(adapter, bufferSize, invokedMethodName);
            }

            return adapter;
        }

        // We do not have an adapter for this Windows Runtime stream yet and we need to create one. We do that in
        // a thread-safe manner in a separate method, such that we only have to pay for the compiler allocating
        // the required closure if this code path is actually hit.
        return AsStreamInternalFactoryHelper(windowsRuntimeStream, bufferSize, invokedMethodName, forceBufferSize);
    }

    /// <summary>
    /// Creates a managed <see cref="Stream"/> adapter for the specified Windows Runtime stream.
    /// </summary>
    /// <param name="windowsRuntimeStream">The Windows Runtime stream to create an adapter for.</param>
    /// <param name="bufferSize">The size of the buffer to use.</param>
    /// <param name="invokedMethodName">The name of the public method that initiated the conversion.</param>
    /// <param name="forceBufferSize">Whether to enforce the specified buffer size on the adapter.</param>
    /// <returns>A managed <see cref="Stream"/> adapter for the specified Windows Runtime stream.</returns>
    private static Stream AsStreamInternalFactoryHelper(object windowsRuntimeStream, int bufferSize, string invokedMethodName, bool forceBufferSize)
    {
        Debug.Assert(windowsRuntimeStream is not null);
        Debug.Assert(bufferSize >= 0);
        Debug.Assert(!string.IsNullOrWhiteSpace(invokedMethodName));

        // Gets or creates a managed stream adapter for the specified Windows Runtime stream (without buffering).
        static Stream GetOrAddWindowsRuntimeToManagedAdapterMapValue(object windowsRuntimeStream)
        {
            return windowsRuntimeToManagedAdapterMap.GetOrAdd(
                key: windowsRuntimeStream,
                valueFactory: WindowsRuntimeManagedStreamAdapter.Create);
        }

        // Gets or creates a buffered managed stream adapter for the specified Windows Runtime stream.
        static Stream GetOrAddWindowsRuntimeToManagedAdapterMapValueWithBufferSize(object windowsRuntimeStream, int bufferSize)
        {
            return windowsRuntimeToManagedAdapterMap.GetOrAdd(
                key: windowsRuntimeStream,
                valueFactory: windowsRuntimeStream => new BufferedStream(WindowsRuntimeManagedStreamAdapter.Create(windowsRuntimeStream), bufferSize));
        }

        // Get the adapter for this Windows Runtime stream again (it may have been
        // created concurrently). If none exists yet, create a new one now.
        Stream adapter = bufferSize == 0
            ? GetOrAddWindowsRuntimeToManagedAdapterMapValue(windowsRuntimeStream)
            : GetOrAddWindowsRuntimeToManagedAdapterMapValueWithBufferSize(windowsRuntimeStream, bufferSize);

        Debug.Assert(adapter is WindowsRuntimeManagedStreamAdapter or BufferedStream { UnderlyingStream: WindowsRuntimeManagedStreamAdapter });

        if (forceBufferSize)
        {
            EnsureAdapterBufferSize(adapter, bufferSize, invokedMethodName);
        }

        // Get the actual adapter object (which might be wrapped by a 'BufferedStream' instance)
        WindowsRuntimeManagedStreamAdapter? actualAdapter = adapter as WindowsRuntimeManagedStreamAdapter;

        actualAdapter ??= ((BufferedStream)adapter).UnderlyingStream as WindowsRuntimeManagedStreamAdapter;

        // Once we have the instance to return, mark it as the one being actually used
        actualAdapter!.SetWonInitializationRace();

        return adapter;
    }

    /// <summary>
    /// Validates that the buffer size of an existing adapter matches the required buffer size.
    /// </summary>
    /// <param name="adapter">The adapter <see cref="Stream"/> to validate.</param>
    /// <param name="requiredBufferSize">The required buffer size.</param>
    /// <param name="methodName">The name of the method being called.</param>
    /// <exception cref="InvalidOperationException">Thrown if the buffer sizes do not match.</exception>
    private static void EnsureAdapterBufferSize(Stream adapter, int requiredBufferSize, string methodName)
    {
        Debug.Assert(adapter is not null);
        Debug.Assert(!string.IsNullOrWhiteSpace(methodName));

        int currentBufferSize = 0;

        if (adapter is BufferedStream bufferedAdapter)
        {
            currentBufferSize = bufferedAdapter.BufferSize;
        }

        if (requiredBufferSize != currentBufferSize)
        {
            throw requiredBufferSize == 0
                ? InvalidOperationException.GetCannotChangeBufferSizeOfStreamAdapterToZeroException(methodName)
                : InvalidOperationException.GetCannotChangeBufferSizeOfStreamAdapterException(methodName);
        }
    }

    /// <summary>
    /// Converts a managed <see cref="Stream"/> to its corresponding Windows Runtime stream representation, handling unwrapping and caching.
    /// </summary>
    /// <param name="stream">The managed stream to convert.</param>
    /// <returns>The Windows Runtime stream object wrapping the specified managed stream.</returns>
    private static object AsWindowsRuntimeStreamInternal(Stream stream)
    {
        // Check if the managed stream is actually a wrapper of a Windows Runtime stream.
        // It can be either an adapter directly, or an adapter wrapped in a BufferedStream.
        WindowsRuntimeManagedStreamAdapter? managedAdapter = stream as WindowsRuntimeManagedStreamAdapter;

        if (managedAdapter is null && stream is BufferedStream bufferedStream)
        {
            managedAdapter = bufferedStream.UnderlyingStream as WindowsRuntimeManagedStreamAdapter;
        }

        // If the managed stream is actually a Windows Runtime stream, unwrap it and return
        // the original. In that case we do not need to put the wrapper into the map.
        if (managedAdapter is not null)
        {
            object? wrappedWindowsRuntimeStream = managedAdapter.GetWindowsRuntimeStream();

            ObjectDisposedException.ThrowIfStreamIsDisposed(wrappedWindowsRuntimeStream);

            return wrappedWindowsRuntimeStream;
        }

        // This is a real managed stream, so we need ot check if it already has an adapter
        if (managedToWindowsRuntimeAdapterMap.TryGetValue(stream, out WindowsRuntimeNativeStreamAdapter? adapter))
        {
            return adapter;
        }

        // We do not have an adapter for this managed stream yet: create one. This is done in a separate method
        // so we only pay for the closure allocation if this code path is actually hit (same as above).
        return AsWindowsRuntimeStreamInternalFactoryHelper(stream);
    }

    /// <summary>
    /// Creates a <see cref="WindowsRuntimeNativeStreamAdapter"/> for the specified managed <see cref="Stream"/>.
    /// </summary>
    /// <param name="stream">The managed stream to create an adapter for.</param>
    /// <returns>The <see cref="WindowsRuntimeNativeStreamAdapter"/> wrapping the specified managed stream.</returns>
    private static WindowsRuntimeNativeStreamAdapter AsWindowsRuntimeStreamInternalFactoryHelper(Stream stream)
    {
        Debug.Assert(stream is not null);

        // Get the adapter for this managed stream again (it may have been
        // created concurrently). If none exists yet, create a new one.
        WindowsRuntimeNativeStreamAdapter adapter = managedToWindowsRuntimeAdapterMap.GetOrAdd(
            key: stream,
            valueFactory: WindowsRuntimeNativeStreamAdapter.Create);

        Debug.Assert(adapter is not null);

        // Mark the instance we're returning as the one having won the race
        adapter.SetWonInitializationRace();

        return adapter;
    }
}
