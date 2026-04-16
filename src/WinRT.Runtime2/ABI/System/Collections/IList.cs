// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable CA2256, IDE0008, IDE1006

[assembly: TypeMapAssociation<DynamicInterfaceCastableImplementationTypeMapGroup>(
    typeof(IList),
    typeof(ABI.System.Collections.IListInterfaceImpl))]

[assembly: TypeMapAssociation<DynamicInterfaceCastableImplementationTypeMapGroup>(
    typeof(ICollection),
    typeof(ABI.System.Collections.IListInterfaceImpl))]

namespace ABI.System.Collections;

/// <summary>
/// Marshaller for <see cref="IList"/>.
/// </summary>
[WindowsRuntimeImplementationOnlyMember]
public static unsafe class IListMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(IList? value)
    {
        return WindowsRuntimeInterfaceMarshaller<IList>.ConvertToUnmanaged(value, in WellKnownWindowsInterfaceIIDs.IID_IBindableVector);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static IList? ConvertToManaged(void* value)
    {
        return (IList?)WindowsRuntimeUnsealedObjectMarshaller.ConvertToManaged<IListComWrappersCallback>(value);
    }
}

/// <summary>
/// The <see cref="IWindowsRuntimeUnsealedObjectComWrappersCallback"/> implementation for <see cref="IList"/>.
/// </summary>
file abstract class IListComWrappersCallback : IWindowsRuntimeUnsealedObjectComWrappersCallback
{
    /// <inheritdoc/>
    public static unsafe bool TryCreateObject(
        void* value,
        ReadOnlySpan<char> runtimeClassName,
        [NotNullWhen(true)] out object? wrapperObject,
        out CreatedWrapperFlags wrapperFlags)
    {
        if (runtimeClassName.SequenceEqual(WellKnownXamlRuntimeClassNames.IBindableVector))
        {
            WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(
                externalComObject: value,
                iid: in WellKnownWindowsInterfaceIIDs.IID_IBindableVector,
                wrapperFlags: out wrapperFlags);

            wrapperObject = new WindowsRuntimeList(valueReference);

            return true;
        }

        wrapperFlags = CreatedWrapperFlags.None;
        wrapperObject = null;

        return false;
    }

    /// <inheritdoc/>
    public static unsafe object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReferenceUnsafe(
            externalComObject: value,
            iid: in WellKnownWindowsInterfaceIIDs.IID_IBindableVector,
            wrapperFlags: out wrapperFlags);

        return new WindowsRuntimeList(valueReference);
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="IList"/>.
/// </summary>
[WindowsRuntimeImplementationOnlyMember]
public sealed unsafe class IListComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReference(
            externalComObject: value,
            iid: in WellKnownWindowsInterfaceIIDs.IID_IBindableVector,
            wrapperFlags: out wrapperFlags);

        return new WindowsRuntimeList(valueReference);
    }
}

/// <summary>
/// Interop methods for <see cref="IList"/>.
/// </summary>
[WindowsRuntimeImplementationOnlyMember]
public static class IListMethods
{
    /// <inheritdoc cref="ICollection.Count"/>
    public static int Count(WindowsRuntimeObjectReference thisReference)
    {
        return BindableIListMethods.Count(thisReference);
    }

    /// <inheritdoc cref="ICollection.Count"/>
    public static void CopyTo(WindowsRuntimeObjectReference thisReference, Array array, int index)
    {
        BindableIListMethods.CopyTo(thisReference, array, index);
    }

    /// <inheritdoc cref="IList.this"/>
    public static object? Item(WindowsRuntimeObjectReference thisReference, int index)
    {
        return BindableIListMethods.Item(thisReference, index);
    }

    /// <inheritdoc cref="IList.this"/>
    public static void Item(WindowsRuntimeObjectReference thisReference, int index, object? value)
    {
        BindableIListMethods.Item(thisReference, index, value);
    }

    /// <inheritdoc cref="IList.Add"/>
    public static int Add(WindowsRuntimeObjectReference thisReference, object? value)
    {
        return BindableIListMethods.Add(thisReference, value);
    }

    /// <inheritdoc cref="IList.Contains"/>
    public static bool Contains(WindowsRuntimeObjectReference thisReference, object? value)
    {
        return BindableIListMethods.Contains(thisReference, value);
    }

    /// <inheritdoc cref="IList.IndexOf"/>
    public static int IndexOf(WindowsRuntimeObjectReference thisReference, object? value)
    {
        return BindableIListMethods.IndexOf(thisReference, value);
    }

    /// <inheritdoc cref="IList.Insert"/>
    public static void Insert(WindowsRuntimeObjectReference thisReference, int index, object? value)
    {
        BindableIListMethods.Insert(thisReference, index, value);
    }

    /// <inheritdoc cref="IList.Remove"/>
    public static void Remove(WindowsRuntimeObjectReference thisReference, object? value)
    {
        BindableIListMethods.Remove(thisReference, value);
    }

    /// <inheritdoc cref="IList.RemoveAt"/>
    public static void RemoveAt(WindowsRuntimeObjectReference thisReference, int index)
    {
        BindableIListMethods.RemoveAt(thisReference, index);
    }

    /// <inheritdoc cref="IList.Clear"/>
    public static void Clear(WindowsRuntimeObjectReference thisReference)
    {
        BindableIListMethods.Clear(thisReference);
    }
}

/// <summary>
/// The <see cref="IList"/> implementation.
/// </summary>
[WindowsRuntimeImplementationOnlyMember]
public static unsafe class IListImpl
{
    /// <summary>
    /// The <see cref="IBindableVectorVftbl"/> value for the managed <see cref="IList"/> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly IBindableVectorVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static IListImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        Vftbl.GetAt = &GetAt;
        Vftbl.get_Size = &get_Size;
        Vftbl.GetView = &GetView;
        Vftbl.IndexOf = &IndexOf;
        Vftbl.SetAt = &SetAt;
        Vftbl.InsertAt = &InsertAt;
        Vftbl.RemoveAt = &RemoveAt;
        Vftbl.Append = &Append;
        Vftbl.RemoveAtEnd = &RemoveAtEnd;
        Vftbl.Clear = &Clear;
    }

    /// <summary>
    /// Gets a pointer to the managed <see cref="IList"/> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.getat"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetAt(void* thisPtr, uint index, void** result)
    {
        if (result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IList>((ComInterfaceDispatch*)thisPtr);

            object? value = BindableIListAdapter.GetAt(thisObject, index);

            *result = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(value).DetachThisPtrUnsafe();

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.size"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT get_Size(void* thisPtr, uint* size)
    {
        if (size is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IList>((ComInterfaceDispatch*)thisPtr);

            *size = BindableIListAdapter.Size(thisObject);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.getview"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT GetView(void* thisPtr, void** view)
    {
        if (view is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IList>((ComInterfaceDispatch*)thisPtr);

            BindableIReadOnlyListAdapter adapter = BindableIListAdapter.GetView(thisObject);

            *view = WindowsRuntime.InteropServices.BindableIReadOnlyListAdapterMarshaller.ConvertToUnmanaged(adapter).DetachThisPtrUnsafe();

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.indexof"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT IndexOf(void* thisPtr, void* value, uint* index, bool* result)
    {
        if (index is null || result is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IList>((ComInterfaceDispatch*)thisPtr);

            object? item = WindowsRuntimeObjectMarshaller.ConvertToManaged(value);

            *result = BindableIListAdapter.IndexOf(thisObject, item, out *index);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.setat"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT SetAt(void* thisPtr, uint index, void* value)
    {
        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IList>((ComInterfaceDispatch*)thisPtr);

            object? item = WindowsRuntimeObjectMarshaller.ConvertToManaged(value);

            BindableIListAdapter.SetAt(thisObject, index, item);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.insertat"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT InsertAt(void* thisPtr, uint index, void* value)
    {
        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IList>((ComInterfaceDispatch*)thisPtr);

            object? item = WindowsRuntimeObjectMarshaller.ConvertToManaged(value);

            BindableIListAdapter.InsertAt(thisObject, index, item);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.removeat"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT RemoveAt(void* thisPtr, uint index)
    {
        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IList>((ComInterfaceDispatch*)thisPtr);

            BindableIListAdapter.RemoveAt(thisObject, index);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.append"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT Append(void* thisPtr, void* value)
    {
        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IList>((ComInterfaceDispatch*)thisPtr);

            object? item = WindowsRuntimeObjectMarshaller.ConvertToManaged(value);

            BindableIListAdapter.Append(thisObject, item);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.removeatend"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT RemoveAtEnd(void* thisPtr)
    {
        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IList>((ComInterfaceDispatch*)thisPtr);

            BindableIListAdapter.RemoveAtEnd(thisObject);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.clear"/>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvMemberFunction)])]
    private static HRESULT Clear(void* thisPtr)
    {
        try
        {
            var thisObject = ComInterfaceDispatch.GetInstance<IList>((ComInterfaceDispatch*)thisPtr);

            BindableIListAdapter.Clear(thisObject);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <summary>
/// The <see cref="IDynamicInterfaceCastable"/> implementation for <see cref="IList"/>.
/// </summary>
[DynamicInterfaceCastableImplementation]
[Guid("393DE7DE-6FD0-4C0D-BB71-47244A113E93")]
file interface IListInterfaceImpl : IList
{
    /// <inheritdoc/>
    object? IList.this[int index]
    {
        get
        {
            var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IList).TypeHandle);

            return BindableIListMethods.Item(thisReference, index);
        }
        set
        {
            var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IList).TypeHandle);

            BindableIListMethods.Item(thisReference, index, value);
        }
    }

    /// <inheritdoc/>
    bool IList.IsFixedSize => false;

    /// <inheritdoc/>
    bool IList.IsReadOnly => false;

    /// <inheritdoc/>
    int IList.Add(object? value)
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IList).TypeHandle);

        return BindableIListMethods.Add(thisReference, value);
    }

    /// <inheritdoc/>
    void IList.Clear()
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IList).TypeHandle);

        BindableIListMethods.Clear(thisReference);
    }

    /// <inheritdoc/>
    bool IList.Contains(object? value)
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IList).TypeHandle);

        return BindableIListMethods.Contains(thisReference, value);
    }

    /// <inheritdoc/>
    int IList.IndexOf(object? value)
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IList).TypeHandle);

        return BindableIListMethods.IndexOf(thisReference, value);
    }

    /// <inheritdoc/>
    void IList.Insert(int index, object? value)
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IList).TypeHandle);

        BindableIListMethods.Insert(thisReference, index, value);
    }

    /// <inheritdoc/>
    void IList.Remove(object? value)
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IList).TypeHandle);

        BindableIListMethods.Remove(thisReference, value);
    }

    /// <inheritdoc/>
    void IList.RemoveAt(int index)
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IList).TypeHandle);

        BindableIListMethods.RemoveAt(thisReference, index);
    }

    /// <inheritdoc/>
    int ICollection.Count
    {
        get
        {
            var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IList).TypeHandle);

            return BindableIListMethods.Count(thisReference);
        }
    }

    /// <inheritdoc/>
    bool ICollection.IsSynchronized => false;

    /// <inheritdoc/>
    object ICollection.SyncRoot => this;

    /// <inheritdoc/>
    void ICollection.CopyTo(Array array, int index)
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(IList).TypeHandle);

        BindableIListMethods.CopyTo(thisReference, array, index);
    }
}
