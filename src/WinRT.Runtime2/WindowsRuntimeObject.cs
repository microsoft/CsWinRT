// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
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
    private static class _IApplicationFactoryMethods
    {
        public unsafe static nint CreateInstance(IObjectReference _obj, object baseInterface, out nint innerInterface)
        {
            nint thisPtr = _obj.ThisPtr;
            ObjectReferenceValue value = default(ObjectReferenceValue);
            nint num = 0;
            nint result = 0;
            try
            {
                value = MarshalInspectable<object>.CreateMarshaler2(baseInterface);
                ExceptionHelpers.ThrowExceptionForHR(((delegate* unmanaged[Stdcall]<nint, nint, nint*, nint*, int>)(*(IntPtr*)((nint)(*(IntPtr*)thisPtr) + (nint)6 * (nint)sizeof(delegate* unmanaged[Stdcall]<nint, nint, nint*, nint*, int>))))(thisPtr, MarshalInspectable<object>.GetAbi(value), &num, &result));
                GC.KeepAlive(_obj);
                innerInterface = num;
                return result;
            }
            finally
            {
                MarshalInspectable<object>.DisposeMarshaler(value);
            }
        }
    }

    /// <summary>
    /// The lazy-loaded, cached object reference for <c>IInspectable</c> for the current object.
    /// </summary>
    /// <remarks>
    /// This is used when marshalling projected types as 'object'. Having a dedicated field to be able
    /// to do this efficiently is worth it, as in some important scenarios (eg. XAML) it is extremely
    /// common to have Windows Runtime APIs just taking 'object' as a parameter. We would not want
    /// to constantly have to do 'QueryInterface' calls in those cases in each marshalling stub.
    /// </remarks>
    private volatile WindowsRuntimeObjectReference? _inspectableObjectReference;

    /// <summary>
    /// The lazy-loaded cache of additional data associated to type handles.
    /// </summary>
    private volatile ConcurrentDictionary<RuntimeTypeHandle, object>? _typeHandleCache;

    /// <summary>
    /// Creates a <see cref="WindowsRuntimeObject"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    protected WindowsRuntimeObject(WindowsRuntimeObjectReference nativeObjectReference)
    {
        ArgumentNullException.ThrowIfNull(nativeObjectReference);

        NativeObjectReference = nativeObjectReference;
    }

    protected WindowsRuntimeObject(WindowsRuntimeObjectReference activationFactoryObjectReference, in Guid iid)
    {
        bool hasUnwrappableNativeObjectReference = HasUnwrappableNativeObjectReference;

        bool flag = GetType() != typeof(Windows.UI.Xaml.Application);
        nint innerInterface;
        nint newInstance = _IApplicationFactoryMethods.CreateInstance(_objRef_global__Windows_UI_Xaml_IApplicationFactory, flag ? this : null, out innerInterface);
        try
        {
            ComWrappersHelper.Init(flag, this, newInstance, innerInterface, IApplicationMethods.IID, out _inner);
        }
        finally
        {
            Marshal.Release(innerInterface);
        }
    }

    /// <summary>
    /// Gets the inner Windows Runtime object reference for the current instance.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected internal WindowsRuntimeObjectReference NativeObjectReference { get; }

    /// <summary>
    /// Gets a value indicating whether the current instance has an unwrappable native object reference.
    /// </summary>
    /// <remarks>
    /// This value is <see langword="false"/> in aggregation scenarios, as the instance that should be marshalled
    /// to native is the derived managed type for the projected class, and not the inner object for the base type.
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected internal abstract bool HasUnwrappableNativeObjectReference { get; }

    /// <summary>
    /// Gets the lazy-loaded, cached object reference for <c>IInspectable</c> for the current object.
    /// </summary>
    internal WindowsRuntimeObjectReference InspectableObjectReference
    {
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            WindowsRuntimeObjectReference InitializeInspectableObjectReference()
            {
                _ = Interlocked.CompareExchange(
                    location1: ref _inspectableObjectReference,
                    value: NativeObjectReference.As(in WellKnownInterfaceIds.IID_IInspectable),
                    comparand: null);

                return _inspectableObjectReference;
            }

            return _inspectableObjectReference ?? InitializeInspectableObjectReference();
        }
    }

    /// <summary>
    /// Gets the lazy-loaded cache of additional data associated to type handles.
    /// </summary>
    private ConcurrentDictionary<RuntimeTypeHandle, object> TypeHandleCache
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
        _ = TypeHandleCache;

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
        return NativeObjectReference.TryAsUnsafe(in iid, out ppv)
            ? CustomQueryInterfaceResult.Handled
            : CustomQueryInterfaceResult.NotHandled;
    }
}
