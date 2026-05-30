# C#/WinRT Object Lifetime and Reference Tracking

## Overview

C#/WinRT is a WinRT projection for C# which at a high level generates wrapper C# types to represent
WinRT types. The lifetime of any of these instantiated C# types is managed by the .NET garbage collector
as with any C# object. But as a WinRT projection, the lifetime of the WinRT objects it wraps is
managed by COM reference tracking. The XAML runtime also manages the lifetime of XAML / WinUI objects
and has its own reference tracking that interacts with .NET and its garbage collector. This document
serves the purpose of documenting how C#/WinRT interacts with all 3 systems to correctly manage the
lifetime of projected WinRT objects.

### COM reference tracking

Each WinRT object that we project is based on COM and implements a set of interfaces.
As per the COM design, every COM interface implements `IUnknown` which has an `AddRef` and `Release`
function. C#/WinRT calls the `AddRef` function anytime it gets a new reference to a WinRT object
which C#/WinRT holds onto using an `IObjectReference` instance. It also calls `AddRef` whenever it gives
out a reference to one of these objects across the ABI as an out parameter. C#/WinRT calls the
`Release` function whenever any of the `IObjectReference` instances holding onto the WinRT object is
disposed or finalized by the .NET garbage collector. As long as there are still references to the
native object, it stays alive even if the projected C# object gets finalized due to there being
no more references to it from C# code. But if the C# reference was the last reference to it, the
release will also end up cleaning up the native object.

The above describes what typically happens for any natively implemented WinRT object that C#/WinRT
projects. There are some differences to this when the object is instead a C# implemented object that is
projected into WinRT via a COM callable wrapper (CCW). This is done via a C# class implementing a set of
WinRT interfaces or via a C# class extending (aggregating in COM) an unsealed WinRT type.

In the former, the object is implemented purely in C# and its lifetime is managed by the .NET
garbage collector. C#/WinRT only comes into play when this C# object is passed across the ABI to a
WinRT function. When this happens, C#/WinRT creates a CCW for it using the .NET 5 ComWrappers API
and that is passed across the ABI. Any references to that CCW from the native side are tracked by
`AddRef` / `Release` calls on the `IUnknown` of the CCW which is provided by the `ComWrappers` API.

This means in addition to any references to the object from C# tracked by the garbage collector
keeping it alive, any native reference which increases the CCW reference count would also keep the
object alive and that is managed by the .NET runtime and `ComWrappers` implementation.


In the latter scenario, extending an unsealed WinRT type is typically done via COM Aggregation
which C#/WinRT does behind the scenes when a C# class extends such a projected type. In COM aggregation,
there is 2 objects in play: the outer object which is the CCW for the C# object and the inner object
which is the WinRT object being extended. Both these objects are made to look like one object known as the

composed object. To achieve that, the outer object delegates calls for any of the inner object

interfaces that aren't overridden to the inner object. Any calls for interfaces that are only
implemented on the outer object or is overridden by the outer object or is for the `IUnknown` interface
would be handled by the outer object itself. The last part means that the lifetime and the COM reference
counting of this aggregated object is maintained by the outer object and more specifically its `IUnknown`
implementation on the CCW from ComWrappers. This is where the standard COM reference tracking
convention described earlier starts to differ. As we know for CCWs, there are 2 things which

keep it alive: any references from C# to the managed object or any native references which had done
an `AddRef` incrementing the COM ref count. But we also know that for projected aggregated types
to make calls on interfaces provided by the inner object, they need to QueryInterface (QI) for them
from the inner object which would result in the COM reference count on the outer (CCW) increasing.
This means any QIs from C# on such objects will end up increasing the COM reference count on the CCW
and thereby keeping it alive and leaking it as any C# reference to such objects are
supposed to be tracked as managed references by the garbage collector and not as native references.
To address this, for any QI calls done as part of the aggregated object's C# projection implementation,
`Release` should be called right after the reference is obtained even if you plan to hold onto
the obtained interface to avoid repeatedly retrieving it. This prevents C# QIs by the composed object
from increasing the CCW reference count meant for tracking native references while allowing the
garbage collector to manage the lifetime of managed objects from managed references via its own
tracking. For any QI calls for which the result is handed out to the native side, `Release` should
not be called right after as it is a native reference which needs to be tracked by the CCW.

One notable caveat to this is tear off interfaces on aggregated objects. With tear off interfaces,
the interfaces typically perform their own COM reference counting separate from the object itself
allowing for the interfaces to manage their own lifetime. But this doesn't work well with aggregated
objects if one of these interfaces need to be QIed for by the composed object as part of the
projection implementation. This is because a `Release` would happen right after which would trigger
the cleanup of the interface as its lifetime isn't tied to the outer. Given that tear off interfaces
are rare and not typically used by C# consumers, C#/WinRT today doesn't address this
other than facilitating QI calls for them from the native side where `Release` isn't called right after.
The recommendation for tear off interfaces which do want to support such uses on aggregated objects
is that they can continue to be constructed on demand upon the first QI for it, but the interface
should not be cleaned up until the object is cleaned up even if there are no longer any reference
to that interface.

### XAML reference tracking

As mentioned earlier, the XAML runtime also manages the lifetime of XAML objects and has its own
supplemental reference tracking to COM reference tracking which it uses when interacting with .NET
and its garbage collector.

For native XAML objects that are being wrapped by C#/WinRT, the XAML runtime needs to
know about all the references to it from another reference tracking system like the .NET
garbage collector. This allows XAML to track scenarios where objects may have circular references
or only have references to it from objects that are pending clean up. Specifically, when a C# wrapper
is created for a XAML runtime tracked object (implements `IReferenceTracker`), the XAML runtime
needs to be informed of it by a call to `ConnectFromTrackerSource` on `IReferenceTracker`. This is
done by the ComWrappers implementation when an RCW is created. After that, any references to that
object that are tracked by the other reference tracking system (.NET garbage collector in this case)
needs to be informed to XAML by a call to `AddRefFromTrackerSource`. This is done by both C#/WinRT and
ComWrappers after any `AddRef` call to increment the COM reference count. Similarly, before the
`Release` call, there would be a `ReleaseFromTrackerSource` to indicate a reference on the object
was released. When the RCW is destructed, there would similarly be a call to
`DisconnectFromTrackerSource` to indicate that the .NET garbage collector no longer tracks the object.

For composed XAML objects where the lifetime is controlled by the .NET garbage collector rather than
the XAML runtime, XAML requires the CCW to implement the `IReferenceTrackerTarget` interface and its
respective methods. This allows XAML to inform the .NET garbage collector of any references the XAML
runtime takes and to indicate that even though an object may not have any COM reference counts that
it shouldn't be cleaned up as it is still in use.

### Related documentation

-	[COM Reference Tracking](https://docs.microsoft.com/windows/win32/com/managing-object-lifetimes-through-reference-counting)

-	[COM Aggregation](https://docs.microsoft.com/windows/win32/com/aggregation)
