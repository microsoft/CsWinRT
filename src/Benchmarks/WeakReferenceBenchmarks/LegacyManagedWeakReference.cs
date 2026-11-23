using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace WeakReferenceBenchmarks;

/// <summary>
/// A stand-in for the native <c>IUnknown</c> pointer that the real implementation gets by marshalling the target object
/// to a CCW (via <c>WindowsRuntimeComWrappers</c>). Both implementations use it, so that only the cost of the weak
/// reference machinery itself is being compared.
/// </summary>
internal static unsafe class FakeTarget
{
    private static readonly void* Instance = Create();

    public static void* GetComInterface(object target) => Instance;

    private static void* Create()
    {
        void** vtbl = (void**)NativeMemory.Alloc((nuint)(3 * sizeof(void*)));

        vtbl[0] = (delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int>)&QueryInterface;
        vtbl[1] = (delegate* unmanaged[MemberFunction]<void*, uint>)&AddRef;
        vtbl[2] = (delegate* unmanaged[MemberFunction]<void*, uint>)&Release;

        void** instance = (void**)NativeMemory.Alloc((nuint)sizeof(void*));

        instance[0] = vtbl;

        return instance;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvMemberFunction)])]
    private static int QueryInterface(void* @this, Guid* riid, void** ppv) => unchecked((int)0x80004002);

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvMemberFunction)])]
    private static uint AddRef(void* @this) => 2;

    [UnmanagedCallersOnly(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvMemberFunction)])]
    private static uint Release(void* @this) => 1;
}

/// <summary>
/// The previous implementation: a managed object exposed as a CCW through the default <see cref="StrategyBasedComWrappers"/>.
/// </summary>
[Guid("00000037-0000-0000-C000-000000000046")]
[GeneratedComInterface]
internal unsafe partial interface ILegacyWeakReference
{
    [PreserveSig]
    int Resolve(in Guid interfaceId, out void* weakReference);
}

/// <inheritdoc cref="ILegacyWeakReference"/>
[GeneratedComClass]
internal sealed unsafe partial class LegacyManagedWeakReference : ILegacyWeakReference
{
    private readonly WeakReference<object> _weakReference;

    public LegacyManagedWeakReference(object targetObject)
    {
        _weakReference = new WeakReference<object>(targetObject);
    }

    public static void* Create(object targetObject)
    {
        return (void*)ComInterfaceMarshaller<ILegacyWeakReference>.ConvertToUnmanaged(new LegacyManagedWeakReference(targetObject));
    }

    public int Resolve(in Guid interfaceId, out void* weakReference)
    {
        if (_weakReference.TryGetTarget(out object? targetObject))
        {
            void* thisPtr = FakeTarget.GetComInterface(targetObject);

            if (interfaceId == Guids.IUnknown)
            {
                weakReference = thisPtr;

                return 0;
            }

            int hresult = Native.QueryInterface(thisPtr, in interfaceId, out weakReference);

            _ = Native.Release(thisPtr);

            return hresult;
        }

        weakReference = null;

        return 0;
    }
}
