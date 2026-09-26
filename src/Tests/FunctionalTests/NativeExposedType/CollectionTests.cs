// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Windows.Data.Json;
using Windows.Foundation;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;

[assembly: TypeMapAssociation<WindowsRuntimeComWrappersTypeMapGroup>(
    typeof(NativeExposedType.CollectionTests.ThrowingObject),
    typeof(NativeExposedType.CollectionTests.ThrowingObjectMetadata))]

namespace NativeExposedType;

internal static unsafe class CollectionTests
{
    internal static readonly Guid IterableObject = new("092B849B-60B1-52BE-A44A-6FE8E933CBE4");
    private static readonly Guid IteratorObject = new("44A94F2D-04F8-5091-B336-BE7892DD10BE");
    private static readonly Guid Inspectable = new("AF86E2E0-B12D-4C6A-9C5A-D7AA65101E90");

    public static void Run(bool rcwFirst)
    {
        JsonArray array = rcwFirst ? ActivateArray() : new JsonArray();
        IJsonValue item = JsonValue.CreateNumberValue(42);
        array.Add(item);

        CheckProjectedCollection(array, [item]);
        Check(ActivateArray().Count == 0, "RCW creation must still work after CCW metadata has been cached.");

        foreach (object previous in new object[] { new List<object>(), new object(), 42 })
        {
            void* previousPointer = WindowsRuntimeMarshal.ConvertToUnmanaged(previous);

            try
            {
                // Repeat a cached marshalling operation, which may not call ComputeVtables.
                void* cachedPointer = WindowsRuntimeMarshal.ConvertToUnmanaged(previous);
                WindowsRuntimeMarshal.Free(cachedPointer);
                CheckProjectedCollection(new JsonArray { item }, [item]);
            }
            finally
            {
                WindowsRuntimeMarshal.Free(previousPointer);
                GC.KeepAlive(previous);
            }
        }

        CheckIteratorCovariance(array, item);
        CheckBindableList(item);
        CheckReentrantMarshalling();
        CheckMarshallingKinds();
        CheckProjectedCollection(new JsonArray { item }, [item]);
    }

    private static JsonArray ActivateArray()
    {
        void* factory = WindowsRuntimeActivationFactory.GetActivationFactoryUnsafe("Windows.Data.Json.JsonArray");
        void* instance = null;

        try
        {
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, void**, int>)(*(void***)factory)[6])(factory, &instance));

            object wrapper = WindowsRuntimeMarshal.ConvertToManaged(instance);
            Check(wrapper is JsonArray, "Native activation must select the projected RCW marshaller.");
            Check(ReferenceEquals(wrapper, WindowsRuntimeMarshal.ConvertToManaged(instance)), "RCW identity was not preserved.");

            return (JsonArray)wrapper;
        }
        finally
        {
            WindowsRuntimeMarshal.Free(instance);
            WindowsRuntimeMarshal.Free(factory);
        }
    }

    private static void CheckProjectedCollection(JsonArray array, object[] expected)
    {
        void* ccw = WindowsRuntimeComWrappersMarshal.GetOrCreateComInterfaceForObject(array, CreateComInterfaceFlags.TrackerSupport);

        try
        {
            Check(WindowsRuntimeMarshal.TryGetManagedObject(ccw, out object managed) && ReferenceEquals(managed, array), "The forced CCW must wrap the projected collection.");
            CheckCollectionIids(ccw);
            CheckEnumerable(ccw, in IterableObject, expected);
            CheckEnumerable(ccw, in WellKnownInterfaceIIDs.IID_Windows_UI_Xaml_Interop_IBindableIterable, expected);

            void* native = WindowsRuntimeMarshal.ConvertToUnmanaged(array);

            try
            {
                Check(!WindowsRuntimeMarshal.IsReferenceToManagedObject(native), "Normal marshalling must unwrap the native collection, not return its CCW.");
                Check(ReferenceEquals(array, WindowsRuntimeMarshal.ConvertToManaged(native)), "A native round trip changed the managed collection.");
            }
            finally
            {
                WindowsRuntimeMarshal.Free(native);
            }
        }
        finally
        {
            WindowsRuntimeMarshal.Free(ccw);
            GC.KeepAlive(array);
        }
    }

    private static void CheckCollectionIids(void* ccw)
    {
        ReadOnlySpan<Guid> interfaces = GetIids(ccw);
        Check(interfaces.Contains(IterableObject), "GetIids must use the native-exposure proxy.");
        Check(interfaces.Contains(WellKnownInterfaceIIDs.IID_Windows_UI_Xaml_Interop_IBindableIterable), "The CCW must advertise bindable enumeration.");
        Check(!interfaces.Contains(WellKnownInterfaceIIDs.IID_Windows_UI_Xaml_Interop_IBindableVector), "The collection must not inherit a previously marshalled List's bindable vtable.");
    }

    private static Guid[] GetIids(void* instance)
    {
        void* inspectable = QueryInterface(instance, in Inspectable);
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

    internal static void CheckEnumerable(void* instance, in Guid iid, object[] expected)
    {
        void* iterable = QueryInterface(instance, in iid);
        void* iterator = null;

        try
        {
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, void**, int>)(*(void***)iterable)[6])(iterable, &iterator));
            CheckIterator(iterator, expected);
        }
        finally
        {
            WindowsRuntimeMarshal.Free(iterator);
            WindowsRuntimeMarshal.Free(iterable);
        }
    }

    private static void CheckIterator(void* iterator, object[] expected)
    {
        void** vtable = *(void***)iterator;
        byte hasCurrent = 0;
        Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, byte*, int>)vtable[7])(iterator, &hasCurrent));

        foreach (object item in expected)
        {
            Check(hasCurrent != 0, "Native enumeration ended early.");
            void* current = null;

            try
            {
                Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, void**, int>)vtable[6])(iterator, &current));
                Check(ReferenceEquals(item, WindowsRuntimeMarshal.ConvertToManaged(current)), "Native enumeration returned a different element.");
            }
            finally
            {
                WindowsRuntimeMarshal.Free(current);
            }

            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, byte*, int>)vtable[8])(iterator, &hasCurrent));
        }

        Check(hasCurrent == 0, "Native enumeration returned extra elements.");
    }

    private static void CheckIteratorCovariance(JsonArray array, IJsonValue item)
    {
        using IEnumerator<object> enumerator = array.GetEnumerator();
        void* native = WindowsRuntimeMarshal.ConvertToUnmanaged(enumerator);

        try
        {
            int hresult = Marshal.QueryInterface((nint)native, in IteratorObject, out nint covariant);
            WindowsRuntimeMarshal.Free((void*)covariant);
            Check(hresult == unchecked((int)0x80004002), "The regression requires a native iterator without the covariant interface.");

            try
            {
                using WindowsRuntimeObjectReferenceValue unexpected = WindowsRuntimeInterfaceMarshaller<IEnumerator<object>>.ConvertToUnmanaged(enumerator, in IteratorObject);
                throw new InvalidOperationException("Ordinary interface marshalling must remain strict.");
            }
            catch (InvalidCastException)
            {
            }
        }
        finally
        {
            WindowsRuntimeMarshal.Free(native);
        }

        void* iterator = null;

        try
        {
            IEnumerableAdapter<object>.First(array, in IteratorObject, &iterator);
            CheckIterator(iterator, [item]);
        }
        finally
        {
            WindowsRuntimeMarshal.Free(iterator);
        }
    }

    private static void CheckBindableList(IJsonValue item)
    {
        List<object> list = [item];
        void* ccw = WindowsRuntimeMarshal.ConvertToUnmanaged(list);
        void* vector = null;
        void* nativeItem = null;
        void* view = null;

        try
        {
            vector = QueryInterface(ccw, in WellKnownInterfaceIIDs.IID_Windows_UI_Xaml_Interop_IBindableVector);
            nativeItem = WindowsRuntimeMarshal.ConvertToUnmanaged(item);
            void** vtable = *(void***)vector;
            uint count = 0;
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, uint*, int>)vtable[7])(vector, &count));
            Check(count == 1, "The bindable list Count is incorrect.");

            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, void*, int>)vtable[13])(vector, nativeItem));
            Check(list.Count == 2, "Bindable Append did not reach the managed list.");
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, uint, void*, int>)vtable[10])(vector, 0, nativeItem));
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, uint, void*, int>)vtable[11])(vector, 1, nativeItem));
            Check(list.Count == 3, "Bindable InsertAt did not reach the managed list.");

            uint index = 0;
            byte found = 0;
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, void*, uint*, byte*, int>)vtable[9])(vector, nativeItem, &index, &found));
            Check(found != 0 && index == 0, "Bindable IndexOf returned the wrong result.");
            CheckEnumerable(ccw, in WellKnownInterfaceIIDs.IID_Windows_UI_Xaml_Interop_IBindableIterable, [item, item, item]);

            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, void**, int>)vtable[8])(vector, &view));
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, uint*, int>)(*(void***)view)[7])(view, &count));
            Check(count == 3, "The bindable view Count is incorrect.");

            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, uint, int>)vtable[12])(vector, 0));
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, int>)vtable[14])(vector));
            Check(list.Count == 1, "Bindable removals did not reach the managed list.");
            Marshal.ThrowExceptionForHR(((delegate* unmanaged[MemberFunction]<void*, int>)vtable[15])(vector));
            Check(list.Count == 0, "Bindable Clear did not reach the managed list.");
        }
        finally
        {
            WindowsRuntimeMarshal.Free(view);
            WindowsRuntimeMarshal.Free(nativeItem);
            WindowsRuntimeMarshal.Free(vector);
            WindowsRuntimeMarshal.Free(ccw);
            GC.KeepAlive(list);
        }
    }

    private static void CheckReentrantMarshalling()
    {
        try
        {
            void* unexpected = WindowsRuntimeMarshal.ConvertToUnmanaged(new ThrowingObject());
            WindowsRuntimeMarshal.Free(unexpected);
            throw new InvalidOperationException("The custom marshaller did not throw.");
        }
        catch (MarshallingFailureException)
        {
        }
    }

    private static void CheckMarshallingKinds()
    {
        object[] values =
        [
            new OpaqueObject(),
            new MarshallingFailureException(),
            typeof(Point),
            new Point(1, 2),
            PropertyType.Int32,
            (AsyncActionCompletedHandler)((_, _) => { }),
            (EventHandler<int>)((_, _) => { })
        ];

        foreach (object value in values)
        {
            void* ccw = WindowsRuntimeMarshal.ConvertToUnmanaged(value);

            try
            {
                Check(ReferenceEquals(value, WindowsRuntimeMarshal.ConvertToManaged(ccw)), $"A round trip changed the managed {value.GetType()} instance.");

                Guid[] interfaces = GetIids(ccw);
                Check(interfaces.Length != 0, $"The {value.GetType()} CCW did not advertise any interfaces.");

                foreach (Guid iid in interfaces)
                {
                    void* interfacePointer = QueryInterface(ccw, in iid);
                    WindowsRuntimeMarshal.Free(interfacePointer);
                }
            }
            finally
            {
                WindowsRuntimeMarshal.Free(ccw);
                GC.KeepAlive(value);
            }
        }
    }

    internal static void Check(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void* QueryInterface(void* instance, in Guid iid)
    {
        Marshal.ThrowExceptionForHR(Marshal.QueryInterface((nint)instance, in iid, out nint result));

        return (void*)result;
    }

    internal sealed class ThrowingObject;

    private sealed class OpaqueObject;

    [ThrowingMarshaller]
    internal sealed class ThrowingObjectMetadata;

    private sealed class MarshallingFailureException : Exception;

    private sealed class ThrowingMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
    {
        public override void* GetOrCreateComInterfaceForObject(object value)
        {
            void* boxed = WindowsRuntimeMarshal.ConvertToUnmanaged(42);
            WindowsRuntimeMarshal.Free(boxed);

            // This statically typed path has no precomputed marshalling info of its own.
            Guid iid = new("548CEFBD-BC8A-5FA0-8DF2-957440FC8BF4");
            using WindowsRuntimeObjectReferenceValue scalar = WindowsRuntimeValueTypeMarshaller.BoxToUnmanaged<int>(42, CreateComInterfaceFlags.None, in iid);
            Check(WindowsRuntimeValueTypeMarshaller.UnboxToManaged<int>(scalar.GetThisPtrUnsafe()) == 42, "Nested explicit-flags marshalling used the outer object's metadata.");
            CheckMarshallingKinds();

            // Simulate a tracker callback while another object's marshaller is still running.
            object item = new();
            List<object> list = [item];
            void* ccw = WindowsRuntimeComWrappersMarshal.GetOrCreateComInterfaceForObject(list, CreateComInterfaceFlags.TrackerSupport);

            try
            {
                CheckEnumerable(ccw, in IterableObject, [item]);
            }
            finally
            {
                WindowsRuntimeMarshal.Free(ccw);
                GC.KeepAlive(list);
            }

            throw new MarshallingFailureException();
        }
    }
}
