// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable IDE0008

namespace WindowsRuntime.InteropServices;

internal sealed class WindowsRuntimeMarshallingInfo
{
    private static readonly ConcurrentDictionary<string, Type> TypeNameToMappedTypes = new();
    private static readonly ConditionalWeakTable<Type, WindowsRuntimeMarshallingInfo> TypeToMetadataProviderTypes = [];

    private readonly Type _metadataProviderType;

    private volatile WindowsRuntimeMarshallerAttribute? marshaller;
    private volatile WindowsRuntimeVtableProviderAttribute? _vtableProvider;

    public static WindowsRuntimeMarshallingInfo Get(ReadOnlySpan<char> runtimeClassName)
    {
        static Type GetMappedType(ReadOnlySpan<char> runtimeClassName)
        {
            var alternate = TypeNameToMappedTypes.GetAlternateLookup<ReadOnlySpan<char>>();

            if (alternate.TryGetValue(runtimeClassName, out Type? type))
            {
                return type;
            }
            type = typeof(int); // Get the value from somewhere
            if (alternate.TryAdd(runtimeClassName, type))
            {
                return type;
            }
            return alternate[runtimeClassName];
        }
        var alternate = TypeNameToMappedTypes.GetAlternateLookup<ReadOnlySpan<char>>();

        if (alternate.TryGetValue(runtimeClassName, out Type? type))
        {
            return type;
        }

        type = typeof(int); // Get the value from somewhere

        if (alternate.TryAdd(runtimeClassName, type))
        {
            return type;
        }

        return alternate[runtimeClassName];
    }

    public static WindowsRuntimeMarshallingInfo Get(Type managedType)
    {

    }

    public unsafe void* ConvertToUnmanaged(object? value)
    {

    }

    public void ComputeVtables(IBufferWriter<ComInterfaceEntry> bufferWriter)
    {
    }

    /// <summary>
    /// Gets the <see cref="WindowsRuntimeMarshallerAttribute"/> instance associated with the current metadata provider type.
    /// </summary>
    private WindowsRuntimeMarshallerAttribute? GetOrLoadMarshaller()
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        WindowsRuntimeMarshallerAttribute Load()
        {
            return _metadataProviderType.GetCustomAttribute<WindowsRuntimeMarshallerAttribute>()
                ?? PlaceholderWindowsRuntimeMarshallerAttribute.Instance;
        }

        return marshaller ??= Load();
    }

    /// <summary>
    /// Gets the <see cref="WindowsRuntimeMarshallerAttribute"/> instance associated with the current metadata provider type.
    /// </summary>
    private WindowsRuntimeVtableProviderAttribute? GetOrLoadVtableProvider()
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        WindowsRuntimeVtableProviderAttribute Load()
        {
            return _metadataProviderType.GetCustomAttribute<WindowsRuntimeVtableProviderAttribute>()
                ?? PlaceholderWindowsRuntimeVtableProviderAttribute.Instance;
        }

        return _vtableProvider ??= Load();
    }
}

/// <summary>
/// A placeholder <see cref="WindowsRuntimeMarshallerAttribute"/> type.
/// </summary>
file sealed unsafe class PlaceholderWindowsRuntimeMarshallerAttribute : WindowsRuntimeMarshallerAttribute
{
    /// <summary>
    /// The shared placeholder instance.
    /// </summary>
    public static PlaceholderWindowsRuntimeMarshallerAttribute Instance = new();

    /// <inheritdoc/>
    public override unsafe void* ConvertToUnmanaged(object? value)
    {
        return null;
    }
}

/// <summary>
/// A placeholder <see cref="WindowsRuntimeVtableProviderAttribute"/> type.
/// </summary>
file sealed class PlaceholderWindowsRuntimeVtableProviderAttribute : WindowsRuntimeVtableProviderAttribute
{
    /// <summary>
    /// The shared placeholder instance.
    /// </summary>
    public static PlaceholderWindowsRuntimeVtableProviderAttribute Instance = new();

    /// <inheritdoc/>
    public override void ComputeVtables(IBufferWriter<ComWrappers.ComInterfaceEntry> bufferWriter)
    {
    }
}