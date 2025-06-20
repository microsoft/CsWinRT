# CsWinRT 3.0 array infrastructure

This document builds on top of the [generic interface marshalling infrastructure document](marshalling-generic-interfaces.md) and describes the design of the CsWinRT 3.0 array marshalling infrastructure. The same basic concepts outlined in the other document apply here, with the amin difference being a different API surface for marshaller types. This will be covered in more detail below.

## Native -> managed (in `WinRT.Runtime.dll`)

The runtime .dll will expose two infrastructure APIs for `WinRT.Interop.dll` to use for marshalling arrays (implementation specific).

The first API is a new type of "`ComWrappers` callback", specialized for array types:

```csharp
public interface IWindowsRuntimeArrayComWrappersCallback
{
    static abstract Array CreateArray(uint count, void* value);
}
```

And a marshaller type to support this:

```csharp
public static class WindowsRuntimeArrayMarshaller
{
    public static Array? ConvertToManaged<TCallback>(void* value, in Guid iid)
        where TCallback : IWindowsRuntimeArrayComWrappersCallback, allows ref struct;
}
```

The `ConvertToManaged` method will perform a `QueryInterface` on the input `value` pointer for the specified IID (as this method is only used to unbox arrays that are passed as opaque objects). It will then call `Value` on the ```IReferenceArray`1<T>``` interface pointer, and invoke the supplied callback to return the unboxed managed array instance.

## Native -> managed (in `WinRT.Interop.dll`)

Each array type will have a corresponding marshaller callback type, implementing `IWindowsRuntimeArrayComWrappersCallback`. Note that inputs to the `CreateArray` method will always be not-null. The generated `<string>ArrayMarshaller` type is detailed at the end of this document. This is what a generated callback type would look like for a `string` array:

```csharp
public abstract class <string>ArrayComWrappersCallback : IWindowsRuntimeArrayComWrappersCallback
{
    public static Array CreateArray(uint count, void* value)
    {
        return <string>ArrayMarshaller.ConvertToManaged(count, (HSTRING*)value);
    }
}
```

Along with this, a marshaller attribute is also needed, for unboxing arrays from opaque objects:

```csharp
public sealed class <string>ArrayComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    public override object CreateObject(void* value)
    {
        return WindowsRuntimeArrayMarshaller.ConvertToManaged<<string>ArrayComWrappersCallback>(value, in <string>ArrayImpl.IID)!;
    }
}
```

This attribute only implements `CreateObject`, and simply forwards the logic to the generated callback type.

## CCW

The CCW scenario for arrays is quite simple. We just need one vtable type, shared across all array types:

```csharp
public struct <IReferenceArrayVftbl>
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, uint*, void**, HRESULT> get_Value;
}
```

And then a CCW implementation type, which closely match the structure of CCW implementation types for generic interfaces:

```csharp
public static class <string>ArrayImpl
{
    [FixedAddressValueType]
    private static readonly <IReferenceArrayVftbl> Vftbl;

    static <string>ArrayImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Value = &get_Value;
    }

    public static ref readonly Guid IID
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]   
        get
        {
            ReadOnlySpan<byte> data =
            [
                0x94, 0x2B, 0xEA, 0x94,
                0xCC, 0xE9,
                0xE0, 0x49,
                0xC0,
                0xFF,
                0xEE,
                0x64,
                0xCA,
                0x8F,
                0x5B,
                0x90
            ];

            return ref Unsafe.As<byte, Guid>(ref MemoryMarshal.GetReference(data));
        }
    }

    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(ref Unsafe.AsRef(in Vftbl));
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Value(void* thisPtr, uint* count, HSTRING** result)
    {
        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<string[]>((ComInterfaceDispatch*)thisPtr);

            <string>ArrayMarshaller.CopyToUnmanaged(unboxedValue, out *count, out *result);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception ex)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
        }
    }
}
```

## Marshalling

Each array type will have a corresponding marshaller type generated in `WinRT.Interop.dll` (with the expected name mangling scheme):

```csharp
public static class <string>ArrayMarshaller
{
    public static void ConvertToUnmanaged(ReadOnlySpan<string> value, out uint size, out HSTRING* array);
    public static string[] ConvertToManaged(uint size, HSTRING* value);

    public static void CopyToUnmanaged(ReadOnlySpan<string> value, uint size, HSTRING* destination);
    public static void CopyToManaged(uint size, HSTRING* value, Span<string> destination);

    public static void Free(uint size, HSTRING* value);
}
```

And along with this, also a proxy type to support opaque object marshalling scenarios:

```csharp
[WindowsRuntimeClassName("Windows.Foundation.IReferenceArray`1<HString>")]
[<string>ArrayComWrappersMarshaller]
public static class <string>Array;
```

This code allows fully supporting arrays of any types, in an efficient manner, and in a trimmable way. The design of the marshaller types also allows marshalling stubs for both RCW methods and CCW methods to leverage things such as `stackalloc` and/or array pooling, where needed. For instance, passing a "FillArray" to a native method from an RCW marshalling stub doesn't need to actually allocate the marshalled array in native memory. Rather, it can do a "stackalloc or rent from pool" for the array of marshalled values, pass that to native, and then use `CopyToManaged` from that to copy the values back to the destination managed `Span<T>`. And of course, blittable types don't even need this at all, and can directly pass pinned buffers to native methods 🚀 
