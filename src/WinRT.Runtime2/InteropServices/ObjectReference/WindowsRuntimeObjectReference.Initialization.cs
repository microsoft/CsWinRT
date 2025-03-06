// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="WindowsRuntimeObjectReference"/>
public unsafe partial class WindowsRuntimeObjectReference
{
    internal static WindowsRuntimeObjectReference InitializeForManagedType(
        bool isAggregation,
        WindowsRuntimeObject thisInstance,
        void* newInstanceUnknown,
        void* innerInstanceUnknown,
        in Guid newInstanceIid)
    {
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
        void* externalComObject = isAggregation ? innerInstanceUnknown : newInstanceUnknown;

        // We need to check whether the target COM object is free-threaded or not, as that will
        // influence how we'll create the resulting 'WindowsRuntimeObjectReference' instance.
        // We can do that right away, as this is the only call that might throw. We want to do
        // this as early as possible, so we can rely on the rest of the code never throwing,
        // which avoids the need to worry about releasing references if we fail halfway through.
        HRESULT isFreeThreaded = ComObjectHelpers.IsFreeThreadedUnsafe(externalComObject);

        Marshal.ThrowExceptionForHR(isFreeThreaded);

        // From this point forwards, it is critical that the rest of the code in this method never
        // throws an exception. By that we only refer to exceptions that would be possible to handle.
        // That is, technically speaking we are ultimately going to create a 'WindowsRuntimeObjectReference'
        // object, which could throw 'OutOfMemoryException' in extreme circumstances. However, that is
        // considered an error state that cannot be recovered from, so that is not a concern here.
        //
        // Before proceeding, increment the reference count on the target object, as we are going to wrap
        // it in the returned 'WindowsRuntimeObjectReference' object, but without transferring ownership.
        _ = IUnknownVftbl.AddRefUnsafe(externalComObject);

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

        // The instance supports 'IReferenceTracker'
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

            // Special case the RCW creation and registration in 'IReferenceTracker' scenarios. If we are doing
            // COM aggregation, then the reference tracking support is not actually needed. This is because
            // all the 'QueryInterface' calls on an object will be immediately followed by a 'Release' call on
            // the returned COM object pointer (see below for additional details).
            if (referenceTracker != null)
            {
                // We can release the reference tracker first, since we said it's not going to be needed here
                _ = IUnknownVftbl.ReleaseUnsafe(referenceTracker);

                // Now we can create and register the RCW, by also passing the inner object
                _ = WindowsRuntimeComWrappers.Default.GetOrRegisterObjectForComInstance((nint)newInstanceUnknown, createObjectFlags, thisInstance, (nint)innerInstanceUnknown);
            }
            else
            {
                _ = WindowsRuntimeComWrappers.Default.GetOrRegisterObjectForComInstance((nint)newInstanceUnknown, createObjectFlags, thisInstance);
            }
        }
        else
        {
            // Same registration as for COM aggregation without reference tracker support (see above)
            _ = WindowsRuntimeComWrappers.Default.GetOrRegisterObjectForComInstance((nint)newInstanceUnknown, createObjectFlags, thisInstance);
        }

        // The following branches set up the object reference to correctly handle 'AddRef' and 'Release'
        // calls on our object, which requires different logic depending on whether we're aggregating.
        // All the information gathered will flow into the creation flags for our object reference instance.
        CreateObjectReferenceFlags createObjectReferenceFlags = CreateObjectReferenceFlags.None;

        if (isAggregation)
        {
            // Aggregation scenarios should avoid calling 'AddRef' on the 'newInstanceUnknown' object.
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
            }
        }
        else
        {
            // Special handling in case we have a reference tracker. Like mentioned, this object is used to tell
            // the reference tracker runtime whenever 'AddRef' and 'Release' are performend on 'newInstanceUnknown'.
            // This is what allows the native/GC synchronization to correctly track references across the native and
            // unmanaged boundary. That this, it will allow the GC to detect reference cycles crossing through this
            // boundary, and avoid leaking objects that are only being kept alive by such cycles.
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
            // This results in the following object graph:
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
            // Reference tracking all
            if (referenceTracker != null)
            {
                // WinUI scenario
                // This instance should be used to tell the
                // Reference Tracker runtime whenever an AddRef()/Release()
                // is performed on newInstance.
                objRef.ReferenceTrackerPtr = referenceTracker;

                // The runtime has already done 'AddRefFromTrackerSource' for this instance, so it would also handle doing
                // 'ReleaseFromTrackerSource' upon finalization. Because of that, we prevent it in the object reference.
                createObjectReferenceFlags |= CreateObjectReferenceFlags.PreventReleaseFromTrackerSourceOnDispose;

                _ = IUnknownVftbl.ReleaseUnsafe(referenceTracker);
                Marshal.Release(referenceTracker);
            }

            _ = IUnknownVftbl.ReleaseUnsafe(newInstanceUnknown);
        }
    }

    internal static WindowsRuntimeObjectReference InitializeForNativeObject()
    {
        return null!;
    }
}
