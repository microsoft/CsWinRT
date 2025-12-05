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
/// A type providing cached information on a Windows Runtime type, either projected or managed only.
/// </summary>
internal sealed class WindowsRuntimeMarshallingInfo
{
    /// <summary>
    /// The external types mapping for Windows Runtime types.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We expect external types to only be preserved if used in runtime casts.")]
    private static readonly IReadOnlyDictionary<string, Type> ExternalTypeMapping = TypeMapping.GetOrCreateExternalTypeMapping<WindowsRuntimeComWrappersTypeMapGroup>();

    /// <summary>
    /// The proxy types mapping for Windows Runtime types.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We expect only proxy types for constructed types to be preserved.")]
    private static readonly IReadOnlyDictionary<Type, Type> ProxyTypeMapping = TypeMapping.GetOrCreateProxyTypeMapping<WindowsRuntimeComWrappersTypeMapGroup>();

    /// <summary>
    /// The cached external types mapping for Windows Runtime types.
    /// </summary>
    /// <remarks>
    /// This is used to introduce a cache layer around <see cref="ExternalTypeMapping"/>. It is done for two reasons:
    /// allow lookups without always instantiating a <see cref="string"/>, and reducing the overhead. Lookups into the actual
    /// runtime-provided dictionary can be quite expensive, as they might also do UTF8 transcoding internally.
    /// </remarks>
    private static readonly ConcurrentDictionary<string, Type?> TypeNameToMappedTypeDictionary = [];

    /// <summary>
    /// The cached marshalling infos for Windows Runtime types, accounting for inheritance.
    /// </summary>
    /// <remarks>
    /// This is used to avoid traversing the type hierarchy for a given type multiple times.
    /// </remarks>
    private static readonly ConcurrentDictionary<string, WindowsRuntimeMarshallingInfo?> TypeNameToMostDerivedMarshallingInfoDictionary = [];

    /// <summary>
    /// The table of marshalling info for all types that can participate in marshalling.
    /// </summary>
    /// <remarks>
    /// This will only have non <see langword="null"/> values for types needing special marshalling. Types which are meant to
    /// be marshalled as opaque <c>IInspectable</c> objects will have no associated values, and should be handled separately.
    /// </remarks>
    private static readonly ConditionalWeakTable<Type, WindowsRuntimeMarshallingInfo?> TypeToMarshallingInfoTable = [];

    /// <summary>
    /// Cached creation factory for <see cref="CreateMarshallingInfo"/>.
    /// </summary>
    private static readonly Func<Type, WindowsRuntimeMarshallingInfo?> CreateMarshallingInfoCallback = new(CreateMarshallingInfo);

    /// <summary>
    /// Cached creation factory for <see cref="GetMetadataProviderType"/>.
    /// </summary>
    private static readonly Func<Type, WindowsRuntimeMarshallingInfo?> GetMetadataProviderTypeCallback = new(GetMetadataProviderType);

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
    ///     The same applies to custom-mapped types (e.g. fundamental types).
    ///   </item>
    /// </list>
    /// </remarks>
    private readonly Type _metadataProviderType;

    /// <summary>
    /// The public type associated with the current instance (ie. the type that would be used directly by developers).
    /// </summary>
    private volatile Type? _publicType;

    /// <summary>
    /// The reference type (a constructed <see cref="Nullable{T}"/> type) for the current instance.
    /// </summary>
    private volatile Type? _referenceType;

    /// <summary>
    /// The cached <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> instance (possibly a placeholder).
    /// </summary>
    private volatile WindowsRuntimeComWrappersMarshallerAttribute? _comWrappersMarshaller;

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
    private volatile string? _runtimeClassName;

    /// <summary>
    /// The cached metadata type name for the type.
    /// </summary>
    /// <remarks>
    /// This is only used for <see cref="Type"/> marshalling, and it will only be available for some types (e.g. value types).
    /// </remarks>
    private volatile string? _metadataTypeName;

    /// <summary>
    /// A flag indicating whether the current type is a type defined in metadata (either projected or custom-mapped).
    /// </summary>
    /// <remarks>
    /// A value of <c>-1</c> indicates a value that has not been computed yet.
    /// </remarks>
    private volatile int _isMetadataType;

    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeMarshallingInfo"/> instance with the specified parameters.
    /// </summary>
    /// <param name="metadataProviderType"><inheritdoc cref="_metadataProviderType" path="/summary/node()"/></param>
    /// <param name="publicType"><inheritdoc cref="_publicType" path="/summary/node()"/></param>
    private WindowsRuntimeMarshallingInfo(Type metadataProviderType, Type? publicType)
    {
        _metadataProviderType = metadataProviderType;
        _publicType = publicType;
        _isMetadataType = -1;
    }

    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeMarshallingInfo"/> instance with the specified parameters.
    /// </summary>
    /// <param name="metadataProviderType"><inheritdoc cref="_metadataProviderType" path="/summary/node()"/></param>
    /// <param name="publicType"><inheritdoc cref="_publicType" path="/summary/node()"/></param>
    /// <param name="isMetadataType"><inheritdoc cref="_isMetadataType" path="/summary/node()"/></param>
    private WindowsRuntimeMarshallingInfo(Type metadataProviderType, Type? publicType, bool isMetadataType)
    {
        _metadataProviderType = metadataProviderType;
        _publicType = publicType;
        _isMetadataType = isMetadataType ? 1 : 0;
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
                return _publicType ??= mappedTypeAttribute.PublicType;
            }

            return _publicType ?? InitializePublicType();
        }
    }

    /// <summary>
    /// Gets the reference type (a constructed <see cref="Nullable{T}"/> type) for the current instance.
    /// </summary>
    public Type ReferenceType
    {
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            Type InitializeReferenceType()
            {
                // Try to get the attribute, which should always be present for value types
                WindowsRuntimeReferenceTypeAttribute? referenceTypeAttribute = _metadataProviderType.GetCustomAttribute<WindowsRuntimeReferenceTypeAttribute>(inherit: false);

                // Analogous validation as for when retrieving the marshaller attribute
                [DoesNotReturn]
                [StackTraceHidden]
                void ThrowNotSupportedException()
                {
                    throw new NotSupportedException(
                        $"The metadata provider type '{_metadataProviderType}' does not have an associated reference type. " +
                        $"This code path should have never been reached. Please file an issue at https://github.com/microsoft/CsWinRT.");
                }

                // We expect this to always be present for value types. If the attribute is 'null', it means that
                // either a value type was missing it, or that 'ReferenceType' was accessed for an invalid public
                // type (e.g. some Windows Runtime class type). In both cases, this is a bug, and we should throw.
                if (referenceTypeAttribute is null)
                {
                    ThrowNotSupportedException();
                }

                // Cache the reference type for later (no interlocked operations are needed, same as above)
                return _referenceType ??= referenceTypeAttribute.ReferenceType;
            }

            return _referenceType ?? InitializeReferenceType();
        }
    }

    /// <summary>
    /// Gets whether or not the managed type for the current instance is a Windows Runtime type (either projected or custom-mapped).
    /// </summary>
    public bool IsMetadataType
    {
        get
        {
            // This fallback will only ever be triggered for custom-mapped types (e.g. 'System.Guid').
            // We structure the code this way so this lookup on the proxy is only ever paid in those
            // cases, and only if someone is actually trying to check the value of this property.
            // In practice, this is only needed for 'TypeName' marshalling, which is exclusively a
            // XAML scenario. So this code path should never be hit for any non-UI applications.
            [MethodImpl(MethodImplOptions.NoInlining)]
            bool InitializeIsMetadataType()
            {
                bool isMetadataType = _metadataProviderType.IsDefined(typeof(WindowsRuntimeMetadataAttribute), inherit: false);

                _isMetadataType = isMetadataType ? 1 : 0;

                return isMetadataType;
            }

            // Convert the flag back to a boolean, or compute the deferred attribute lookup
            return _isMetadataType switch
            {
                0 => false,
                1 => true,
                _ => InitializeIsMetadataType()
            };
        }
    }

    /// <summary>
    /// Tries to get a <see cref="WindowsRuntimeMarshallingInfo"/> instance for a given runtime class name.
    /// </summary>
    /// <param name="runtimeClassName">The input runtime class name to use for lookups.</param>
    /// <param name="info">The resulting <see cref="WindowsRuntimeMarshallingInfo"/> instance, if found.</param>
    /// <returns>Whether <paramref name="info"/> was retrieved successfully.</returns>
    public static bool TryGetInfo(ReadOnlySpan<char> runtimeClassName, [NotNullWhen(true)] out WindowsRuntimeMarshallingInfo? info)
    {
        // Tries to get the external type for the input runtime class name
        static Type? TryGetExternalType(ReadOnlySpan<char> runtimeClassName)
        {
            var alternate = TypeNameToMappedTypeDictionary.GetAlternateLookup<ReadOnlySpan<char>>();

            // Check if we already have a cached result (it might be 'null')
            if (alternate.TryGetValue(runtimeClassName, out Type? externalType))
            {
                return externalType;
            }

            // Try to get the external type (which might not be present, if we don't have projections or if the entry has been removed)
            _ = ExternalTypeMapping.TryGetValue(runtimeClassName.ToString(), out externalType);

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
            info = TypeToMarshallingInfoTable.GetOrAdd(externalType, CreateMarshallingInfoCallback)!;

            return true;
        }

        info = null;

        return false;
    }

    /// <summary>
    /// Tries to get a <see cref="WindowsRuntimeMarshallingInfo"/> instance for the most derived type in a given hierarchy.
    /// </summary>
    /// <param name="runtimeClassName">The input runtime class name to use for lookups.</param>
    /// <param name="info">The resulting <see cref="WindowsRuntimeMarshallingInfo"/> instance, if found.</param>
    /// <returns>Whether <paramref name="info"/> was retrieved successfully.</returns>
    /// <remarks>
    /// <para>
    /// This can be used to support runtime type checks for objects marshalled from native to managed.
    /// </para>
    /// <para>
    /// The search will include <paramref name="runtimeClassName"/> itself, if we find marshalling info for it right away.
    /// </para>
    /// </remarks>
    public static bool TryGetMostDerivedInfo(ReadOnlySpan<char> runtimeClassName, [NotNullWhen(true)] out WindowsRuntimeMarshallingInfo? info)
    {
        var alternate = TypeNameToMostDerivedMarshallingInfoDictionary.GetAlternateLookup<ReadOnlySpan<char>>();

        // If we already have a cached info, return it. Note that the result from the map
        // might be 'null', if no marshalling info exists for a given type hierarchy. In that
        // case, we need to check for 'null' here, to return the correct result.
        if (alternate.TryGetValue(runtimeClassName, out info))
        {
            return info is not null;
        }

        // Next, check whether we have info for this exact type. This is needed because even if we're marshalling an unsealed
        // type and we are able to provide a specialized callback for it, it's possible that the actual object being returned
        // is a more derived type that's also projected. In that case, we'll need to still be able to find the marshalling info
        // for that type, and return that. If that is not available, the search will continue upwards and will also pass through
        // the starting type (for which the marshalling info is guaranteed to be present, in this scenario).
        if (TryGetInfo(runtimeClassName, out info))
        {
            // Store the result for later, to avoid repeated lookups. If we're racing with another thread,
            // it doesn't matter if we lose, as the result for a given runtime class name is always the same.
            _ = alternate.TryAdd(runtimeClassName, info);

            return true;
        }

        // Start the lookup with the immediate parent, if this fails we stop immediately
        if (!WindowsRuntimeTypeHierarchy.TryGetBaseRuntimeClassName(
            runtimeClassName: runtimeClassName,
            out ReadOnlySpan<char> baseRuntimeClassName,
            out int nextBaseRuntimeClassNameIndex))
        {
            info = null;

            _ = alternate.TryAdd(runtimeClassName, null);

            return false;
        }

        // After the first lookup, we now have a fast path to walk any remaining base types in the hierarchy.
        // For each of them, we just try to get the marshalling info, and then move up if that failed.
        while (true)
        {
            // Try to find the marshalling info for the base type, and check the value as above
            if (TryGetInfo(baseRuntimeClassName, out info))
            {
                _ = alternate.TryAdd(runtimeClassName, info);

                return info is not null;
            }

            // Move up to the next base type, if available
            if (!WindowsRuntimeTypeHierarchy.TryGetNextBaseRuntimeClassName(
                baseRuntimeClassNameIndex: nextBaseRuntimeClassNameIndex,
                baseRuntimeClassName: out baseRuntimeClassName,
                nextBaseRuntimeClassNameIndex: out nextBaseRuntimeClassNameIndex))
            {
                break;
            }
        }

        _ = alternate.TryAdd(runtimeClassName, null);

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
        WindowsRuntimeMarshallingInfo? result = TypeToMarshallingInfoTable.GetOrAdd(managedType, GetMetadataProviderTypeCallback);

        info = result;

        return result is not null;
    }

    /// <summary>
    /// Gets the <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> instance associated with the current metadata provider type.
    /// </summary>
    /// <returns>The resulting <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> instance.</returns>
    /// <exception cref="NotSupportedException">Thrown if no <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> instance could be resolved.</exception>
    /// <remarks>
    /// This method is meant to be used when marshalling user-defined types to native. In this case, the marshalling info should point to
    /// the generated (or built-in) proxy types, which will always have a marshaller attribute on them. Other scenarios are not supported.
    /// </remarks>
    public WindowsRuntimeComWrappersMarshallerAttribute GetComWrappersMarshaller()
    {
        if (!TryGetComWrappersMarshaller(out WindowsRuntimeComWrappersMarshallerAttribute? marshaller))
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
    /// Tries to get the <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> instance associated with the current metadata provider type.
    /// </summary>
    /// <param name="marshaller">The resulting <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> instance, if available.</param>
    /// <returns>Whether <paramref name="marshaller"/> was retrieved successfully.</returns>
    /// <remarks>This will not be present for eg. types not implementing any Windows Runtime interfaces, which are also not projected.</remarks>
    public bool TryGetComWrappersMarshaller([NotNullWhen(true)] out WindowsRuntimeComWrappersMarshallerAttribute? marshaller)
    {
        // Initializes the 'WindowsRuntimeComWrappersMarshallerAttribute' instance, if present
        [MethodImpl(MethodImplOptions.NoInlining)]
        bool Load([NotNullWhen(true)] out WindowsRuntimeComWrappersMarshallerAttribute? marshaller)
        {
            WindowsRuntimeComWrappersMarshallerAttribute? value = _metadataProviderType.GetCustomAttribute<WindowsRuntimeComWrappersMarshallerAttribute>(inherit: false);

            value ??= PlaceholderWindowsRuntimeComWrappersMarshallerAttribute.Instance;

            _comWrappersMarshaller = value;

            if (value is not (null or PlaceholderWindowsRuntimeComWrappersMarshallerAttribute))
            {
                marshaller = value;

                return true;
            }

            marshaller = null;

            return false;
        }

        WindowsRuntimeComWrappersMarshallerAttribute? value = _comWrappersMarshaller;

        // We have a cached marshaller, so return it immediately
        if (value is not null)
        {
            if (value is PlaceholderWindowsRuntimeComWrappersMarshallerAttribute)
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
    /// Gets the <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> instance associated with the current metadata provider type.
    /// </summary>
    /// <returns>The resulting <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> instance.</returns>
    /// <exception cref="NotSupportedException">Thrown if no <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> instance could be resolved.</exception>
    /// <remarks>This method is meant to be used when preparing CCW vtables for managed types.</remarks>
    public WindowsRuntimeVtableInfo GetVtableInfo()
    {
        // Initialize the vtable if not already present (it is safe to do this concurrently)
        [MethodImpl(MethodImplOptions.NoInlining)]
        unsafe WindowsRuntimeVtableInfo InitializeVtableInfo()
        {
            // Get the '[WindowsRuntimeComWrappersMarshaller]' attribute from the type, to get custom vtable entries.
            // This should always find the attribute. The attribute not being present would mean that somehow
            // our 'ComWrappers' instance tried creating a CCW for a type that had an associated marshalling
            // info, but not a vtable provider. That is, it could only mean the type is a projected type,
            // which should never hit this path, or that the generator somehow didn't generate the attribute.
            // That would be a bug, and it should never happen in practice (and we'd want to crash if it did).
            WindowsRuntimeComWrappersMarshallerAttribute comWrappersMarshaller = GetComWrappersMarshaller();

            // Delegate to the vtable provider to produce the first vtable entries
            ComWrappers.ComInterfaceEntry* vtableEntries = comWrappersMarshaller.ComputeVtables(out int count);

            return _vtableInfo ??= new(vtableEntries, count);
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

            return _runtimeClassName ??= runtimeClassNameAttribute.RuntimeClassName;
        }

        return _runtimeClassName ?? InitializeRuntimeClassName();
    }

    /// <summary>
    /// Gets the metadata type name for the public type associated with the current metadata provider type.
    /// </summary>
    /// <returns>The resulting metadata type name.</returns>
    /// <exception cref="NotSupportedException">Thrown if no metadata type name could be resolved.</exception>
    public string GetMetadataTypeName()
    {
        if (!TryGetMetadataTypeName(out string? metadataTypeName))
        {
            // Analogous validation as for when retrieving the marshaller attribute
            [DoesNotReturn]
            [StackTraceHidden]
            void ThrowNotSupportedException()
            {
                throw new NotSupportedException(
                    $"The metadata provider type '{_metadataProviderType}' does not have any metadata type name info. " +
                    $"This path should never be reached. Please file an issue at https://github.com/microsoft/CsWinRT.");
            }

            ThrowNotSupportedException();
        }

        return metadataTypeName;
    }

    /// <summary>
    /// Tries to get the metadata type name for the public type associated with the current metadata provider type.
    /// </summary>
    /// <param name="metadataTypeName">The resulting metadata type name, if available.</param>
    /// <returns>Whether <paramref name="metadataTypeName"/> was retrieved successfully.</returns>
    public bool TryGetMetadataTypeName([NotNullWhen(true)] out string? metadataTypeName)
    {
        // Initializes the reference type instance, if present
        [MethodImpl(MethodImplOptions.NoInlining)]
        bool Load([NotNullWhen(true)] out string? metadataTypeName)
        {
            WindowsRuntimeMetadataTypeNameAttribute? metadataTypeNameAttribute = _metadataProviderType.GetCustomAttribute<WindowsRuntimeMetadataTypeNameAttribute>(inherit: false);

            if (metadataTypeNameAttribute is null)
            {
                _metadataTypeName ??= "";

                metadataTypeName = null;

                return false;
            }

            _metadataTypeName = metadataTypeNameAttribute.MetadataTypeName;

            metadataTypeName = metadataTypeNameAttribute.MetadataTypeName;

            return true;
        }

        string? value = _metadataTypeName;

        // We have a cached metadata type name, so return it immediately
        if (value is not null)
        {
            if (value is "")
            {
                metadataTypeName = null;

                return false;
            }

            metadataTypeName = value;

            return true;
        }

        return Load(out metadataTypeName);
    }

    /// <summary>
    /// Creates a <see cref="WindowsRuntimeMarshallingInfo"/> instance for a specified metadata provider type.
    /// </summary>
    /// <param name="metadataProviderType">The metadata provider type to wrap.</param>
    /// <returns>The resulting <see cref="WindowsRuntimeMarshallingInfo"/> instance.</returns>
    private static WindowsRuntimeMarshallingInfo CreateMarshallingInfo(Type metadataProviderType)
    {
        // If '[WindowsRuntimeMetadata]' is defined, this is a projected type, so it's the public type too.
        // Otherwise, we don't know what the public type is at this point. We could look it up now, but
        // since we don't need that information right away, we can delay this to later to reduce the
        // overhead at startup. That value is only needed eg. when associating native memory for vtables.
        return metadataProviderType.IsDefined(typeof(WindowsRuntimeMetadataAttribute), inherit: false)
            ? new(metadataProviderType, metadataProviderType, isMetadataType: true)
            : new(metadataProviderType, publicType: null, isMetadataType: false);
    }

    /// <summary>
    /// Creates a <see cref="WindowsRuntimeMarshallingInfo"/> instance associated with a given managed type, if possible.
    /// </summary>
    /// <param name="managedType">The managed type to create an instance for, if possible.</param>
    /// <returns>The resulting <see cref="WindowsRuntimeMarshallingInfo"/> instance, if created successfully.</returns>
    private static WindowsRuntimeMarshallingInfo? GetMetadataProviderType(Type managedType)
    {
        bool isMetadataType = managedType.IsDefined(typeof(WindowsRuntimeMetadataAttribute), inherit: false);

        // Same as above: if the type is a projected type, then it is also used as the metadata source.
        // We need to special-case generic types, as the marshalling code for them is also on proxies.
        if (isMetadataType && !managedType.IsGenericType)
        {
            return new(managedType, publicType: managedType, isMetadataType: true);
        }

        // Check if we have a mapped proxy type for this managed type. If we do, that type
        // will be the metadata provider, and the current managed type will be the public
        // type. In this case, we don't need to query for '[WindowsRuntimeMappedType]'.
        if (ProxyTypeMapping.TryGetValue(managedType, out Type? proxyType))
        {
            // If the managed type is a metadata type, we have all the information we need.
            // However, if the attribute wasn't present, we cannot be certain that the type
            // is not in fact a metadata type, as it could also be a custom-mapped type. In
            // that case, we defer this check to later, with a lookup on the proxy type.
            return isMetadataType
                ? new(proxyType, publicType: managedType, isMetadataType)
                : new(proxyType, publicType: managedType);
        }

        // We don't have a metadata provider for the type (we'll just marshal it as a generic 'IInspectable')
        return null;
    }
}

/// <summary>
/// A placeholder <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> type.
/// </summary>
file sealed unsafe class PlaceholderWindowsRuntimeComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <summary>
    /// The shared placeholder instance.
    /// </summary>
    public static readonly PlaceholderWindowsRuntimeComWrappersMarshallerAttribute Instance = new();

    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        return null;
    }

    /// <inheritdoc/>
    public override ComWrappers.ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = 0;

        return null;
    }

    /// <inheritdoc/>
    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        wrapperFlags = CreatedWrapperFlags.None;

        return null!;
    }
}

/// <summary>
/// A placeholder <see cref="WindowsRuntimeReferenceTypeAttribute"/> type.
/// </summary>
file sealed class PlaceholderWindowsRuntimeReferenceTypeAttribute
{
    /// <summary>
    /// The shared <see cref="WindowsRuntimeReferenceTypeAttribute"/> instance (it will return <see langword="null"/> for its reference type).
    /// </summary>
    public static readonly WindowsRuntimeReferenceTypeAttribute Instance = new(null!);
}