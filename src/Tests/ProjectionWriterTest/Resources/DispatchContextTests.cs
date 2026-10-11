// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Contoso;
using WindowsRuntime.InteropServices;

[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Runtime")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Interop")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Sdk.Projection")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeMetadataTypeMapGroup>("WinRT.Runtime")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeMetadataTypeMapGroup>("WinRT.Interop")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeMetadataTypeMapGroup>("WinRT.Sdk.Projection")]
[assembly: TypeMapAssemblyTarget<DynamicInterfaceCastableImplementationTypeMapGroup>("WinRT.Runtime")]
[assembly: TypeMapAssemblyTarget<DynamicInterfaceCastableImplementationTypeMapGroup>("WinRT.Interop")]
[assembly: TypeMapAssemblyTarget<DynamicInterfaceCastableImplementationTypeMapGroup>("WinRT.Sdk.Projection")]
[assembly: DisableRuntimeMarshalling]
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows6.3")]

/*DECLARATIONS*/

public static unsafe class Program
{
    private static object Value = null!;

    public static int Main(string[] args)
    {
#if !NATIVEAOT
        // Load this standalone fixture's generated sidecar before creating runtime type maps.
        _ = System.Reflection.Assembly.Load("WinRT.Interop");
#endif
        void* native = NativeVector.Create();
        void* typed = null;
        Guid iid = NativeVector.VectorIid;
        Marshal.ThrowExceptionForHR(NativeVector.QueryRaw(native, &iid, &typed));
        if (typed == null)
        {
            return 101;
        }

        NativeVector.ReleaseRaw(typed);
        Console.WriteLine("Raw native QI succeeded.");
        NativeVector.VectorQueries = 0;
        Value = WindowsRuntimeMarshal.ConvertToManaged(native)!;
        if (Value.GetType().Name != "WindowsRuntimeInspectable")
        {
            return 102;
        }

        bool cast = args.Length > 0 && args[0] == "cast";
        bool result;
        try
        {
            result = /*INVOCATION*/;
            Console.WriteLine($"Result={result}; VectorQueries={NativeVector.VectorQueries}");
        }
        catch (InvalidCastException)
        {
            Console.WriteLine($"InvalidCastException; VectorQueries={NativeVector.VectorQueries}");
            result = false;
        }

        NativeVector.ReleaseRaw(native);
        GC.KeepAlive(Value);
        return result && NativeVector.VectorQueries == 1 ? 0 : 103;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Build<T>(bool cast) => Cast<IList<T>>(cast);

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Cast<T>(bool cast)
    {
        if (cast)
        {
            object converted = (T)Value!;
            return ReferenceEquals(converted, Value);
        }

        return Value is T;
    }

    /*CALLERS*/
}

public static unsafe class NativeVector
{
    // pinterface(IVector;enum(Contoso.DispatchElement;i4)), fixed independently of generated maps.
    public static readonly Guid VectorIid = new("1f13610e-00a2-5c5f-8f1d-72afd7ccc7ad");
    private static readonly Guid Unknown = new("00000000-0000-0000-c000-000000000046");
    private static readonly Guid Inspectable = new("af86e2e0-b12d-4c6a-9c5a-d7aa65101e90");
    private static readonly Guid Agile = new("94ea2b94-e9cc-49e0-c0ff-ee64ca8f5b90");
    public static int VectorQueries;

    private struct Instance
    {
        public void** Vtable;
        public int References;
    }

    public static void* Create()
    {
        void** table = (void**)NativeMemory.AllocZeroed((nuint)(18 * sizeof(void*)));
        table[0] = (void*)(delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int>)&QueryInterface;
        table[1] = (void*)(delegate* unmanaged[MemberFunction]<void*, uint>)&AddRef;
        table[2] = (void*)(delegate* unmanaged[MemberFunction]<void*, uint>)&Release;
        table[3] = (void*)(delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, int>)&GetIids;
        table[4] = (void*)(delegate* unmanaged[MemberFunction]<void*, void**, int>)&GetRuntimeClassName;
        table[5] = (void*)(delegate* unmanaged[MemberFunction]<void*, int*, int>)&GetTrustLevel;
        table[6] = (void*)(delegate* unmanaged[MemberFunction]<void*, uint, int*, int>)&GetAt;
        table[7] = (void*)(delegate* unmanaged[MemberFunction]<void*, uint*, int>)&GetSize;
        Instance* instance = (Instance*)NativeMemory.AllocZeroed((nuint)sizeof(Instance));
        instance->Vtable = table;
        instance->References = 1;
        return instance;
    }

    public static int QueryRaw(void* instance, Guid* iid, void** result) =>
        ((delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int>)(*(void***)instance)[0])(instance, iid, result);

    public static uint ReleaseRaw(void* instance) =>
        ((delegate* unmanaged[MemberFunction]<void*, uint>)(*(void***)instance)[2])(instance);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int QueryInterface(void* instance, Guid* iid, void** result)
    {
        if (*iid == Unknown || *iid == Inspectable || *iid == Agile || *iid == VectorIid)
        {
            if (*iid == VectorIid)
            {
                VectorQueries++;
            }

            *result = instance;
            _ = AddRefCore(instance);
            return 0;
        }

        *result = null;
        return unchecked((int)0x80004002);
    }

    private static uint AddRefCore(void* instance) => (uint)Interlocked.Increment(ref ((Instance*)instance)->References);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static uint AddRef(void* instance) => AddRefCore(instance);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static uint Release(void* instance)
    {
        Instance* value = (Instance*)instance;
        int remaining = Interlocked.Decrement(ref value->References);
        if (remaining == 0)
        {
            NativeMemory.Free(value->Vtable);
            NativeMemory.Free(value);
        }

        return (uint)remaining;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int GetIids(void* instance, uint* count, Guid** iids) { *count = 0; *iids = null; return 0; }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int GetRuntimeClassName(void* instance, void** name) { *name = null; return unchecked((int)0x80004001); }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int GetTrustLevel(void* instance, int* level) { *level = 0; return 0; }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int GetAt(void* instance, uint index, int* value) { *value = 1; return 0; }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int GetSize(void* instance, uint* count) { *count = 3; return 0; }
}
