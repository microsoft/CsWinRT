# CsWinRT 3.0 generic interfaces infrastructure

This document outlines the design of Windows Runtime generic collection interfaces in CsWinRT 3.0.

There are multiple scenarios to handle for each generic interface:
- Marshalling with static type information (eg. `IEnumerator<string>` return)
- Marshalling without static type information (ie. `object` return)
  - Both of these apply to either marshalling direction
  - RCW marshalling can be both via anonymous types, and projected RCW types
- Projection methods (accounting for semantics differences, if needed)
- CCW vtable implementation
- IDIC interface implementation
- IDIC interface implementation lookup

Supporting all of these scenarios, and doing so as efficiently as possible while also respecting all of CsWinRT 3.0 design goals (eg. proper trimming support), requires several additional types being generated per generic interface instantiation, along with some related APIs exposed from the CsWinRT runtime.

We will use `IEnumerator<T>` (`IIterator<T>`) as reference to document all of these types. There are some differences with other generic interface types, but they all follow the same general principles. This document will also mention such differences where relevant.

> [!NOTE]
> This document specifically only covers generic **collection** interfaces. Other generic type instantiations, such as delegate types (and associated event source infrastructure, when applicable) and `KeyValuePair<TKey, TValue>`, are handled differently, and are already implemented in `cswinrtinteropgen`. See code there for reference.

## RCW (in `WinRT.Runtime.dll`)

The starting point for RCW scenarios is an RCW class that can be used to marshal interfaces to native. The Windows Runtime interfaces are mapped to different interfaces in the .NET world (eg. `IIterator<T>` -> `IEnumerator<T>`), and sometimes there might be subtle semantic differences, which CsWinRT takes care of hiding. Here is the API surface for the `IEnumerator<T>` RCW:

```csharp
public abstract class WindowsRuntimeEnumerator<T, TIIteratorMethods> : WindowsRuntimeObject, IEnumerator<T>, IWindowsRuntimeInterface<IEnumerator<T>>
    where TIIteratorMethods : IIteratorMethodsImpl<T>
{
    protected WindowsRuntimeEnumerator(WindowsRuntimeObjectReference nativeObjectReference);

    protected internal sealed override bool HasUnwrappableNativeObjectReference => true;
    protected sealed override bool IsOverridableInterface(in Guid iid);

    // These implement the managed 'IEnumerator<T>' interface, with the expected .NET semantics
    public T Current { get; }
    object IEnumerator.Current { get; }
    public bool MoveNext();
    public void Dispose();
    public void Reset();

    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IEnumerator<T>>.GetInterface();
}
```

This type derives from `WindowsRuntimeObject`, which provides all the usual support for IDIC, generated COM interfaces, and more. The key design here is that the RCW base type is generic on both the element type `T` and a `TIIteratorMethods` implementation. The `TIIteratorMethods` type parameter is constrained to `IIteratorMethodsImpl<T>`, and is used to invoke the type-specific `Current` method. The base class itself handles the state management necessary to properly map the `IIterator<T>` behavior to the `IEnumerator<T>` semantics (eg. `IIterator<T>` starts at index 0, while `IEnumerator<T>` starts before the first element).

Next, we have two separate "Methods" building blocks. First, an interface for the type-specific `Current` method:

```csharp
public interface IIteratorMethodsImpl<T>
{
    static abstract T Current(WindowsRuntimeObjectReference thisReference);
}
```

This is a static abstract interface that each generic instantiation will implement to provide the type-specific logic to retrieve the current item (including any necessary marshalling). Only the `Current` method is part of this interface because it requires type-specific marshalling.

For the non-type-specific methods (`HasCurrent` and `MoveNext`), there is a shared non-generic static class:

```csharp
public static class IIteratorMethods
{
    public static bool HasCurrent(WindowsRuntimeObjectReference thisReference);
    public static bool MoveNext(WindowsRuntimeObjectReference thisReference);
}
```

These methods are shared across all `IIterator<T>` instantiations because they don't require type-specific marshalling. They invoke the underlying `IIterator<T>` vtable methods directly.

The `WindowsRuntimeEnumerator<T, TIIteratorMethods>` base class uses both of these to implement the full `IEnumerator<T>` interface. The `MoveNext` method uses `IIteratorMethods.HasCurrent` and `IIteratorMethods.MoveNext` for control flow, and `TIIteratorMethods.Current` to retrieve and cache the current value when a valid item is available.

Lastly, we also need to introduce some additional building blocks to wire things up at marshalling time. First, a new interface:

```csharp
public interface IWindowsRuntimeUnsealedObjectComWrappersCallback
{
    static abstract bool TryCreateObject(
        void* value,
        ReadOnlySpan<char> runtimeClassName,
        [NotNullWhen(true)] out object? wrapperObject,
        out CreatedWrapperFlags wrapperFlags);

    static abstract object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags);
}
```

This is conceptually similar to `IWindowsRuntimeObjectComWrappersCallback` (which is used for delegates and sealed runtime class names), with the main difference being that it is specialized for unsealed types (and interfaces). In this case, implementations need to know the runtime class name of the native object, to determine whether they can marshal it via the fast path using a statically visible RCW type (see below), or whether they should fallback to the logic to marshal arbitrary opaque object that is used when no static type information is available (which involves going through the interop type lookup to get the marshalling info, etc.). The `CreateObject` method provides a fallback path if `TryCreateObject` fails but no more specific RCW type could be resolved (or if `GetRuntimeClassName` fails). Both methods also return `CreatedWrapperFlags` to provide additional information about the created wrapper.

And lastly, a new marshaller type to support this interface:

```csharp
public static class WindowsRuntimeUnsealedObjectMarshaller
{
    public static object? ConvertToManaged<TCallback>(void* value)
        where TCallback : IWindowsRuntimeUnsealedObjectComWrappersCallback, allows ref struct;
}
```

This is conceptually very similar to `WindowsRuntimeDelegateMarshaller`, which uses `IWindowsRuntimeObjectComWrappersCallback`.

## RCW (in `WinRT.Interop.dll`)

Moving on to `WinRT.Interop.dll`, the first generated type is an implementation for the `IIteratorMethodsImpl<T>` interface above:

```csharp
public abstract class IIterator_stringMethods : IIteratorMethodsImpl<string>
{
    public static string Current(WindowsRuntimeObjectReference objectReference)
    {
        throw new NotImplementedException();
    }
}
```

This is where the specialized invocation and marshalling logic for the `Current` method lives, for each constructed generic interface. Note that only `Current` needs to be type-specific — `HasCurrent` and `MoveNext` are handled by the shared non-generic `IIteratorMethods` class in the runtime.

To pair this "Methods" interface implementation, a non-generic "Methods" type is also generated for the mapped .NET interface:

```csharp
public static class IEnumerator_stringMethods
{
    public static string Current(WindowsRuntimeObjectReference objectReference)
    {
        return IIterator_stringMethods.Current(objectReference);
    }

    // Other forwarding methods for HasCurrent, MoveNext (via IIteratorMethods)...
}
```

This is simply calling the `Current` method through the generated `IIteratorMethodsImpl<T>` implementation. This is used by all actual consumers of this interface on the RCW side (eg. by projection code in any projection assembly), via `[UnsafeAccessor]` calls on this generated "Methods" type. Keeping these types non-generic makes invocations simpler too, as it avoids the additional complexity of handling generic type arguments of inaccessible types, which would require proxy types at each callsite.

For instance, here's an example of what a callsite from a projection assembly can look like, when needing to use any of these methods:

```csharp
[UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
public static extern string Current(
    [UnsafeAccessorType("ABI.System.Collections.Generic.IEnumerator_stringMethods, WinRT.Interop.dll")] object? _,
    WindowsRuntimeObjectReference objectReference);
```

With all these set up, we can now also generate the "NativeObject" type, which is the RCW implementation:

```csharp
public sealed class IEnumerator_stringNativeObject : WindowsRuntimeEnumerator<string, IIterator_stringMethods>
{
    public IEnumerator_stringNativeObject(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference)
    {
    }
}
```

This derives from `WindowsRuntimeEnumerator<T, TIIteratorMethods>` with the generated `IIterator_stringMethods` as the type argument. The base class handles all the `IEnumerator<T>` semantics, using `IIteratorMethods.HasCurrent`/`MoveNext` for control flow and `IIterator_stringMethods.Current` (via the `TIIteratorMethods` type parameter) for retrieving the current element. No abstract methods need to be overridden.

With this, we can now introduce the `IWindowsRuntimeUnsealedObjectComWrappersCallback` implementation for this generic type instantiation:

```csharp
public abstract unsafe class IEnumerator_stringComWrappersCallback : IWindowsRuntimeUnsealedObjectComWrappersCallback
{
    public static bool TryCreateObject(
        void* value,
        ReadOnlySpan<char> runtimeClassName,
        [NotNullWhen(true)] out object? wrapperObject,
        out CreatedWrapperFlags wrapperFlags)
    {
        if (runtimeClassName.SequenceEqual("Windows.Foundation.Collections.IIterator`1<String>"))
        {
            WindowsRuntimeObjectReference objectReference = WindowsRuntimeObjectReference.CreateUnsafe(value, in IEnumerator_stringImpl.IID)!;

            wrapperObject = new IEnumerator_stringNativeObject(objectReference);
            wrapperFlags = default;

            return true;
        }

        wrapperObject = null;
        wrapperFlags = default;

        return false;
    }

    public static object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        WindowsRuntimeObjectReference objectReference = WindowsRuntimeObjectReference.CreateUnsafe(value, in IEnumerator_stringImpl.IID)!;

        wrapperFlags = default;

        return new IEnumerator_stringNativeObject(objectReference);
    }
}
```

This provides the RCW marshalling with a fast path when the runtime class name is an exact match via `TryCreateObject`. Note that when this is used, the internal `ComWrappers` implementation can still do several optimizations, such as flowing the initial interface pointer through, so that no additional `QueryInterface` call is needed if the fast path is taken, and only calling `GetRuntimeClassName` once, and reusing it even in case the fast path fails and the fallback logic has to be used (as the runtime class name cannot change for a given object). The `CreateObject` fallback is called if `TryCreateObject` fails but no more specific RCW type could be resolved.

Of course, this can only be used when static type information is available. We'll need an attribute when that is not the case:

```csharp
public sealed unsafe class IEnumerator_stringComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        WindowsRuntimeObjectReference objectReference = WindowsRuntimeObjectReference.Create(value, in IEnumerator_stringImpl.IID)!;

        wrapperFlags = default;

        return new IEnumerator_stringNativeObject(objectReference);
    }
}
```

This provides a similar implementation to the `ComWrappersCallback` above, but using `Create` instead of `CreateUnsafe`. The difference between the two is that `Create` performs a `QueryInterface` call on the input pointer to retrieve the requested interface pointer, whereas `CreateUnsafe` assumes the input pointer already points to the correct interface and simply increments its reference count. The `ComWrappersMarshallerAttribute.CreateObject` method receives an opaque `IInspectable` pointer (not a specific interface pointer), so it must use `Create` to resolve the correct interface. In contrast, the `TryCreateObject` fast path in the `ComWrappersCallback` can use `CreateUnsafe` because the `ComWrappers` infrastructure ensures the input pointer is already the exact interface pointer that was statically visible at the marshalling callsite.

### IDIC

To make dynamic casts work, IDIC is also supported. For this, an implementation interface is generated, per instantiation:

```csharp
[DynamicInterfaceCastableImplementation]
public interface IEnumerator_stringInterfaceImpl : IEnumerator<string>
{
    string IEnumerator<string>.Current
    {
        get
        {
            var thisObject = (WindowsRuntimeObject)this;
            var thisReference = thisObject.GetObjectReferenceForInterface(typeof(IEnumerator<string>).TypeHandle);

            return IEnumerator_stringMethods.Current(thisReference);
        }
    }

    // Other methods...
}
```

This is then added in the associated type map as follows:

```csharp
[assembly: TypeMapAssociation<DynamicInterfaceCastableImplementationTypeMapGroup>(
    source: typeof(IEnumerator<string>),
    proxy: typeof(IEnumerator_stringInterfaceImpl))]
```

Where `DynamicInterfaceCastableImplementationTypeMapGroup` is the type map group type defined in `WinRT.Runtime.dll`.

## CCW

To marshal managed objects to native, CsWinRT uses pre-built adapter types in the runtime to correctly adapt the semantics of the managed interfaces to the native ones. For `IEnumerator<T>`, the adapter type is defined in `WinRT.Runtime.dll`:

```csharp
public sealed class IEnumeratorAdapter<T> : IEnumerator<T>
{
    internal IEnumeratorAdapter(IEnumerator<T> enumerator);

    public static IEnumeratorAdapter<T> GetInstance(IEnumerator<T> enumerator);

    // These implement the 'IIterator<T>' methods with the adapted semantics
    public T Current { get; }
    public bool HasCurrent { get; }
    public bool MoveNext();
}
```

This adapter wraps an `IEnumerator<T>` instance and provides `IIterator<T>` semantics on top of it. For example, it handles the difference that `IIterator<T>` starts at index 0, while `IEnumerator<T>` starts before the first element. It maps `E_BOUNDS` and `E_CHANGED_STATE` error codes as appropriate.

The `GetInstance` method efficiently retrieves or creates adapter instances using a `ConditionalWeakTable<IEnumerator<T>, IEnumeratorAdapter<T>>` that lives in a `file`-scoped companion type. It also has a fast path that checks if the input is already an `IEnumeratorAdapter<T>` instance to avoid redundant wrapping.

> [!NOTE]
> Unlike the previous design, the adapter types and their associated tables are not generated per generic instantiation. Instead, they are generic types defined in `WinRT.Runtime.dll` and are shared across all instantiations. The generated CCW vtable methods in `WinRT.Interop.dll` call into these pre-built adapters directly.

For the `GetMany` method and other type-specific operations, extension methods on `IEnumeratorAdapter<T>` are provided in the runtime, with specialized overloads for different element type categories (blittable value types, managed value types, unmanaged value types, key-value pairs, reference types, strings, objects, etc.).

### Collection element marshallers

For `GetMany` CCW methods, the runtime's collection adapter extension types (`IEnumeratorAdapterExtensions`, `IListAdapterExtensions`, `IReadOnlyListAdapterExtensions`) accept a `TElementMarshaller` generic type parameter to perform per-element managed → ABI conversion. This follows the same strategy pattern used for SZ array element marshallers (see `references/marshalling-arrays.md`), but with a key difference: **collection element marshallers are one-way** (managed → ABI only), whereas array element marshallers are bidirectional.

**Runtime interfaces** (in `WinRT.Runtime.dll`, under `InteropServices/Marshalling/Collections/`):

| Interface | Element type | Members |
|-----------|-------------|---------|
| `IWindowsRuntimeReferenceTypeElementMarshaller<T>` | Reference types | `ConvertToUnmanaged(T?)` |
| `IWindowsRuntimeManagedValueTypeElementMarshaller<T, TAbi>` | Managed value types | `ConvertToUnmanaged(T)`, `Dispose(TAbi)` |
| `IWindowsRuntimeUnmanagedValueTypeElementMarshaller<T, TAbi>` | Unmanaged value types | `ConvertToUnmanaged(T)` |
| `IWindowsRuntimeKeyValuePairTypeElementMarshaller<TKey, TValue>` | `KeyValuePair<K,V>` | `ConvertToUnmanaged(KeyValuePair<K,V>)` |
| `IWindowsRuntimeNullableTypeElementMarshaller<T>` | `Nullable<T>` | `ConvertToUnmanaged(T?)` |

These are `static abstract` interfaces, `[Obsolete]`, and `[EditorBrowsable(Never)]` — implementation details consumed only by generated code.

**Runtime consumer example** — a `GetMany` extension method on `IEnumeratorAdapterExtensions`:

```csharp
public unsafe uint GetMany<TElementMarshaller>(uint itemsSize, void** items)
    where TElementMarshaller : IWindowsRuntimeReferenceTypeElementMarshaller<T>;
```

Similar overloads exist constrained to each of the other element marshaller interfaces. `IListAdapterExtensions` and `IReadOnlyListAdapterExtensions` also provide matching `GetMany<TElementMarshaller>` methods.

**Generated element marshaller types** (in `WinRT.Interop.dll`):

The interop generator emits concrete element marshaller types via `InteropTypeDefinitionFactory.IEnumeratorElementMarshaller`, with the same 5 factory methods as for SZ arrays: `ReferenceType()`, `ManagedValueType()`, `UnmanagedValueType()`, `KeyValuePair()`, `NullableValueType()`. The generated types:

- Implement the matching `IWindowsRuntime*ElementMarshaller<T>` interface
- Are emitted as `sealed struct` (value types) or `abstract class` (reference types), same as for array element marshallers
- Contain a `ConvertToUnmanaged(...)` stub method (rewritten during pass 2)
- Are named `<EncodedTypeName>ElementMarshaller`
- Are **emitted by the `IEnumerator<T>` builder** and tracked in emit state via `emitState.TrackTypeDefinition(elementMarshallerType, elementType, "ElementMarshaller")`
- Are **reused by the `IList<T>` and `IReadOnlyList<T>` method factories** via `emitState.LookupTypeDefinition(elementType, "ElementMarshaller")` — they are not re-emitted

The same element type categories that skip element marshallers for arrays also skip them for collections: blittable value types, `object`, `string`, `Type`, and `Exception` use specialized direct paths.

**Why one-way?** Collection `GetMany` methods copy items from a managed collection *out* to a native buffer. The reverse direction (native → managed) is handled by the RCW `Methods` types (e.g., `IIteratorMethodsImpl<T>.Current`), which use the full two-pass rewrite pipeline directly — they don't go through element marshallers.

We can now move on to the actual CCW implementation. As with all interfaces, we'll need a vtable:

```csharp
public unsafe struct IEnumerator_stringVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> get_Current;
    public delegate* unmanaged[MemberFunction]<void*, bool*, HRESULT> get_HasCurrent;
    public delegate* unmanaged[MemberFunction]<void*, bool*, HRESULT> MoveNext;
    public delegate* unmanaged[MemberFunction]<void*, int, void**, uint*, HRESULT> GetMany;
}
```

The interop .dll can share a vtable implementation for compatible ABI types, to minimize codegen (eg. for all reference types).

Now the actual CCW implementation type can be defined:

```csharp
public static unsafe class IEnumerator_stringImpl
{
    [FixedAddressValueType]
    private static readonly IEnumerator_stringVftbl Vftbl;

    static IEnumerator_stringImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.get_Current = &get_Current;
        // Initialize the other vtable slots...
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
    private static HRESULT get_Current(void* thisPtr, HSTRING* result)
    {
        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<IEnumerator<string>>((ComInterfaceDispatch*)thisPtr);
            var adapter = IEnumeratorAdapter<string>.GetInstance(unboxedValue);

            *result = HStringMarshaller.ConvertToUnmanaged(adapter.Current);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception ex)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
        }
    }

    // Other methods (get_HasCurrent, MoveNext, GetMany) follow the same pattern,
    // all calling through the IEnumeratorAdapter<T> instance.
}
```

The CCW vtable methods retrieve the managed `IEnumerator<T>` from the CCW dispatch, then obtain the corresponding `IEnumeratorAdapter<T>` instance via `GetInstance`. This adapter handles the semantics adjustments needed for native callers. For instance, `get_HasCurrent` calls `adapter.HasCurrent`, `MoveNext` calls `adapter.MoveNext()`, and `GetMany` calls the appropriate type-specific extension method.

> [!NOTE]
> For interfaces that do not need additional state for adapting CCWs for native consumers, the adapter types are not used. Rather, the CCW vtable methods would directly invoke the managed interface methods on the unwrapped CCW target.

## Marshalling

The only missing components now are the types to wire up marshalling. First, one for when static type information is available:

```csharp
public static unsafe class IEnumerator_stringMarshaller
{
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(IEnumerator<string>? value)
    {
        return WindowsRuntimeInterfaceMarshaller<IEnumerator<string>>.ConvertToUnmanaged(value, in IEnumerator_stringImpl.IID);
    }

    public static IEnumerator<string>? ConvertToManaged(void* value)
    {
        return Unsafe.As<IEnumerator<string>>(WindowsRuntimeUnsealedObjectMarshaller.ConvertToManaged<IEnumerator_stringComWrappersCallback>(value));
    }
}
```

Note that `WindowsRuntimeInterfaceMarshaller<T>` is now a generic type, where the type parameter `T` is the interface type being marshalled. This enables the marshaller to leverage `IWindowsRuntimeInterface<T>` for fast path optimizations and to provide better diagnostic messages in case of failures.

And along with this, also a proxy type to support opaque object marshalling scenarios:

```csharp
[WindowsRuntimeClassName("Windows.Foundation.Collections.IIterator`1<String>")]
[IEnumerator_stringComWrappersMarshaller]
public static class IEnumerator_string;
```

This code ensures that all scenarios mentioned at the start of the document can be supported, while also providing high performance, trim/AOT support, and allowing ILC to fully fold and pre-initialize vtables for all constructed interfaces, and all `ComInterfaceEntry` arrays for CCWs implementing any of these interfaces 🚀
