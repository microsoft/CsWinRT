using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WeakReferenceBenchmarks;

/// <summary>
/// The new implementation: a thin native object holding a weak GC handle. This mirrors
/// <c>ManagedWeakReference</c> and <c>ManagedWeakReferenceImpl</c> in <c>src\WinRT.Runtime</c>.
/// </summary>
internal unsafe struct ThinManagedWeakReference
{
    private void** _vftbl;
    private nint _weakHandle;
    private volatile uint _referenceCount;

    public static ThinManagedWeakReference* Create(object targetObject)
    {
        WeakGCHandle<object> weakHandle = new(targetObject);

        ThinManagedWeakReference* @this = (ThinManagedWeakReference*)NativeMemory.Alloc((nuint)sizeof(ThinManagedWeakReference));

        @this->_vftbl = (void**)ThinManagedWeakReferenceImpl.Vtable;
        @this->_weakHandle = WeakGCHandle<object>.ToIntPtr(weakHandle);
        @this->_referenceCount = 1;

        return @this;
    }

    public int QueryInterface(Guid* riid, void** ppvObject)
    {
        if (ppvObject is null)
        {
            return unchecked((int)0x80004003);
        }

        if (riid is not null && (*riid == Guids.IUnknown || *riid == Guids.IWeakReference))
        {
            _ = Interlocked.Increment(ref _referenceCount);

            *ppvObject = Unsafe.AsPointer(ref this);

            return 0;
        }

        *ppvObject = null;

        return unchecked((int)0x80004002);
    }

    public uint AddRef() => Interlocked.Increment(ref _referenceCount);

    public uint Release()
    {
        uint referenceCount = Interlocked.Decrement(ref _referenceCount);

        if (referenceCount == 0)
        {
            WeakGCHandle<object>.FromIntPtr(_weakHandle).Dispose();

            NativeMemory.Free(Unsafe.AsPointer(ref this));
        }

        return referenceCount;
    }

    public readonly int Resolve(Guid* riid, void** objectReference)
    {
        if (riid is null || objectReference is null)
        {
            return unchecked((int)0x80004003);
        }

        *objectReference = null;

        if (!WeakGCHandle<object>.FromIntPtr(_weakHandle).TryGetTarget(out object? targetObject))
        {
            return 0;
        }

        void* thisPtr = FakeTarget.GetComInterface(targetObject);

        if (*riid == Guids.IUnknown)
        {
            *objectReference = thisPtr;

            return 0;
        }

        int hresult = Native.QueryInterface(thisPtr, in *riid, out *objectReference);

        _ = Native.Release(thisPtr);

        return hresult;
    }
}

internal static unsafe class ThinManagedWeakReferenceImpl
{
    [FixedAddressValueType]
    private static readonly Vftbl Table;

    static ThinManagedWeakReferenceImpl()
    {
        Table.QueryInterface = &QueryInterface;
        Table.AddRef = &AddRef;
        Table.Release = &Release;
        Table.Resolve = &Resolve;
    }

    public static nint Vtable => (nint)Unsafe.AsPointer(in Table);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int QueryInterface(void* @this, Guid* riid, void** ppvObject) => ((ThinManagedWeakReference*)@this)->QueryInterface(riid, ppvObject);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static uint AddRef(void* @this) => ((ThinManagedWeakReference*)@this)->AddRef();

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static uint Release(void* @this) => ((ThinManagedWeakReference*)@this)->Release();

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int Resolve(void* @this, Guid* riid, void** objectReference)
    {
        try
        {
            return ((ThinManagedWeakReference*)@this)->Resolve(riid, objectReference);
        }
        catch (Exception e)
        {
            return e.HResult;
        }
    }

    private struct Vftbl
    {
        public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int> QueryInterface;
        public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
        public delegate* unmanaged[MemberFunction]<void*, uint> Release;
        public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int> Resolve;
    }
}
