// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using WindowsRuntime.InteropServices;

#pragma warning disable IDE0046

#pragma warning disable CS8618, IDE0059, IDE0060 // TODO

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
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected WindowsRuntimeObject(WindowsRuntimeObjectReference nativeObjectReference)
    {
        ArgumentNullException.ThrowIfNull(nativeObjectReference);

        NativeObjectReference = nativeObjectReference;
    }

    /// <summary>
    /// Creates a <see cref="WindowsRuntimeObject"/> instance with the specified parameters for sealed scenarios.
    /// </summary>
    /// <param name="_">Marker parameter used to select this constructor for sealed types (unused).</param>
    /// <param name="activationFactoryObjectReference">The <see cref="WindowsRuntimeObjectReference"/> for the <c>IActivationFactory</c> instance.</param>
    /// <param name="iid">The IID of the default interface for the Windows Runtime class being constructed.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="activationFactoryObjectReference"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if <paramref name="activationFactoryObjectReference"/> has been disposed.</exception>
    /// <exception cref="Exception">Thrown if there's any errors when activating the underlying native object.</exception>
    /// <remarks>
    /// This constructor should only be used when activating sealed types (both projected and user-defined types).
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected WindowsRuntimeObject(
        WindowsRuntimeActivationTypes.DerivedSealed _,
        WindowsRuntimeObjectReference activationFactoryObjectReference,
        in Guid iid)
    {
        ArgumentNullException.ThrowIfNull(activationFactoryObjectReference);

        // This constructor is only meant to be used for sealed types, so there's never a non-delegating (inner) return value.
        // See additional notes in the overload below for more details about how and when that parameter is necessary.
        WindowsRuntimeActivationHelper.ActivateInstanceUnsafe(
            activationFactoryObjectReference: activationFactoryObjectReference,
            defaultInterface: out void* defaultInterface);

        // TODO
    }

    /// <summary>
    /// Creates a <see cref="WindowsRuntimeObject"/> instance with the specified parameters for composed scenarios.
    /// </summary>
    /// <param name="_">Marker parameter used to select this constructor for composed types (unused).</param>
    /// <param name="activationFactoryObjectReference">The <see cref="WindowsRuntimeObjectReference"/> for the <c>IActivationFactory</c> instance.</param>
    /// <param name="iid">The IID of the default interface for the Windows Runtime class being constructed.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="activationFactoryObjectReference"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if <paramref name="activationFactoryObjectReference"/> has been disposed.</exception>
    /// <exception cref="Exception">Thrown if there's any errors when activating the underlying native object.</exception>
    /// <remarks>
    /// This constructor should only be used when activating composable types (both projected and user-defined types).
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected WindowsRuntimeObject(
        WindowsRuntimeActivationTypes.DerivedComposed _,
        WindowsRuntimeObjectReference activationFactoryObjectReference,
        in Guid iid)
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
        // For additional info, see: https://learn.microsoft.com/uwp/winrt-cref/winrt-type-system#composable-activation.
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
    /// Creates a <see cref="WindowsRuntimeObject"/> instance with the specified parameters for sealed scenarios.
    /// </summary>
    /// <param name="activationFactoryCallback">The <see cref="WindowsRuntimeActivationFactoryCallback"/> instance to delegate activation to.</param>
    /// <param name="iid">The IID of the default interface for the Windows Runtime class being constructed.</param>
    /// <param name="additionalParameters">The additional parameters to provide to <paramref name="activationFactoryCallback"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="activationFactoryCallback"/> is <see langword="null"/>.</exception>
    /// <exception cref="Exception">Thrown if there's any errors when activating the underlying native object.</exception>
    /// <remarks>
    /// <para>
    /// This constructor should only be used when activating sealed types (both projected and user-defined types).
    /// </para>
    /// <para>
    /// Additionally, this constructor is only meant to be used when additional custom parameters are required to invoke the target factory method. If no additional
    /// parameters are needed, the <see cref="WindowsRuntimeObject(WindowsRuntimeActivationTypes.DerivedSealed, WindowsRuntimeObjectReference, in Guid)"/> overload
    /// should be used instead, as that is more efficient in case the default signature is sufficient.
    /// </para>
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected WindowsRuntimeObject(
        WindowsRuntimeActivationFactoryCallback.DerivedSealed activationFactoryCallback,
        in Guid iid,
        params ReadOnlySpan<object?> additionalParameters)
    {
        ArgumentNullException.ThrowIfNull(activationFactoryCallback);

        // Delegate to the activation factory callback (see detailed explanation above)
        activationFactoryCallback(
            additionalParameters: additionalParameters,
            defaultInterface: out void* defaultInterface);

        // TODO
    }

    /// <summary>
    /// Creates a <see cref="WindowsRuntimeObject"/> instance with the specified parameters for composed scenarios.
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
    /// Additionally, this constructor is only meant to be used when additional custom parameters are required to invoke the target factory method. If no additional
    /// parameters are needed, the <see cref="WindowsRuntimeObject(WindowsRuntimeActivationTypes.DerivedComposed, WindowsRuntimeObjectReference, in Guid)"/> overload
    /// should be used instead, as that is more efficient in case the default signature is sufficient.
    /// </para>
    /// </remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected WindowsRuntimeObject(
        WindowsRuntimeActivationFactoryCallback.DerivedComposed activationFactoryCallback,
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
    [EditorBrowsable(EditorBrowsableState.Never)]
    public WindowsRuntimeObjectReference GetObjectReferenceForInterface(RuntimeTypeHandle interfaceType)
    {
        // Throw an exception if we couldn't resolve the interface reference
        if (!TryGetCastResult(
            interfaceType: interfaceType,
            implementationType: out _,
            interfaceReference: out WindowsRuntimeObjectReference? interfaceReference))
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentException(RuntimeTypeHandle interfaceType)
            {
                throw new ArgumentException(
                    $"The type '{Type.GetTypeFromHandle(interfaceType)}' cannot be used to retrieve an object reference for the current object.");
            }

            ThrowArgumentException(interfaceType);
        }

        return interfaceReference;

    }

    /// <inheritdoc/>
    RuntimeTypeHandle IDynamicInterfaceCastable.GetInterfaceImplementation(RuntimeTypeHandle interfaceType)
    {
        _ = TryGetCastResult(
            interfaceType: interfaceType,
            implementationType: out RuntimeTypeHandle implementationType,
            interfaceReference: out _);

        // Always just return the resulting implementation type, which will be 'default' if not available
        return implementationType;
    }

    /// <inheritdoc/>
    bool IDynamicInterfaceCastable.IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
    {
        ConcurrentDictionary<RuntimeTypeHandle, object> typeHandleCache = TypeHandleCache;

        // If we already have cached info for this interface type, it means we already checked
        // that it's implemented. The value can be eg. a 'WindowsRuntimeObjectReference' instance.
        if (typeHandleCache.TryGetValue(interfaceType, out object? value))
        {
            return value is not DynamicInterfaceCastFailure;
        }

        // First, check to see if the target interface is a generated COM interface
        CustomQueryInterfaceResult queryInterfaceResult = LookupGeneratedVTableInfo(
            interfaceType: interfaceType,
            performTypeHandleCacheLookup: false,
            throwOnQueryInterfaceFailure: throwIfNotImplemented,
            retrieveCastResult: false,
            castResult: out _);

        // We can stop early both if the cast succeeded, or if it was handled and just failed. In that case
        // there's no point doing anything else, as we already checked that the interface is not implemented.
        switch (queryInterfaceResult)
        {
            case CustomQueryInterfaceResult.Handled: return true;
            case CustomQueryInterfaceResult.Failed: return false;
            case CustomQueryInterfaceResult.NotHandled:
            default: break;
        }

        // Do the normal check for all other 'IDynamicInterfaceCastable' casts
        return LookupDynamicInterfaceCastableImplementationInfo(
            interfaceType: interfaceType,
            retrieveCastResult: false,
            castResult: out _);
    }

    /// <inheritdoc/>
    VirtualMethodTableInfo IUnmanagedVirtualMethodTableProvider.GetVirtualMethodTableInfoForKey(Type type)
    {
        if (LookupGeneratedVTableInfo(
            interfaceType: type.TypeHandle,
            performTypeHandleCacheLookup: true,
            throwOnQueryInterfaceFailure: true,
            retrieveCastResult: true,
            castResult: out GeneratedComInterfaceCastResult? castResult) is not CustomQueryInterfaceResult.Handled)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentException(Type type)
            {
                throw new ArgumentException(
                    $"The type '{type}' cannot be used to resolve a virtual method table for the current object.");
            }

            ThrowArgumentException(type);
        }

        return new(castResult!.TableInfo.ThisPtr, castResult.TableInfo.Table);
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

    /// <summary>
    /// Tries to get an object reference without additional lookups.
    /// </summary>
    /// <param name="interfaceType">The input interface type.</param>
    /// <param name="implementationType">The implementation type.</param>
    /// <param name="interfaceReference">The <see cref="WindowsRuntimeObjectReference"/> for the interface.</param>
    /// <returns>Whether the cast was successfully retrieved.</returns>
    /// <remarks>
    /// This method mirrors <see cref="IDynamicInterfaceCastable.IsInterfaceImplemented"/> above, just with minor
    /// differences to further optimize it for <see cref="IDynamicInterfaceCastable.GetInterfaceImplementation"/>
    /// and <see cref="IUnmanagedVirtualMethodTableProvider.GetVirtualMethodTableInfoForKey"/> calls, where we
    /// need the cast results. Any changes to either of these methods should be kept in sync.
    /// </remarks>
    private bool TryGetCastResult(
        RuntimeTypeHandle interfaceType,
        [NotNullWhen(true)] out RuntimeTypeHandle implementationType,
        [NotNullWhen(true)] out WindowsRuntimeObjectReference? interfaceReference)
    {
        ConcurrentDictionary<RuntimeTypeHandle, object> typeHandleCache = TypeHandleCache;

        // If we have a cached value, return it immediately to skip further lookups
        if (typeHandleCache.TryGetValue(interfaceType, out object? value))
        {
            switch (value)
            {
                case DynamicInterfaceCastableResult castResult:
                    implementationType = castResult.ImplementationType.TypeHandle;
                    interfaceReference = castResult.InterfaceObjectReference;
                    return true;
                case GeneratedComInterfaceCastResult castResult:
                    implementationType = castResult.TableInfo.ManagedType;
                    interfaceReference = castResult.InterfaceObjectReference;
                    return true;
                default:
                    implementationType = default;
                    interfaceReference = null;
                    return false;
            }
        }

        // First, check to see if the target interface is a generated COM interface
        CustomQueryInterfaceResult queryInterfaceResult = LookupGeneratedVTableInfo(
            interfaceType: interfaceType,
            performTypeHandleCacheLookup: false,
            throwOnQueryInterfaceFailure: false,
            retrieveCastResult: true,
            castResult: out GeneratedComInterfaceCastResult? generatedComInterfaceCastResult);

        // Return the appropriate result based on the 'QueryInterface' result, if handled (same as below)
        switch (queryInterfaceResult)
        {
            case CustomQueryInterfaceResult.Handled:
                implementationType = generatedComInterfaceCastResult!.TableInfo.ManagedType;
                interfaceReference = generatedComInterfaceCastResult.InterfaceObjectReference;
                return true;
            case CustomQueryInterfaceResult.Failed:
                implementationType = default;
                interfaceReference = null;
                return false;
            case CustomQueryInterfaceResult.NotHandled:
            default: break;
        }

        // Do the normal check for all other 'IDynamicInterfaceCastable' casts
        if (LookupDynamicInterfaceCastableImplementationInfo(
            interfaceType: interfaceType,
            retrieveCastResult: true,
            castResult: out DynamicInterfaceCastableResult? dynamicInterfaceCastableResult))
        {
            implementationType = dynamicInterfaceCastableResult!.ImplementationType.TypeHandle;
            interfaceReference = dynamicInterfaceCastableResult.InterfaceObjectReference;

            return true;
        }

        implementationType = default;
        interfaceReference = null;

        return false;
    }

    /// <summary>
    /// Looks up whether the input interface type is implemented for an <see cref="IDynamicInterfaceCastable"/> cast.
    /// </summary>
    /// <param name="interfaceType">The input interface type.</param>
    /// <param name="retrieveCastResult">Whether to retrieve <paramref name="castResult"/> for successful lookups.</param>
    /// <param name="castResult">The resulting <see cref="GeneratedComInterfaceCastResult"/> value, if the cast is successful.</param>
    /// <returns>Whether <paramref name="interfaceType"/> is implemented.</returns>
    /// <remarks>
    /// When successful, this method will cache a <see cref="DynamicInterfaceCastableResult"/> value into <see cref="TypeHandleCache"/>.
    /// </remarks>
    private bool LookupDynamicInterfaceCastableImplementationInfo(
        RuntimeTypeHandle interfaceType,
        bool retrieveCastResult,
        out DynamicInterfaceCastableResult? castResult)
    {
        castResult = null;

        Type type = Type.GetTypeFromHandle(interfaceType)!;

        // If we can't resolve the implementation info at all, the cast can't possibly succeed
        if (!DynamicInterfaceCastableImplementationInfo.TryGetInfo(
            interfaceType: type,
            info: out DynamicInterfaceCastableImplementationInfo? implementationInfo))
        {
            return false;
        }

        Type? implementationType;
        WindowsRuntimeObjectReference? interfaceReference;

        // Special case generic interface types that can be implemented via multiple types. For instance, a cast to
        // 'ICollection<KeyValuePair<int, int>>' should succeed if the native objects implement 'IList<KeyValuePair<int, int>>',
        // but also if the native object implements 'IDictionary<int, int>'. In these cases, we won't know which IID to use
        // nor what actual implementation type to use (as it depends on what the native object implements), so we defer
        // to the generated forwarder attribute to check that.
        if (type.IsGenericType && (
            type.GetGenericTypeDefinition() == typeof(ICollection<>) ||
            type.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>)))
        {
            // If we can't resolve an implemented type, the cast failed
            if (!implementationInfo.GetDynamicInterfaceCastableImplementationForwarder().TryGetImplementationType(
                thisReference: NativeObjectReference,
                interfaceReference: out interfaceReference,
                implementationType: out implementationType))
            {
                return false;
            }
        }
        else
        {
            implementationType = implementationInfo.ImplementationType;

            // Try to cast to the IID specified by the interface, and stop if that failed.
            // In this case we can just get the IID directly from the implementation type.
            if (!NativeObjectReference.TryAs(
                iid: implementationType.GUID,
                objectReference: out interfaceReference))
            {
                return false;
            }
        }

        // Initialize the cache result to return to callers
        castResult = new DynamicInterfaceCastableResult()
        {
            ImplementationType = implementationType,
            InterfaceObjectReference = interfaceReference
        };

        // Track the result for later. If we raced against a thread and lost,
        // we can just discard our object reference. The result is still valid.
        if (!TypeHandleCache.TryAdd(interfaceType, castResult))
        {
            interfaceReference.Dispose();

            // If we lost a race but callers need the cast result, look it up manually here
            if (retrieveCastResult)
            {
                _ = TypeHandleCache.TryGetValue(interfaceType, out object? newCastResult);

                // We can rely on this never being 'null' here
                castResult = (DynamicInterfaceCastableResult)newCastResult!;
            }
            else
            {
                // Reset the result to ensure nobody can see a stale value
                castResult = null;
            }
        }

        return true;
    }

    /// <summary>
    /// Looks up whether the input interface type is implemented for a generated COM interface cast.
    /// </summary>
    /// <param name="interfaceType">The input interface type.</param>
    /// <param name="performTypeHandleCacheLookup">Whether to lookup into <see cref="TypeHandleCache"/> first.</param>
    /// <param name="throwOnQueryInterfaceFailure">Whether to throw an exception of <c>QueryInterface</c> fails.</param>
    /// <param name="retrieveCastResult">Whether to retrieve <paramref name="castResult"/> for successful lookups.</param>
    /// <param name="castResult">The resulting <see cref="GeneratedComInterfaceCastResult"/> value, if the cast is successful.</param>
    /// <returns>Whether <paramref name="interfaceType"/> is implemented.</returns>
    /// <remarks>
    /// When successful, this method will cache a <see cref="GeneratedComInterfaceCastResult"/> value into <see cref="TypeHandleCache"/>.
    /// </remarks>
    private CustomQueryInterfaceResult LookupGeneratedVTableInfo(
        RuntimeTypeHandle interfaceType,
        bool performTypeHandleCacheLookup,
        bool throwOnQueryInterfaceFailure,
        bool retrieveCastResult,
        out GeneratedComInterfaceCastResult? castResult)
    {
        castResult = null;

        // We only do the lookup if the caller hasn't already done so
        if (performTypeHandleCacheLookup && TypeHandleCache.TryGetValue(interfaceType, out object? typeHandleCacheValue))
        {
            // Try to see if we have a cached table info, and if we do, we can stop here
            if (typeHandleCacheValue is GeneratedComInterfaceCastResult cachedCastResult)
            {
                castResult = cachedCastResult;

                return CustomQueryInterfaceResult.Handled;
            }
        }

        // This method is specifically only for checking generated COM interfaces. If we can't
        // resolve the 'IIUnknownDerivedDetails' value for the interface, we have nothing to do.
        if (StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetIUnknownDerivedDetails(interfaceType) is IIUnknownDerivedDetails details)
        {
            HRESULT hresult = NativeObjectReference.DerivedTryAsNative(details.Iid, out WindowsRuntimeObjectReference? interfaceObjectReference);

            // If the 'QueryInterface' call failed, we know the interface can't possibly be implemented.
            // Because the actual 'QueryInterface' failed, we also know there would be no point for the
            // rest of the 'IDynamicInterfaceCastable' logic to run, as the cast can never succeed.
            // So we can just pre-cache the failure result here, to speedup future identical casts.
            if (!WellKnownErrorCodes.Succeeded(hresult))
            {
                _ = TypeHandleCache.TryAdd(interfaceType, DynamicInterfaceCastFailure.Instance);

                // Throw only if requested by callers
                if (throwOnQueryInterfaceFailure)
                {
                    Marshal.ThrowExceptionForHR(hresult);
                }

                return CustomQueryInterfaceResult.Failed;
            }

            // Here we are intentionally getting the pointer without incrementing its reference count.
            // The target native object will be kept alive by the cached 'WindowsRuntimeObjectReference'.
            void* interfacePtr = interfaceObjectReference!.GetThisPtrUnsafe();

            // Initialize the cache result to return to callers
            castResult = new GeneratedComInterfaceCastResult
            {
                TableInfo = new IIUnknownCacheStrategy.TableInfo()
                {
                    ThisPtr = interfacePtr,
                    Table = *(void***)interfacePtr,
                    ManagedType = details.Implementation.TypeHandle
                },
                InterfaceObjectReference = interfaceObjectReference
            };

            // Try to add the new cache result to the cache
            if (!TypeHandleCache.TryAdd(interfaceType, castResult))
            {
                // We can also manually dispose our object reference, as it will never be used
                interfaceObjectReference.Dispose();

                // Only do the additional lookup if callers actually need te value
                if (retrieveCastResult)
                {
                    // If we raced against another thread and lost, we load the new cache result
                    // added by that thread, and return that one to callers. Ours is not needed.
                    _ = TypeHandleCache.TryGetValue(interfaceType, out object? newCastResult);

                    // We can rely on this never being 'null' here
                    castResult = (GeneratedComInterfaceCastResult)newCastResult!;
                }
                else
                {
                    // Reset the result, to ensure nobody reads a stale value
                    castResult = null;
                }
            }

            return CustomQueryInterfaceResult.Handled;
        }

        return CustomQueryInterfaceResult.NotHandled;
    }

    /// <summary>
    /// A placeholder type for failed casts.
    /// </summary>
    private sealed class DynamicInterfaceCastFailure
    {
        /// <summary>
        /// The singleton <see cref="DynamicInterfaceCastFailure"/> instance.
        /// </summary>
        public static readonly DynamicInterfaceCastFailure Instance = new();
    }

    /// <summary>
    /// A type for successful generated COM interface casts.
    /// </summary>
    private sealed class GeneratedComInterfaceCastResult
    {
        /// <summary>
        /// Gets the <see cref="IIUnknownCacheStrategy.TableInfo"/> vaulue for the interface.
        /// </summary>
        public required IIUnknownCacheStrategy.TableInfo TableInfo { get; init; }

        /// <summary>
        /// Gets the <see cref="WindowsRuntimeObjectReference"/> instance for the interface pointer.
        /// </summary>
        public required WindowsRuntimeObjectReference InterfaceObjectReference { get; init; }
    }

    /// <summary>
    /// A type for successful <see cref="IDynamicInterfaceCastable"/> cast.
    /// </summary>
    private sealed class DynamicInterfaceCastableResult
    {
        /// <summary>
        /// Gets the implementation type for the interface.
        /// </summary>
        public required Type ImplementationType { get; init; }

        /// <summary>
        /// Gets the <see cref="WindowsRuntimeObjectReference"/> instance for the interface pointer.
        /// </summary>
        public required WindowsRuntimeObjectReference InterfaceObjectReference { get; init; }
    }
}
