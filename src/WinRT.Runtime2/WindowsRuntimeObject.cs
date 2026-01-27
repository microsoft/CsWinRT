// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
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
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
        DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
        UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
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
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
        DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
        UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
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

        // The inner interface pointer isn't used for non-composable types, so we just pass 'null'
        void* innerInterface = null;

        // Initialize a 'WindowsRuntimeObjectReference' for the current native objects and the managed instance we're
        // constructing. This will also take care of registering things with 'ComWrappers', and setting up all the
        // reference tracker infrastructure, in case the native object implements the 'IReferenceTracker' interface.
        NativeObjectReference = WindowsRuntimeObjectReference.InitializeFromManagedTypeUnsafe(
            isAggregation: false,
            thisInstance: this,
            newInstanceUnknown: ref defaultInterface,
            innerInstanceUnknown: ref innerInterface,
            newInstanceIid: in iid);
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
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
        DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
        UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
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
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
        DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
        UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected WindowsRuntimeObject(
        WindowsRuntimeActivationFactoryCallback.DerivedSealed activationFactoryCallback,
        in Guid iid,
        params ReadOnlySpan<object?> additionalParameters)
    {
        ArgumentNullException.ThrowIfNull(activationFactoryCallback);

        // Delegate to the activation factory callback (see detailed explanation above)
        activationFactoryCallback.Invoke(
            additionalParameters: additionalParameters,
            defaultInterface: out void* defaultInterface);

        // The inner interface pointer isn't used for non-composable types, so we just pass 'null'
        void* innerInterface = null;

        // Initialize the 'WindowsRuntimeObjectReference' for the default interface (same as above)
        NativeObjectReference = WindowsRuntimeObjectReference.InitializeFromManagedTypeUnsafe(
            isAggregation: false,
            thisInstance: this,
            newInstanceUnknown: ref defaultInterface,
            innerInstanceUnknown: ref innerInterface,
            newInstanceIid: in iid);
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
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
        DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
        UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected WindowsRuntimeObject(
        WindowsRuntimeActivationFactoryCallback.DerivedComposed activationFactoryCallback,
        in Guid iid,
        params ReadOnlySpan<object?> additionalParameters)
    {
        ArgumentNullException.ThrowIfNull(activationFactoryCallback);

        bool hasUnwrappableNativeObjectReference = HasUnwrappableNativeObjectReference;

        // Delegate to the activation factory callback (see detailed explanation above)
        activationFactoryCallback.Invoke(
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
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
        DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
        UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected internal WindowsRuntimeObjectReference NativeObjectReference { get; }

    /// <summary>
    /// Gets a value indicating whether the current instance has an unwrappable native object reference.
    /// </summary>
    /// <remarks>
    /// This value is <see langword="false"/> in aggregation scenarios, as the instance that should be marshalled
    /// to native is the derived managed type for the projected class, and not the inner object for the base type.
    /// </remarks>
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
        DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
        UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
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
                    value: NativeObjectReference.As(in WellKnownWindowsInterfaceIIDs.IID_IInspectable),
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
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
        DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
        UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected abstract bool IsOverridableInterface(in Guid iid);

    /// <summary>
    /// Retrieves a <see cref="WindowsRuntimeObjectReference"/> object for the specified interface.
    /// </summary>
    /// <param name="interfaceType">The type handle for the interface to retrieve the object reference for.</param>
    /// <returns>The resulting <see cref="WindowsRuntimeObjectReference"/> object.</returns>
    /// <exception cref="Exception">Thrown if the interface specified by <paramref name="interfaceType"/> is not implemented.</exception>
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
        DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
        UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
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

    /// <summary>
    /// Tries to retrieve a <see cref="WindowsRuntimeObjectReference"/> object for the specified interface.
    /// </summary>
    /// <param name="interfaceType">The type handle for the interface to retrieve the object reference for.</param>
    /// <param name="interfaceReference">The resulting <see cref="WindowsRuntimeObjectReference"/> object, if the interface could be retrieved.</param>
    /// <returns>Whether <paramref name="interfaceReference"/> could be retrieved successfully.</returns>
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
        DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
        UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public bool TryGetObjectReferenceForInterface(RuntimeTypeHandle interfaceType, [NotNullWhen(true)] out WindowsRuntimeObjectReference? interfaceReference)
    {
        return TryGetCastResult(
            interfaceType: interfaceType,
            implementationType: out _,
            interfaceReference: out interfaceReference);
    }

    /// <summary>
    /// Retrieves a <see cref="WindowsRuntimeObjectReference"/> object for the <see cref="IEnumerable"/> interface.
    /// </summary>
    /// <returns>The resulting <see cref="WindowsRuntimeObjectReference"/> object.</returns>
    /// <exception cref="Exception">Thrown if the <see cref="IEnumerable"/> interface is not implemented.</exception>
    internal WindowsRuntimeObjectReference GetObjectReferenceForIEnumerableInterfaceInstance()
    {
        // Throw an exception if we couldn't resolve the 'IEnumerable' interface reference
        if (!TryGetObjectReferenceForIEnumerableInterfaceInstance(out WindowsRuntimeObjectReference? interfaceReference))
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentException()
            {
                throw new ArgumentException(
                    $"The type '{typeof(IEnumerable)}' cannot be used to retrieve an object reference for the current object.");
            }

            ThrowArgumentException();
        }

        return interfaceReference;
    }

    /// <summary>
    /// Tries to retrieve a <see cref="WindowsRuntimeObjectReference"/> object for the <see cref="IEnumerable"/> interface.
    /// </summary>
    /// <param name="interfaceReference">The resulting <see cref="WindowsRuntimeObjectReference"/> object, if the interface could be retrieved.</param>
    /// <returns>Whether <paramref name="interfaceReference"/> could be retrieved successfully.</returns>
    internal bool TryGetObjectReferenceForIEnumerableInterfaceInstance([NotNullWhen(true)] out WindowsRuntimeObjectReference? interfaceReference)
    {
        ConcurrentDictionary<RuntimeTypeHandle, object> typeHandleCache = TypeHandleCache;

        // If we have a cached value, return it immediately to skip further lookups. We use
        // the 'IEnumerableInstance' type to cache the specialized object reference in this
        // scenario. The only thing that matters is no other code will try this same lookup.
        if (typeHandleCache.TryGetValue(typeof(IEnumerableInstance).TypeHandle, out object? value))
        {
            switch (value)
            {
                case DynamicInterfaceCastableResult castResult:
                    interfaceReference = castResult.InterfaceObjectReference;
                    return true;
                default:
                    interfaceReference = null;
                    return false;
            }
        }

        // Go through the cached object references for dynamic casts to try to find a compatible one
        foreach (KeyValuePair<RuntimeTypeHandle, object> typeHandleEntry in TypeHandleCache)
        {
            // Resolve the interface type (this can never be 'null', as we're the only ones adding elements)
            Type interfaceType = Type.GetTypeFromHandle(typeHandleEntry.Key)!;

            // Scan the cached entries, and skip all the ones that are not some 'IEnumerable<T>' instantiation
            if (!(interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
            {
                continue;
            }

            // Also skip all 'IEnumerable<T>' instantiations that are not actually implemented by the native object
            if (typeHandleEntry.Value is not DynamicInterfaceCastableResult result)
            {
                continue;
            }

            // Try to cache the cast result that we retrieved, but we can't assume we'll win the race here.
            // If we lost and the result is different, that's fine, we'll just use that one instead. This
            // also guarantees that even if we have race conditions, the same implementation is always used.
            object cachedResult = typeHandleCache.GetOrAdd(typeof(IEnumerableInstance).TypeHandle, result);

            // Return the resulting object reference from the cached result. Note that this might in the event
            // of a lost race, possibly result in us reporting a failure even though this thread had actually
            // found an object reference. This is intentional, as we want to make sure that we don't return
            // different implementations at different callsites, as that could lead to hard to debug problems.
            // In practice, this doesn't matter, as we always expect 'IEnumerable' casts to be done after some
            // other cast to 'IEnumerable<T>'. The main scenario where this happens is within LINQ operations.
            switch (cachedResult)
            {
                case DynamicInterfaceCastableResult castResult:
                    interfaceReference = castResult.InterfaceObjectReference;
                    return true;
                default:
                    interfaceReference = null;
                    return false;
            }
        }

        // Cache the failed cast, but still validate the return in case of an unlikely race against an 'IEnumerable<T>' cast
        object cachedFailure = typeHandleCache.GetOrAdd(typeof(IEnumerableInstance).TypeHandle, DynamicInterfaceCastFailure.Instance);

        // Switch on the cached failure as above, just in case we somehow still got a valid result on another thread first
        switch (cachedFailure)
        {
            case DynamicInterfaceCastableResult castResult:
                interfaceReference = castResult.InterfaceObjectReference;
                return true;
            default:
                interfaceReference = null;
                return false;
        }
    }

    /// <inheritdoc/>
    RuntimeTypeHandle IDynamicInterfaceCastable.GetInterfaceImplementation(RuntimeTypeHandle interfaceType)
    {
        Type type = Type.GetTypeFromHandle(interfaceType)!;

        // If we can resolve the implementation type through the Windows Runtime infrastructure, return it
        if (DynamicInterfaceCastableImplementationInfo.TryGetInfo(
            interfaceType: type,
            info: out DynamicInterfaceCastableImplementationInfo? implementationInfo))
        {
            return implementationInfo.ImplementationType.TypeHandle;
        }

        // If the interface is a generated COM interface, return the implementation type from there
        if (StrategyBasedComWrappers.DefaultIUnknownInterfaceDetailsStrategy.GetIUnknownDerivedDetails(interfaceType) is IIUnknownDerivedDetails details)
        {
            return details.Implementation.TypeHandle;
        }

        // Otherwise we don't have an implementation type, so this cast can't possibly succeed
        return default;
    }

    /// <inheritdoc/>
    bool IDynamicInterfaceCastable.IsInterfaceImplemented(RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
    {
        return TryGetCastResult(
            interfaceType: interfaceType,
            implementationType: out _,
            interfaceReference: out _);
    }

    /// <inheritdoc/>
    VirtualMethodTableInfo IUnmanagedVirtualMethodTableProvider.GetVirtualMethodTableInfoForKey(Type type)
    {
        if (LookupGeneratedVTableInfo(
            interfaceType: type.TypeHandle,
            performTypeHandleCacheLookup: true,
            throwOnQueryInterfaceFailure: true,
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
        if (IsOverridableInterface(in iid) || WellKnownWindowsInterfaceIIDs.IID_IInspectable == iid || WellKnownWindowsInterfaceIIDs.IID_IWeakReference == iid)
        {
            ppv = default;

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

        // First, check for 'IDynamicInterfaceCastable' casts through the Windows Runtime infrastructure
        if (LookupDynamicInterfaceCastableImplementationInfo(
            interfaceType: interfaceType,
            castResult: out DynamicInterfaceCastableResult? dynamicInterfaceCastableResult))
        {
            implementationType = dynamicInterfaceCastableResult!.ImplementationType.TypeHandle;
            interfaceReference = dynamicInterfaceCastableResult.InterfaceObjectReference;

            return true;
        }

        // Next, check to see if the target interface is a generated COM interface
        CustomQueryInterfaceResult queryInterfaceResult = LookupGeneratedVTableInfo(
            interfaceType: interfaceType,
            performTypeHandleCacheLookup: false,
            throwOnQueryInterfaceFailure: false,
            castResult: out GeneratedComInterfaceCastResult? generatedComInterfaceCastResult);

        // Return the appropriate result based on the 'QueryInterface' result, if handled
        switch (queryInterfaceResult)
        {
            case CustomQueryInterfaceResult.Handled:
                implementationType = generatedComInterfaceCastResult!.TableInfo.ManagedType;
                interfaceReference = generatedComInterfaceCastResult.InterfaceObjectReference;
                return true;
            case CustomQueryInterfaceResult.Failed:
            case CustomQueryInterfaceResult.NotHandled:
            default: break;
        }

        implementationType = default;
        interfaceReference = null;

        // Before returning, cache the fact the cast failed, so future calls will be faster
        _ = typeHandleCache.TryAdd(interfaceType, DynamicInterfaceCastFailure.Instance);

        return false;
    }

    /// <summary>
    /// Looks up whether the input interface type is implemented for an <see cref="IDynamicInterfaceCastable"/> cast.
    /// </summary>
    /// <param name="interfaceType">The input interface type.</param>
    /// <param name="castResult">The resulting <see cref="GeneratedComInterfaceCastResult"/> value, if the cast is successful.</param>
    /// <returns>Whether <paramref name="interfaceType"/> is implemented.</returns>
    /// <remarks>
    /// When successful, this method will cache a <see cref="DynamicInterfaceCastableResult"/> value into <see cref="TypeHandleCache"/>.
    /// </remarks>
    private bool LookupDynamicInterfaceCastableImplementationInfo(RuntimeTypeHandle interfaceType, out DynamicInterfaceCastableResult? castResult)
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

        WindowsRuntimeObjectReference? interfaceReference;

        // Special case generic interface types that can be implemented via multiple types. For instance, a cast to
        // 'ICollection<KeyValuePair<int, int>>' should succeed if the native objects implement 'IList<KeyValuePair<int, int>>',
        // but also if the native object implements 'IDictionary<int, int>'. In these cases, we won't know which IID to use
        // nor what actual implementation type to use (as it depends on what the native object implements), so we defer
        // to the generated forwarder attribute to check that.
        if (type.IsGenericType && (
            type.GetGenericTypeDefinition() == typeof(ICollection<>) ||
            type.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>)) &&
            type.GenericTypeArguments[0].IsGenericType &&
            type.GenericTypeArguments[0].GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
        {
            // Invoke 'IsInterfaceImplemented' on the implementation type, which will perform a series of calls
            // to 'TryGetObjectReferenceForInterface' for all the compatible Windows Runtime interfaces. The
            // resulting object references will be for the interface that actually exists on the native object.
            // This interface shouldn't really be needed from this key for the lookup, but we still store it.
            if (!implementationInfo.GetDynamicInterfaceCastableForwarder().IsInterfaceImplemented(this, out interfaceReference))
            {
                return false;
            }
        }
        else
        {
            // Try to cast to the IID specified by the interface, and stop if that failed.
            // In this case we can just get the IID directly from the implementation type.
            if (!NativeObjectReference.TryAs(
                iid: implementationInfo.ImplementationType.GUID,
                objectReference: out interfaceReference))
            {
                return false;
            }
        }

        // Initialize the cache result to return to callers
        castResult = new DynamicInterfaceCastableResult()
        {
            ImplementationType = implementationInfo.ImplementationType,
            InterfaceObjectReference = interfaceReference
        };

        // Add the cast result to the cache, or get the updated value if we raced against another thread
        object effectiveCastResult = TypeHandleCache.GetOrAdd(interfaceType, castResult);

        // If the result is different than the one we tried to add, it means we lost a race against another
        // thread. In this case we just discard our object reference. The updated result is perfectly valid.
        if (effectiveCastResult != castResult)
        {
            interfaceReference.Dispose();

            castResult = (DynamicInterfaceCastableResult)effectiveCastResult;
        }

        return true;
    }

    /// <summary>
    /// Looks up whether the input interface type is implemented for a generated COM interface cast.
    /// </summary>
    /// <param name="interfaceType">The input interface type.</param>
    /// <param name="performTypeHandleCacheLookup">Whether to lookup into <see cref="TypeHandleCache"/> first.</param>
    /// <param name="throwOnQueryInterfaceFailure">Whether to throw an exception of <c>QueryInterface</c> fails.</param>
    /// <param name="castResult">The resulting <see cref="GeneratedComInterfaceCastResult"/> value, if the cast is successful.</param>
    /// <returns>Whether <paramref name="interfaceType"/> is implemented.</returns>
    /// <remarks>
    /// When successful, this method will cache a <see cref="GeneratedComInterfaceCastResult"/> value into <see cref="TypeHandleCache"/>.
    /// </remarks>
    private CustomQueryInterfaceResult LookupGeneratedVTableInfo(
        RuntimeTypeHandle interfaceType,
        bool performTypeHandleCacheLookup,
        bool throwOnQueryInterfaceFailure,
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
            HRESULT hresult = NativeObjectReference.DerivedTryAsNative(details.Iid, out WindowsRuntimeObjectReference? interfaceReference);

            // If the 'QueryInterface' call failed, we know the interface can't possibly be implemented.
            // Because the actual 'QueryInterface' failed, we also know there would be no point for the
            // rest of the 'IDynamicInterfaceCastable' logic to run, as the cast can never succeed.
            if (hresult.Failed())
            {
                // Throw only if requested by callers
                if (throwOnQueryInterfaceFailure)
                {
                    Marshal.ThrowExceptionForHR(hresult);
                }

                return CustomQueryInterfaceResult.Failed;
            }

            // Here we are intentionally getting the pointer without incrementing its reference count.
            // The target native object will be kept alive by the cached 'WindowsRuntimeObjectReference'.
            void* interfacePtr = interfaceReference!.GetThisPtrUnsafe();

            // Initialize the cache result to return to callers
            castResult = new GeneratedComInterfaceCastResult
            {
                TableInfo = new IIUnknownCacheStrategy.TableInfo()
                {
                    ThisPtr = interfacePtr,
                    Table = *(void***)interfacePtr,
                    ManagedType = details.Implementation.TypeHandle
                },
                InterfaceObjectReference = interfaceReference
            };

            // Try to add the cast result to the cache
            object effectiveCastResult = TypeHandleCache.GetOrAdd(interfaceType, castResult);

            // If we lost a thread, dispose the reference and return the updated result we
            // just retrieved from the cache. This is the same logic as for other casts.
            if (effectiveCastResult != castResult)
            {
                interfaceReference.Dispose();

                castResult = (GeneratedComInterfaceCastResult)effectiveCastResult;
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

    /// <summary>
    /// A dummy type to use for caching adaptive <see cref="System.Collections.IEnumerable"/> object references in <see cref="TryGetObjectReferenceForIEnumerableInterfaceInstance"/>.
    /// </summary>
    private static class IEnumerableInstance;
}