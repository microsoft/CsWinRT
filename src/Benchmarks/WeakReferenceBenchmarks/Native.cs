using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WeakReferenceBenchmarks;

internal static class Guids
{
    public static readonly Guid IUnknown = new("00000000-0000-0000-C000-000000000046");
    public static readonly Guid IWeakReference = new("00000037-0000-0000-C000-000000000046");
}

/// <summary>
/// Helpers to call into a native COM object through its vtable.
/// </summary>
internal static unsafe class Native
{
    public static int QueryInterface(void* thisPtr, in Guid iid, out void* result)
    {
        fixed (Guid* pIid = &iid)
        fixed (void** pResult = &result)
        {
            return ((delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int>)(*(void***)thisPtr)[0])(thisPtr, pIid, pResult);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint AddRef(void* thisPtr) => ((delegate* unmanaged[MemberFunction]<void*, uint>)(*(void***)thisPtr)[1])(thisPtr);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Release(void* thisPtr) => ((delegate* unmanaged[MemberFunction]<void*, uint>)(*(void***)thisPtr)[2])(thisPtr);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Resolve(void* thisPtr, Guid* iid, void** result) => ((delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int>)(*(void***)thisPtr)[3])(thisPtr, iid, result);
}
