# COM Interop Guide

## Overview

CsWinRT 3.0 provides a complete COM interop layer for Windows Runtime types on .NET 10+. All interop types are in the `WindowsRuntime.InteropServices` namespace (assembly: `WinRT.Runtime.dll`). Most marshalling is handled automatically by the generated projection and interop assemblies, but advanced scenarios may require direct use of the APIs below.

## Summary

| Scenario | CsWinRT 3.0 API |
|-|-|
| [Interface identifiers](#interface-identifiers) | `WindowsRuntimeMarshal.GetIid<T>` |
| [Object identity](#object-identity) | `WindowsRuntimeMarshal.NativeReferenceEquals` |
| [COM object detection](#com-object-detection) | `obj is WindowsRuntimeObject`, `WindowsRuntimeMarshal.IsReferenceToManagedObject` |
| [Unwrap native pointer](#unwrap-native-pointer) | `WindowsRuntimeMarshal.TryGetNativeObject` |
| [Unwrap managed object](#unwrap-managed-object) | `WindowsRuntimeMarshal.TryGetManagedObject` |
| [Create RCW](#create-rcw) | `WindowsRuntimeMarshal.ConvertToManaged` |
| [Create CCW](#create-ccw) | `WindowsRuntimeMarshal.ConvertToUnmanaged` |
| [Release native pointer](#release-native-pointer) | `WindowsRuntimeMarshal.Free` |
| [Activation factories](#activation-factories) | `WindowsRuntimeActivationFactory` |
| [Buffer access](#buffer-access) | `WindowsRuntimeBufferMarshal.TryGetDataUnsafe` |

## WindowsRuntimeMarshal

The `WindowsRuntimeMarshal` class (`WindowsRuntime.InteropServices`) is the primary public API for low-level marshalling of Windows Runtime objects.

### Interface identifiers

Use `GetIid<T>()` to obtain the native interface identifier associated with a projected or custom-mapped Windows Runtime type:

```csharp
Guid vectorIid = WindowsRuntimeMarshal.GetIid<IList<string>>();
Guid classIid = WindowsRuntimeMarshal.GetIid<Windows.Foundation.Collections.PropertySet>();
Guid inspectableIid = WindowsRuntimeMarshal.GetIid<object>();
```

Interfaces and delegates return their own IID; runtime classes return the IID of their default interface, including generic default interfaces. Values and enums return their boxing-interface (`IReference<T>`) IID, so `GetIid<int>()` and `GetIid<int?>()` return the same value. `KeyValuePair<K,V>` instead returns its `IKeyValuePair<K,V>` IID. Single-dimensional arrays return their `IReferenceArray<T>` IID. XAML-specific mappings follow the application's WinUI/UWP XAML configuration.

The accepted inputs follow the Windows Runtime mappings used by `Type` ↔ `TypeName` marshalling. When `T` itself derives from `Exception` or `Type`, the lookup uses the corresponding mapped base type; this normalization does not apply to array elements or generic arguments. Ordinary managed types, non-generic `EventHandler`, jagged arrays, and nullable `KeyValuePair<K,V>` have no supported IID entry and are rejected by the lookup. Unsupported constructed types remain subject to the interop generator's existing marshalling constraints when they appear in analyzed metadata. Static runtime classes and Windows Runtime attributes also throw: they can have metadata names, but have no native interface IID.

IIDs are computed entirely at build time and stored on empty `[Guid]` provider types in a dedicated external type map. Non-generic entries are conditional on their Windows Runtime types being retained, not allocated. Constructed generic and array entries instead depend on a non-generic argument or element type: Native AOT can construct the closed type from shared generic code at runtime without retaining that exact instantiation at compile time. This can retain additional IID metadata when the API is used, but does not retain the associated marshalling code. The runtime looks up a provider and returns its `Type.GUID`; it never computes an IID or reconstructs a Windows Runtime signature.

Keys include the assembly simple name at every nesting level, without versions or public key tokens. For example, `IList<Uri>` uses ``[System.Private.CoreLib]System.Collections.Generic.IList`1[[System.Private.Uri]System.Uri]``. This distinguishes unrelated types with the same full name in different assemblies. Results are cached by managed type.

Generic helpers work when the concrete generic or array type is discoverable from the compiled application or its references. Shared discovery includes generic method arguments and propagates statically discoverable contexts through interface and virtual implementations, default interface bodies, and constrained static interface calls. This also covers implementations that construct types such as `IList<T>` internally. Discovery follows the configured assembly-selection rules and bounded expansion limits; it does not guarantee coverage of types created only at runtime, such as a previously unseen `MakeGenericType` instantiation. A missing precomputed entry causes `NotSupportedException`; there is no runtime-computation fallback.

IID metadata is emitted from the existing discovery state and fixed runtime and projection catalogs, without another application traversal. An assembly excluded with `CsWinRTMarshallingDisabledAssembly` does not contribute its own IID-only usages. A constructed type used there must also be visible in an analyzed assembly; built-in and projected non-generic metadata remains available from the catalogs.

Generator warnings about incomplete generic discovery also indicate possible missing IID entries. They do not prove that a particular lookup will fail: the type may already have been discovered through another usage.

A closed generic or array argument participates in the same marshalling discovery as other method arguments, even when it is used only for IID lookup. The IID map itself is independent of the generated marshalling code and `TypeName` metadata proxies.

The lookup is independent of marshalling tables and does not initialize vtables, activate runtime classes, or require objects to be constructed or boxed. When the API is unused, trimming and Native AOT remove the entire IID map, its metadata providers, and lookup code. IID metadata is generated independently of whether an application calls this API; entries for constructed types depend on the shared build-time discovery state.

### Object identity

To check whether two objects are the same managed instance or wrap the same underlying native COM object:

```csharp
bool areSame = WindowsRuntimeMarshal.NativeReferenceEquals(obj1, obj2);
```

This performs an identity comparison: if both objects wrap native WinRT objects, it unwraps them and compares their `IUnknown` identity pointers.

### COM object detection

To check whether a native COM pointer is actually a CCW (callable COM wrapper) for a managed object that was marshalled to native code:

```csharp
bool isManagedCCW = WindowsRuntimeMarshal.IsReferenceToManagedObject(nativePtr);
```

### Unwrap native pointer

To retrieve the underlying native COM pointer from a managed wrapper (with an `AddRef`):

```csharp
if (WindowsRuntimeMarshal.TryGetNativeObject(obj, out void* nativePtr))
{
    try
    {
        // nativePtr is the underlying native object (reference count incremented)
    }
    finally
    {
        WindowsRuntimeMarshal.Free(nativePtr);
    }
}
```

### Unwrap managed object

To retrieve the original managed object from a native COM pointer that is a CCW:

```csharp
if (WindowsRuntimeMarshal.TryGetManagedObject(nativePtr, out object? managed))
{
    // managed is the original .NET object behind the CCW
}
```

### Create RCW

To create a managed wrapper (RCW) for a native Windows Runtime object pointer. If the pointer is a CCW, this returns the original managed object instead of creating a new wrapper:

```csharp
object? managed = WindowsRuntimeMarshal.ConvertToManaged(nativePtr);
```

For an explanation of how CsWinRT decides *which* managed type to create for a given native object, see [RCW marshalling](rcw-marshalling.md).

### Create CCW

To marshal a managed object to a native COM pointer. If the object already wraps a native object, this unwraps it (with an `AddRef`). Otherwise, a CCW is created:

```csharp
void* nativePtr = WindowsRuntimeMarshal.ConvertToUnmanaged(managedObj);
try
{
    // use nativePtr
}
finally
{
    WindowsRuntimeMarshal.Free(nativePtr);
}
```

Native code can also need a CCW **around a projected native object's managed wrapper**. For example, XAML uses reference tracking to obtain managed collection interfaces when a native collection does not expose the bindable or covariant interfaces it needs. Opt the projected class into CCW generation with an assembly attribute:

```csharp
[assembly: WindowsRuntimeNativeExposedType(typeof(Windows.UI.Xaml.DependencyObjectCollection))]
```

This supplies the wrapper's CCW vtables without replacing its RCW marshaller. Ordinary marshalling still unwraps the original native object, and native-to-managed conversion still creates the projected type. Collection adapters can wrap native enumerators when a required bindable or covariant iterator interface is unavailable; ordinary interface marshalling remains strict.

### Release native pointer

To release a native COM pointer. Unlike `Marshal.Release`, this method is null-safe:

```csharp
WindowsRuntimeMarshal.Free(nativePtr);
```

## Activation factories

The `WindowsRuntimeActivationFactory` class provides APIs for retrieving activation factories for Windows Runtime types by their runtime class name.

### Custom activation handler

You can register a custom activation handler that intercepts all Windows Runtime type activations. This is useful for testing or for custom hosting scenarios:

```csharp
WindowsRuntimeActivationFactory.SetWindowsRuntimeActivationHandler(
    (string className, out void* factory) =>
    {
        // Custom activation logic
        factory = null;
        return false; // return true if handled
    });
```

> **Note:** The handler can only be set once per process. Subsequent calls throw `InvalidOperationException`.

### Get activation factory

To retrieve an activation factory as a raw pointer:

```csharp
// Throws on failure
void* factory = WindowsRuntimeActivationFactory.GetActivationFactoryUnsafe("MyNamespace.MyClass");

// With a specific interface IID
void* factory = WindowsRuntimeActivationFactory.GetActivationFactoryUnsafe("MyNamespace.MyClass", in iid);

// Non-throwing variants
if (WindowsRuntimeActivationFactory.TryGetActivationFactoryUnsafe("MyNamespace.MyClass", out void* factory))
{
    // factory is valid
}
```

## Buffer access

The `WindowsRuntimeBufferMarshal` class provides direct access to the underlying data of Windows Runtime buffer types.

### IBuffer data access

To get a pointer to the underlying data of an `IBuffer`:

```csharp
if (WindowsRuntimeBufferMarshal.TryGetDataUnsafe(buffer, out byte* data))
{
    // data points to the buffer's underlying memory
    // Keep the buffer alive while using the pointer (e.g. with GC.KeepAlive)
}
```

### IMemoryBufferReference data access

To get a pointer and capacity for an `IMemoryBufferReference`:

```csharp
if (WindowsRuntimeBufferMarshal.TryGetDataUnsafe(bufferRef, out byte* data, out uint capacity))
{
    var span = new ReadOnlySpan<byte>(data, (int)capacity);
}
```

## Unsupported

The following `Marshal` CCW/RCW-related functions are not supported in CsWinRT:

```
AreComObjectsAvailableForCleanup
ChangeWrapperHandleStrength
CleanupUnusedObjectsInCurrentContext
CreateAggregatedObject
CreateWrapperOfType
GetEndComSlot
GetIDispatchForObject
GetStartComSlot
GetUniqueObjectForIUnknown
```
