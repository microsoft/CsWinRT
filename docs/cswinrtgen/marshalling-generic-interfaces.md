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
> This document specifically only covers generic **collection** interfaces. Other generic type instantiations, such as delegate types (and associated event source infrastructure, when applicable) and `KeyValuePair<TKey, TValue>`, are handled differently, and are already implemented in `cswinrtgen`. See code there for reference.

## RCW (in `WinRT.Runtime.dll`)

The starting point for RCW scenarios is an RCW class that can be used to marshal interfaces to native. The Windows Runtime interfaces are mapped to different interfaces in the .NET world (eg. `IIterator<T>` -> `IEnumerator<T>`), and sometimes there might be subtle semantic differences, which CsWinRT takes care of hiding. Here is the API surface for the `IEnumerator<T>` RCW:

```csharp
public abstract class WindowsRuntimeEnumerator<T> : WindowsRuntimeObject, IEnumerator<T>, IWindowsRuntimeInterface<IEnumerator<T>>
{
    public WindowsRuntimeEnumerator(WindowsRuntimeObjectReference nativeObjectReference);

    protected sealed override bool IsOverridableInterface(in Guid iid);

    // These will be implemented to invoke the underlying 'IIterator<T>' methods directly
    protected abstract T CurrentNative { get; }
    protected abstract bool HasCurrentNative { get; }
    protected abstract bool MoveNextNative();

    // These will implement the managed 'IEnumerator<T>' interface, with the expected .NET semantics
    public T Current { get; }
    object IEnumerator.Current { get; }
    public bool MoveNext();
    public void Dispose();
    public void Reset();

    public WindowsRuntimeObjectReferenceValue GetInterface();
}
```

This type derives from `WindowsRuntimeObject`, which provides all the usual support for IDIC, generated COM interfaces, and more. If needed, these RCW types (one per generic interface) can also contain additional state necessary to properly map the Windows Runtime interface behavior to the managed one. This is the case for `IEnumerator<T>`, which is slightly different than `IIterator<T>`, for instance.

Next, an interface is needed for each "Methods" implementation, for each generic interface. For `IIterator<T>`, it looks like this:

```csharp
public interface IIteratorMethods<T>
{
    static abstract T Current(WindowsRuntimeObjectReference objectReference);

    static abstract bool HasCurrent(WindowsRuntimeObjectReference objectReference);

    static abstract bool MoveNext(WindowsRuntimeObjectReference obj);

    static abstract uint GetMany(WindowsRuntimeObjectReference obj, Span<T> items);
}
```

This exactly matches the shape that any "Methods" type would have for any given projected interface.

To pair each of these "Methods" interfaces, we have a "Methods" type for the mapped .NET interface type. For instance:

```csharp
public static class IEnumeratorMethods<T>
{
    public static T Current<TMethods>(WindowsRuntimeObjectReference objectReference)
        where TMethods : IIteratorMethods<T>
    {
        return TMethods.Current(objectReference);
    }

    public static bool HasCurrent<TMethods>(WindowsRuntimeObjectReference objectReference)
        where TMethods : IIteratorMethods<T>
    {
        return TMethods.HasCurrent(objectReference);
    }

    // Other methods...
}
```

These methods are also generic on a given "Methods" interface implementation, and can implement the mapped functionality on top of the underlying Windows Runtime interface logic, where needed. For `IEnumerator<T>`, the implementation is trivial, but for other interfaces (eg. `IList<T>`) there is a fair amount of additional "semantics adjustments" that is required, that can all be done here. This also allows CsWinRT to be versioned easily in this area, as all this logic on top of the underlying APIs is just in `WinRT.Runtime.dll`.

Lastly, we also need to introduce some additional building blocks to wire things up at marshalling time. First, a new interface:

```csharp
public interface IWindowsRuntimeUnsealedComWrappersCallback
{
    static abstract bool TryCreateObject(
        void* value,
        ReadOnlySpan<char> runtimeClassName,
        [NotNullWhen(true)] out object? result);
}
```

This is conceptually similar to `IWindowsRuntimeComWrappersCallback` (which is used for delegates and sealed runtime class names), with the main difference being that it is specialized for unsealed types (and interfaces). In this case, implementations need to know the runtime class name of the native object, to determine whether they can marshal it via the fast path using a statically visible RCW type (see below), or whether they should fallback to the logic to marshal arbitrary opaque object that is used when no static type information is available (which involves going through the interop type lookup to get the marshalling info, etc.).

And lastly, a new marshaller type to support this interface:

```csharp
public static class WindowsRuntimeUnsealedObjectMarshaller
{
    public static object? ConvertToManaged<TCallback>(void* value)
        where TCallback : IWindowsRuntimeUnsealedComWrappersCallback, allows ref struct;
}
```

This is conceptually very similar to `WindowsRuntimeDelegateMarshaller`, which uses `IWindowsRuntimeComWrappersCallback`.

## RCW (in `WinRT.Interop.dll`)

Moving on to `WinRT.Interop.dll`, the first generated type is an implementation for the "Methods" interface above:

```csharp
public abstract class IIterator_stringMethods : IIteratorMethods<string>
{
    public static string Current(WindowsRuntimeObjectReference objectReference)
    {
        throw new NotImplementedException();
    }

    public static bool HasCurrent(WindowsRuntimeObjectReference objectReference)
    {
        throw new NotImplementedException();
    }

    // Other methods...
}
```

This is where all the specialized invocation and marshalling logic will live, for each constructed generic interface.

To pair this "Methods" interface implementation, a "stub" non-generic implementation for associated the "Methods" type is also used:

```csharp
public static class IEnumerator_stringMethods
{
    public static string Current(WindowsRuntimeObjectReference objectReference)
    {
        return IEnumeratorMethods<string>.Current<IIterator_stringMethods>(objectReference);
    }

    // Other methods...
}
```

This is simply calling all interface methods through the mapped "Methods" type, with the generated "Methods" interface implementation as type argument. This is used by all actual consumers of this interface on the RCW side (eg. by projection code in any projection assembly), via `[UnsafeAccessor]` calls on this generated "Methods" type. Keeping these types non-generic makes invocations simpler too, as it avoids the additional complexity of handling generic type arguments of inaccessible types, which would require proxy types at each callsite.

For instance, here's an example of what a callsite from a projection assembly can look like, when needing to use any of these methods:

```csharp
[UnsafeAccessor(UnsafeAccessorKind.StaticMethod)]
public static extern string Current(
    [UnsafeAccessorType("ABI.System.Collections.Generic.IEnumerator_stringMethods, WinRT.Interop.dll")] object? _,
    WindowsRuntimeObjectReference objectReference);
```

With all these set up, we can now also generate the "NativeObject" type, which is the RCW implementation of the abstract type:

```csharp
public sealed class IEnumerator_stringNativeObject : WindowsRuntimeEnumerator<string>
{
    public IEnumerator_stringNativeObject(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference)
    {
    }

    protected override string CurrentNative => IEnumerator_stringMethods.Current(NativeObjectReference);

    protected override bool HasCurrentNative => IIterator_stringMethods.HasCurrent(NativeObjectReference);

    protected override bool MoveNextNative()
    {
        return IIterator_stringMethods.MoveNext(NativeObjectReference);
    }

    // Other methods...
}
```

This implements all "Native" methods forwarding the underlying implementation on the Windows Runtime interface, that the base type will use to implement the "semantics adjustment" for each given managed .NET interface. Of course, the exact methods will vary for each interface.

With this, we can now introduce the `IWindowsRuntimeUnsealedComWrappersCallback` implementation for this generic type instantiation:

```csharp
public abstract unsafe class IEnumerator_stringComWrappersCallback : IWindowsRuntimeUnsealedComWrappersCallback
{
    public static bool TryCreateObject(void* value, ReadOnlySpan<char> runtimeClassName, [NotNullWhen(true)] out object? result)
    {
        if (runtimeClassName.SequenceEqual("Windows.Foundation.Collections.IIterator`1<String>"))
        {
            WindowsRuntimeObjectReference objectReference = WindowsRuntimeObjectReference.CreateUnsafe(value, in IEnumerator_stringImpl.IID)!;

            result = new IEnumerator_stringNativeObject(objectReference);

            return true;
        }

        result = null;

        return false;
    }
}
```

This provides the RCW marshalling with a fast path when the runtime class name is an exact match. Note that when this is used, the internal `ComWrappers` implementation can still do several optimizations, such as flowing the initial interface pointer through, so that no additional `QueryInterface` call is needed if the fast path is taken, and only calling `GetRuntimeClassName` once, and reusing it even in case the fast path fails and the fallback logic has to be used (as the runtime class name cannot change for a given object).

Of course, this can only be used when static type information is available. We'll need an attribute when that is not the case:

```csharp
public sealed unsafe class IEnumerator_stringComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    public override unsafe ComInterfaceEntry* ComputeVtables(out int count)
    {
        throw new UnreachableException();
    }

    public override object CreateObject(void* value)
    {
        WindowsRuntimeObjectReference objectReference = WindowsRuntimeObjectReference.AsUnsafe(value, in IEnumerator_stringImpl.IID)!;

        return new IEnumerator_stringNativeObject(objectReference);
    }
}
```

This provides a similar implementation, with the main difference being the use of `AsUnsafe`, which performs a `QueryInterface` instead of wrapping the input interface pointer directly. This is because in this scenario we cannot make any assumption on which exact interface pointer we will receive (and can therefore not leverage the fast path we can use when static type information is available).

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

To marshal managed objects to native, we might also need some additional state to correctly adapt the semantics of the managed interfaces to the native ones. This might not be needed for all generic interfaces, but for those that do need additional state (such as `IEnumerator<T>`), this is what the adapter type would look like:

```csharp
public sealed class IEnumerator_stringAdapterImpl
{
    public IEnumerator_stringAdapterImpl(IEnumerator<string> value)
    {
    }

    public bool MoveNext()
    {
        // Adapted implementation
    }

    // Other methods...
}
```

This is a simple thing wrapper around a given interface instance, with additional state, and implementations of all necessary methods. It is not required for this wrapper to actually implement the interface itself, nor to implement all interface methods. Only the ones that require additional semantics modifications have to be implemented, the other methods can just be called directly by the marshalled CCWs. Also note that this adapter type would be implementing the native Windows Runtime interface (which is not projected), not the managed .NET interface.

Next, we need a table to associate these adapters to each managed object instance:

```csharp
public static class IEnumerator_stringAdapterImplTable
{
    private static readonly ConditionalWeakTable<IEnumerator<string>, IEnumerator_stringAdapterImpl> Table = [];

    public static IEnumerator_stringAdapterImpl GetAdapterImpl(IEnumerator<string> value)
    {
        return Table.GetValue(value, static value => new IEnumerator_stringAdapterImpl(value));
    }
}
```

This provides an efficient way to get or create adapters tied to each CCW target.

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
    private static HRESULT get_Current(void* thisPtr, bool* result)
    {
        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<IEnumerator<string>>((ComInterfaceDispatch*)thisPtr);

            *result = IEnumerator_stringImplAdapterTable.GetAdapterImpl(unboxedValue).Current();

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception ex)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
        }
    }

    // Other methods...
}
```

This will either invoke methods directly on the CCW target, if no additional adapter logic is needed, or invoke them on the adapter instance retrieved via the generated table. This will allow tracking any additional state per-CCW, like mentioned above (which is eg. required for properly marshalling `IEnumerator<T>` to native).

> [!NOTE]
> For interfaces that do not need additional state for adapting CCWs for native consumers, the "AdapterImpl" and "AdapterImplTable" types can be completely emitted. Rather, the CCW vtable methods would directly call the stateless adapter APIs via the generated "Methods" types.

## Marshalling

The only missing components now are the types to wire up marshalling. First, one for when static type information is available:

```csharp
public static unsafe class IEnumerator_stringMarshaller
{
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(IEnumerator<string>? value)
    {
        return WindowsRuntimeInterfaceMarshaller.ConvertToUnmanaged(value, in IEnumerator_stringImpl.IID);
    }

    public static IEnumerator<string>? ConvertToManaged(void* value)
    {
        return Unsafe.As<IEnumerator<string>>(WindowsRuntimeUnsealedObjectMarshaller.ConvertToManaged<IEnumerator_stringComWrappersCallback>(value));
    }
}
```

And along with this, also a proxy type to support opaque object marshalling scenarios:

```csharp
[WindowsRuntimeClassName("Windows.Foundation.Collections.IIterator`1<String>")]
[IEnumerator_stringComWrappersMarshaller]
public static class IEnumerator_string;
```

This code ensures that all scenarios mentioned at the start of the document can be supported, while also providing high performance, trim/AOT support, and allowing ILC to fully fold and pre-initialize vtables for all constructed interfaces, and all `ComInterfaceEntry` arrays for CCWs implementing any of these interfaces 🚀 
