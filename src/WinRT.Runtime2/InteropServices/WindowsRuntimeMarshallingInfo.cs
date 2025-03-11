// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

#pragma warning disable IDE0008

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A type providing cached information on a Windows Runtime type, either projected or managed only.
/// </summary>
internal sealed class WindowsRuntimeMarshallingInfo
{
    /// <summary>
    /// The external types mapping for Windows Runtime types.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We expect external types to only be preserved if used in runtime casts.")]
    private static readonly IReadOnlyDictionary<string, Type> WindowsRuntimeExternalTypes = TypeMapping.GetExternalTypeMapping<WindowsRuntimeTypeMapUniverse>();

    /// <summary>
    /// The proxy types mapping for Windows Runtime types.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We expect only proxy types for constructed types to be preserved.")]
    private static readonly IReadOnlyDictionary<Type, Type> WindowsRuntimeProxyTypes = TypeMapping.GetTypeProxyMapping<WindowsRuntimeTypeMapUniverse>();

    /// <summary>
    /// The cached external types mapping for Windows Runtime types.
    /// </summary>
    /// <remarks>
    /// This is used to introduce a cache layer around <see cref="WindowsRuntimeExternalTypes"/>. It is done for two reasons:
    /// allow lookups without always instantiating a <see cref="string"/>, and reducing the overhead. Lookups into the actual
    /// runtime-provided dictionary can be quite expensive, as they might also do UTF8 transcoding internally.
    /// </remarks>
    private static readonly ConcurrentDictionary<string, Type?> TypeNameToMappedTypes = new();

    /// <summary>
    /// The map of marshalling info for all types that can participate in marshalling.
    /// </summary>
    /// <remarks>
    /// This will only have non <see langword="null"/> values for types needing special marshalling. Types which are meant to
    /// be marshalled as opaque <c>IInspectable</c> objects will have no associated values, and should be handled separately.
    /// </remarks>
    private static readonly ConditionalWeakTable<Type, WindowsRuntimeMarshallingInfo?> TypeToMetadataProviderTypes = [];

    /// <summary>
    /// Cached creation factory for <see cref="CreateMarshallingInfo"/>.
    /// </summary>
    private static readonly ConditionalWeakTable<Type, WindowsRuntimeMarshallingInfo?>.CreateValueCallback CreateMarshallingInfoCallback = new(CreateMarshallingInfo);

    /// <summary>
    /// Cached creation factory for <see cref="GetMetadataProviderType"/>.
    /// </summary>
    private static readonly ConditionalWeakTable<Type, WindowsRuntimeMarshallingInfo?>.CreateValueCallback GetMetadataProviderTypeCallback = new(GetMetadataProviderType);

    /// <summary>
    /// The metadata provider type associated with the current instance (ie. the mapped type to use to resolve attributes).
    /// </summary>
    /// <remarks>
    /// Here's some examples of how this type would relate to <see cref="_publicType"/> in different scenarios:
    /// <list type="bullet">
    ///   <item>
    ///     For a Windows Runtime projected type, this would be the same as <see cref="_publicType"/>.
    ///     This is because for generated types, the necessary attributes to provide additional metadata
    ///     can be applied directly on the types themselves, making additional lookups not needed.
    ///   </item>
    ///   <item>For user-defined managed types, this would be the generated proxy type.</item>
    ///   <item>
    ///     For other generated associations (eg. generic type instantiations), this would also
    ///     be the generated proxy type. This is because there would be no other way to link the
    ///     additional metadata required for marshalling to the original types otherwise.
    ///   </item>
    /// </list>
    /// </remarks>
    private readonly Type _metadataProviderType;

    /// <summary>
    /// The public type associated with the current instance (ie. the type that would be used directly by developers).
    /// </summary>
    private volatile Type? _publicType;

    /// <summary>
    /// The cached <see cref="WindowsRuntimeObjectMarshallerAttribute"/> instance (possibly a placeholder).
    /// </summary>
    private volatile WindowsRuntimeObjectMarshallerAttribute? _marshaller;

    /// <summary>
    /// The cached <see cref="WindowsRuntimeVtableProviderAttribute"/> instance (possibly a placeholder).
    /// </summary>
    private volatile WindowsRuntimeVtableProviderAttribute? _vtableProvider;

    /// <summary>
    /// The cached <see cref="WindowsRuntimeVtableInfo"/> instance.
    /// </summary>
    private volatile WindowsRuntimeVtableInfo? _vtableInfo;

    /// <summary>
    /// The cached runtime class name for the type.
    /// </summary>
    /// <remarks>
    /// This is only used for managed types that are marshalled to native. For RCWs (ie. for Windows
    /// Runtime projected types), the runtime class name would just be provided by the native object.
    /// </remarks>
    private string? _runtimeClassName;

    /// <summary>
    /// A lazy-loaded <see cref="Lock"/> object to synchronize expensive work being performed.
    /// </summary>
    private volatile Lock? _lock;

    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeMarshallingInfo"/> instance with the specified parameters.
    /// </summary>
    /// <param name="publicType"><inheritdoc cref="_publicType" path="/summary/node()"/></param>
    /// <param name="metadataProviderType"><inheritdoc cref="_metadataProviderType" path="/summary/node()"/></param>
    private WindowsRuntimeMarshallingInfo(Type? publicType, Type metadataProviderType)
    {
        _publicType = publicType;
        _metadataProviderType = metadataProviderType;
    }

    /// <summary>
    /// Gets the public type associated with the current instance (ie. the type that would be used directly by developers).
    /// </summary>
    public Type PublicType
    {
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            Type InitializePublicType()
            {
                // If we don't yet have a public type, it means that we have some metadata provider type
                // which is not a Windows Runtime projected type, for which we haven't loaded the mapped
                // public type yet. We can do that here.
                WindowsRuntimeMappedTypeAttribute mappedTypeAttribute = _metadataProviderType.GetCustomAttribute<WindowsRuntimeMappedTypeAttribute>(inherit: false)!;

                // In this scenario, it is guaranteed that the '[WindowsRuntimeMappedType]' attribute will be present on the
                // metadata provider type, as we would not have any way to go back to the associated public type otherwise,
                // which is needed in some cases. The attribute being missing would indicate some code generation error.
                Debug.Assert(mappedTypeAttribute is not null);

                // Cache the public type for later. We don't need a compare exchange here, as even if we did concurrent
                // queries for this value, the result would always be the same. So we can skip that small overhead here.
                return _publicType = mappedTypeAttribute.PublicType;
            }

            return _publicType ?? InitializePublicType();
        }
    }

    /// <summary>
    /// Tries to get a <see cref="WindowsRuntimeMarshallingInfo"/> instance for a given runtime class name.
    /// </summary>
    /// <param name="runtimeClassName">The input runtime class name to use for lookups.</param>
    /// <param name="info">The resulting <see cref="WindowsRuntimeMarshallingInfo"/> instance, if found.</param>
    /// <returns>Whether <paramref name="info"/> was retrieved successfully.</returns>
    /// <remarks>
    /// This can be used to support runtime type checks for objects marshalled from native to managed.
    /// </remarks>
    public static bool TryGetInfo(ReadOnlySpan<char> runtimeClassName, [NotNullWhen(true)] out WindowsRuntimeMarshallingInfo? info)
    {
        // Tries to get the external type for the input runtime class name
        static Type? TryGetExternalType(ReadOnlySpan<char> runtimeClassName)
        {
            var alternate = TypeNameToMappedTypes.GetAlternateLookup<ReadOnlySpan<char>>();

            // Check if we already have a cached result (it might be 'null')
            if (alternate.TryGetValue(runtimeClassName, out Type? externalType))
            {
                return externalType;
            }

            // Try to get the external type (which might not be present, if we don't have projections or if the entry has been removed)
            _ = WindowsRuntimeExternalTypes.TryGetValue(runtimeClassName.ToString(), out externalType);

            // Try to add the cached value to the table
            _ = alternate.TryAdd(runtimeClassName, externalType);

            // Regardless of whether we lost the race with another thread, so we can just return the type we have. We don't need
            // to perform another lookup, as the resulting external type will always be the same for a given runtime class name.
            return externalType;
        }

        Type? externalType = TryGetExternalType(runtimeClassName);

        // We found a mapped external type, return its associated marshalling info
        if (externalType is not null)
        {
            info = TypeToMetadataProviderTypes.GetValue(externalType, CreateMarshallingInfoCallback)!;

            return true;
        }

        info = null;

        return false;
    }

    /// <summary>
    /// Gets a <see cref="WindowsRuntimeMarshallingInfo"/> instance for a given managed type.
    /// </summary>
    /// <param name="managedType">The input managed type to use for lookups.</param>
    /// <returns>The resulting <see cref="WindowsRuntimeMarshallingInfo"/> instance.</returns>
    /// <remarks>
    /// This can be used to support type-specific marshalling for managed types passed to native.
    /// </remarks>
    /// <exception cref="NotSupportedException">Thrown if no <see cref="WindowsRuntimeMarshallingInfo"/> instance could be resolved.</exception>
    public static WindowsRuntimeMarshallingInfo GetInfo(Type managedType)
    {
        if (!TryGetInfo(managedType, out WindowsRuntimeMarshallingInfo? info))
        {
            // Analogous validation as for when retrieving the marshaller attribute
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowNotSupportedException(Type managedType)
            {
                throw new NotSupportedException(
                    $"The managed type '{managedType}' does not have any associated marshalling info. " +
                    $"This should never be the case. Please file an issue at https://github.com/microsoft/CsWinRT.");
            }

            ThrowNotSupportedException(managedType);
        }

        return info;
    }

    /// <summary>
    /// Tries to get a <see cref="WindowsRuntimeMarshallingInfo"/> instance for a given managed type.
    /// </summary>
    /// <param name="managedType">The input managed type to use for lookups.</param>
    /// <param name="info">The resulting <see cref="WindowsRuntimeMarshallingInfo"/> instance, if found.</param>
    /// <returns>Whether <paramref name="info"/> was retrieved successfully.</returns>
    /// <remarks>
    /// This can be used to support type-specific marshalling for managed types passed to native.
    /// </remarks>
    public static bool TryGetInfo(Type managedType, [NotNullWhen(true)] out WindowsRuntimeMarshallingInfo? info)
    {
        WindowsRuntimeMarshallingInfo? result = TypeToMetadataProviderTypes.GetValue(managedType, GetMetadataProviderTypeCallback);

        info = result;

        return result is not null;
    }

    /// <summary>
    /// Gets the <see cref="WindowsRuntimeObjectMarshallerAttribute"/> instance associated with the current metadata provider type.
    /// </summary>
    /// <returns>The resulting <see cref="WindowsRuntimeObjectMarshallerAttribute"/> instance.</returns>
    /// <exception cref="NotSupportedException">Thrown if no <see cref="WindowsRuntimeObjectMarshallerAttribute"/> instance could be resolved.</exception>
    /// <remarks>
    /// This method is meant to be used when marshalling user-defined types to native. In this case, the marshalling info should point to
    /// the generated (or built-in) proxy types, which will always have a marshaller attribute on them. Other scenarios are not supported.
    /// </remarks>
    public WindowsRuntimeObjectMarshallerAttribute GetMarshaller()
    {
        if (!TryGetMarshaller(out WindowsRuntimeObjectMarshallerAttribute? marshaller))
        {
            // All projected types will have an associated marshaller, so this could only
            // happen with some proxy types that were not configured correctly. In practice,
            // this failure case should never happen for valid invocations of this method.
            [DoesNotReturn]
            [StackTraceHidden]
            void ThrowNotSupportedException()
            {
                throw new NotSupportedException(
                    $"The metadata provider type '{_metadataProviderType}' does not have any associated marshalling logic. " +
                    $"This should never be the case. Please file an issue at https://github.com/microsoft/CsWinRT.");
            }

            ThrowNotSupportedException();
        }

        return marshaller;
    }

    /// <summary>
    /// Tries to get the <see cref="WindowsRuntimeObjectMarshallerAttribute"/> instance associated with the current metadata provider type.
    /// </summary>
    /// <param name="marshaller">The resulting <see cref="WindowsRuntimeObjectMarshallerAttribute"/> instance, if available.</param>
    /// <returns>Whether <paramref name="marshaller"/> was retrieved successfully.</returns>
    /// <remarks>This will not be present for eg. types not implementing any Windows Runtime interfaces, which are also not projected.</remarks>
    public bool TryGetMarshaller([NotNullWhen(true)] out WindowsRuntimeObjectMarshallerAttribute? marshaller)
    {
        // Initializes the 'WindowsRuntimeMarshallerAttribute' instance, if present
        [MethodImpl(MethodImplOptions.NoInlining)]
        bool Load([NotNullWhen(true)] out WindowsRuntimeObjectMarshallerAttribute? marshaller)
        {
            WindowsRuntimeObjectMarshallerAttribute? value = _metadataProviderType.GetCustomAttribute<WindowsRuntimeObjectMarshallerAttribute>(inherit: false);

            value ??= PlaceholderWindowsRuntimeMarshallerAttribute.Instance;

            _marshaller = value;

            if (value is not (null or PlaceholderWindowsRuntimeMarshallerAttribute))
            {
                marshaller = value;

                return true;
            }

            marshaller = null;

            return false;
        }

        WindowsRuntimeObjectMarshallerAttribute? value = _marshaller;

        // We have a cached marshaller, so return it immediately
        if (value is not null)
        {
            if (value is PlaceholderWindowsRuntimeMarshallerAttribute)
            {
                marshaller = null;

                return false;
            }

            marshaller = value;

            return true;
        }

        return Load(out marshaller);
    }

    /// <summary>
    /// Gets the <see cref="WindowsRuntimeVtableProviderAttribute"/> instance associated with the current metadata provider type.
    /// </summary>
    /// <returns>The resulting <see cref="WindowsRuntimeVtableProviderAttribute"/> instance.</returns>
    /// <exception cref="NotSupportedException">Thrown if no <see cref="WindowsRuntimeVtableProviderAttribute"/> instance could be resolved.</exception>
    /// <remarks>This method is meant to be used when preparing CCW vtables for managed types.</remarks>
    public WindowsRuntimeVtableProviderAttribute GetVtableProvider()
    {
        if (!TryGetVtableProvider(out WindowsRuntimeVtableProviderAttribute? vtableProvider))
        {
            // Analogous validation as for when retrieving the marshaller attribute
            [DoesNotReturn]
            [StackTraceHidden]
            void ThrowNotSupportedException()
            {
                throw new NotSupportedException(
                    $"The metadata provider type '{_metadataProviderType}' does not have any associated vtable provider logic. " +
                    $"This should never be the case. Please file an issue at https://github.com/microsoft/CsWinRT.");
            }

            ThrowNotSupportedException();
        }

        return vtableProvider;
    }

    /// <summary>
    /// Tries to get the <see cref="WindowsRuntimeVtableProviderAttribute"/> instance associated with the current metadata provider type.
    /// </summary>
    /// <param name="vtableProvider">The resulting <see cref="WindowsRuntimeVtableProviderAttribute"/> instance, if available.</param>
    /// <returns>Whether <paramref name="vtableProvider"/> was retrieved successfully.</returns>
    /// <remarks>This will not be present for eg. projected Windows Runtime types, as their vtable will be implemented in native code.</remarks>
    public bool TryGetVtableProvider([NotNullWhen(true)] out WindowsRuntimeVtableProviderAttribute? vtableProvider)
    {
        // Initializes the 'WindowsRuntimeVtableProviderAttribute' instance, if present
        [MethodImpl(MethodImplOptions.NoInlining)]
        bool Load([NotNullWhen(true)] out WindowsRuntimeVtableProviderAttribute? vtableProvider)
        {
            WindowsRuntimeVtableProviderAttribute? value = _metadataProviderType.GetCustomAttribute<WindowsRuntimeVtableProviderAttribute>(inherit: false);

            value ??= PlaceholderWindowsRuntimeVtableProviderAttribute.Instance;

            _vtableProvider = value;

            if (value is not (null or PlaceholderWindowsRuntimeVtableProviderAttribute))
            {
                vtableProvider = value;

                return true;
            }

            vtableProvider = null;

            return false;
        }

        WindowsRuntimeVtableProviderAttribute? value = _vtableProvider;

        // We have a cached marshaller, so return it immediately just like above
        if (value is not null)
        {
            if (value is PlaceholderWindowsRuntimeVtableProviderAttribute)
            {
                vtableProvider = null;

                return false;
            }

            vtableProvider = value;

            return true;
        }

        return Load(out vtableProvider);
    }

    /// <summary>
    /// Gets the <see cref="WindowsRuntimeVtableProviderAttribute"/> instance associated with the current metadata provider type.
    /// </summary>
    /// <returns>The resulting <see cref="WindowsRuntimeVtableProviderAttribute"/> instance.</returns>
    /// <exception cref="NotSupportedException">Thrown if no <see cref="WindowsRuntimeVtableProviderAttribute"/> instance could be resolved.</exception>
    /// <remarks>This method is meant to be used when preparing CCW vtables for managed types.</remarks>
    public WindowsRuntimeVtableInfo GetVtableInfo()
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        WindowsRuntimeVtableInfo InitializeVtableInfo()
        {
            // Initialize the lock if it hasn't been done yet
            _ = Interlocked.CompareExchange(
                location1: ref _lock,
                value: new Lock(),
                comparand: null);

            // Lock and initialize the vtable if still not available
            lock (_lock)
            {
                return _vtableInfo ??= WindowsRuntimeVtableInfo.CreateUnsafe(this);
            }
        }

        return _vtableInfo ?? InitializeVtableInfo();
    }

    /// <summary>
    /// Gets the runtime class name for the public type associated with the current metadata provider type.
    /// </summary>
    /// <returns>The resulting runtime class name.</returns>
    /// <exception cref="NotSupportedException">Thrown if no runtime class name could be resolved.</exception>
    /// <remarks>This method is only meant to be used on managed types passed to native.</remarks>
    public string GetRuntimeClassName()
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        string InitializeRuntimeClassName()
        {
            WindowsRuntimeClassNameAttribute? runtimeClassNameAttribute =
                _metadataProviderType.GetCustomAttribute<WindowsRuntimeClassNameAttribute>(inherit: false)
                ?? PublicType.GetCustomAttribute<WindowsRuntimeClassNameAttribute>(inherit: false);

            if (runtimeClassNameAttribute is null)
            {
                // Analogous validation as for when retrieving the marshaller attribute
                [DoesNotReturn]
                [StackTraceHidden]
                void ThrowNotSupportedException()
                {
                    throw new NotSupportedException(
                        $"The metadata provider type '{_metadataProviderType}' does not have any runtime class name info. " +
                        $"This should never be the case. Please file an issue at https://github.com/microsoft/CsWinRT.");
                }

                ThrowNotSupportedException();
            }

            return _runtimeClassName = runtimeClassNameAttribute.RuntimeClassName;
        }

        return _runtimeClassName ?? InitializeRuntimeClassName();
    }

    /// <summary>
    /// Creates a <see cref="WindowsRuntimeMarshallingInfo"/> instance for a specified metadata provider type.
    /// </summary>
    /// <param name="metadataProviderType">The metadata provider type to wrap.</param>
    /// <returns>The resulting <see cref="WindowsRuntimeMarshallingInfo"/> instance.</returns>
    private static WindowsRuntimeMarshallingInfo CreateMarshallingInfo(Type metadataProviderType)
    {
        // If '[WindowsRuntimeType]' is defined, this is a projected type, so it's the public type too.
        // Otherwise, we don't know what the public type is at this point. We could look it up now, but
        // since we don't need that information right away, we can delay this to later to reduce the
        // overhead at startup. That value is only needed eg. when associating native memory for vtables.
        return metadataProviderType.IsDefined(typeof(WindowsRuntimeTypeAttribute), inherit: false)
            ? new(metadataProviderType, metadataProviderType)
            : new(publicType: null, metadataProviderType);
    }

    /// <summary>
    /// Creates a <see cref="WindowsRuntimeMarshallingInfo"/> instance associated with a given managed type, if possible.
    /// </summary>
    /// <param name="managedType">The managed type to create an instance for, if possible..</param>
    /// <returns>The resulting <see cref="WindowsRuntimeMarshallingInfo"/> instance, if created successfully.</returns>
    private static WindowsRuntimeMarshallingInfo? GetMetadataProviderType(Type managedType)
    {
        // Same as above: if the type is a projected type, then it is also used as the metadata source
        if (managedType.IsDefined(typeof(WindowsRuntimeTypeAttribute), inherit: false))
        {
            return new(managedType, managedType);
        }

        // Check if we have a mapped proxy type for this managed type. If we do, that type
        // will be the metadata provider, and the current managed type will be the public
        // type. In this case, we don't need to query for '[WindowsRuntimeMappedType]'.
        if (WindowsRuntimeProxyTypes.TryGetValue(managedType, out Type? proxyType))
        {
            return new(managedType, proxyType);
        }

        // We don't have a metadata provider for the type (we'll just marshal it as a generic 'IInspectable')
        return null;
    }
}

/// <summary>
/// A placeholder <see cref="WindowsRuntimeObjectMarshallerAttribute"/> type.
/// </summary>
file sealed unsafe class PlaceholderWindowsRuntimeMarshallerAttribute : WindowsRuntimeObjectMarshallerAttribute
{
    /// <summary>
    /// The shared placeholder instance.
    /// </summary>
    public static PlaceholderWindowsRuntimeMarshallerAttribute Instance = new();

    /// <inheritdoc/>
    public override unsafe void* ConvertToUnmanagedUnsafe(object? value)
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
