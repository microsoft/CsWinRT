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
public abstract unsafe class WindowsRuntimeObject :
    IDynamicInterfaceCastable,
    IUnmanagedVirtualMethodTableProvider,
    ICustomQueryInterface
{
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
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    protected WindowsRuntimeObject(WindowsRuntimeObjectReference nativeObjectReference)
    {
        ArgumentNullException.ThrowIfNull(nativeObjectReference);

        NativeObjectReference = nativeObjectReference;
    }

    /// <summary>
    /// Creates a <see cref="WindowsRuntimeObject"/> instance with the specified parameters.
    /// </summary>
    /// <param name="activationFactoryObjectReference">The <see cref="WindowsRuntimeObjectReference"/> for the <c>IActivationFactory</c> instance.</param>
    /// <param name="iid">The IID of the default interface for the Windows Runtime class being constructed.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="activationFactoryObjectReference"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if <paramref name="activationFactoryObjectReference"/> has been disposed.</exception>
    /// <exception cref="Exception">Thrown if there's any errors when activating the underlying native object.</exception>
    /// <remarks>
    /// This constructor should only be used when activating composable types (both projected and user-defined types).
    /// </remarks>
    protected WindowsRuntimeObject(WindowsRuntimeObjectReference activationFactoryObjectReference, in Guid iid)
    {
        ArgumentNullException.ThrowIfNull(activationFactoryObjectReference);

        bool hasUnwrappableNativeObjectReference = HasUnwrappableNativeObjectReference;

        // Activate the instance for the composition scenario. This constructor is only used when instantiating
        // Windows Runtime composable types (either projected types, or user-defined types deriving from one).
        // However, depending on which case it is, the activation is executed slightly differently:
        //
        //   1) Activation for composition: this happens when activating a user-defined type that derives from a
        //      projected composable Windows Runtime type. In this case, the managed object being constructed will
        //      be the controlling 'IInspectable' instance, which is passed as the 'baseInterface' parameter.
        //      The returned 'innerInterface' will be used to invoke methods on the base interfaces.
        //   2) Standalone activation: this happens when activating a composable type directly (eg. 'Button'). In
        //      this case, 'baseInterface' will be 'null', as there is no explicit controlling 'IInspectable' object
        //      that needs to be passed (the controlling instance is the same one as the object being constructed).
        //      Callers will ignore the returned 'innerInterface' as well in this example.
        //
        // For additional info, see: https://learn.microsoft.com/en-us/uwp/winrt-cref/winrt-type-system#composable-activation.
        WindowsRuntimeActivationHelper.ActivateInstanceUnsafe(
            activationFactoryObjectReference: activationFactoryObjectReference,
            baseInterface: hasUnwrappableNativeObjectReference ? null : this,
            innerInterface: out void* innerInterface,
            defaultInterface: out void* defaultInterface);

        // Initialize a 'WindowsRuntimeObjectReference' for the current native objects and the managed instance we're
        // constructing. This will also take care of registering things with 'ComWrappers', and setting up all the
        // reference tracker infrastructure, in case the native object implements the 'IReferenceTracker' interface.
        NativeObjectReference = WindowsRuntimeObjectReference.InitializeFromManagedTypeUnsafe(
            isAggregation: !hasUnwrappableNativeObjectReference,
            thisInstance: this,
            newInstanceUnknown: ref defaultInterface,
            innerInstanceUnknown: ref innerInterface,
            newInstanceIid: in iid);

        // Optimization: if we are activating the current type for composition, then the returned object reference
        // will wrap the 'IInspectable' pointer for the controlling instance (ie. 'innerInterface'). In this case,
        // we can assign it to the cached 'IInspectable' object reference as well, since that would represent the
        // exact same interface pointer. This entirley skips allocating that object reference in the future, if
        // the instance being constructed were to be marshalled as 'IInspectable' (ie. as 'object') to native. We
        // can assign the field directly rather than the property, to avoid doing a 'cmpxchg' operation here. That
        // is not needed at this point anyway, as we're constructing the object, so no other thread can access it.
        //
        // We can also perform a similar optimization when activating types in standalone mode. If we are activating
        // not for composition, then 'NativeObjectReference' would be wrapping the default interface pointer for the
        // current type, meaning that it can be copied to the field caching that interface as well. We just can't do
        // that in this base constructor though, as all the default interface fields are generated in each derived
        // projected types. That optimization can be done right after the call to the base constructor, in each type.
        if (!hasUnwrappableNativeObjectReference)
        {
            _inspectableObjectReference = NativeObjectReference;
        }
    }

    /// <summary>
    /// Creates a <see cref="WindowsRuntimeObject"/> instance with the specified parameters.
    /// </summary>
    /// <param name="activationFactoryCallback">The <see cref="WindowsRuntimeActivationFactoryCallback"/> instance to delegate activation to.</param>
    /// <param name="iid">The IID of the default interface for the Windows Runtime class being constructed.</param>
    /// <param name="additionalParameters">The additional parameters to provide to <paramref name="activationFactoryCallback"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="activationFactoryCallback"/> is <see langword="null"/>.</exception>
    /// <exception cref="Exception">Thrown if there's any errors when activating the underlying native object.</exception>
    /// <remarks>
    /// <para>
    /// This constructor should only be used when activating composable types (both projected and user-defined types).
    /// </para>
    /// <para>
    /// Additionally, this constructor is only meant to be used when additional custom parameters are required to invoke the target factory
    /// method. If no additional parameters are needed, the <see cref="WindowsRuntimeObject(WindowsRuntimeObjectReference, in Guid)"/> overload
    /// should be used instead, as that is more efficient in case the default signature is sufficient.
    /// </para>
    /// </remarks>
    protected WindowsRuntimeObject(
        WindowsRuntimeActivationFactoryCallback activationFactoryCallback,
        in Guid iid,
        params ReadOnlySpan<object?> additionalParameters)
    {
        ArgumentNullException.ThrowIfNull(activationFactoryCallback);

        bool hasUnwrappableNativeObjectReference = HasUnwrappableNativeObjectReference;

        // Delegate to the activation factory callback (see detailed explanation above)
        activationFactoryCallback(
            additionalParameters: additionalParameters,
            baseInterface: hasUnwrappableNativeObjectReference ? null : this,
            innerInterface: out void* innerInterface,
            defaultInterface: out void* defaultInterface);

        // Initialize a 'WindowsRuntimeObjectReference' object (see detailed explanation above)
        NativeObjectReference = WindowsRuntimeObjectReference.InitializeFromManagedTypeUnsafe(
            isAggregation: !hasUnwrappableNativeObjectReference,
            thisInstance: this,
            newInstanceUnknown: ref defaultInterface,
            innerInstanceUnknown: ref innerInterface,
            newInstanceIid: in iid);

        // Optimization: pre-cache the inspectable object reference if possible (see detailed explanation above)
        if (!hasUnwrappableNativeObjectReference)
        {
            _inspectableObjectReference = NativeObjectReference;
        }
    }

    /// <summary>
    /// Gets the inner Windows Runtime object reference for the current instance.
    /// </summary>
    /// <remarks>
    /// This object reference should point to an <c>IInspectable</c> native object.
    /// </remarks>
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
        set => Interlocked.CompareExchange(ref _inspectableObjectReference, value, comparand: null);
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
