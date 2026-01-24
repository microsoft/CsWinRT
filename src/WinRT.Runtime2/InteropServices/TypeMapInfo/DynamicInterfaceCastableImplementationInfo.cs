// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A type providing cached information on interface types used for dynamic interface castable implementations.
/// </summary>
internal sealed class DynamicInterfaceCastableImplementationInfo
{
    /// <summary>
    /// The proxy types mapping for <see cref="DynamicInterfaceCastableImplementationAttribute"/> types.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "We expect only proxy types for constructed types to be preserved.")]
    private static readonly IReadOnlyDictionary<Type, Type> ProxyTypeMapping = TypeMapping.GetOrCreateProxyTypeMapping<DynamicInterfaceCastableImplementationTypeMapGroup>();

    /// <summary>
    /// The table of marshalling info for all types that can participate in marshalling.
    /// </summary>
    private static readonly ConditionalWeakTable<Type, DynamicInterfaceCastableImplementationInfo?> TypeToImplementationInfoTable = [];

    /// <summary>
    /// Cached creation factory for <see cref="CreateImplementationInfo"/>.
    /// </summary>
    private static readonly Func<Type, DynamicInterfaceCastableImplementationInfo?> CreateImplementationInfoCallback = new(CreateImplementationInfo);

    /// <summary>
    /// The cached <see cref="DynamicInterfaceCastableForwarderAttribute"/> instance (possibly a placeholder).
    /// </summary>
    private volatile DynamicInterfaceCastableForwarderAttribute? _implementationForwarder;

    /// <summary>
    /// Creates a new <see cref="DynamicInterfaceCastableImplementationInfo"/> instance with the specified parameters.
    /// </summary>
    /// <param name="implementationType">The implementation type associated with the current instance.</param>
    private DynamicInterfaceCastableImplementationInfo(Type implementationType)
    {
        ImplementationType = implementationType;
    }

    /// <summary>
    /// Gets the implementation type associated with the current instance.
    /// </summary>
    public Type ImplementationType { get; }

    /// <summary>
    /// Gets a <see cref="DynamicInterfaceCastableImplementationInfo"/> instance for a given interface type.
    /// </summary>
    /// <param name="interfaceType">The input interface type to use for lookups.</param>
    /// <returns>The resulting <see cref="DynamicInterfaceCastableImplementationInfo"/> instance.</returns>
    /// <exception cref="NotSupportedException">Thrown if no <see cref="DynamicInterfaceCastableImplementationInfo"/> instance could be resolved.</exception>
    public static DynamicInterfaceCastableImplementationInfo GetInfo(Type interfaceType)
    {
        if (!TryGetInfo(interfaceType, out DynamicInterfaceCastableImplementationInfo? info))
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowNotSupportedException(Type interfaceType)
            {
                throw new NotSupportedException(
                    $"The interface type '{interfaceType}' does not have any associated '[DynamicCastableImplementation]' info.");
            }

            ThrowNotSupportedException(interfaceType);
        }

        return info;
    }

    /// <summary>
    /// Tries to get a <see cref="DynamicInterfaceCastableImplementationInfo"/> instance for a given interface type.
    /// </summary>
    /// <param name="interfaceType">The input interface type to use for lookups.</param>
    /// <param name="info">The resulting <see cref="DynamicInterfaceCastableImplementationInfo"/> instance, if found.</param>
    /// <returns>Whether <paramref name="info"/> was retrieved successfully.</returns>
    public static bool TryGetInfo(Type interfaceType, [NotNullWhen(true)] out DynamicInterfaceCastableImplementationInfo? info)
    {
        DynamicInterfaceCastableImplementationInfo? result = TypeToImplementationInfoTable.GetOrAdd(interfaceType, CreateImplementationInfoCallback);

        info = result;

        return result is not null;
    }

    /// <summary>
    /// Gets the <see cref="DynamicInterfaceCastableForwarderAttribute"/> instance associated with the current metadata provider type.
    /// </summary>
    /// <returns>The resulting <see cref="DynamicInterfaceCastableForwarderAttribute"/> instance.</returns>
    /// <exception cref="NotSupportedException">Thrown if no <see cref="DynamicInterfaceCastableForwarderAttribute"/> instance could be resolved.</exception>
    public DynamicInterfaceCastableForwarderAttribute GetDynamicInterfaceCastableForwarder()
    {
        if (!TryGetDynamicInterfaceCastableForwarder(out DynamicInterfaceCastableForwarderAttribute? forwarder))
        {
            [DoesNotReturn]
            [StackTraceHidden]
            void ThrowNotSupportedException()
            {
                throw new NotSupportedException(
                    $"The implementation type '{ImplementationType}' does not have any associated forwarder logic. " +
                    $"This should never be the case. Please file an issue at https://github.com/microsoft/CsWinRT.");
            }

            ThrowNotSupportedException();
        }

        return forwarder;
    }

    /// <summary>
    /// Tries to get the <see cref="DynamicInterfaceCastableForwarderAttribute"/> instance associated with the current metadata provider type.
    /// </summary>
    /// <param name="forwarder">The resulting <see cref="DynamicInterfaceCastableForwarderAttribute"/> instance, if available.</param>
    /// <returns>Whether <paramref name="forwarder"/> was retrieved successfully.</returns>
    public bool TryGetDynamicInterfaceCastableForwarder([NotNullWhen(true)] out DynamicInterfaceCastableForwarderAttribute? forwarder)
    {
        // Initializes the 'DynamicInterfaceCastableForwarderAttribute' instance, if present
        [MethodImpl(MethodImplOptions.NoInlining)]
        bool Load([NotNullWhen(true)] out DynamicInterfaceCastableForwarderAttribute? forwarder)
        {
            DynamicInterfaceCastableForwarderAttribute? value = ImplementationType.GetCustomAttribute<DynamicInterfaceCastableForwarderAttribute>(inherit: false);

            value ??= PlaceholderDynamicInterfaceCastableForwarderAttribute.Instance;

            _implementationForwarder = value;

            if (value is not (null or PlaceholderDynamicInterfaceCastableForwarderAttribute))
            {
                forwarder = value;

                return true;
            }

            forwarder = null;

            return false;
        }

        DynamicInterfaceCastableForwarderAttribute? value = _implementationForwarder;

        // We have a cached forwarder, so return it immediately
        if (value is not null)
        {
            if (value is PlaceholderDynamicInterfaceCastableForwarderAttribute)
            {
                forwarder = null;

                return false;
            }

            forwarder = value;

            return true;
        }

        return Load(out forwarder);
    }

    /// <summary>
    /// Creates a <see cref="DynamicInterfaceCastableImplementationInfo"/> instance for a specified interface type.
    /// </summary>
    /// <param name="interfaceType">The interface type to wrap.</param>
    /// <returns>The resulting <see cref="DynamicInterfaceCastableImplementationInfo"/> instance.</returns>
    private static DynamicInterfaceCastableImplementationInfo? CreateImplementationInfo(Type interfaceType)
    {
        // If we can resolve an implementation type, return it
        if (ProxyTypeMapping.TryGetValue(interfaceType, out Type? proxyType))
        {
            return new(proxyType);
        }

        // There's no '[DynamicCastableImplementation]' info for the provided interface type
        return null;
    }
}

/// <summary>
/// A placeholder <see cref="DynamicInterfaceCastableForwarderAttribute"/> type.
/// </summary>
file sealed class PlaceholderDynamicInterfaceCastableForwarderAttribute : DynamicInterfaceCastableForwarderAttribute
{
    /// <summary>
    /// The shared placeholder instance.
    /// </summary>
    public static PlaceholderDynamicInterfaceCastableForwarderAttribute Instance = new();

    /// <inheritdoc/>
    public override bool IsInterfaceImplemented(WindowsRuntimeObject thisReference, [NotNullWhen(true)] out WindowsRuntimeObjectReference? interfaceReference)
    {
        interfaceReference = null;

        return false;
    }
}