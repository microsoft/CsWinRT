// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Windows.Data.Json;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

// Projected types that are native-exposed must keep using their own CCW proxy when opaque RCW marshalling
// is enabled, so this test also includes one, to verify that the opaque fallback doesn't take precedence.
[assembly: WindowsRuntimeNativeExposedType(typeof(JsonArray))]

namespace OpaqueRcwMarshalling;

internal static unsafe class Program
{
    private const int E_NOINTERFACE = unchecked((int)0x80004002);

    private static readonly Guid IJsonValueIid = new("A3219ECB-F0B3-4DCD-BEEE-19D48CD3ED1E");
    private static readonly Guid IJsonObjectIid = new("064E24DD-29C2-4F83-9AC1-9EE11578BEB3");
    private static readonly Guid IIterableObjectIid = new("092B849B-60B1-52BE-A44A-6FE8E933CBE4");

    [STAThread]
    private static int Main()
    {
        // Each of these RCWs is exposed to native code as an opaque CCW. The first and last cases use the same
        // projected type, so that the second one also validates reusing the marshalling info that was cached.
        CheckOpaqueRcw(new JsonObject(), JsonValueType.Object, "Windows.Data.Json.JsonObject", isJsonObject: true);
        CheckOpaqueRcw(JsonValue.CreateNumberValue(42), JsonValueType.Number, "Windows.Data.Json.JsonValue", isJsonObject: false);
        CheckOpaqueRcw(new JsonObject(), JsonValueType.Object, "Windows.Data.Json.JsonObject", isJsonObject: true);

        CheckNativeExposedRcw(new JsonArray());

        return 100;
    }

    private static void CheckOpaqueRcw(object rcw, JsonValueType expectedValueType, string expectedRuntimeClassName, bool isJsonObject)
    {
        // Native code (e.g. the XAML reference tracker host) can ask for a CCW for any RCW it was handed. This
        // is what produces the CCW here too: it fails for projected types that are not native-exposed by default.
        void* ccw = WindowsRuntimeComWrappersMarshal.GetOrCreateComInterfaceForObject(rcw, CreateComInterfaceFlags.TrackerSupport);

        try
        {
            Check(WindowsRuntimeMarshal.IsReferenceToManagedObject(ccw), "The CCW must be a managed object wrapper.");
            Check(WindowsRuntimeMarshal.TryGetManagedObject(ccw, out object managed) && ReferenceEquals(managed, rcw), "The CCW must wrap the original RCW.");

            CheckStandardInterfaces(ccw);
            CheckForwardedInterfaces(ccw, rcw, expectedValueType, isJsonObject);
            CheckIInspectable(ccw, expectedRuntimeClassName);
        }
        finally
        {
            WindowsRuntimeMarshal.Free(ccw);
            GC.KeepAlive(rcw);
        }

        // Regular marshalling must be unaffected, and keep unwrapping the native object instead of using the CCW
        void* native = WindowsRuntimeMarshal.ConvertToUnmanaged(rcw);

        try
        {
            Check(!WindowsRuntimeMarshal.IsReferenceToManagedObject(native), "Regular marshalling must unwrap the native object.");
            Check(ReferenceEquals(rcw, WindowsRuntimeMarshal.ConvertToManaged(native)), "A native round trip changed the RCW.");
        }
        finally
        {
            WindowsRuntimeMarshal.Free(native);
        }
    }

    private static void CheckStandardInterfaces(void* ccw)
    {
        // These are always implemented by the CCW itself, and must never be forwarded to the native object
        foreach (Guid iid in (ReadOnlySpan<Guid>)[WellKnownInterfaceIIDs.IID_IUnknown, WellKnownInterfaceIIDs.IID_IInspectable, WellKnownInterfaceIIDs.IID_IWeakReferenceSource])
        {
            void* @interface = QueryInterface(ccw, in iid);

            try
            {
                Check(WindowsRuntimeMarshal.IsReferenceToManagedObject(@interface), $"The {iid} interface must resolve to the CCW.");
            }
            finally
            {
                WindowsRuntimeMarshal.Free(@interface);
            }
        }

        // These are also part of the CCW, but they can be forwarded to the native object when it implements them
        foreach (Guid iid in (ReadOnlySpan<Guid>)[WellKnownInterfaceIIDs.IID_IStringable, WellKnownInterfaceIIDs.IID_IAgileObject, WellKnownInterfaceIIDs.IID_IMarshal])
        {
            WindowsRuntimeMarshal.Free(QueryInterface(ccw, in iid));
        }

        // An interface that is neither implemented by the CCW nor by the native object must not be available
        Guid unknownIid = new("9C0D1F3C-6E19-4E2C-9A55-1B2C3D4E5F60");
        int hresult = Marshal.QueryInterface((nint)ccw, in unknownIid, out nint unknown);

        Check(hresult == E_NOINTERFACE && unknown == 0, "An unrelated interface must not be available.");
    }

    private static void CheckForwardedInterfaces(void* ccw, object rcw, JsonValueType expectedValueType, bool isJsonObject)
    {
        // The CCW doesn't implement any interface from the projected type, but the RCW forwards the requests to
        // the native object. This is how native code can still use the object it originally received, which
        // can include XAML calling back into the object with interfaces it knows the native object implements.
        void* jsonValue = QueryInterface(ccw, in IJsonValueIid);

        try
        {
            Check(!WindowsRuntimeMarshal.IsReferenceToManagedObject(jsonValue), "The IJsonValue interface must be forwarded to the native object.");

            JsonValueType valueType = default;

            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, JsonValueType*, int>)(*(void***)jsonValue)[6])(jsonValue, &valueType));
            Check(valueType == expectedValueType, $"The forwarded IJsonValue interface returned the wrong value type: {valueType}.");
            Check(ReferenceEquals(rcw, WindowsRuntimeMarshal.ConvertToManaged(jsonValue)), "The forwarded native object resolved to a different RCW.");
        }
        finally
        {
            WindowsRuntimeMarshal.Free(jsonValue);
        }

        int hresult = Marshal.QueryInterface((nint)ccw, in IJsonObjectIid, out nint jsonObject);

        if (isJsonObject)
        {
            Marshal.ThrowExceptionForHR(hresult);
            Check(!WindowsRuntimeMarshal.IsReferenceToManagedObject((void*)jsonObject), "The IJsonObject interface must be forwarded to the native object.");
            WindowsRuntimeMarshal.Free((void*)jsonObject);
        }
        else
        {
            Check(hresult == E_NOINTERFACE && jsonObject == 0, "The native object does not implement IJsonObject, so the request must fail.");
        }
    }

    private static void CheckIInspectable(void* ccw, string expectedRuntimeClassName)
    {
        ReadOnlySpan<Guid> iids = GetIids(ccw);

        // The CCW only reports the standard interfaces for opaque objects (with 'IUnknown' last), which
        // are the same ones used for any other opaque managed object. It can also have the property provider.
        HashSet<Guid> distinctIids = [];

        foreach (Guid iid in iids)
        {
            Check(iid != Guid.Empty && distinctIids.Add(iid), "GetIids returned an empty or duplicate interface.");
        }

        Check(iids.Length is 6 or 7, $"GetIids returned an unexpected number of interfaces: {iids.Length}.");
        Check(iids[^1] == WellKnownInterfaceIIDs.IID_IUnknown, "GetIids must have IUnknown as the last interface.");
        Check(
            distinctIids.IsSupersetOf([
                WellKnownInterfaceIIDs.IID_IUnknown,
                WellKnownInterfaceIIDs.IID_IInspectable,
                WellKnownInterfaceIIDs.IID_IStringable,
                WellKnownInterfaceIIDs.IID_IWeakReferenceSource,
                WellKnownInterfaceIIDs.IID_IMarshal,
                WellKnownInterfaceIIDs.IID_IAgileObject]),
            "GetIids must return the standard interfaces.");

        // Projected classes don't have a runtime class name attribute, so the projected type name is used
        string runtimeClassName = GetRuntimeClassName(ccw);

        Check(runtimeClassName == expectedRuntimeClassName, $"GetRuntimeClassName returned an unexpected name: '{runtimeClassName}'.");
    }

    private static void CheckNativeExposedRcw(JsonArray array)
    {
        void* ccw = WindowsRuntimeComWrappersMarshal.GetOrCreateComInterfaceForObject(array, CreateComInterfaceFlags.TrackerSupport);

        try
        {
            Check(WindowsRuntimeMarshal.TryGetManagedObject(ccw, out object managed) && ReferenceEquals(managed, array), "The CCW must wrap the native-exposed RCW.");

            // The proxy for the native-exposed type has priority, so the CCW must not be the opaque one
            ReadOnlySpan<Guid> iids = GetIids(ccw);

            Check(iids.Contains(IIterableObjectIid), "The native-exposed type must keep using the vtables from its own proxy.");
            Check(GetRuntimeClassName(ccw) == "Windows.Data.Json.JsonArray", "The native-exposed type must report its projected type name.");
        }
        finally
        {
            WindowsRuntimeMarshal.Free(ccw);
            GC.KeepAlive(array);
        }
    }

    private static Guid[] GetIids(void* instance)
    {
        void* inspectable = QueryInterface(instance, in WellKnownInterfaceIIDs.IID_IInspectable);
        Guid* iids = null;
        uint count = 0;

        try
        {
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, int>)(*(void***)inspectable)[3])(inspectable, &count, &iids));

            ReadOnlySpan<Guid> interfaces = new(iids, checked((int)count));

            return interfaces.ToArray();
        }
        finally
        {
            Marshal.FreeCoTaskMem((nint)iids);
            WindowsRuntimeMarshal.Free(inspectable);
        }
    }

    private static string GetRuntimeClassName(void* instance)
    {
        void* inspectable = QueryInterface(instance, in WellKnownInterfaceIIDs.IID_IInspectable);
        void* runtimeClassName = null;

        try
        {
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, void**, int>)(*(void***)inspectable)[4])(inspectable, &runtimeClassName));

            return HStringMarshaller.ConvertToManaged(runtimeClassName);
        }
        finally
        {
            HStringMarshaller.Free(runtimeClassName);
            WindowsRuntimeMarshal.Free(inspectable);
        }
    }

    private static void* QueryInterface(void* instance, in Guid iid)
    {
        Marshal.ThrowExceptionForHR(Marshal.QueryInterface((nint)instance, in iid, out nint result));

        return (void*)result;
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
