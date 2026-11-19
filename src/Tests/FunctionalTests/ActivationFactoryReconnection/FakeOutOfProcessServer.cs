// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

namespace ActivationFactoryReconnection;

/// <summary>
/// The Windows Runtime classes whose activation factories are provided by <see cref="FakeOutOfProcessServer"/>.
/// </summary>
internal enum ServerClass
{
    /// <summary><c>Windows.Data.Json.JsonObject</c> (default constructor, <c>IActivationFactory</c>).</summary>
    JsonObject,

    /// <summary><c>Windows.Globalization.Language</c> (factory constructor, <c>ILanguageFactory</c>).</summary>
    Language,

    /// <summary><c>Windows.Data.Json.JsonValue</c> (static method, <c>IJsonValueStatics</c>).</summary>
    JsonValue,

    /// <summary><c>Windows.Globalization.ApplicationLanguages</c> (static property, <c>IApplicationLanguagesStatics</c>, not agile).</summary>
    ApplicationLanguages,

    /// <summary><c>Windows.Networking.Connectivity.NetworkInformation</c> (static event, <c>INetworkInformationStatics</c>).</summary>
    NetworkInformation,

    /// <summary><c>TestComponent.Composable</c> (composable constructors, <c>IComposableFactory</c>).</summary>
    Composable
}

/// <summary>
/// Simulates out-of-process servers providing activation factories for some Windows Runtime classes (one server per class).
/// </summary>
/// <remarks>
/// Each activation factory is a native object behaving like a COM proxy, which either forwards calls to the real activation
/// factory, or implements them directly, as long as its server is alive. Once a server is killed, every call on its existing
/// activation factories fails with <see cref="FailureHResult"/> (<c>RPC_S_SERVER_UNAVAILABLE</c> by default), like real proxies
/// do. Retrieving the activation factory again "starts" a new server, so the new activation factory works correctly.
/// </remarks>
internal static unsafe partial class FakeOutOfProcessServer
{
    public const int RPC_E_SERVER_DIED = unchecked((int)0x80010007);
    public const int RPC_E_SERVER_DIED_DNE = unchecked((int)0x80010012);
    public const int RPC_E_DISCONNECTED = unchecked((int)0x80010108);
    public const int RPC_S_SERVER_UNAVAILABLE = unchecked((int)0x800706BA);
    public const int RPC_S_CALL_FAILED = unchecked((int)0x800706BE);
    public const int RPC_S_CALL_FAILED_DNE = unchecked((int)0x800706BF);

    private const int S_OK = 0;
    private const int E_NOINTERFACE = unchecked((int)0x80004002);
    private const int E_NOTIMPL = unchecked((int)0x80004001);
    private const int E_INVALIDARG = unchecked((int)0x80070057);
    private const int REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);

    private const int ServerClassCount = (int)ServerClass.Composable + 1;

    private static readonly Guid IID_ILanguageFactory = new("9B0252AC-0C27-44F8-B792-9793FB66C63E");
    private static readonly Guid IID_IJsonValueStatics = new("5F6B544A-2F53-48E1-91A3-F78B50A6345C");
    private static readonly Guid IID_IApplicationLanguagesStatics = new("75B40847-0A4C-4A92-9565-FD63C95F7AED");
    private static readonly Guid IID_INetworkInformationStatics = new("5074F851-950D-4165-9C15-365619481EEA");
    private static readonly Guid IID_IComposableFactory = new("B7C48344-637C-5FBC-A7F7-1A27FE08CF6B");

    private static readonly void** ActivationFactoryVftbl = CreateVftbl(7, (6, (nint)(delegate* unmanaged[MemberFunction]<FakeActivationFactory*, void**, int>)&ActivateInstance));
    private static readonly void** LanguageFactoryVftbl = CreateVftbl(7, (6, (nint)(delegate* unmanaged[MemberFunction]<FakeActivationFactory*, void*, void**, int>)&CreateLanguage));
    private static readonly void** JsonValueStaticsVftbl = CreateVftbl(11, (9, (nint)(delegate* unmanaged[MemberFunction]<FakeActivationFactory*, double, void**, int>)&CreateNumberValue));
    private static readonly void** ApplicationLanguagesStaticsVftbl = CreateVftbl(10, (6, (nint)(delegate* unmanaged[MemberFunction]<FakeActivationFactory*, void**, int>)&GetPrimaryLanguageOverride));
    private static readonly void** NetworkInformationStaticsVftbl = CreateVftbl(
        14,
        (12, (nint)(delegate* unmanaged[MemberFunction]<FakeActivationFactory*, void*, EventRegistrationToken*, int>)&AddNetworkStatusChanged),
        (13, (nint)(delegate* unmanaged[MemberFunction]<FakeActivationFactory*, EventRegistrationToken, int>)&RemoveNetworkStatusChanged));
    private static readonly void** ComposableFactoryVftbl = CreateVftbl(
        8,
        (6, (nint)(delegate* unmanaged[MemberFunction]<FakeActivationFactory*, void*, void**, void**, int>)&CreateComposable),
        (7, (nint)(delegate* unmanaged[MemberFunction]<FakeActivationFactory*, int, void*, void**, void**, int>)&CreateComposableWithValue));

    /// <summary>
    /// The identifier of the current server instance for each class (activation factories from previous servers are disconnected).
    /// </summary>
    private static readonly int[] ServerIds = new int[ServerClassCount];

    /// <summary>
    /// The number of activation factories that have been handed out, for each class.
    /// </summary>
    private static readonly int[] ActivationFactoryCounts = new int[ServerClassCount];

    /// <summary>
    /// The number of calls that failed because their server was disconnected, for each class.
    /// </summary>
    private static readonly int[] DisconnectedCallCounts = new int[ServerClassCount];

    /// <summary>
    /// The event handlers registered to the fake <c>NetworkStatusChanged</c> event, by token.
    /// </summary>
    private static readonly Dictionary<long, nint> NetworkStatusChangedHandlers = [];

    /// <summary>
    /// The last token used for a fake <c>NetworkStatusChanged</c> event registration.
    /// </summary>
    private static long lastNetworkStatusChangedToken;

    /// <summary>
    /// Gets or sets the <c>HRESULT</c> returned by calls on activation factories whose server was killed.
    /// </summary>
    public static int FailureHResult { get; set; } = RPC_S_SERVER_UNAVAILABLE;

    /// <summary>
    /// Gets the number of currently registered handlers for the fake <c>NetworkStatusChanged</c> event.
    /// </summary>
    public static int NetworkStatusChangedHandlerCount => NetworkStatusChangedHandlers.Count;

    /// <summary>
    /// Kills the current server for a given class, disconnecting all its existing activation factories.
    /// </summary>
    /// <param name="serverClass">The class whose server to kill.</param>
    public static void Kill(ServerClass serverClass)
    {
        ServerIds[(int)serverClass]++;
    }

    /// <summary>
    /// Gets the number of activation factories that have been handed out for a given class.
    /// </summary>
    /// <param name="serverClass">The class to get the count for.</param>
    /// <returns>The number of activation factories that have been handed out for <paramref name="serverClass"/>.</returns>
    public static int GetActivationFactoryCount(ServerClass serverClass)
    {
        return ActivationFactoryCounts[(int)serverClass];
    }

    /// <summary>
    /// Gets the number of calls on activation factories for a given class that failed because their server was disconnected.
    /// </summary>
    /// <param name="serverClass">The class to get the count for.</param>
    /// <returns>The number of calls that failed for <paramref name="serverClass"/>.</returns>
    public static int GetDisconnectedCallCount(ServerClass serverClass)
    {
        return DisconnectedCallCounts[(int)serverClass];
    }

    /// <inheritdoc cref="WindowsRuntimeActivationHandler"/>
    public static int GetActivationFactory(string runtimeClassName, in Guid iid, out void* activationFactory)
    {
        activationFactory = null;

        ServerClass? serverClassOrNull = runtimeClassName switch
        {
            "Windows.Data.Json.JsonObject" when iid == WellKnownInterfaceIIDs.IID_IActivationFactory => ServerClass.JsonObject,
            "Windows.Globalization.Language" when iid == IID_ILanguageFactory => ServerClass.Language,
            "Windows.Data.Json.JsonValue" when iid == IID_IJsonValueStatics => ServerClass.JsonValue,
            "Windows.Globalization.ApplicationLanguages" when iid == IID_IApplicationLanguagesStatics => ServerClass.ApplicationLanguages,
            "Windows.Networking.Connectivity.NetworkInformation" when iid == IID_INetworkInformationStatics => ServerClass.NetworkInformation,
            "TestComponent.Composable" when iid == IID_IComposableFactory => ServerClass.Composable,
            _ => null
        };

        // Let all other types be activated normally
        if (serverClassOrNull is not { } serverClass)
        {
            return REGDB_E_CLASSNOTREG;
        }

        void** vftbl = serverClass switch
        {
            ServerClass.JsonObject => ActivationFactoryVftbl,
            ServerClass.Language => LanguageFactoryVftbl,
            ServerClass.JsonValue => JsonValueStaticsVftbl,
            ServerClass.ApplicationLanguages => ApplicationLanguagesStaticsVftbl,
            ServerClass.NetworkInformation => NetworkInformationStaticsVftbl,
            _ => ComposableFactoryVftbl
        };

        void* realActivationFactory = null;

        // Get the real activation factory to forward calls to, if needed (the other classes are implemented directly)
        if (serverClass is not (ServerClass.ApplicationLanguages or ServerClass.NetworkInformation))
        {
            int hresult = GetRealActivationFactory(serverClass, runtimeClassName, in iid, &realActivationFactory);

            if (hresult < 0)
            {
                return hresult;
            }
        }

        FakeActivationFactory* fakeActivationFactory = (FakeActivationFactory*)NativeMemory.Alloc((nuint)sizeof(FakeActivationFactory));

        fakeActivationFactory->Vftbl = vftbl;
        fakeActivationFactory->ActivationFactory = realActivationFactory;
        fakeActivationFactory->Iid = iid;
        fakeActivationFactory->ReferenceCount = 1;
        fakeActivationFactory->ServerClass = serverClass;
        fakeActivationFactory->ServerId = ServerIds[(int)serverClass];
        fakeActivationFactory->IsAgile = serverClass != ServerClass.ApplicationLanguages;

        activationFactory = fakeActivationFactory;

        ActivationFactoryCounts[(int)serverClass]++;

        return S_OK;
    }

    private static int GetRealActivationFactory(ServerClass serverClass, string runtimeClassName, in Guid iid, void** activationFactory)
    {
        void* runtimeClassNameHString = HStringMarshaller.ConvertToUnmanaged(runtimeClassName);
        int hresult;

        // 'TestComponent' is not registered, so get its activation factory directly from its 'DllGetActivationFactory' export
        if (serverClass is ServerClass.Composable)
        {
            nint dllGetActivationFactory = NativeLibrary.GetExport(NativeLibrary.Load("TestComponent.dll"), "DllGetActivationFactory");
            void* defaultActivationFactory = null;

            hresult = ((delegate* unmanaged[Stdcall]<void*, void**, int>)dllGetActivationFactory)(runtimeClassNameHString, &defaultActivationFactory);

            if (hresult >= 0)
            {
                hresult = Marshal.QueryInterface((nint)defaultActivationFactory, in iid, out nint result);

                *activationFactory = (void*)result;

                _ = Marshal.Release((nint)defaultActivationFactory);
            }
        }
        else
        {
            fixed (Guid* riid = &iid)
            {
                hresult = RoGetActivationFactory(runtimeClassNameHString, riid, activationFactory);
            }
        }

        HStringMarshaller.Free(runtimeClassNameHString);

        return hresult;
    }

    private static void** CreateVftbl(int count, params ReadOnlySpan<(int Slot, nint Method)> methods)
    {
        void** vftbl = (void**)NativeMemory.AllocZeroed((nuint)count, (nuint)sizeof(void*));

        vftbl[0] = (delegate* unmanaged[MemberFunction]<FakeActivationFactory*, Guid*, void**, int>)&QueryInterface;
        vftbl[1] = (delegate* unmanaged[MemberFunction]<FakeActivationFactory*, uint>)&AddRef;
        vftbl[2] = (delegate* unmanaged[MemberFunction]<FakeActivationFactory*, uint>)&Release;
        vftbl[3] = (delegate* unmanaged[MemberFunction]<FakeActivationFactory*, uint*, Guid**, int>)&GetIids;
        vftbl[4] = (delegate* unmanaged[MemberFunction]<FakeActivationFactory*, void**, int>)&GetRuntimeClassName;
        vftbl[5] = (delegate* unmanaged[MemberFunction]<FakeActivationFactory*, int*, int>)&GetTrustLevel;

        // Methods that are not used by the test are just stubs, but each vtable slot must still be valid
        for (int i = 6; i < count; i++)
        {
            vftbl[i] = (delegate* unmanaged[MemberFunction]<FakeActivationFactory*, int>)&NotImplemented;
        }

        foreach ((int slot, nint method) in methods)
        {
            vftbl[slot] = (void*)method;
        }

        return vftbl;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int QueryInterface(FakeActivationFactory* thisPtr, Guid* iid, void** result)
    {
        // Like for COM proxies, 'QueryInterface' calls for known interfaces are answered locally (also on disconnected proxies)
        if (*iid == WellKnownInterfaceIIDs.IID_IUnknown ||
            *iid == WellKnownInterfaceIIDs.IID_IInspectable ||
            (*iid == WellKnownInterfaceIIDs.IID_IAgileObject && thisPtr->IsAgile) ||
            *iid == thisPtr->Iid)
        {
            _ = Interlocked.Increment(ref thisPtr->ReferenceCount);

            *result = thisPtr;

            return S_OK;
        }

        *result = null;

        return E_NOINTERFACE;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static uint AddRef(FakeActivationFactory* thisPtr)
    {
        return (uint)Interlocked.Increment(ref thisPtr->ReferenceCount);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static uint Release(FakeActivationFactory* thisPtr)
    {
        int referenceCount = Interlocked.Decrement(ref thisPtr->ReferenceCount);

        if (referenceCount == 0)
        {
            if (thisPtr->ActivationFactory is not null)
            {
                _ = Marshal.Release((nint)thisPtr->ActivationFactory);
            }

            NativeMemory.Free(thisPtr);
        }

        return (uint)referenceCount;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int GetIids(FakeActivationFactory* thisPtr, uint* iidCount, Guid** iids)
    {
        return thisPtr->IsDisconnected ? FailureHResult : E_NOTIMPL;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int GetRuntimeClassName(FakeActivationFactory* thisPtr, void** className)
    {
        return thisPtr->IsDisconnected ? FailureHResult : E_NOTIMPL;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int GetTrustLevel(FakeActivationFactory* thisPtr, int* trustLevel)
    {
        if (thisPtr->IsDisconnected)
        {
            return FailureHResult;
        }

        *trustLevel = 0;

        return S_OK;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int NotImplemented(FakeActivationFactory* thisPtr)
    {
        return E_NOTIMPL;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int ActivateInstance(FakeActivationFactory* thisPtr, void** instance)
    {
        if (TryFailWithDisconnectedServer(thisPtr, out int hresult))
        {
            return hresult;
        }

        void* activationFactory = thisPtr->ActivationFactory;

        return ((delegate* unmanaged[MemberFunction]<void*, void**, int>)(*(void***)activationFactory)[6])(activationFactory, instance);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int CreateLanguage(FakeActivationFactory* thisPtr, void* languageTag, void** result)
    {
        if (TryFailWithDisconnectedServer(thisPtr, out int hresult))
        {
            return hresult;
        }

        void* activationFactory = thisPtr->ActivationFactory;

        return ((delegate* unmanaged[MemberFunction]<void*, void*, void**, int>)(*(void***)activationFactory)[6])(activationFactory, languageTag, result);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int CreateNumberValue(FakeActivationFactory* thisPtr, double input, void** jsonValue)
    {
        if (TryFailWithDisconnectedServer(thisPtr, out int hresult))
        {
            return hresult;
        }

        void* activationFactory = thisPtr->ActivationFactory;

        return ((delegate* unmanaged[MemberFunction]<void*, double, void**, int>)(*(void***)activationFactory)[9])(activationFactory, input, jsonValue);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int CreateComposable(FakeActivationFactory* thisPtr, void* baseInterface, void** innerInterface, void** result)
    {
        if (TryFailWithDisconnectedServer(thisPtr, out int hresult))
        {
            return hresult;
        }

        void* activationFactory = thisPtr->ActivationFactory;

        return ((delegate* unmanaged[MemberFunction]<void*, void*, void**, void**, int>)(*(void***)activationFactory)[6])(activationFactory, baseInterface, innerInterface, result);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int CreateComposableWithValue(FakeActivationFactory* thisPtr, int init, void* baseInterface, void** innerInterface, void** result)
    {
        if (TryFailWithDisconnectedServer(thisPtr, out int hresult))
        {
            return hresult;
        }

        void* activationFactory = thisPtr->ActivationFactory;

        return ((delegate* unmanaged[MemberFunction]<void*, int, void*, void**, void**, int>)(*(void***)activationFactory)[7])(activationFactory, init, baseInterface, innerInterface, result);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int GetPrimaryLanguageOverride(FakeActivationFactory* thisPtr, void** value)
    {
        if (TryFailWithDisconnectedServer(thisPtr, out int hresult))
        {
            return hresult;
        }

        *value = HStringMarshaller.ConvertToUnmanaged("fake-language");

        return S_OK;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int AddNetworkStatusChanged(FakeActivationFactory* thisPtr, void* handler, EventRegistrationToken* token)
    {
        if (TryFailWithDisconnectedServer(thisPtr, out int hresult))
        {
            return hresult;
        }

        // Keep the handler alive while it's registered, like a real event source would do
        _ = Marshal.AddRef((nint)handler);

        long value = ++lastNetworkStatusChangedToken;

        NetworkStatusChangedHandlers.Add(value, (nint)handler);

        *token = new EventRegistrationToken(value);

        return S_OK;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static int RemoveNetworkStatusChanged(FakeActivationFactory* thisPtr, EventRegistrationToken token)
    {
        if (TryFailWithDisconnectedServer(thisPtr, out int hresult))
        {
            return hresult;
        }

        if (!NetworkStatusChangedHandlers.Remove(token.Value, out nint handler))
        {
            return E_INVALIDARG;
        }

        _ = Marshal.Release(handler);

        return S_OK;
    }

    private static bool TryFailWithDisconnectedServer(FakeActivationFactory* thisPtr, out int hresult)
    {
        if (thisPtr->IsDisconnected)
        {
            DisconnectedCallCounts[(int)thisPtr->ServerClass]++;

            hresult = FailureHResult;

            return true;
        }

        hresult = S_OK;

        return false;
    }

    [LibraryImport("combase.dll")]
    private static partial int RoGetActivationFactory(void* activatableClassId, Guid* iid, void** factory);

    /// <summary>
    /// The native object for a fake activation factory (a proxy to a real activation factory, or a fake implementation).
    /// </summary>
    private struct FakeActivationFactory
    {
        public void** Vftbl;
        public void* ActivationFactory;
        public Guid Iid;
        public int ReferenceCount;
        public ServerClass ServerClass;
        public int ServerId;
        public bool IsAgile;

        public readonly bool IsDisconnected => ServerId != ServerIds[(int)ServerClass];
    }
}
