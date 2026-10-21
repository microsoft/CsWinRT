// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Contoso;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

/// <summary>
/// Native collection with distinct IVector/IMap and IIterable pointers and unnamed iterators.
/// </summary>
[WindowsRuntimeManagedOnlyType]
internal sealed unsafe class NativeCollection : IDisposable
{
    internal enum ElementKind { Int32, String, Pair }
    private enum ObjectKind { Collection, Iterator, Pair, Source }
    private const int E_NOINTERFACE = unchecked((int)0x80004002);
    private const int E_NOTIMPL = unchecked((int)0x80004001);
    private const int SourcePropertyCount = 11;

    private static readonly Guid ApiIid = new("20ad659c-7a8c-4e53-8347-f9c2242fb901");
    private static readonly Guid RequiredIid = new("417d2345-6a80-4a44-bff7-6abe92c5c141");
    private static readonly Guid RequiredMapIid = new("417d2345-6a80-4a44-bff7-6abe92c5c145");
    private static readonly Guid AmbiguousIid = new("417d2345-6a80-4a44-bff7-6abe92c5c143");
    private static readonly Guid UnknownIid = new("00000000-0000-0000-c000-000000000046");
    private static readonly Guid InspectableIid = new("af86e2e0-b12d-4c6a-9c5a-d7aa65101e90");
    private static readonly Guid AgileIid = new("94ea2b94-e9cc-49e0-c0ff-ee64ca8f5b90");
    private static readonly Guid BindableIid = new("036d2c08-df29-41af-8aa2-d774be62ba6f");
    private static readonly Guid BindableIteratorIid = new("6a1d6c07-076d-49f2-8314-f52c9c9a8331");
    // Independently computed parameterized IIDs, indexed by ElementKind.
    private static readonly Guid[] IterableIids =
    [
        new("81a643fb-f51c-5565-83c4-f96425777b66"),
        new("e2fcc7c1-3bfc-5a0b-b2b0-72e769d1cb7e"),
        new("8e5a70be-6911-5869-9749-4f8d50f27f85")
    ];
    private static readonly Guid[] IteratorIids =
    [
        new("bfea7f78-50c2-5f1d-a6ea-9e978d2699ff"),
        new("8c304ebb-6615-50a4-8829-879ecd443236"),
        new("41aa9c36-1926-5006-8366-01d7e3ca777b")
    ];
    private static readonly Guid[] VectorIids =
    [
        new("b939af5b-b45d-5489-9149-61442c1905fe"),
        new("98b9acc1-4b56-532e-ac73-03d5291cca90"),
        new("d1e47e8f-ac67-59a1-b78a-5e2582db7797")
    ];
    private static readonly Guid[] ViewIids =
    [
        new("8d720cdf-3934-5d3f-9a55-40e8063b086a"),
        new("2f13c006-a03a-5f69-b090-75a43e33423e"),
        new("1e479087-ee31-539b-bf14-0c11d5f05c81")
    ];
    private static readonly Guid MapIid = new("3835d7ac-fcf8-5c8b-9b5c-2b1bc42cec74");
    private static readonly Guid MapViewIid = new("3cb34a71-4741-59c3-a2d6-fa7a00c0e90c");
    private static readonly Guid ObservableVectorIid = new("95c02345-5b4a-5310-a6bf-f17e0d682da7");
    private static readonly Guid ObservableMapIid = new("9459c72c-afce-55f2-a08d-67cadd69cd4f");
    private static readonly Guid PairIid = new("c5d217e3-a987-583c-8253-74c40204c523");
    private static readonly void** InspectableVtable = CreateInspectableVtable(6);
    private static readonly void** CollectionVtable = CreateCollectionVtable();
    private static readonly void** IterableVtable = CreateIterableVtable();
    private static readonly void** IntIteratorVtable = CreateIteratorVtable(blittable: true);
    private static readonly void** ReferenceIteratorVtable = CreateIteratorVtable(blittable: false);
    private static readonly void** PairVtable = CreatePairVtable();
    private static readonly void** SourceVtable = CreateSourceVtable();
    private static int liveObjects;
    private readonly Instance* collection;
    private readonly Instance* source;

    public NativeCollection(ElementKind element = ElementKind.Int32, bool map = false, bool bindable = false,
        void* firstIterator = null)
    {
        collection = Create(ObjectKind.Collection, element, bindable);
        collection->Map = map;
        // The fixture takes ownership of the already-marshalled iterator reference.
        collection->FirstIterator = firstIterator;
        source = Create(ObjectKind.Source);
        source->ReturnedCollection = collection;
        Interlocked.Increment(ref collection->References);
    }

    public CollectionSource Source => ABI.Contoso.CollectionSourceMarshaller.ConvertToManaged(source);
    public static int LiveObjects => Volatile.Read(ref liveObjects);
    public int FirstCalls => collection->FirstCalls;
    public int IterableQueries => collection->IterableQueries;
    public int TrapCalls => collection->TrapCalls;
    public int FirstFailure { get => collection->FirstFailure; set => collection->FirstFailure = value; }
    public bool RejectTypedIterable { get => collection->RejectTypedIterable; set => collection->RejectTypedIterable = value; }
    public bool ReturnNullIterator { get => collection->ReturnNullIterator; set => collection->ReturnNullIterator = value; }

    public void Dispose()
    {
        ReleaseCore(source);
        ReleaseCore(collection);
    }

    private struct Interface
    {
        public void** Vtable;
        public Instance* Owner;
    }

    private struct Instance
    {
        public Interface Inspectable;
        public Interface Collection;
        public Interface Iterable;
        public int References;
        public ObjectKind Kind;
        public ElementKind Element;
        public bool Map;
        public bool Bindable;
        public bool RejectTypedIterable;
        public bool ReturnNullIterator;
        public int Index;
        public int FirstCalls;
        public int IterableQueries;
        public int TrapCalls;
        public int FirstFailure;
        public Instance* ReturnedCollection;
        public void* FirstIterator;
    }

    private static Instance* Create(ObjectKind kind, ElementKind element = ElementKind.Int32, bool bindable = false)
    {
        void** vtable = kind switch
        {
            ObjectKind.Source => SourceVtable,
            ObjectKind.Pair => PairVtable,
            ObjectKind.Iterator => element == ElementKind.Int32 && !bindable ? IntIteratorVtable : ReferenceIteratorVtable,
            _ => InspectableVtable
        };
        Instance* value = (Instance*)NativeMemory.AllocZeroed((nuint)sizeof(Instance));
        value->Inspectable = new Interface { Vtable = vtable, Owner = value };
        value->Collection = new Interface { Vtable = CollectionVtable, Owner = value };
        value->Iterable = new Interface { Vtable = IterableVtable, Owner = value };
        value->References = 1;
        value->Element = element;
        value->Kind = kind;
        value->Bindable = bindable;
        Interlocked.Increment(ref liveObjects);
        return value;
    }

    private static Instance* Owner(void* pointer) => ((Interface*)pointer)->Owner;

    private static uint ReleaseCore(Instance* value)
    {
        int count = Interlocked.Decrement(ref value->References);
        if (count == 0)
        {
            if (value->FirstIterator != null)
            {
                void* iterator = value->FirstIterator;
                _ = ((delegate* unmanaged[MemberFunction]<void*, uint>)(*(void***)iterator)[2])(iterator);
            }
            if (value->ReturnedCollection != null)
            {
                ReleaseCore(value->ReturnedCollection);
            }
            NativeMemory.Free(value);
            Interlocked.Decrement(ref liveObjects);
        }
        return (uint)count;
    }

    private static void** CreateInspectableVtable(int slots)
    {
        void** result = (void**)RuntimeHelpers.AllocateTypeAssociatedMemory(typeof(NativeCollection), slots * sizeof(void*));
        result[0] = (delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int>)&QueryInterface;
        result[1] = (delegate* unmanaged[MemberFunction]<void*, uint>)&AddRef;
        result[2] = (delegate* unmanaged[MemberFunction]<void*, uint>)&Release;
        result[3] = (delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, int>)&GetIids;
        result[4] = (delegate* unmanaged[MemberFunction]<void*, void**, int>)&GetName;
        result[5] = (delegate* unmanaged[MemberFunction]<void*, int*, int>)&GetTrust;
        return result;
    }

    private static void** CreateCollectionVtable()
    {
        void** result = CreateInspectableVtable(7);
        result[6] = (delegate* unmanaged[MemberFunction]<void*, uint, void*, int>)&UnexpectedCollectionCall;
        return result;
    }

    private static void** CreateIterableVtable()
    {
        void** result = CreateInspectableVtable(7);
        result[6] = (delegate* unmanaged[MemberFunction]<void*, void**, int>)&First;
        return result;
    }

    private static void** CreateIteratorVtable(bool blittable)
    {
        void** result = CreateInspectableVtable(9);
        result[6] = blittable
            ? (delegate* unmanaged[MemberFunction]<void*, int*, int>)&IntCurrent
            : (delegate* unmanaged[MemberFunction]<void*, void**, int>)&ReferenceCurrent;
        result[7] = (delegate* unmanaged[MemberFunction]<void*, byte*, int>)&HasCurrent;
        result[8] = (delegate* unmanaged[MemberFunction]<void*, byte*, int>)&MoveNext;
        return result;
    }

    private static void** CreatePairVtable()
    {
        void** result = CreateInspectableVtable(8);
        result[6] = (delegate* unmanaged[MemberFunction]<void*, int*, int>)&PairKey;
        result[7] = (delegate* unmanaged[MemberFunction]<void*, void**, int>)&PairValue;
        return result;
    }

    private static void** CreateSourceVtable()
    {
        void** result = CreateInspectableVtable(6 + SourcePropertyCount);
        // ICollectionSource contains only collection getters, in metadata declaration order.
        for (int slot = 6; slot < 6 + SourcePropertyCount; slot++)
        {
            result[slot] = (delegate* unmanaged[MemberFunction]<void*, void**, int>)&GetCollection;
        }
        return result;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int QueryInterface(void* pointer, Guid* iid, void** result)
    {
        Instance* owner = Owner(pointer);
        *result = null;
        if (*iid == UnknownIid || *iid == InspectableIid || *iid == AgileIid ||
            owner->Kind is ObjectKind.Collection or ObjectKind.Source && *iid == ApiIid)
        {
            // Collections expose ICollectionSource only for wrapper identity; its members are called only on Source.
            *result = &owner->Inspectable;
        }
        else if (owner->Kind == ObjectKind.Pair && *iid == PairIid ||
            owner->Kind == ObjectKind.Iterator && *iid == (owner->Bindable ? BindableIteratorIid : IteratorIids[(int)owner->Element]))
        {
            *result = &owner->Inspectable;
        }
        else if (owner->Kind == ObjectKind.Collection)
        {
            if (*iid == RequiredIid || *iid == RequiredMapIid || *iid == AmbiguousIid)
            {
                *result = &owner->Inspectable;
            }
            else if (owner->Bindable && *iid == BindableIid)
            {
                *result = &owner->Iterable;
            }
            else if (*iid == IterableIids[(int)owner->Element])
            {
                Interlocked.Increment(ref owner->IterableQueries);
                if (!owner->RejectTypedIterable)
                {
                    *result = &owner->Iterable;
                }
            }
            else if (owner->Map
                ? *iid == MapIid || *iid == MapViewIid || *iid == ObservableMapIid
                : *iid == VectorIids[(int)owner->Element] || *iid == ViewIids[(int)owner->Element] ||
                    owner->Element == ElementKind.Int32 && *iid == ObservableVectorIid)
            {
                *result = &owner->Collection;
            }
        }
        if (*result == null)
        {
            return E_NOINTERFACE;
        }
        Interlocked.Increment(ref owner->References);
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static uint AddRef(void* pointer) => (uint)Interlocked.Increment(ref Owner(pointer)->References);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static uint Release(void* pointer) => ReleaseCore(Owner(pointer));

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int GetIids(void* pointer, uint* count, Guid** result)
    {
        *count = 0;
        *result = null;
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int GetName(void* pointer, void** result)
    {
        *result = Owner(pointer)->Kind is ObjectKind.Iterator or ObjectKind.Pair
            ? null : HStringMarshaller.ConvertToUnmanaged("Contoso.CollectionSource".AsSpan());
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int GetTrust(void* pointer, int* result)
    {
        *result = 0;
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int GetCollection(void* pointer, void** result)
    {
        Instance* collection = Owner(pointer)->ReturnedCollection;
        Interlocked.Increment(ref collection->References);
        *result = &collection->Collection;
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int UnexpectedCollectionCall(void* pointer, uint index, void* result)
    {
        Interlocked.Increment(ref Owner(pointer)->TrapCalls);
        return E_NOTIMPL;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int First(void* pointer, void** result)
    {
        Instance* owner = Owner(pointer);
        Interlocked.Increment(ref owner->FirstCalls);
        *result = null;
        if (owner->FirstFailure != 0)
        {
            return owner->FirstFailure;
        }
        if (owner->ReturnNullIterator)
        {
            return 0;
        }
        if (owner->FirstIterator != null)
        {
            void* iterator = owner->FirstIterator;
            _ = ((delegate* unmanaged[MemberFunction]<void*, uint>)(*(void***)iterator)[1])(iterator);
            *result = iterator;
            return 0;
        }
        *result = Create(ObjectKind.Iterator, owner->Element, owner->Bindable);
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int IntCurrent(void* pointer, int* result)
    {
        *result = (Owner(pointer)->Index + 1) * 10;
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int ReferenceCurrent(void* pointer, void** result)
    {
        Instance* owner = Owner(pointer);
        if (owner->Bindable)
        {
            *result = null;
        }
        else if (owner->Element == ElementKind.String)
        {
            *result = HStringMarshaller.ConvertToUnmanaged(("item" + owner->Index).AsSpan());
        }
        else
        {
            Instance* pair = Create(ObjectKind.Pair);
            pair->Index = owner->Index;
            *result = pair;
        }
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int PairKey(void* pointer, int* result)
    {
        *result = Owner(pointer)->Index;
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int PairValue(void* pointer, void** result)
    {
        *result = HStringMarshaller.ConvertToUnmanaged(("item" + Owner(pointer)->Index).AsSpan());
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int HasCurrent(void* pointer, byte* result)
    {
        *result = (byte)(Owner(pointer)->Index < 3 ? 1 : 0);
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int MoveNext(void* pointer, byte* result)
    {
        *result = (byte)(++Owner(pointer)->Index < 3 ? 1 : 0);
        return 0;
    }
}
