# CsWinRT 3.0 array infrastructure

This document builds on top of the [generic interface marshalling infrastructure document](marshalling-generic-interfaces.md) and describes the design of the CsWinRT 3.0 array marshalling infrastructure. The same basic concepts outlined in the other document apply here, with the main difference being a different API surface for marshaller types. This will be covered in more detail below.

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
    public static Array? UnboxToManaged<TCallback>(void* value, in Guid iid)
        where TCallback : IWindowsRuntimeArrayComWrappersCallback, allows ref struct;
}
```

The `UnboxToManaged` method will perform a `QueryInterface` on the input `value` pointer for the specified IID (as this method is only used to unbox arrays that are passed as opaque objects). It will then call `Value` on the ```IReferenceArray`1<T>``` interface pointer, and invoke the supplied callback to return the unboxed managed array instance.

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
    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        wrapperFlags = default;

        return WindowsRuntimeArrayMarshaller.UnboxToManaged<<string>ArrayComWrappersCallback>(value, in <string>ArrayImpl.IID)!;
    }
}
```

This attribute only implements `CreateObject` (with `CreatedWrapperFlags`), and simply forwards the logic to the generated callback type.

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

            <string>ArrayMarshaller.ConvertToUnmanaged(unboxedValue, out *count, out *result);

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

## Element marshallers

For non-trivial element types, array marshalling uses an **element marshaller pattern** — a separate generated type that handles bidirectional conversion of individual elements. The runtime's generic array marshaller classes accept the element marshaller as a `TElementMarshaller` generic type parameter, enabling the conversion logic to be resolved statically (via static abstract interface dispatch) with zero virtual overhead.

### Why element marshallers exist

Array marshalling involves iterating over elements and converting each one between its managed and ABI representation. For simple cases (blittable value types, strings, `object`, `Type`, `Exception`), dedicated array marshaller classes in the runtime handle this directly. But for types that require type-specific conversion logic — reference types, `KeyValuePair<K,V>`, `Nullable<T>`, managed value types, unmanaged value types — the runtime can't know the marshalling logic at compile time. Element marshallers bridge this gap:

- The **runtime** defines the array marshalling algorithm (allocate, iterate, convert, free) in generic classes
- The **interop generator** provides the per-type conversion strategy by emitting concrete element marshaller types
- The element marshaller type is passed as a generic type parameter, enabling **zero-cost abstraction** — the JIT/AOT compiler can inline the element conversion calls

### Runtime interfaces (in `WinRT.Runtime.dll`)

Five element marshaller interfaces are defined in `WindowsRuntime.InteropServices.Marshalling` (under `InteropServices/Marshalling/SzArrays/`):

| Interface | Element type | Members |
|-----------|-------------|---------|
| `IWindowsRuntimeReferenceTypeArrayElementMarshaller<T>` | Reference types | `ConvertToUnmanaged(T?)`, `ConvertToManaged(void*)` |
| `IWindowsRuntimeManagedValueTypeArrayElementMarshaller<T, TAbi>` | Managed value types (ABI needs cleanup) | `ConvertToUnmanaged(T)`, `ConvertToManaged(TAbi)`, `Dispose(TAbi)` |
| `IWindowsRuntimeUnmanagedValueTypeArrayElementMarshaller<T, TAbi>` | Unmanaged value types | `ConvertToUnmanaged(T)`, `ConvertToManaged(TAbi)` |
| `IWindowsRuntimeKeyValuePairTypeArrayElementMarshaller<TKey, TValue>` | `KeyValuePair<K,V>` | `ConvertToUnmanaged(KeyValuePair<K,V>)`, `ConvertToManaged(void*)` |
| `IWindowsRuntimeNullableTypeArrayElementMarshaller<T>` | `Nullable<T>` | `ConvertToUnmanaged(T?)`, `ConvertToManaged(void*)` |

All are `static abstract` interfaces (using the C# static abstract member pattern), `[Obsolete]`, and `[EditorBrowsable(Never)]` — they are implementation details consumed only by generated code.

### Runtime array marshaller classes (in `WinRT.Runtime.dll`)

Each element marshaller interface has a corresponding generic array marshaller class that takes `TElementMarshaller` as a generic type parameter on its methods:

| Array marshaller class | Element marshaller constraint | Methods |
|----------------------|------------------------------|---------|
| `WindowsRuntimeReferenceTypeArrayMarshaller<T>` | `IWindowsRuntimeReferenceTypeArrayElementMarshaller<T>` | `ConvertToUnmanaged<TElementMarshaller>`, `ConvertToManaged<TElementMarshaller>`, `CopyToUnmanaged<TElementMarshaller>`, `CopyToManaged<TElementMarshaller>` |
| `WindowsRuntimeManagedValueTypeArrayMarshaller<T, TAbi>` | `IWindowsRuntimeManagedValueTypeArrayElementMarshaller<T, TAbi>` | Same + `Dispose<TElementMarshaller>`, `Free<TElementMarshaller>` |
| `WindowsRuntimeUnmanagedValueTypeArrayMarshaller<T, TAbi>` | `IWindowsRuntimeUnmanagedValueTypeArrayElementMarshaller<T, TAbi>` | `ConvertToUnmanaged<TElementMarshaller>`, `ConvertToManaged<TElementMarshaller>`, `CopyToUnmanaged<TElementMarshaller>`, `CopyToManaged<TElementMarshaller>` |
| `WindowsRuntimeKeyValuePairTypeArrayMarshaller<TKey, TValue>` | `IWindowsRuntimeKeyValuePairTypeArrayElementMarshaller<TKey, TValue>` | Same 4 methods |
| `WindowsRuntimeNullableTypeArrayMarshaller<T>` | `IWindowsRuntimeNullableTypeArrayElementMarshaller<T>` | Same 4 methods |

The remaining array marshaller classes do **not** use element marshallers because they handle element conversion directly:

| Array marshaller class | Element type | Why no element marshaller |
|----------------------|-------------|--------------------------|
| `WindowsRuntimeBlittableValueTypeArrayMarshaller<T>` | Blittable value types | Memory layout is identical; uses bulk copy |
| `HStringArrayMarshaller` | `string` | Specialized HSTRING fast path |
| `WindowsRuntimeObjectArrayMarshaller` | `object` | Uses `WindowsRuntimeObjectMarshaller` directly |
| `ExceptionArrayMarshaller` | `Exception` | Blittable ABI representation |
| `TypeArrayMarshaller` | `Type` | Specialized marshalling |

### Generated element marshaller types (in `WinRT.Interop.dll`)

The interop generator emits one concrete element marshaller type per element type that needs one. Generation is handled by `InteropTypeDefinitionFactory.SzArrayElementMarshaller`, with separate factory methods per category: `ReferenceType()`, `ManagedValueType()`, `UnmanagedValueType()`, `KeyValuePair()`, `NullableValueType()`.

**Type shape:**
- **Name**: `<EncodedTypeName>ElementMarshaller` (e.g., `<MyNamespace.MyType>ElementMarshaller`)
- **Kind**: `sealed struct` (value type) for value-type elements, `abstract class` for reference-type elements
- **Interface**: Implements the matching `IWindowsRuntime*ArrayElementMarshaller<T>` interface
- **Members**: `ConvertToUnmanaged(...)`, `ConvertToManaged(...)`, and optionally `Dispose(...)` — all emitted as stub methods with `nop` markers, rewritten during the two-pass IL emit phase

**Selection logic** (in `InteropTypeDefinitionBuilder.SzArray.Marshaller()`):

| Element type category | Element marshaller factory | Array marshaller factory |
|----------------------|---------------------------|------------------------|
| Blittable value type | *(none)* | `SzArrayMarshaller.BlittableValueType()` |
| `KeyValuePair<K,V>` | `SzArrayElementMarshaller.KeyValuePair()` | `SzArrayMarshaller.KeyValuePair()` |
| `Nullable<T>` | `SzArrayElementMarshaller.NullableValueType()` | `SzArrayMarshaller.NullableValueType()` |
| Managed value type | `SzArrayElementMarshaller.ManagedValueType()` | `SzArrayMarshaller.ManagedValueType()` |
| Other value type | `SzArrayElementMarshaller.UnmanagedValueType()` | `SzArrayMarshaller.UnmanagedValueType()` |
| `object` | *(none)* | `SzArrayMarshaller.Object()` |
| `string` | *(none)* | `SzArrayMarshaller.String()` |
| `System.Type` | *(none)* | `SzArrayMarshaller.Type()` |
| `System.Exception` | *(none)* | `SzArrayMarshaller.Exception()` |
| Other reference type | `SzArrayElementMarshaller.ReferenceType()` | `SzArrayMarshaller.ReferenceType()` |

When an element marshaller is generated, the array marshaller factory receives it and passes the element marshaller type as a generic argument when calling the runtime array marshaller methods. For example, a generated `<<#Windows>JsonObject>ArrayMarshaller.ConvertToManaged(...)` would call `WindowsRuntimeReferenceTypeArrayMarshaller<JsonObject>.ConvertToManaged<<<#Windows>JsonObject>ArrayElementMarshaller>(size, value)`.

### Example: generated reference-type array element marshaller

Array element marshallers are only generated for Windows Runtime types (i.e., types projected from `.winmd` metadata), because only those types support `IReferenceArray<T>` boxing. User-defined managed types like `MyApp.MyViewModel` can get CCW marshalling code if they implement Windows Runtime interfaces, but they would not get array element marshallers.

For a Windows Runtime type like `Windows.Data.Json.JsonObject`, the interop generator emits (in namespace `ABI.Windows.Data.Json`):

```csharp
public abstract class <<#Windows>JsonObject>ArrayElementMarshaller
    : IWindowsRuntimeReferenceTypeArrayElementMarshaller<JsonObject>
{
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(JsonObject? value)
    {
        return ABI.Windows.Data.Json.JsonObjectMarshaller.ConvertToUnmanaged(value);
    }

    public static JsonObject? ConvertToManaged(void* value)
    {
        return ABI.Windows.Data.Json.JsonObjectMarshaller.ConvertToManaged(value);
    }
}
```

The name follows the standard mangling scheme: the SZ array type `JsonObject[]` produces `<<#Windows>JsonObject>Array` (see `references/name-mangling-scheme.md`), and the `ElementMarshaller` suffix is appended. The element marshaller methods forward to the type's existing marshaller from the generated projection assembly. The method bodies are emitted as stub `nop` markers during pass 1 and filled in with the appropriate marshalling IL during pass 2 (see the two-pass IL emit section in the main skill document).
