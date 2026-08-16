// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WindowsRuntime.InteropServices;

namespace UnitTest;

[TestClass]
public unsafe class BindableAdapterTests
{
    private static readonly Guid IBindableVector = new("393DE7DE-6FD0-4C0D-BB71-47244A113E93");
    private static readonly Guid IBindableIterable = new("036D2C08-DF29-41AF-8AA2-D774BE62BA6F");
    private static readonly Guid IUnknown = new("00000000-0000-0000-C000-000000000046");

    [TestMethod]
    public void GetViewReturnsSameComIdentity()
    {
        IList list = new TestList { 1 };

        using WindowsRuntimeObjectReferenceValue listValue = ABI.System.Collections.IListMarshaller.ConvertToUnmanaged(list);
        void* vector = QueryInterface(listValue.GetThisPtrUnsafe(), in IBindableVector);
        void* view = GetView(vector);

        try
        {
            Assert.AreNotEqual((nint)vector, (nint)view);

            void* vectorIdentity = QueryInterface(vector, in IUnknown);
            void* viewIdentity = QueryInterface(view, in IUnknown);

            try
            {
                Assert.AreEqual((nint)vectorIdentity, (nint)viewIdentity);
            }
            finally
            {
                _ = Marshal.Release((nint)vectorIdentity);
                _ = Marshal.Release((nint)viewIdentity);
            }
        }
        finally
        {
            _ = Marshal.Release((nint)view);
            _ = Marshal.Release((nint)vector);
        }
    }

    [TestMethod]
    public void GetViewReflectsChangesToUnderlyingList()
    {
        IList list = new TestList { 1 };

        using WindowsRuntimeObjectReferenceValue listValue = ABI.System.Collections.IListMarshaller.ConvertToUnmanaged(list);
        void* vector = QueryInterface(listValue.GetThisPtrUnsafe(), in IBindableVector);
        void* view = GetView(vector);

        try
        {
            list.Add(2);

            void** vtable = *(void***)view;
            // IBindableVectorViewVftbl.get_Size follows the six IInspectable entries.
            var getSize = (delegate* unmanaged[MemberFunction]<void*, uint*, int>)vtable[7];
            uint size;

            Marshal.ThrowExceptionForHR(getSize(view, &size));

            Assert.AreEqual(2u, size);
        }
        finally
        {
            _ = Marshal.Release((nint)view);
            _ = Marshal.Release((nint)vector);
        }
    }

    [TestMethod]
    public void GetViewCanQueryBackToBindableVector()
    {
        IList list = new TestList();

        using WindowsRuntimeObjectReferenceValue listValue = ABI.System.Collections.IListMarshaller.ConvertToUnmanaged(list);
        void* vector = QueryInterface(listValue.GetThisPtrUnsafe(), in IBindableVector);
        void* view = GetView(vector);

        try
        {
            void* queriedVector = QueryInterface(view, in IBindableVector);

            try
            {
                Assert.AreEqual((nint)vector, (nint)queriedVector);
            }
            finally
            {
                _ = Marshal.Release((nint)queriedVector);
            }
        }
        finally
        {
            _ = Marshal.Release((nint)view);
            _ = Marshal.Release((nint)vector);
        }
    }

    [TestMethod]
    public void GetViewSupportsBindableIterable()
    {
        IList list = new TestList();

        using WindowsRuntimeObjectReferenceValue listValue = ABI.System.Collections.IListMarshaller.ConvertToUnmanaged(list);
        void* vector = QueryInterface(listValue.GetThisPtrUnsafe(), in IBindableVector);
        void* view = GetView(vector);

        try
        {
            void* iterable = QueryInterface(view, in IBindableIterable);
            _ = Marshal.Release((nint)iterable);
        }
        finally
        {
            _ = Marshal.Release((nint)view);
            _ = Marshal.Release((nint)vector);
        }
    }

    [TestMethod]
    public void GetViewIndexOfSupportsNull()
    {
        IList list = new TestList { null };

        using WindowsRuntimeObjectReferenceValue listValue = ABI.System.Collections.IListMarshaller.ConvertToUnmanaged(list);
        void* vector = QueryInterface(listValue.GetThisPtrUnsafe(), in IBindableVector);
        void* view = GetView(vector);

        try
        {
            void** vtable = *(void***)view;
            // IBindableVectorViewVftbl.IndexOf follows GetAt and get_Size after the six IInspectable entries.
            var indexOf = (delegate* unmanaged[MemberFunction]<void*, void*, uint*, bool*, int>)vtable[8];
            uint index;
            bool found;

            Marshal.ThrowExceptionForHR(indexOf(view, null, &index, &found));

            Assert.IsTrue(found);
            Assert.AreEqual(0u, index);
        }
        finally
        {
            _ = Marshal.Release((nint)view);
            _ = Marshal.Release((nint)vector);
        }
    }

    private static void* GetView(void* vector)
    {
        void** vtable = *(void***)vector;
        // IBindableVectorVftbl.GetView follows GetAt and get_Size after the six IInspectable entries.
        var getView = (delegate* unmanaged[MemberFunction]<void*, void**, int>)vtable[8];
        void* view;

        Marshal.ThrowExceptionForHR(getView(vector, &view));

        return view;
    }

    private static void* QueryInterface(void* value, in Guid iid)
    {
        Marshal.ThrowExceptionForHR(Marshal.QueryInterface((nint)value, in iid, out nint result));

        return (void*)result;
    }

    private sealed class TestList : ArrayList;
}
