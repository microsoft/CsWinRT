// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="WindowsRuntimeObjectReference"/>
public unsafe partial class WindowsRuntimeObjectReference
{
    /// <summary>
    /// Initializes a <see cref="WindowsRuntimeObjectReference"/> object for a given COM pointer (to the specified interface).
    /// </summary>
    /// <param name="isAggregation">Indicates whether the object being constructed participates in COM aggregation.</param>
    /// <param name="thisInstance">The managed object being constructed.</param>
    /// <param name="newInstanceUnknown">The native COM object for the base class being instantiated.</param>
    /// <param name="innerInstanceUnknown">The inner COM object, for COM aggregation scenarios.</param>
    /// <param name="newInstanceIid">The IID for the new instance.</param>
    /// <returns>A <see cref="WindowsRuntimeObjectReference"/> wrapping the input COM objects.</returns>
    /// <remarks>
    /// <para>
    /// This method differs from <see cref="AttachUnsafe(ref void*, in Guid)"/> and other factory methods in that it also handles
    /// setting up the reference tracker infrastructure for the native COM object, if needed, as well as taking care of managing
    /// the overall reference count from <see cref="ComWrappers"/>. Only use this method when constructing RCWs from managed objects.
    /// </para>
    /// <para>
    /// This method takes ownership of both <paramref name="newInstanceUnknown"/> and <paramref name="innerInstanceUnknown"/>.
    /// </para>
    /// </remarks>
    internal static WindowsRuntimeObjectReference InitializeFromManagedType(
        bool isAggregation,
        WindowsRuntimeObject thisInstance,
        ref void* newInstanceUnknown,
        ref void* innerInstanceUnknown,
        in Guid newInstanceIid)
    {
        void* acquiredNewInstanceUnknown = newInstanceUnknown;
        void* acquiredInnerInstanceUnknown = innerInstanceUnknown;

        newInstanceUnknown = null;
        innerInstanceUnknown = null;

        // First we need to determine what's the actual target COM object that we're going to wrap
        // by the returned 'WindowsRuntimeObjectReference' instance. That depends on whether this
        // is a COM aggregation scenario or not. That is, this method is used in one of these cases:
        //
        //   1) COM aggregation: the type being instantiated is some managed user-defined type that
        //      derives from a projected Windows Runtime class. For instance, this is commonly the
        //      case when instantiating some 'MyControl : UserControl' type (eg. this is in XAML).
        //   2) Not aggregation: the type being instantiated is a projected Windows Runtime class.
        //      For instance, this would be the case when instantiating a 'Button' object directly.
        //
        // In the first case, the actual COM object we are going to wrap is the inner interface object,
        // which will internally track the base object. Otherwise, we can wrap the new instance directly.
        // In the non aggregation case, that would just be the normal native object we just instantiated.
        //
        // Also see: https://learn.microsoft.com/en-us/windows/win32/com/aggregation.
        void* externalComObject = isAggregation ? innerInstanceUnknown : acquiredNewInstanceUnknown;

        // We need to check whether the target COM object is free-threaded or not, as that will
        // influence how we'll create the resulting 'WindowsRuntimeObjectReference' instance.
        // We can do that right away, as this is the only call that might throw. We want to do
        // this as early as possible, so we can rely on the rest of the code never throwing,
        // which avoids the need to worry about releasing references if we fail halfway through.
        HRESULT isFreeThreaded = ComObjectHelpers.IsFreeThreadedUnsafe(externalComObject);

        // After this 'HRESULT' validation, it is critical that the rest of the code in this method never
        // throws an exception. By that we only refer to exceptions that would be possible to handle.
        // That is, technically speaking we are ultimately going to create a 'WindowsRuntimeObjectReference'
        // object, which could throw 'OutOfMemoryException' in extreme circumstances. However, that is
        // considered an error state that cannot be recovered from, so that is not a concern here.
        if (!WellKnownErrorCodes.Succeeded(isFreeThreaded))
        {
            // Before proceeding, we need to increment the reference count on the inner instance, if we're doing
            // COM aggregation. This is part of a delicate balance of 'AddRef' and 'Release' calls on the input
            // object, which varies based on whether we're aggregating. We basically have two possible scenarios:
            //
            //   1) COM aggregation: we would need to do an 'AddRef' on the inner instance (for the object reference),
            //      and then callers of this method would do a 'Release' from a 'finally' block. We can skip that
            //      entire pair of 'AddRef'/'Release' on the inner object in the successful case.
            //   2) Not aggregation: we would need to do an 'AddRef' on the new instance (for the object reference),
            //      but then we'd do an unconditional 'Release' at the end of this method. This is because the
            //      runtime (ie. 'ComWrappers') would already be holding its own 'AddRef' on this same object, as
            //      that is the one that will be passed to 'GetOrRegisterObjectForComInstance' below. Which means
            //      we can skip this entire pair of 'AddRef'/'Release' calls on the new instance as well.
            //
            // To ensure things don't fall out of balance in the failure case, we just need to release the inner
            // instance if we're about to return early. That's because by doing so, we wouldn't have had time to
            // transfer the ownership to the returned object reference, which would handle releases later on.
            // This applies to both when doing aggregation or not. That is, regardless of how the lifetime of
            // the inner instance would've been extended, if we fail, we just need to ensure we release that object.
            _ = IUnknownVftbl.ReleaseUnsafe(acquiredInnerInstanceUnknown);

            Marshal.ThrowExceptionForHR(isFreeThreaded);
        }

        // Next, determine if the instance supports 'IReferenceTracker' (eg. for XAML scenarios).
        // Acquiring this interface is useful for:
        //
        //   1) Providing an indication of what value to pass during RCW creation.
        //   2) Informing the reference tracker runtime during non-aggregation scenarios about new references.
        //
        // If we are in a COM aggregation scenario, this will query the inner instance, since that's the one that
        // will have the 'IReferenceTracker' implementation. Otherwise, the new instance will be used. Since the
        // inner was composed, it should answer immediately without going through the outer. Either way, the
        // reference count will go to the new instance.
        _ = IUnknownVftbl.QueryInterfaceUnsafe(externalComObject, in WellKnownInterfaceIds.IID_IReferenceTracker, out void* referenceTracker);

        // Determine the flags needed for the native object wrapper (ie. RCW) creation.
        // These are the ones we'll pass to 'ComWrappers' to register the new object.
        CreateObjectFlags createObjectFlags = CreateObjectFlags.None;

        // Set the corresponding flag for the RCW if the instance supports 'IReferenceTracker'. This is what allows
        // the native/GC synchronization to correctly track references across the native and unmanaged boundary. That
        // this, it will allow the GC to detect reference cycles crossing through this boundary, and avoid leaking
        // objects that are only being kept alive by such cycles.
        //
        // Here is a practical example of where this would happen in a real world application (XAML):
        //
        // '''
        // Grid grid = new();
        // Button button = new();
        // grid.Children.Add(button);
        // button.Click += (s, e) => Console.WriteLine($"Action from inside '{grid.Name}'.");
        // '''
        //
        // This results in the following object graph (simplified):
        //
        // 'Grid' -> 'Button' -> CCW -> 'RoutedEventHandler' -> closure -> RCW -> 'Grid'.
        //
        // Without the reference tracking infrastructure allowing the GC to track objects throughout this graph,
        // the following object graph would just leak. This is because the following would be true:
        //   - The 'Grid' RCW is keeping the native 'Grid' alive
        //   - The native 'Grid' is keeping the native 'Button' alive
        //   - The native 'Button' is keeping the event handler alive
        //   - That is in turn keeping the initial 'Grid' RCW alive
        //
        // Reference tracking allows the GC to distinguish which 'AddRef' calls are from native objects, and which ones
        // come from managed objects, in the graph it can crawl on the managed heap. Furthermore, the reference tracker
        // infrastructure also allows the GC to crawl through objects across boundaries, to reconstruct the real dependency
        // graph. This is what allows the GC to still be able to detect cycles and avoid memory leaks, and what makes the
        // example code above "just work", like people writing C# would intuitively expect.
        if (referenceTracker != null)
        {
            createObjectFlags |= CreateObjectFlags.TrackerObject;
        }

        // We now need to create and register a native object wrapper (ie. RCW). Note that the 'ComWrappers' API that
        // does this operation will call 'QueryInterface' on the supplied instance, therefore it is important that the
        // enclosing managed object wrapper (ie. CCW) forwards to its inner object, if COM aggregation is involved.
        // This is typically accomplished through an implementation of 'ICustomQueryInterface'. Because all of the
        // Windows Runtime projected types have a base class, we have this common logic in 'WindowsRuntimeObject'.
        if (isAggregation)
        {
            // Indicate the scenario is COM aggregation
            createObjectFlags |= CreateObjectFlags.Aggregation;

            // Create the RCW by Here we just need to pass the inner object to 'ComWrappers' in this case.
            // The reason why we only do so in this case is that if we don't have a reference tracker, it means
            // that we're not in a XAML scenario where we need the .NET runtime to manage the inner lifetime.
            _ = WindowsRuntimeComWrappers.Default.GetOrRegisterObjectForComInstance((nint)acquiredNewInstanceUnknown, createObjectFlags, thisInstance, (nint)acquiredInnerInstanceUnknown);
        }
        else
        {
            // Same registration as for COM aggregation without reference tracker support (see above)
            _ = WindowsRuntimeComWrappers.Default.GetOrRegisterObjectForComInstance((nint)acquiredNewInstanceUnknown, createObjectFlags, thisInstance);
        }

        // This value is the reference tracker pointer we will wrap in the returned object reference.
        // It mirrors 'externalComObject'. We neeed this because in some scenarios we will not actually
        // keep a reference to the input reference tracker, but rather we'll discard it in this method.
        void* externalReferenceTracker = null;

        // The following branches set up the object reference to correctly handle 'AddRef' and 'Release'
        // calls on our object, which requires different logic depending on whether we're aggregating.
        // All the information gathered will flow into the creation flags for our object reference instance.
        CreateObjectReferenceFlags createObjectReferenceFlags = CreateObjectReferenceFlags.None;

        // The rest of the logic handles the reference tracker setup, after the 'ComWrappers' registration above
        if (isAggregation)
        {
            // Aggregation scenarios should avoid calling 'AddRef' on the 'acquiredNewInstanceUnknown' object.
            // This is due to the semantics of COM aggregation, and the fact that calling an 'AddRef'
            // on that instance will increment the reference count on the CCW, which in turn will make
            // it so that it cannot be cleaned up. However, note that calling 'AddRef' on the instance when
            // passed to unmanaged code is still correct, since unmanaged code is required to call 'Release'.
            //
            // Additional context: in COM aggregation scenarios, additional interfaces should be queried from
            // pointers to the inner object. Immediately after a 'QueryInterface' call, a 'Release' call should
            // be done on the returned pointer, but that pointer can still be retained and used. This is determined
            // by the 'IsAggregated' and 'PreventReleaseOnDispose' flags on the returned object reference.
            createObjectReferenceFlags |= CreateObjectReferenceFlags.IsAggregated;

            // If we have a reference tracker and we're aggregating, we should not release
            // the wrapped COM object on disposal. The reference tracker will handle things.
            if (referenceTracker != null)
            {
                createObjectReferenceFlags |= CreateObjectReferenceFlags.PreventReleaseOnDispose;

                // We can release the reference tracker first, since we said it's not going to be needed here
                _ = IUnknownVftbl.ReleaseUnsafe(referenceTracker);
            }
        }
        else if (referenceTracker != null)
        {
            // Special handling in case we have a reference tracker. Like mentioned, this object is used to tell
            // the reference tracker runtime whenever 'AddRef' and 'Release' are performed on 'acquiredNewInstanceUnknown'.
            // The non-aggregation case is the only one where we actually need to carry the reference tracker along.
            externalReferenceTracker = referenceTracker;

            // The runtime has already done 'AddRefFromTrackerSource' for this instance, so it would also handle doing
            // 'ReleaseFromTrackerSource' upon finalization. Because of that, we prevent it in the object reference.
            createObjectReferenceFlags |= CreateObjectReferenceFlags.PreventReleaseFromTrackerSourceOnDispose;

            // To balance the overall reference count on the inner instance (see notes at the start of the method),
            // we need to release it here. This would've been handled in a 'finally' block in caller methods otherwise.
            // We can only avoid this in the aggregation scenario, because we balance it out with the initial 'AddRef'
            // that we can skip. But when not aggregating, that 'AddRef' is on the new instance, so the inner instance
            // would have an extra reference count by the end of this method. We can fix that here to balance it again.
            _ = IUnknownVftbl.ReleaseUnsafe(acquiredInnerInstanceUnknown);

            // If we're not in a COM aggregation scenario, we can release the reference tracker object. This is because
            // it is already considered kept alive, even if we don't hold a reference to it, due to the fact that its
            // implementation lives in the same object as the target COM object (it will be a native object).
            _ = IUnknownVftbl.ReleaseUnsafe(referenceTracker);
        }

        // Handle 'S_OK' exactly, see notes for this inside 'IsFreeThreadedUnsafe'
        if (isFreeThreaded == WellKnownErrorCodes.S_OK)
        {
            return new FreeThreadedObjectReference(externalComObject, externalReferenceTracker, createObjectReferenceFlags);
        }

        // Otherwise, use a context aware object reference to track it, with the specialized instance.
        // In this method, the IID for the object reference depends on whether we are in an aggregation
        // scenario. That is because if we are, then the wrapped COM object will be the inner instance,
        // not the outer one (which would have its own default interface as IID). And the inner instance
        // will be an 'IInspectable', when returned by the activation factory.
        return isAggregation
            ? new ContextAwareInspectableObjectReference(externalComObject, externalReferenceTracker, createObjectReferenceFlags)
            : new ContextAwareInterfaceObjectReference(externalComObject, externalReferenceTracker, iid: in newInstanceIid, createObjectReferenceFlags);
    }

    /// <summary>
    /// Initializes a <see cref="WindowsRuntimeObjectReference"/> object for a given COM pointer to <c>IInspectable</c>.
    /// </summary>
    /// <param name="externalComObject">The external COM object to wrap in a managed object reference.</param>
    /// <returns>A <see cref="WindowsRuntimeObjectReference"/> wrapping <paramref name="externalComObject"/>.</returns>
    /// <remarks>
    /// <para>
    /// This method is only meant to be used when creating a managed object reference around native objects. It should not
    /// be used when dealing with Windows Runtime types instantiated from C# (which includes COM aggregation scenarios too).
    /// </para>
    /// <para>
    /// This method takes ownership of <paramref name="externalComObject"/>.
    /// </para>
    /// </remarks>
    internal static WindowsRuntimeObjectReference InitializeFromNativeInspectable(ref void* externalComObject)
    {
        void* acquiredComObject = externalComObject;

        externalComObject = null;

        // Early free-threaded check, to handle failure cases too (see notes above)
        HRESULT isFreeThreaded = ComObjectHelpers.IsFreeThreadedUnsafe(acquiredComObject);

        // If we are about to throw, release the external COM object first, to avoid leaks
        if (!WellKnownErrorCodes.Succeeded(isFreeThreaded))
        {
            _ = IUnknownVftbl.ReleaseUnsafe(acquiredComObject);

            Marshal.ThrowExceptionForHR(isFreeThreaded);
        }

        // Try to resolve an 'IReferenceTracker' pointer (see detailed notes above)
        _ = IUnknownVftbl.QueryInterfaceUnsafe(acquiredComObject, in WellKnownInterfaceIds.IID_IReferenceTracker, out void* referenceTracker);

        if (referenceTracker != null)
        {
            // If we have a reference tracker, we should report an 'AddRef' on it,
            // as we're wrapping the native object in a managed object reference.
            _ = IReferenceTrackerVftbl.AddRefFromTrackerSourceUnsafe(referenceTracker);

            // We can release the reference tracker, as it's implemented on the same native object,
            // meaning it will remain alive anyway as long as the main object is also kept alive.
            // This matches the handling of non aggregation scenarios above.
            _ = IUnknownVftbl.ReleaseUnsafe(referenceTracker);
        }

        // Create the right object reference. Because this method requires the input COM object
        // to be an 'IInspectable' interface pointer, we can also optimize for that case.
        return isFreeThreaded == WellKnownErrorCodes.S_OK
            ? new FreeThreadedObjectReference(acquiredComObject, referenceTracker)
            : new ContextAwareInspectableObjectReference(acquiredComObject, referenceTracker);
    }
}
