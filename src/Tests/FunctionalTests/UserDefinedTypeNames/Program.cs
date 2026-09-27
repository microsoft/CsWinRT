// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using Windows.ApplicationModel.Background;
using Windows.Storage.Streams;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

(object Value, bool IsBuffer)[] cases =
[
    (new First.Operation(), true),
    (new Second.Operation(), false),
    (new First.Container.Operation(), true),
    (new Second.Container.Operation(), false),
    (new First.Operation<int>(), true),
    (new Second.Operation<int>(), false),
    (new First.Operation<string>(), true),
    (new Second.Operation<string>(), false),
    (new First.Container<int>.Operation<string>(), true),
    (new Second.Container<int>.Operation<string>(), false),
    (new First.Container<string>.Operation<int>(), true),
    (new Second.Container<string>.Operation<int>(), false),
    (new Third.Operation(), true),
    (new Operation(), true)
];

for (int i = 0; i < cases.Length; i++)
{
    if (!CheckObject(cases[i].Value, cases[i].IsBuffer))
    {
        Console.Error.WriteLine($"Incorrect CCW for {cases[i].Value.GetType()}.");

        return 101 + i;
    }
}

return 100;

static unsafe bool CheckObject(object value, bool isBuffer)
{
    Guid expectedIid = isBuffer ? typeof(IBuffer).GUID : typeof(IBackgroundTask).GUID;
    Guid unexpectedIid = isBuffer ? typeof(IBackgroundTask).GUID : typeof(IBuffer).GUID;
    void* ccw = WindowsRuntimeMarshal.ConvertToUnmanaged(value);
    nint expected = 0;
    nint unexpected = 0;
    nint callback = 0;
    nint provider = 0;
    ABI.System.Type providerType = default;

    try
    {
        Marshal.ThrowExceptionForHR(Marshal.QueryInterface((nint)ccw, expectedIid, out expected));

        if (!WindowsRuntimeMarshal.TryGetManagedObject((void*)expected, out object? roundTrip) ||
            !ReferenceEquals(value, roundTrip))
        {
            return false;
        }

        // A wrong marshaller association must not silently expose the other type's interface set.
        if (Marshal.QueryInterface((nint)ccw, unexpectedIid, out unexpected) != unchecked((int)0x80004002) ||
            unexpected != 0)
        {
            return false;
        }

        Marshal.ThrowExceptionForHR(Marshal.QueryInterface((nint)ccw, new Guid("7C925755-3E48-42B4-8677-76372267033F"), out provider));
        Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<nint, ABI.System.Type*, int>)(*(void***)provider)[9])(provider, &providerType));

        if ((int)providerType.Kind != 2 || HStringMarshaller.ConvertToManaged(providerType.Name) != value.GetType().AssemblyQualifiedName)
        {
            return false;
        }

        void** vtable = *(void***)expected;

        if (isBuffer)
        {
            uint capacity = 0;

            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, uint*, int>)vtable[6])((void*)expected, &capacity));

            if (capacity != ((IBuffer)value).Capacity)
            {
                return false;
            }
        }
        else
        {
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, void*, int>)vtable[6])((void*)expected, null));

            if (!((IBackgroundTaskProbe)value).WasRun)
            {
                return false;
            }
        }

        if (value is IStatusCallback statusCallback)
        {
            Marshal.ThrowExceptionForHR(Marshal.QueryInterface((nint)ccw, typeof(IStatusCallback).GUID, out callback));

            if (((delegate* unmanaged[MemberFunction]<void*, uint>)(*(void***)callback)[3])((void*)callback) != statusCallback.GetStatus())
            {
                return false;
            }
        }

        return true;
    }
    finally
    {
        ABI.System.TypeMarshaller.Dispose(providerType);
        WindowsRuntimeMarshal.Free((void*)provider);
        WindowsRuntimeMarshal.Free((void*)callback);
        WindowsRuntimeMarshal.Free((void*)unexpected);
        WindowsRuntimeMarshal.Free((void*)expected);
        WindowsRuntimeMarshal.Free(ccw);
    }
}
