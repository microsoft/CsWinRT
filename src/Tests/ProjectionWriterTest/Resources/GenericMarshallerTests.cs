// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Contoso;
using Windows.Foundation;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

[assembly: System.Runtime.Versioning.TargetFramework(".NETCoreApp,Version=v10.0")]
[assembly: System.Runtime.Versioning.SupportedOSPlatform("windows6.3")]
[assembly: DisableRuntimeMarshalling]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Interop")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Runtime")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Projection")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeComWrappersTypeMapGroup>("WinRT.Sdk.Projection")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeMetadataTypeMapGroup>("WinRT.Interop")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeMetadataTypeMapGroup>("WinRT.Runtime")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeMetadataTypeMapGroup>("WinRT.Projection")]
[assembly: TypeMapAssemblyTarget<WindowsRuntimeMetadataTypeMapGroup>("WinRT.Sdk.Projection")]
[assembly: TypeMapAssemblyTarget<DynamicInterfaceCastableImplementationTypeMapGroup>("WinRT.Interop")]
[assembly: TypeMapAssemblyTarget<DynamicInterfaceCastableImplementationTypeMapGroup>("WinRT.Runtime")]
[assembly: TypeMapAssemblyTarget<DynamicInterfaceCastableImplementationTypeMapGroup>("WinRT.Projection")]
[assembly: TypeMapAssemblyTarget<DynamicInterfaceCastableImplementationTypeMapGroup>("WinRT.Sdk.Projection")]

internal static unsafe class Program
{
    private static readonly Guid Key = new("eed0a268-a995-4d78-a90e-42d357e92692");
    private static readonly Guid TileIid = new("20ad659c-7a8c-4e53-8347-f9c2242fb901");
    private static readonly Guid RegistrationIid = new("20ad659c-7a8c-4e53-8347-f9c2242fb902");
    // Independently computed WinRT interface IDs for 'IAsyncOperation<TileCollection>' and 'IMapView<Guid, ITaskRegistration>'.
    private static readonly Guid AsyncIid = new("fee18492-5b15-5d03-9530-b49f24b4fa8a");
    private static readonly Guid MapIid = new("1d0bf9ae-c8d5-5474-8e8b-efbb474ed07c");
    private static Instance* tiles;
    private static Instance* operation;
    private static Instance* map;
    private static Instance* registration;
    private static void* changed;
    private static void* guidChanged;
    private static int nativeCalls;
    private static IReadOnlyDictionary<Guid, IReadOnlyDictionary<Guid, ITaskRegistration>> nestedTasks;
    private static EventHandler<IReadOnlyDictionary<Guid, ITaskRegistration>> nestedHandler;

    public static int Main()
    {
        tiles = Create(TileIid, 19);
        operation = Create(AsyncIid, 9);
        map = Create(MapIid, 10);
        registration = Create(RegistrationIid, 7);
        tiles->Vtable[6] = (delegate* unmanaged[MemberFunction]<void*, void**, int>)&GetTiles;
        tiles->Vtable[7] = (delegate* unmanaged[MemberFunction]<void*, int, void**, int>)&GetTilesWithOptions;
        tiles->Vtable[8] = (delegate* unmanaged[MemberFunction]<void*, void**, int>)&GetMap;
        tiles->Vtable[9] = tiles->Vtable[10] = (delegate* unmanaged[MemberFunction]<void*, void**, int>)&GetNull;
        tiles->Vtable[11] = (delegate* unmanaged[MemberFunction]<void*, void*, long*, int>)&AddChanged;
        tiles->Vtable[12] = (delegate* unmanaged[MemberFunction]<void*, long, int>)&RemoveChanged;
        tiles->Vtable[13] = (delegate* unmanaged[MemberFunction]<void*, void*, long*, int>)&AddGuidChanged;
        tiles->Vtable[14] = (delegate* unmanaged[MemberFunction]<void*, long, int>)&RemoveGuidChanged;
        tiles->Vtable[15] = (delegate* unmanaged[MemberFunction]<void*, uint, Guid*, uint*, Guid**, int>)&EchoIds;
        tiles->Vtable[16] = (delegate* unmanaged[MemberFunction]<void*, uint, void**, uint*, void***, int>)&EchoInterfaces;
        tiles->Vtable[17] = (delegate* unmanaged[MemberFunction]<void*, uint, void**, int>)&AcceptMaps;
        tiles->Vtable[18] = (delegate* unmanaged[MemberFunction]<void*, uint, void**, int>)&AcceptHandlers;
        operation->Vtable[8] = (delegate* unmanaged[MemberFunction]<void*, void**, int>)&GetResults;
        map->Vtable[6] = (delegate* unmanaged[MemberFunction]<void*, Guid, void**, int>)&Lookup;
        map->Vtable[7] = (delegate* unmanaged[MemberFunction]<void*, uint*, int>)&Size;
        map->Vtable[8] = (delegate* unmanaged[MemberFunction]<void*, Guid, byte*, int>)&HasKey;
        registration->Vtable[6] = (delegate* unmanaged[MemberFunction]<void*, Guid*, int>)&GetId;

        try
        {
            TileCollection value = ABI.Contoso.TileCollectionMarshaller.ConvertToManaged(tiles);
            IAsyncOperation<TileCollection> asyncValue = value.GetTilesAsync();
            Check(ReferenceEquals(value, asyncValue.GetResults()), "async runtime-class result");
            Check(ReferenceEquals(value, value.GetTilesAsync(42).GetResults()), "overloaded async method");

            IReadOnlyDictionary<Guid, ITaskRegistration> tasks = value.AllTasks;
            Check(tasks.Count == 1 && tasks.TryGetValue(Key, out ITaskRegistration task) && task.Id == Key, "Guid-keyed native map");
            Check(!tasks.TryGetValue(Guid.Empty, out _), "native map missing key");
            nestedTasks = value.NestedTasks;
            nestedHandler = value.NestedHandler;
            Check(nestedTasks is null && nestedHandler is null, "nested Guid generic accessors");

            int eventCount = 0;
            EventHandler<TileCollection, Guid> typedHandler = (sender, argument) =>
            {
                Check(ReferenceEquals(value, sender) && argument == Key, "typed event arguments");
                eventCount++;
            };
            EventHandler<Guid> handler = (sender, argument) =>
            {
                Check(ReferenceEquals(value, sender) && argument == Key, "Guid event arguments");
                eventCount++;
            };
            value.Changed += typedHandler;
            value.GuidChanged += handler;
            Raise(changed);
            Raise(guidChanged);
            value.Changed -= typedHandler;
            value.GuidChanged -= handler;
            Check(eventCount == 2 && changed is null && guidChanged is null, "generic event-source construction and removal");

            Guid[] ids = [Key, Guid.Empty];
            Check(value.EchoIds(ids).AsSpan().SequenceEqual(ids), "top-level Guid array");
            Check(ReferenceEquals(value, value.EchoTiles([value])[0]), "runtime-class array");
            value.AcceptMaps([tasks]);
            value.AcceptHandlers([handler]);
            Check(eventCount == 3, "Guid delegate array invocation");
            Check(nativeCalls >= 12, "native fixture was invoked");
            Console.WriteLine("Generic marshaller accessors passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
        finally
        {
            ReleaseCore(tiles);
            ReleaseCore(operation);
            ReleaseCore(map);
            ReleaseCore(registration);
        }
    }

    private struct Instance
    {
        public void** Vtable;
        public int References;
        public Guid Iid;
    }

    private static Instance* Create(Guid iid, int slots)
    {
        Instance* instance = (Instance*)NativeMemory.AllocZeroed((nuint)sizeof(Instance));
        instance->Vtable = (void**)NativeMemory.AllocZeroed((nuint)slots, (nuint)sizeof(void*));
        instance->References = 1;
        instance->Iid = iid;
        instance->Vtable[0] = (delegate* unmanaged[MemberFunction]<void*, Guid*, void**, int>)&QueryInterface;
        instance->Vtable[1] = (delegate* unmanaged[MemberFunction]<void*, uint>)&AddRef;
        instance->Vtable[2] = (delegate* unmanaged[MemberFunction]<void*, uint>)&Release;
        instance->Vtable[3] = (delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, int>)&GetIids;
        instance->Vtable[4] = (delegate* unmanaged[MemberFunction]<void*, void**, int>)&GetRuntimeClassName;
        instance->Vtable[5] = (delegate* unmanaged[MemberFunction]<void*, int*, int>)&GetTrustLevel;
        return instance;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static int Return(Instance* instance, void** result)
    {
        Interlocked.Increment(ref instance->References);
        nativeCalls++;
        *result = instance;
        return 0;
    }

    private static uint ReleaseCore(Instance* instance)
    {
        int count = Interlocked.Decrement(ref instance->References);
        if (count == 0)
        {
            NativeMemory.Free(instance->Vtable);
            NativeMemory.Free(instance);
        }
        return (uint)count;
    }

    private static void Raise(void* handler)
    {
        Check(handler is not null, "native event registration");
        int hr = ((delegate* unmanaged[MemberFunction]<void*, void*, Guid, int>)(*(void***)handler)[3])(handler, tiles, Key);
        Marshal.ThrowExceptionForHR(hr);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int QueryInterface(void* self, Guid* iid, void** result)
    {
        Instance* instance = (Instance*)self;
        if (*iid == instance->Iid || *iid == new Guid("00000000-0000-0000-c000-000000000046")
            || *iid == new Guid("af86e2e0-b12d-4c6a-9c5a-d7aa65101e90")
            || *iid == new Guid("94ea2b94-e9cc-49e0-c0ff-ee64ca8f5b90"))
        {
            return Return(instance, result);
        }
        *result = null;
        return unchecked((int)0x80004002);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static uint AddRef(void* self) => (uint)Interlocked.Increment(ref ((Instance*)self)->References);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static uint Release(void* self) => ReleaseCore((Instance*)self);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int GetIids(void* self, uint* count, Guid** iids) { *count = 0; *iids = null; return 0; }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int GetRuntimeClassName(void* self, void** name)
    {
        *name = self == tiles ? HStringMarshaller.ConvertToUnmanaged("Contoso.TileCollection") : null;
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int GetTrustLevel(void* self, int* level) { *level = 0; return 0; }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int GetTiles(void* self, void** result) => Return(operation, result);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int GetTilesWithOptions(void* self, int options, void** result)
    {
        if (options == 42) { return Return(operation, result); }
        *result = null;
        return unchecked((int)0x80070057);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int GetResults(void* self, void** result) => Return(tiles, result);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int GetMap(void* self, void** result) => Return(map, result);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int GetNull(void* self, void** result) { *result = null; nativeCalls++; return 0; }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int Lookup(void* self, Guid key, void** result)
    {
        if (key == Key) { return Return(registration, result); }
        *result = null;
        return unchecked((int)0x8000000b);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int Size(void* self, uint* result) { *result = 1; nativeCalls++; return 0; }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int HasKey(void* self, Guid key, byte* result) { *result = key == Key ? (byte)1 : (byte)0; return 0; }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int GetId(void* self, Guid* result) { *result = Key; nativeCalls++; return 0; }

    private static int Subscribe(void* handler, long* token, ref void* field)
    {
        ((delegate* unmanaged[MemberFunction]<void*, uint>)(*(void***)handler)[1])(handler);
        field = handler;
        *token = 1;
        return 0;
    }

    private static int Unsubscribe(ref void* field)
    {
        WindowsRuntimeMarshal.Free(field);
        field = null;
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int AddChanged(void* self, void* handler, long* token) => Subscribe(handler, token, ref changed);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int RemoveChanged(void* self, long token) => Unsubscribe(ref changed);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int AddGuidChanged(void* self, void* handler, long* token) => Subscribe(handler, token, ref guidChanged);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int RemoveGuidChanged(void* self, long token) => Unsubscribe(ref guidChanged);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int EchoIds(void* self, uint count, Guid* values, uint* length, Guid** result)
    {
        *length = count;
        *result = (Guid*)Marshal.AllocCoTaskMem(checked((int)count * sizeof(Guid)));
        new ReadOnlySpan<Guid>(values, (int)count).CopyTo(new Span<Guid>(*result, (int)count));
        nativeCalls++;
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int AcceptMaps(void* self, uint count, void** values)
    {
        if (count != 1 || values[0] is null)
        {
            return unchecked((int)0x80070057);
        }
        uint size = 0;
        int hr = ((delegate* unmanaged[MemberFunction]<void*, uint*, int>)(*(void***)values[0])[7])(values[0], &size);
        nativeCalls++;
        return hr < 0 ? hr : size == 1 ? 0 : unchecked((int)0x80070057);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int AcceptHandlers(void* self, uint count, void** values)
    {
        if (count != 1 || values[0] is null)
        {
            return unchecked((int)0x80070057);
        }
        nativeCalls++;
        return ((delegate* unmanaged[MemberFunction]<void*, void*, Guid, int>)(*(void***)values[0])[3])(values[0], tiles, Key);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int EchoInterfaces(void* self, uint count, void** values, uint* length, void*** result)
    {
        *length = count;
        *result = (void**)Marshal.AllocCoTaskMem(checked((int)count * sizeof(void*)));
        for (int i = 0; i < count; i++)
        {
            (*result)[i] = values[i];
            ((delegate* unmanaged[MemberFunction]<void*, uint>)(*(void***)values[i])[1])(values[i]);
        }
        nativeCalls++;
        return 0;
    }
}
