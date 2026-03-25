# COM Interop Guide

## Overview

CsWinRT 3.0 provides a complete COM interop layer for Windows Runtime types on .NET 10+. All interop types are in the `WindowsRuntime.InteropServices` namespace (assembly: `WinRT.Runtime.dll`). Most marshalling is handled automatically by the generated projection and interop assemblies, but advanced scenarios may require direct use of the APIs below.

> **Note:** CsWinRT exposes many types and methods marked `[Obsolete]` with diagnostic `CSWINRT3001`. These are **private implementation details** consumed only by generated code (`cswinrt.exe` and `cswinrtinteropgen.exe`). They are not part of the versioned API surface, may change without notice, and should not be used in application code. This guide covers only the supported public APIs.

## Summary

| Scenario | CsWinRT 3.0 API |
|-|-|
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
