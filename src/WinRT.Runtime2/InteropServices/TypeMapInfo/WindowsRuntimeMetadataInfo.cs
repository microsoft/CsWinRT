// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable IDE0008

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A type providing cached metadata information on Windows Runtime types.
/// </summary>
internal sealed class WindowsRuntimeMetadataInfo
{
    /// <inheritdoc cref="WindowsRuntimeMarshallingInfo.ExternalTypeMapping"/>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We expect external types to only be preserved if used in runtime casts.")]
    private static readonly IReadOnlyDictionary<string, Type> ExternalTypeMapping = TypeMapping.GetOrCreateExternalTypeMapping<WindowsRuntimeMetadataTypeMapGroup>();

    /// <inheritdoc cref="WindowsRuntimeMarshallingInfo.ProxyTypeMapping"/>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We expect only proxy types for constructed types to be preserved.")]
    private static readonly IReadOnlyDictionary<Type, Type> ProxyTypeMapping = TypeMapping.GetOrCreateProxyTypeMapping<WindowsRuntimeMetadataTypeMapGroup>();

    /// <inheritdoc cref="WindowsRuntimeMarshallingInfo.TypeNameToMappedTypeDictionary"/>
    private static readonly ConcurrentDictionary<string, Type?> TypeNameToMappedTypeDictionary = [];

    /// <summary>
    /// The table of metadata info for all types that require special handling.
    /// </summary>
    /// <remarks>
    /// This will only have non <see langword="null"/> values for types needing special metadata handling.
    /// </remarks>
    private static readonly ConditionalWeakTable<Type, WindowsRuntimeMetadataInfo?> TypeToMarshallingInfoTable = [];

    /// <summary>
    /// Cached creation factory for <see cref="CreateMetadataInfo"/>.
    /// </summary>
    private static readonly Func<Type, WindowsRuntimeMetadataInfo?> CreateMetadataInfoCallback = new(CreateMetadataInfo);

    /// <summary>
    /// Cached creation factory for <see cref="GetMetadataProviderType"/>.
    /// </summary>
    private static readonly Func<Type, WindowsRuntimeMetadataInfo?> GetMetadataProviderTypeCallback = new(GetMetadataProviderType);

    /// <summary>
    /// The metadata provider type associated with the current instance (ie. the mapped type to use to resolve attributes).
    /// </summary>
    /// <remarks>
    /// Here's some examples of how this type would relate to the associated metadata type in different scenarios:
    /// <list type="bullet">
    ///   <item>For a Windows Runtime projected type, this would be the same as the metadata type.</item>
    ///   <item>For custom-mapped types, this would be some proxy type with the right attributes on it.</item>
    /// </list>
    /// </remarks>
    private readonly Type _metadataProviderType;

    /// <inheritdoc cref="WindowsRuntimeMarshallingInfo._publicType"/>
    private volatile Type? _publicType;

    /// <summary>
    /// The cached metadata type name for the type.
    /// </summary>
    private volatile string? _metadataTypeName;

    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeMetadataInfo"/> instance with the specified parameters.
    /// </summary>
    /// <param name="metadataProviderType"><inheritdoc cref="_metadataProviderType" path="/summary/node()"/></param>
    private WindowsRuntimeMetadataInfo(Type metadataProviderType)
    {
        _metadataProviderType = metadataProviderType;
    }

    /// <inheritdoc cref="WindowsRuntimeMarshallingInfo.PublicType"/>
    public Type PublicType
    {
        get
        {
            // Same implementation as in 'WindowsRuntimeMarshallingInfo.PublicType', see notes there
            [MethodImpl(MethodImplOptions.NoInlining)]
            Type InitializePublicType()
            {
                WindowsRuntimeMappedTypeAttribute mappedTypeAttribute = _metadataProviderType.GetCustomAttribute<WindowsRuntimeMappedTypeAttribute>(inherit: false)!;

                Debug.Assert(mappedTypeAttribute is not null);

                return _publicType ??= mappedTypeAttribute.PublicType;
            }

            return _publicType ?? InitializePublicType();
        }
    }

    /// <summary>
    /// Tries to get a <see cref="WindowsRuntimeMetadataInfo"/> instance for a given metadata type name.
    /// </summary>
    /// <param name="metadataTypeName">The input metadata type name to use for lookups.</param>
    /// <param name="info">The resulting <see cref="WindowsRuntimeMetadataInfo"/> instance, if found.</param>
    /// <returns>Whether <paramref name="info"/> was retrieved successfully.</returns>
    public static bool TryGetInfo(ReadOnlySpan<char> metadataTypeName, [NotNullWhen(true)] out WindowsRuntimeMetadataInfo? info)
    {
        // Tries to get the external type for the input metadata type name
        static Type? TryGetExternalType(ReadOnlySpan<char> runtimeClassName)
        {
            var alternate = TypeNameToMappedTypeDictionary.GetAlternateLookup<ReadOnlySpan<char>>();

            // Check if we already have a cached result (it might be 'null')
            if (alternate.TryGetValue(runtimeClassName, out Type? externalType))
            {
                return externalType;
            }

            // Try to get the external type (which might not be present, if the entry has been removed)
            _ = ExternalTypeMapping.TryGetValue(runtimeClassName.ToString(), out externalType);

            // Try to add the cached value to the table
            _ = alternate.TryAdd(runtimeClassName, externalType);

            // Always return the external type (see notes in 'WindowsRuntimeMarshallingInfo')
            return externalType;
        }

        Type? externalType = TryGetExternalType(metadataTypeName);

        // We found a mapped external type, return its associated marshalling info
        if (externalType is not null)
        {
            info = TypeToMarshallingInfoTable.GetOrAdd(externalType, CreateMetadataInfoCallback)!;

            return true;
        }

        info = null;

        return false;
    }

    /// <summary>
    /// Tries to get a <see cref="WindowsRuntimeMetadataInfo"/> instance for a given managed type.
    /// </summary>
    /// <param name="managedType">The input managed type to use for lookups.</param>
    /// <param name="info">The resulting <see cref="WindowsRuntimeMetadataInfo"/> instance, if found.</param>
    /// <returns>Whether <paramref name="info"/> was retrieved successfully.</returns>
    public static bool TryGetInfo(Type managedType, [NotNullWhen(true)] out WindowsRuntimeMetadataInfo? info)
    {
        WindowsRuntimeMetadataInfo? result = TypeToMarshallingInfoTable.GetOrAdd(managedType, GetMetadataProviderTypeCallback);

        info = result;

        return result is not null;
    }

    /// <summary>
    /// Gets the metadata type name for the public type associated with the current metadata provider type.
    /// </summary>
    /// <returns>The resulting metadata type name.</returns>
    public string GetMetadataTypeName()
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        string InitializeMetadataTypeName()
        {
            WindowsRuntimeMetadataTypeNameAttribute? metadataTypeNameAttribute =
                _metadataProviderType.GetCustomAttribute<WindowsRuntimeMetadataTypeNameAttribute>(inherit: false);

            string metadataTypeName = metadataTypeNameAttribute?.MetadataTypeName ?? _metadataProviderType.FullName!;

            return _metadataTypeName ??= metadataTypeName;
        }

        return _metadataTypeName ?? InitializeMetadataTypeName();
    }

    /// <summary>
    /// Creates a <see cref="WindowsRuntimeMetadataInfo"/> instance for a specified metadata provider type.
    /// </summary>
    /// <param name="metadataProviderType">The metadata provider type to wrap.</param>
    /// <returns>The resulting <see cref="WindowsRuntimeMetadataInfo"/> instance.</returns>
    private static WindowsRuntimeMetadataInfo CreateMetadataInfo(Type metadataProviderType)
    {
        return new(metadataProviderType);
    }

    /// <summary>
    /// Creates a <see cref="WindowsRuntimeMetadataInfo"/> instance associated with a given managed type, if possible.
    /// </summary>
    /// <param name="managedType">The managed type to create an instance for, if possible.</param>
    /// <returns>The resulting <see cref="WindowsRuntimeMetadataInfo"/> instance, if created successfully.</returns>
    private static WindowsRuntimeMetadataInfo? GetMetadataProviderType(Type managedType)
    {
        // Same as above: if the type is a projected type, then it is also used as the metadata source
        if (managedType.IsDefined(typeof(WindowsRuntimeMetadataAttribute), inherit: false) && !managedType.IsGenericType)
        {
            return new(managedType);
        }

        // If we have a proxy type, then that will be the metadata provider
        if (ProxyTypeMapping.TryGetValue(managedType, out Type? proxyType))
        {
            return new(proxyType);
        }

        // We don't have a metadata provider for the type (we'll just marshal it as a generic 'IInspectable')
        return null;
    }
}