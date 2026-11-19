// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

#pragma warning disable CS1573

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="WindowsRuntimeObjectReference"/>
public unsafe partial class WindowsRuntimeObjectReference
{
    /// <returns>A <see cref="WindowsRuntimeObjectReference"/> instance wrapping an instance of the activation factory for the specified Windows Runtime type.</returns>
    /// <inheritdoc cref="WindowsRuntimeActivationFactory.GetActivationFactoryUnsafe(string)"/>
    public static WindowsRuntimeObjectReference GetActivationFactory(string runtimeClassName)
    {
        void* activationFactory = WindowsRuntimeActivationFactory.GetActivationFactoryUnsafe(runtimeClassName);

        return AttachUnsafe(ref activationFactory, in WellKnownWindowsInterfaceIIDs.IID_IActivationFactory)!;
    }

    /// <param name="iid">The IID of the interface pointer (from the resolved activation factory) to wrap in the returned object reference.</param>
    /// <returns>A <see cref="WindowsRuntimeObjectReference"/> instance wrapping an instance of the activation factory for the specified Windows Runtime type.</returns>
    /// <inheritdoc cref="WindowsRuntimeActivationFactory.GetActivationFactoryUnsafe(string)"/>
    public static WindowsRuntimeObjectReference GetActivationFactory(string runtimeClassName, in Guid iid)
    {
        void* activationFactory = WindowsRuntimeActivationFactory.GetActivationFactoryUnsafe(runtimeClassName, in iid);

        return AttachUnsafe(ref activationFactory, in iid)!;
    }

    /// <summary>
    /// Gets a cached activation factory for the specified Windows Runtime type, resolving a new one if needed.
    /// </summary>
    /// <param name="location">The location of the cached activation factory (initially <see langword="null"/>), which is updated whenever a new activation factory is resolved.</param>
    /// <param name="iid">The IID of the interface pointer (from the resolved activation factory) to wrap in the returned object reference.</param>
    /// <returns>A <see cref="WindowsRuntimeObjectReference"/> instance wrapping an instance of the activation factory for the specified Windows Runtime type.</returns>
    /// <remarks>
    /// <para>
    /// The cached activation factory is reused as long as it can be used from the current context, and as long as it has not been marked as
    /// disconnected (ie. a previous call on it failed because its server is no longer available, eg. an out-of-process server that died). In
    /// all other cases, a new activation factory is resolved and stored in <paramref name="location"/>. Note that calls are never retried: the
    /// call that observes a disconnected server still fails, and only the following accesses will use a new activation factory.
    /// </para>
    /// <para>
    /// If multiple threads resolve a new activation factory concurrently, the last one to be stored wins (all resolved instances are valid).
    /// </para>
    /// </remarks>
    /// <inheritdoc cref="WindowsRuntimeActivationFactory.GetActivationFactoryUnsafe(string)"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WindowsRuntimeObjectReference GetActivationFactory(
        ref WindowsRuntimeObjectReference? location,
        string runtimeClassName,
        in Guid iid)
    {
        // Fast path for free-threaded activation factories that are not disconnected (ie. almost always). Context-aware activation
        // factories always go through the slow path, which also checks whether they're in the current context. We intentionally don't
        // check 'IsInCurrentContext' here: it calls into a virtual method, and the guarded devirtualization for it would inline a copy
        // of that check for each context-aware type (plus the 'CoGetContextToken' call) into every getter, bloating binary size.
        // The slot is read with a volatile operation, as it can be updated concurrently by other threads.
        if (Volatile.Read(ref location) is { IsFreeThreaded: true, IsDisconnected: false } activationFactory)
        {
            return activationFactory;
        }

        // Slow path: there's no cached activation factory yet, or it's context-aware, or it's disconnected. Unlike the fast path above,
        // this also handles reusing a context-aware factory from the right context, and resolving (and caching) a new activation factory.
        return GetOrResolveActivationFactory(ref location, runtimeClassName, in iid);
    }

    /// <inheritdoc cref="GetActivationFactory(ref WindowsRuntimeObjectReference?, string, in Guid)"/>
    /// <remarks>
    /// This overload resolves the default <c>IActivationFactory</c> interface (see <see cref="GetActivationFactory(string)"/>).
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static WindowsRuntimeObjectReference GetActivationFactory(ref WindowsRuntimeObjectReference? location, string runtimeClassName)
    {
        // A 'null' IID means that the default 'IActivationFactory' interface should be resolved (same as for 'WindowsRuntimeActivationFactory')
        return GetActivationFactory(ref location, runtimeClassName, in Unsafe.NullRef<Guid>());
    }

    /// <param name="activationFactory">A <see cref="WindowsRuntimeObjectReference"/> instance wrapping an instance of the activation factory for the specified Windows Runtime type, if successfully retrieved.</param>
    /// <inheritdoc cref="WindowsRuntimeActivationFactory.TryGetActivationFactoryUnsafe(string, out void*)"/>
    public static bool TryGetActivationFactory(string runtimeClassName, [NotNullWhen(true)] out WindowsRuntimeObjectReference? activationFactory)
    {
        if (!WindowsRuntimeActivationFactory.TryGetActivationFactoryUnsafe(runtimeClassName, out void* activationFactoryPtr))
        {
            activationFactory = null;

            return false;
        }

        activationFactory = AttachUnsafe(ref activationFactoryPtr, in WellKnownWindowsInterfaceIIDs.IID_IActivationFactory)!;

        return true;
    }

    /// <param name="activationFactory">A <see cref="WindowsRuntimeObjectReference"/> instance wrapping an instance of the activation factory for the specified Windows Runtime type, if successfully retrieved.</param>
    /// <inheritdoc cref="WindowsRuntimeActivationFactory.TryGetActivationFactoryUnsafe(string, in Guid, out void*)"/>
    public static bool TryGetActivationFactory(string runtimeClassName, in Guid iid, [NotNullWhen(true)] out WindowsRuntimeObjectReference? activationFactory)
    {
        if (!WindowsRuntimeActivationFactory.TryGetActivationFactoryUnsafe(runtimeClassName, in iid, out void* activationFactoryPtr))
        {
            activationFactory = null;

            return false;
        }

        activationFactory = AttachUnsafe(ref activationFactoryPtr, in iid)!;

        return true;
    }

    /// <summary>
    /// Gets the cached activation factory if it can be reused, or resolves (and caches) a new one otherwise.
    /// </summary>
    /// <param name="iid">The IID of the interface pointer to resolve, or a <see langword="null"/> reference for the default <c>IActivationFactory</c> interface.</param>
    /// <inheritdoc cref="GetActivationFactory(ref WindowsRuntimeObjectReference?, string, in Guid)"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WindowsRuntimeObjectReference GetOrResolveActivationFactory(
        ref WindowsRuntimeObjectReference? location,
        string runtimeClassName,
        in Guid iid)
    {
        // The fast path above only accepts free-threaded activation factories, so this is where cached context-aware ones are validated.
        // They can be reused if they're not disconnected and in the current context. Without this check, they'd be resolved on every access.
        if (Volatile.Read(ref location) is { IsDisconnected: false, IsInCurrentContext: true } cachedActivationFactory)
        {
            return cachedActivationFactory;
        }

        WindowsRuntimeObjectReference activationFactory = Unsafe.IsNullRef(in iid)
            ? GetActivationFactory(runtimeClassName)
            : GetActivationFactory(runtimeClassName, in iid);

        Volatile.Write(ref location, activationFactory);

        return activationFactory;
    }
}