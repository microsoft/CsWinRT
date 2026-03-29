// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Diagnostics;
#if DEBUG
using System.Diagnostics.CodeAnalysis;
#endif
using System.IO;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Provides mapping and caching logic for converting between Windows Runtime streams and managed <see cref="Stream"/> objects.
/// </summary>
internal static class WindowsRuntimeStreamMapping
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
    /// Converts a Windows Runtime stream to a managed <see cref="Stream"/> adapter, handling unwrapping and caching.
    /// </summary>
    /// <param name="windowsRuntimeStream">The Windows Runtime stream object to convert.</param>
    /// <param name="bufferSize">The size of the buffer to use.</param>
    /// <param name="invokedMethodName">The name of the public method that initiated the conversion.</param>
    /// <param name="forceBufferSize">Whether to enforce the specified buffer size on an existing adapter.</param>
    /// <returns>A managed <see cref="Stream"/> wrapping the specified Windows Runtime stream.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="windowsRuntimeStream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="bufferSize"/> is negative.</exception>
    public static Stream AsManagedStream(object windowsRuntimeStream, int bufferSize, string invokedMethodName, bool forceBufferSize)
    {
        ArgumentNullException.ThrowIfNull(windowsRuntimeStream);
        ArgumentOutOfRangeException.ThrowIfNegative(bufferSize);

        Debug.Assert(!string.IsNullOrWhiteSpace(invokedMethodName));

        // If the Windows Runtime stream is actually a wrapped managed stream, unwrap it and return the original.
        // In that case we do not need to put the wrapper into the map. We currently do capability-based adapter
        // selection for Windows Runtime to .NET, but not vice versa (due to time constraints). Once we added the
        // reverse direction, we will be able to replace this entire section with just a few lines.
        if (windowsRuntimeStream is WindowsRuntimeNativeStreamAdapter nativeAdapter)
        {
            Stream? wrappedManagedStream = nativeAdapter.GetManagedStream();

            ObjectDisposedException.ThrowIfStreamIsDisposed(wrappedManagedStream);

            // In debug builds, verify that the original managed stream is correctly entered
            // into the .NET -> Windows Runtime map, so we can retrieve it on the way back.
#if DEBUG
            AssertMapContains(
                map: managedToWindowsRuntimeAdapterMap,
                key: wrappedManagedStream,
                value: nativeAdapter,
                valueMayBeWrappedInBufferedStream: false);
#endif

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
        return AsManagedStreamCore(windowsRuntimeStream, bufferSize, invokedMethodName, forceBufferSize);
    }

    /// <summary>
    /// Converts a managed <see cref="Stream"/> to its corresponding Windows Runtime stream representation, handling unwrapping and caching.
    /// </summary>
    /// <param name="stream">The managed stream to convert.</param>
    /// <returns>The Windows Runtime stream object wrapping the specified managed stream.</returns>
    public static object AsNativeStream(Stream stream)
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

            // In debug builds, verify that the original Windows Runtime stream is correctly entered
            // into the Windows Runtime -> .NET map, so we can retrieve it on the way back as well
#if DEBUG
            AssertMapContains(
                map: windowsRuntimeToManagedAdapterMap,
                key: wrappedWindowsRuntimeStream,
                value: managedAdapter,
                valueMayBeWrappedInBufferedStream: true);
#endif

            return wrappedWindowsRuntimeStream;
        }

        // This is a real managed stream, so we need to check if it already has an adapter
        if (managedToWindowsRuntimeAdapterMap.TryGetValue(stream, out WindowsRuntimeNativeStreamAdapter? adapter))
        {
            return adapter;
        }

        // We do not have an adapter for this managed stream yet: create one. This is done in a separate method
        // so we only pay for the closure allocation if this code path is actually hit (same as above).
        return AsNativeStreamCore(stream);
    }

    /// <summary>
    /// Creates a managed <see cref="Stream"/> adapter for the specified Windows Runtime stream.
    /// </summary>
    /// <param name="windowsRuntimeStream">The Windows Runtime stream to create an adapter for.</param>
    /// <param name="bufferSize">The size of the buffer to use.</param>
    /// <param name="invokedMethodName">The name of the public method that initiated the conversion.</param>
    /// <param name="forceBufferSize">Whether to enforce the specified buffer size on the adapter.</param>
    /// <returns>A managed <see cref="Stream"/> adapter for the specified Windows Runtime stream.</returns>
    private static Stream AsManagedStreamCore(object windowsRuntimeStream, int bufferSize, string invokedMethodName, bool forceBufferSize)
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
    /// Creates a <see cref="WindowsRuntimeNativeStreamAdapter"/> for the specified managed <see cref="Stream"/>.
    /// </summary>
    /// <param name="stream">The managed stream to create an adapter for.</param>
    /// <returns>The <see cref="WindowsRuntimeNativeStreamAdapter"/> wrapping the specified managed stream.</returns>
    private static WindowsRuntimeNativeStreamAdapter AsNativeStreamCore(Stream stream)
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

#if DEBUG
    /// <summary>
    /// Asserts that the specified key exists in the map and that the associated value matches the expected value.
    /// </summary>
    /// <typeparam name="TKey">The type of keys contained in the map.</typeparam>
    /// <typeparam name="TValue">The type of values contained in the map.</typeparam>
    /// <param name="map">The <see cref="ConditionalWeakTable{TKey, TValue}"/> instance in which to check for the key and value association.</param>
    /// <param name="key">The key to locate in the map.</param>
    /// <param name="value">The expected value associated with the key.</param>
    /// <param name="valueMayBeWrappedInBufferedStream">Indicates whether the value may be wrapped in a <see cref="BufferedStream"/>.</param>
    private static void AssertMapContains<TKey, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TValue>(
        ConditionalWeakTable<TKey, TValue> map,
        TKey key,
        TValue value,
        bool valueMayBeWrappedInBufferedStream)
        where TKey : class
        where TValue : class
    {
        Debug.Assert(key is not null);

        bool hasValueForKey = map.TryGetValue(key, out TValue? valueInMap);

        Debug.Assert(hasValueForKey);

        if (valueMayBeWrappedInBufferedStream)
        {
            BufferedStream? bufferedValueInMap = valueInMap as BufferedStream;

            Debug.Assert(ReferenceEquals(value, valueInMap) || (bufferedValueInMap is not null && ReferenceEquals(value, bufferedValueInMap.UnderlyingStream)));
        }
        else
        {
            Debug.Assert(ReferenceEquals(value, valueInMap));
        }
    }
#endif
}
#endif
