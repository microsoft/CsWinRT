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

### Authored unsealed types extended from outside .NET

The opposite direction is also supported: a public unsealed C# class in a Windows Runtime component is projected as a composable runtime class, so it can be extended from C++/WinRT (or any other language projection). In that case the roles are reversed: the **outer** object is the native derived object, and the **inner** object is the CCW for the authored C# instance.

The composition factory generated by C#/WinRT implements the same contract as `winrt::impl::composable_factory` in C++/WinRT:

- When it is called with a `null` controlling outer (standalone activation, e.g. `new MyComposableClass()` from C++), the instance is marshalled as a normal CCW and no inner object is produced.
- When it is called with a non-`null` controlling outer, the instance is registered as aggregated, and the factory returns two distinct objects: the **non-delegating inner** `IInspectable` (with a reference count of `1`, owned by the outer), and the default interface of the aggregate, which holds a reference on the controlling outer.

The non-delegating inner is a small hand-written COM object (it deliberately does not go through `ComWrappers`, because its identity and lifetime must be distinct from the ones of the aggregate). It answers `IUnknown` and `IInspectable` with itself, and owns everything else the aggregate needs: the CCW of the authored object, the per-aggregate vtables that CCW uses, and the interface pointers it hands out.

All the other interfaces of an aggregated object are handed out by that CCW, and their `IUnknown` methods have to delegate to the controlling outer, so that the whole aggregate shares one COM identity and one reference count. C#/WinRT achieves this **per aggregate**, without touching the CCW of any other object:

- The CCW of an aggregated instance is created through a dedicated `ComWrappers` instance (`WindowsRuntimeAggregationComWrappers`), so its interface entries are private to that one aggregate.
- For each interface the composable runtime class declares, the inner object allocates a private copy of the normal CCW vtable, and replaces only its six `IInspectable` (and therefore `IUnknown`) entries with delegating ones (`WindowsRuntimeAggregatedIInspectableImpl`). Every other entry is left untouched, so interface methods keep running the exact same CCW stubs, and still receive a real `ComInterfaceDispatch` pointer.
- Each vtable copy is preceded by a single slot holding the controlling outer, so the delegating entries resolve it with a pair of pointer loads. There is no lookaside table on that path, and no managed object has to be resolved to delegate an `IUnknown` call.

CCWs for objects that are **not** aggregated are completely unaffected: they keep the `IUnknown` implementation provided by the runtime, which is native precisely so that it keeps working when it is called from native code during a GC. Standalone activation of a composable class produces exactly the same CCW any other authored object gets.

Because the inner object resolves and caches every delegating interface pointer up front, answering a `QueryInterface` on it is just a lookup plus an `AddRef` on the controlling outer (which is where the returned pointer will release). Finally, marshalling an aggregated object back out to native code (e.g. returning `this` from an authored method, whether typed as the runtime class, as an authored interface, or as `object`) resolves the requested interface through the controlling outer, so the aggregate identity — and the reference counting — is preserved in that direction too.

As with any COM aggregation, the inner object never reference counts the controlling outer: the outer holds the only reference to the inner, and is therefore guaranteed to outlive it.

#### Interfaces that cannot take part in aggregation

A per-aggregate vtable copy can only be made for an interface whose CCW vtable C#/WinRT knows about *together with* the composable class, i.e. the Windows Runtime interfaces authored in the same component (including the synthesized per-runtime-class interfaces, the ones inherited from authored base classes, and everything in the transitive closure of their required interfaces). Every other interface gets its vtable from shared infrastructure the projection has no handle to:

- The interfaces every CCW carries (`IStringable`, `IWeakReferenceSource`, `IMarshal`, `IAgileObject`), whose vtables live in `WinRT.Runtime`.
- Custom-mapped interfaces (e.g. `IDisposable` → `IClosable`, `INotifyPropertyChanged`), also in `WinRT.Runtime`.
- Generic instantiations (e.g. `IList<T>`), emitted per-instantiation into `WinRT.Interop.dll`.
- Interfaces from the Windows SDK or from another Windows Runtime component.

C#/WinRT therefore rejects them explicitly, at two levels:

- **At build time**, the WinMD generator fails with `CSWINRTWINMDGEN0015` when a class that receives a public composition factory implements any such interface. The class has to be sealed, its constructors have to be made non-public, or the interface has to be dropped.
- **At run time**, the non-delegating inner object only ever hands out the interfaces it built a delegating vtable copy for, and returns `E_NOINTERFACE` for everything else. That covers the built-in interfaces every CCW carries, which no build-time check can remove. The controlling outer is responsible for implementing them, which is exactly what `winrt::implements` already does for `IUnknown`, `IInspectable`, `IAgileObject`, `IMarshal`, and `IWeakReferenceSource`.

The aggregate therefore never has more than one COM identity: an interface is either handed out with the identity of the controlling outer, or not handed out at all.

#### Which classes are composable

Only a public unsealed class **with at least one public constructor** receives a composition factory, and it is therefore the only shape native code can derive from. Unsealed classes that never get one — abstract base types, and types whose constructors are all non-public — are plain, non-derivable Windows Runtime base types, so none of the aggregation restrictions apply to them. This keeps common patterns (such as an abstract MVVM base implementing `INotifyPropertyChanged`) working unchanged.

Composition factory methods also cannot take array or generic parameters, because their CCW body runs the aggregation handshake at the ABI level and does not carry the extra marshalling state those need. That is reported at build time as `CSWINRTWINMDGEN0016`; if such a factory is ever seen anyway (e.g. in a component authored by another toolchain), the generated CCW body returns `E_NOTIMPL` rather than letting an exception escape an `[UnmanagedCallersOnly]` method and terminate the process.

#### Overridable members, and dispatching to the most derived implementation

Marking a member of a composable class `virtual` in C# projects it onto the `[Overridable]` interface C#/WinRT synthesizes for that class (`I{ClassName}Overrides`), which is what a derived Windows Runtime type replaces. That interface is generated from the compiled component, so the component itself cannot name it: it only supports dispatching *down*, from the derived type to the base implementation, which is what the default forwarders generated by the other language projection do.

An authored component can also declare its overridable surface explicitly, by applying `[WindowsRuntimeOverridable]` to an interface it authors and implementing it on the composable class. That is the C# equivalent of an `[overridable] interface` member on a runtime class in MIDL (the shape XAML uses for `IControlOverrides` and friends), and C#/WinRT projects it as `[Overridable]` on the interface implementation of every composable class implementing it. Because it is a real interface authored in the component, it can be named from managed code as well.

That matters because plain virtual dispatch in C# cannot see a native override: the aggregate is two distinct objects, and the managed instance is the base one, so calling one of its own overridable members always runs the managed implementation. Dispatching to the most derived implementation requires going through the controlling outer object, exactly like the `overridable()` helper in C++/WinRT does:

```csharp
[WindowsRuntimeOverridable]
public interface IThingOverrides
{
    int ComputeCoreValue();
}

public class Thing : IThingOverrides
{
    // The most derived implementation: the controlling outer object when this instance is aggregated,
    // and this instance otherwise (where ordinary managed virtual dispatch already does the right thing)
    private IThingOverrides Overridable =>
        WindowsRuntimeComposition.GetControllingOuterObject(this) as IThingOverrides ?? this;

    public virtual int ComputeCoreValue() => 42;

    public int CallComputeCoreValue() => Overridable.ComputeCoreValue();
}
```

`WindowsRuntimeComposition.GetControllingOuterObject` resolves the controlling outer object through the (non reference counted) pointer the composition factory was handed, and marshals it into managed code like any other native object. The resulting RCW takes its own reference on it, which is what makes it safe to use for the duration of the call — and also why it must never be stored on the aggregated instance (nor on anything it keeps alive): the controlling outer holds the only reference to the aggregated object, so a strong reference in the opposite direction would keep the whole aggregate alive forever. As with any managed wrapper for a native object, the reference it holds is released when the RCW is collected, so an aggregate that managed code dispatched through is destroyed after the next garbage collection rather than at the last `Release`.

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
