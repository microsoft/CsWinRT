# C#/WinRT object lifetime and reference tracking

## Overview

C#/WinRT is a Windows Runtime (WinRT) projection for C#. At a high level, it generates wrapper C# types to represent WinRT types. The lifetime of these C# wrapper instances is managed by the .NET garbage collector (GC), like any other C# object. But as a WinRT projection, the lifetime of the underlying WinRT objects that they wrap is managed by COM reference counting. The XAML runtime additionally manages the lifetime of XAML and WinUI objects, and has its own reference tracking that interacts with .NET and its GC. This document describes how C#/WinRT interacts with all three systems to correctly manage the lifetime of projected WinRT objects.

Because WinRT is built on COM, two kinds of wrappers are involved:

- A **Runtime Callable Wrapper (RCW)** is the managed wrapper around a native WinRT object. In C#/WinRT, projected runtime classes derive from the `WindowsRuntimeObject` base class, and the underlying native COM pointer is owned by a `WindowsRuntimeObjectReference`, which is responsible for its reference counting.
- A **COM Callable Wrapper (CCW)** is the native COM representation of a managed object, created through the .NET [`ComWrappers`](https://learn.microsoft.com/dotnet/api/system.runtime.interopservices.comwrappers) API. C#/WinRT uses a `WindowsRuntimeComWrappers` implementation to create a CCW when a managed object is passed across the Application Binary Interface (ABI) to native WinRT code. If that managed object is itself an RCW already wrapping a native WinRT object, C#/WinRT unwraps it and passes the underlying native object across, instead of creating a CCW.

## COM reference tracking

Each WinRT object that C#/WinRT projects is based on COM and implements a set of interfaces. As per the COM design, every COM interface derives from `IUnknown`, which exposes the `AddRef` and `Release` methods that maintain the object's reference count. C#/WinRT calls `AddRef` whenever it obtains a new reference to a WinRT object (which it then holds onto using a `WindowsRuntimeObjectReference` instance), and whenever it hands out a reference to one of these objects across the ABI (for example, as an `out` parameter). It calls `Release` whenever a `WindowsRuntimeObjectReference` holding onto the WinRT object is disposed or finalized by the GC.

### Natively-implemented Windows Runtime objects

This is the common case: a native WinRT object wrapped by an RCW. The native object stays alive as long as something holds a COM reference to it, independently of the managed wrapper. The typical lifecycle is:

1. C#/WinRT obtains a native pointer to the object (for example, as the result of activation, a property getter, or a method call), wraps it in a `WindowsRuntimeObjectReference`, and takes a COM reference with `AddRef`.
2. While the `WindowsRuntimeObjectReference` is alive, the native object is kept alive even if the projected C# wrapper is finalized. As long as there are still references to the native object, it stays alive.
3. When the last `WindowsRuntimeObjectReference` to the object is disposed or finalized, C#/WinRT calls `Release`. If this was the last COM reference, the release also cleans up the native object.

Because a `WindowsRuntimeObjectReference` keeps native memory alive that the GC cannot see, each instance adds a small amount of GC memory pressure when it is created, and removes it when it is disposed or finalized. This gives the GC a hint about the additional native memory held on its behalf, so it can schedule collections appropriately.

A `WindowsRuntimeObjectReference` is either free-threaded or context-aware, depending on the agility of the native object that it wraps. A `FreeThreadedObjectReference` wraps an agile object that can be accessed from any thread, whereas a `ContextAwareObjectReference` is tied to the COM context it was created in, and marshals its `Release` call back to that context. This distinction does not change the reference-counting rules described above; it only affects how and where the underlying `IUnknown` calls are made.

### Managed objects implementing Windows Runtime interfaces

A managed object that implements one or more Windows Runtime interfaces is implemented purely in C#, and its lifetime is managed by the GC. C#/WinRT only comes into play when this object is passed across the ABI to a WinRT function. When that happens, C#/WinRT creates a CCW for it through the [`ComWrappers`](https://learn.microsoft.com/dotnet/api/system.runtime.interopservices.comwrappers) API, and that CCW is what is passed across the ABI. Any references to the CCW from the native side are tracked by `AddRef` and `Release` calls on the `IUnknown` of the CCW, which is provided by the `ComWrappers` API.

This means that, in addition to any references to the object from C# (tracked by the GC), any native reference that increments the CCW's reference count also keeps the object alive. This is managed by the .NET runtime and its `ComWrappers` implementation.

### Managed objects extending an unsealed Windows Runtime type

Extending an unsealed WinRT type is done through [COM aggregation](https://learn.microsoft.com/windows/win32/com/aggregation), which C#/WinRT performs behind the scenes when a C# class derives from such a projected type. In COM aggregation, there are two objects in play: the **outer** object, which is the CCW for the C# object, and the **inner** object, which is the WinRT object being extended. Together, they are made to look like a single object, known as the **composed** object.

To achieve this, the outer object delegates calls for any of the inner object's interfaces that are not overridden to the inner object. Calls for interfaces that are only implemented on the outer object, or are overridden by the outer object, or are for the `IUnknown` interface, are handled by the outer object itself. This last part means that the lifetime and the COM reference counting of the composed object are maintained by the outer object, and more specifically by the `IUnknown` implementation on the CCW provided by `ComWrappers`.

This is where the standard COM reference-counting convention starts to differ. For a CCW, there are two things that keep it alive: any references from C# to the managed object, and any native references that have incremented its COM reference count. However, for projected aggregated types to invoke methods on interfaces provided by the inner object, they must `QueryInterface` (QI) for them from the inner object, which increments the COM reference count on the outer (CCW). This means that QIs performed from C# on such objects would increase the CCW's reference count and leak it, because any C# reference to these objects is supposed to be tracked as a managed reference by the GC, and not as a native reference.

To address this, for any QI performed as part of the aggregated object's C# projection implementation, `Release` is called immediately after the reference is obtained, even when the interface is retained to avoid repeatedly retrieving it. This prevents C# QIs by the composed object from inflating the CCW reference count (which is meant for tracking native references), while still allowing the GC to manage the lifetime of the managed object through its own tracking. For any QI whose result is handed out to the native side, `Release` is **not** called immediately after, because it represents a native reference that must be tracked by the CCW.

In C#/WinRT, this behavior is captured by the `WindowsRuntimeObjectReference` created for aggregated interfaces: it is flagged as aggregated, so that after each successful `QueryInterface` it releases the returned pointer while still retaining it for later use.

### Tear-off interfaces on aggregated objects

One notable caveat concerns tear-off interfaces on aggregated objects. A tear-off interface performs its own COM reference counting, separate from the object itself, so that it can manage its own lifetime. This does not work well with aggregated objects when one of these interfaces must be QIed for by the composed object as part of the projection implementation: a `Release` happens immediately after the QI (as described above), which can trigger the cleanup of the tear-off interface, because its lifetime is not tied to the outer object.

Tear-off interfaces are rare, and C#/WinRT does not special-case them today, beyond facilitating QI calls for them from the native side (where `Release` is not called immediately after). A tear-off interface that wants to support aggregation should cache all of its instances for the lifetime of the composed object: it can still be constructed on demand on the first QI for it, but it should not be cleaned up until the object itself is cleaned up, even if there are no longer any references to that interface.

## XAML reference tracking

The XAML runtime also manages the lifetime of XAML objects, and has its own reference tracking that supplements COM reference counting when it interacts with .NET and the GC. This is exposed through the [`IReferenceTracker`](https://learn.microsoft.com/windows/win32/api/windows.ui.xaml.hosting.referencetracker/) family of interfaces.

For native XAML objects wrapped by C#/WinRT, the XAML runtime needs to know about all references to the object from another reference-tracking system, such as the .NET GC. This allows XAML to handle scenarios where objects have circular references, or are only kept alive by objects that are themselves pending cleanup. Specifically:

1. When an RCW is created for a XAML runtime-tracked object (one that implements `IReferenceTracker`), C#/WinRT marks the wrapper as a tracker object. The .NET `ComWrappers` tracker-support infrastructure then informs the XAML runtime by calling `ConnectFromTrackerSource`, and takes the first tracked reference with `AddRefFromTrackerSource`.
2. When C#/WinRT acquires an additional interface reference to the same tracked object (for example, through a `QueryInterface`), it takes a further tracked reference with `AddRefFromTrackerSource`.
3. When such an additional reference is released, C#/WinRT issues a `ReleaseFromTrackerSource` call (before the underlying COM `Release`) to indicate that the reference was released.
4. When the RCW is finalized, the runtime issues the final `ReleaseFromTrackerSource`, followed by `DisconnectFromTrackerSource`, to indicate that the GC no longer tracks the object.

This infrastructure is what allows the GC to detect reference cycles that cross the native/managed boundary. For example, consider a XAML `Grid` that contains a `Button` whose `Click` event is handled by a managed lambda that captures the `Grid`:

```csharp
Grid grid = new();
Button button = new();

grid.Children.Add(button);

button.Click += (s, e) => Console.WriteLine($"Action from inside '{grid.Name}'.");
```

This creates a cycle: the `Grid` RCW keeps the native `Grid` alive, which keeps the native `Button` alive, which keeps the event handler alive, which (through its captured closure) keeps the `Grid` RCW alive. Reference tracking lets the GC distinguish which `AddRef` calls come from native objects and which come from managed objects, and lets it crawl through objects across the boundary to reconstruct the real dependency graph. This is what allows the GC to collect such cycles instead of leaking them, and what makes the example above behave as a C# developer would intuitively expect.

For composed XAML objects whose lifetime is controlled by the GC rather than the XAML runtime, XAML requires the CCW to implement the `IReferenceTrackerTarget` interface and its respective methods. This allows XAML to inform the GC of any references the XAML runtime takes, and to indicate that even though an object may currently have no COM reference counts, it should not be cleaned up because it is still in use. C#/WinRT requests this support on the CCWs it creates for such objects, and the .NET runtime provides the `IReferenceTrackerTarget` implementation.

## Related documentation

- [Managing object lifetimes through reference counting](https://learn.microsoft.com/windows/win32/com/managing-object-lifetimes-through-reference-counting)
- [COM aggregation](https://learn.microsoft.com/windows/win32/com/aggregation)
- [`ComWrappers` class](https://learn.microsoft.com/dotnet/api/system.runtime.interopservices.comwrappers)
- [`IReferenceTracker` interface](https://learn.microsoft.com/windows/win32/api/windows.ui.xaml.hosting.referencetracker/nn-windows-ui-xaml-hosting-referencetracker-ireferencetracker)
