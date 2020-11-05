# COM Interop Guide

## Overview
With .NET 5, direct support of WinRT has been removed from the C# language and runtime, including most of the functionality previously provided in [System.Runtime.InteropServices](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices?view=netcore-3.1).  In some cases, C#/WinRT provides equivalent functionality. In other cases, functionality may no longer be supported. This article provides a migration guide for interop scenarios in C#/WinRT.  

## Summary
Generally, the RCW/CCW functions in Marshal should be avoided, as they are incompatible with the new [ComWrappers](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.comwrappers?view=net-5.0) support in .NET 5. Unless otherwise noted, any other functionality in [Marshal](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.marshal?view=netcore-3.1) should still be available with .NET 5. C#/WinRT interop types are contained in the WinRT namespace.

| Scenario | .NET Core 3.1 | C#/WinRT |
|-|-|-|
| [IUnknown](#IUnknown) | GetIUnknownForObject | ((IWinRTObject)obj).NativeObject |
| [Ref Counting](#Ref-Counting) | AddRef, Release, FinalReleaseComObject, ReleaseComObject | ObjectReference.Attach, ObjectReference.FromAbi |
| [Casting](#Casting) | QueryInterface | IObjectReference.As\*\<T> |
| [COM Interop](#COM-Interop) | (TInterop)obj | obj.As\<TInterop>, T.As\<TInterop> |
| [Create RCW](#Create-RCW) | GetObjectForIUnknown, GetTypedObjectForIUnknown | FromAbi |
| [Create CCW](#Create-CCW) | GetComInterfaceForObject | FromManaged |
| [COM Data](#COM-Data) | GetComObjectData, SetComObjectData, IsComObject | IWinRTObject |


## IUnknown
The Marshal method for accessing the underlying native COM IntPtr of a projected type:
```csharp
IntPtr ptr = Marshal.GetIUnknownForObject(obj);
```
can be replaced with 
```csharp
IObjectReference objRef = ((IWinRTObject)obj).NativeObject;
```
And the IObjectReference's underlying native pointer can be accessed in two ways:
```csharp
IntPtr ptr = objRef.ThisPtr;    // no AddRef
IntPtr ptr = objRef.GetRef();   // calls AddRef
```

## Ref Counting
The Marshal ref counting methods:
```csharp
Marshal.AddRef(ptr);
Marshal.Release(ptr);
Marshal.FinalReleaseComObject(obj);
Marshal.ReleaseComObject(obj);
```
are now indirectly supported via IObjectReference, which manages the underlying COM object's refcount via IDisposable.  An IObjectReference can be created around a raw IntPtr:
```csharp
var objRef = ObjectReference<T>.Attach(ptr);     // transfers ownership (no AddRef)
var objRef = ObjectReference<T>.FromAbi(ptr);    // creates a new reference (calls AddRef)
```

## Casting
IObjectReference also provides several casting methods to replace Marshal.QueryInterface:
```csharp
objRef.As<T>(iid);              // cast to an arbitrary interface  
objRef.AsType<T>();             // cast to projected types and interfaces
objRef.AsInterface<T>();        // cast to user-defined (non-projected) ComImport interfaces 
```

## COM Interop
The previous runtime support for casting to a [ComImport](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.comimportattribute?view=netcore-3.1)-attributed IUnknown interface:
```csharp
TInterop interop = (TInterop)obj;
```
can be replaced with casting method calls on the projected object or type:
```csharp
TInterop interop = obj.As<TInterop>();     // interop on instance
TInterop interop = T.As<TInterop>();       // interop on static or class factory
```
as well as via IObjectReference, noted above:
```csharp
TInterop interop = objRef.AsInterface<TInterop>();  
```

### Custom Marshaling
When defining a [ComImport](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.comimportattribute?view=netcore-3.1) interop interface, marshaling of WinRT types must be manual (not using the runtime).  

#### Strings
The [MarshalAs](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.marshalasattribute?view=netcore-3.1) attribute no longer supports [UnmanagedType.HString](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.unmanagedtype?view=netcore-3.1).  Instead, strings should be marshaled with the C#/WinRT MarshalString class.
```csharp
// public void SetString([MarshalAs(UnmanagedType.HString)] String s);
public void SetString(IntPtr hstr);
// public void GetString([MarshalAs(UnmanagedType.HString)] out String s);
public void GetString(IntPtr out hstr);
// ...
// Marshal HSTRING to System.String
IntPtr hstr;
GetString(out hstr);
var str = MarshalString.FromAbi(hstr);
// Marshal System.String as fast-pass HSTRING reference
var marshalStr = MarshalString.CreateMarshaler("String");
SetString(MarshalString.GetAbi(marshalStr));
```

#### IInspectables
The runtime no longer supports marshaling IInspectable-based ComImport interfaces. But these can be approximated with IUnknown:
```csharp
[ComImport]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("68B3A2DF-8173-539F-B524-C8A2348F5AFB")]
internal unsafe interface IServiceProviderInterop
{
    // Note: Invoking methods on ComInterfaceType.InterfaceIsIInspectable interfaces
    // is no longer supported in the runtime, but can be simulated with IUnknown.
    void GetIids(out int iidCount, out IntPtr iids);
    void GetRuntimeClassName(out IntPtr className);
    void GetTrustLevel(out TrustLevel trustLevel);

    void GetService(IntPtr type, out IntPtr service);
}
```


## Create RCW
The Marshal RCW creation functions:
```csharp
T obj = (T)Marshal.GetObjectForIUnknown(ptr);
T obj = Marshal.GetTypedObjectForIUnknown<T>(ptr);
```
can be replaced with the appropriate projected type's (or marshaler's) FromAbi method:
```csharp
T obj = ABI.Namespace.T.FromAbi(ptr);
T obj = MarshalInterface<T>.FromAbi(ptr);
T obj = MarshalInspectable<T>.FromAbi(ptr);
T obj = MarshalGeneric<T>.FromAbi(ptr);
```

## Create CCW
The Marshal CCW creation function:
```csharp
Marshal.GetComInterfaceForObject(obj)
```
can be replaced with the appropriate projected type's (or marshaler's) FromManaged method, which supports ICustomQueryInterface.
```csharp
IntPtr ptr = ABI.Namespace.T.FromManaged(obj);
IntPtr ptr = MarshalInterface<T>.FromManaged(obj);
IntPtr ptr = MarshalInspectable<T>.FromManaged(obj);
IntPtr ptr = MarshalGeneric<T>.FromManaged(obj);
```

## COM Data
Use of the Marshal additional data functions:
```csharp
var obj = Marshal.GetComObjectData(obj, key);
Marshal.SetComObjectData(obj, key, data);
```
can be replaced with the dictionary property:
```csharp
((IWinRTObject)obj).AdditionalTypeData
```

The COM object classification function:
```csharp
Marshal.IsComObject(obj)
```
can be replaced with the expression:
```csharp
obj is IWinRTObject
```

## Unsupported
The following Marshal CCW/RCW-related functions are not supported in C#/WinRT:
```csharp
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
