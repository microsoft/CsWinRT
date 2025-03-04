// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using WindowsRuntime.InteropServices;

namespace WindowsRuntime;

/// <summary>
/// The base class for all projected Windows Runtime types.
/// </summary>
/// <remarks>
/// This type should only be used as a base type by generated projected types.
/// </remarks>
public abstract class WindowsRuntimeObject :
    IDynamicInterfaceCastable,
    IUnmanagedVirtualMethodTableProvider,
    ICustomQueryInterface
{
    /// <summary>
    /// The lazy-loaded cache of additional data associated to type handles.
    /// </summary>
    private volatile ConcurrentDictionary<RuntimeTypeHandle, object>? _typeHandleCache;

    /// <summary>
    /// Creates a <see cref="WindowsRuntimeObject"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObject">The inner Windows Runtime object to wrap in the current instance.</param>
    protected WindowsRuntimeObject(WindowsRuntimeObjectReference nativeObject)
    {
        ArgumentNullException.ThrowIfNull(nativeObject);

        NativeObject = nativeObject;
    }

    /// <summary>
    /// Gets the lazy-loaded cache of additional data associated to type handles.
    /// </summary>
    private protected ConcurrentDictionary<RuntimeTypeHandle, object> TypeHandleCache
    {
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            ConcurrentDictionary<RuntimeTypeHandle, object> InitializeTypeHandleCache()
            {
                _ = Interlocked.CompareExchange(
                    location1: ref _typeHandleCache,
                    value: new ConcurrentDictionary<RuntimeTypeHandle, object>(concurrencyLevel: 1, capacity: 16),
                    comparand: null);

                return _typeHandleCache;
            }

            return _typeHandleCache ?? InitializeTypeHandleCache();
        }
    }

    /// <summary>
    /// Gets the inner Windows Runtime object for the current instance.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected internal WindowsRuntimeObjectReference NativeObject { get; }

    /// <summary>
    /// Gets a value indicating whether the current instance has an unwrappable native object.
    /// </summary>
    /// <remarks>
    /// This value is <see langword="false"/> in aggregation scenarios, as the instance that should be marshalled
    /// to native is the derived managed type for the projected class, and not the inner object for the base type.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected internal bool HasUnwrappableNativeObject { get; }

    /// <summary>
    /// Determines whether a given interface is an overridable interface for the current type.
    /// </summary>
    /// <param name="iid">The interface to check.</param>
    /// <returns>Whether the interface represented by <paramref name="iid"/> is an overridable interface for the current type.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected abstract bool IsOverridableInterface(in Guid iid);

    /// <summary>
    /// Retrieves a <see cref="WindowsRuntimeObjectReference"/> object for the specified interface.
    /// </summary>
    /// <param name="interfaceType">The type handle for the interface to retrieve the object reference for.</param>
    /// <returns>The resulting <see cref="WindowsRuntimeObjectReference"/> object.</returns>
    /// <exception cref="Exception">Thrown if the interface specified by <paramref name="interfaceType"/> is not implemented.</exception>
    internal WindowsRuntimeObjectReference GetObjectReferenceForInterface(RuntimeTypeHandle interfaceType)
    {
        throw null!;
    }

    /// <inheritdoc/>
    RuntimeTypeHandle IDynamicInterfaceCastable.GetInterfaceImplementation(RuntimeTypeHandle interfaceType)
    {
        throw null!;
    }

    /// <inheritdoc/>
    bool IDynamicInterfaceCastable.IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
    {
        return false;
    }

    /// <inheritdoc/>
    VirtualMethodTableInfo IUnmanagedVirtualMethodTableProvider.GetVirtualMethodTableInfoForKey(Type type)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    CustomQueryInterfaceResult ICustomQueryInterface.GetInterface(ref Guid iid, out nint ppv)
    {
        // We explicitly don't handle overridable interfaces and 'IInspectable'
        if (IsOverridableInterface(in iid) || WellKnownInterfaceIds.IID_IInspectable == iid)
        {
            ppv = (nint)null;

            return CustomQueryInterfaceResult.NotHandled;
        }

        // Delegate the 'QueryInterface' call to the inner object reference
        return NativeObject.TryAsUnsafe(in iid, out ppv)
            ? CustomQueryInterfaceResult.Handled
            : CustomQueryInterfaceResult.NotHandled;
    }
}
