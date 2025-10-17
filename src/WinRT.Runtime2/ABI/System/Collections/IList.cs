// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ABI.WindowsRuntime;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable CA2256, IDE0008, IDE1006

#pragma warning disable IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code
[assembly: TypeMap<WindowsRuntimeComWrappersTypeMapGroup>(
    value: "Windows.UI.Xaml.Interop.IBindableVector",
    target: typeof(ABI.System.Collections.IList),
    trimTarget: typeof(IList))]

[assembly: TypeMap<WindowsRuntimeComWrappersTypeMapGroup>(
    value: "Microsoft.UI.Xaml.Interop.IBindableVector",
    target: typeof(ABI.System.Collections.IList),
    trimTarget: typeof(IList))]
#pragma warning restore IL2026 // Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code

[assembly: TypeMapAssociation<DynamicInterfaceCastableImplementationTypeMapGroup>(
    typeof(IList),
    typeof(ABI.System.Collections.IListInterfaceImpl))]

namespace ABI.System.Collections;

/// <summary>
/// ABI type for <see cref="global::System.Collections.IList"/>.
/// </summary>
/// <remarks>
/// This interface is equivalent to <see href="https://learn.microsoft.com/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.interop.IBindableVector"/>.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.IBindableVector"/>
[IListComWrappersMarshaller]
file static class IList;

/// <summary>
/// Marshaller for <see cref="global::System.Collections.IList"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IListMarshaller
{
    /// <inheritdoc cref="WindowsRuntimeObjectMarshaller.ConvertToUnmanaged"/>
    public static WindowsRuntimeObjectReferenceValue ConvertToUnmanaged(global::System.Collections.IList? value)
    {
        return WindowsRuntimeInterfaceMarshaller<global::System.Collections.IList>.ConvertToUnmanaged(value, in WellKnownInterfaceIds.IID_IBindableVector);
    }

    /// <inheritdoc cref="WindowsRuntimeDelegateMarshaller.ConvertToManaged"/>
    public static global::System.Collections.IList? ConvertToManaged(void* value)
    {
        return (global::System.Collections.IList?)WindowsRuntimeUnsealedObjectMarshaller.ConvertToManaged<IListComWrappersCallback>(value);
    }
}

/// <summary>
/// The <see cref="IWindowsRuntimeUnsealedObjectComWrappersCallback"/> implementation for <see cref="global::System.Collections.IList"/>.
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
                iid: in WellKnownInterfaceIds.IID_IBindableVector,
                wrapperFlags: out wrapperFlags);

            wrapperObject = new WindowsRuntimeEnumerable(valueReference);

            return true;
        }

        wrapperFlags = CreatedWrapperFlags.None;
        wrapperObject = null;

        return false;
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="global::System.Collections.IList"/>.
/// </summary>
file sealed unsafe class IListComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        WindowsRuntimeObjectReference valueReference = WindowsRuntimeComWrappersMarshal.CreateObjectReference(
            externalComObject: value,
            iid: in WellKnownInterfaceIds.IID_IBindableVector,
            wrapperFlags: out wrapperFlags);

        return new WindowsRuntimeEnumerable(valueReference);
    }
}

/// <summary>
/// Interop methods for <see cref="global::System.Collections.IList"/>.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IListMethods
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

    /// <inheritdoc cref="global::System.Collections.IList.this"/>
    public static object? Item(WindowsRuntimeObjectReference thisReference, int index)
    {
        return BindableIListMethods.Item(thisReference, index);
    }

    /// <inheritdoc cref="global::System.Collections.IList.this"/>
    public static void Item(WindowsRuntimeObjectReference thisReference, int index, object? value)
    {
        BindableIListMethods.Item(thisReference, index, value);
    }

    /// <inheritdoc cref="global::System.Collections.IList.Add"/>
    public static int Add(WindowsRuntimeObjectReference thisReference, object? value)
    {
        return BindableIListMethods.Add(thisReference, value);
    }

    /// <inheritdoc cref="global::System.Collections.IList.Contains"/>
    public static bool Contains(WindowsRuntimeObjectReference thisReference, object? value)
    {
        return BindableIListMethods.Contains(thisReference, value);
    }

    /// <inheritdoc cref="global::System.Collections.IList.IndexOf"/>
    public static int IndexOf(WindowsRuntimeObjectReference thisReference, object? value)
    {
        return BindableIListMethods.IndexOf(thisReference, value);
    }

    /// <inheritdoc cref="global::System.Collections.IList.Insert"/>
    public static void Insert(WindowsRuntimeObjectReference thisReference, int index, object? value)
    {
        BindableIListMethods.Insert(thisReference, index, value);
    }

    /// <inheritdoc cref="global::System.Collections.IList.Remove"/>
    public static void Remove(WindowsRuntimeObjectReference thisReference, object? value)
    {
        BindableIListMethods.Remove(thisReference, value);
    }

    /// <inheritdoc cref="global::System.Collections.IList.RemoveAt"/>
    public static void RemoveAt(WindowsRuntimeObjectReference thisReference, int index)
    {
        BindableIListMethods.RemoveAt(thisReference, index);
    }

    /// <inheritdoc cref="global::System.Collections.IList.Clear"/>
    public static void Clear(WindowsRuntimeObjectReference thisReference)
    {
        BindableIListMethods.Clear(thisReference);
    }
}

/// <summary>
/// The <see cref="global::System.Collections.IList"/> implementation.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static unsafe class IListImpl
{
    /// <summary>
    /// The <see cref="IBindableVectorVftbl"/> value for the managed <see cref="global::System.Collections.IList"/> implementation.
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
    /// Gets the IID for <see cref="global::System.Collections.IList"/>.
    /// </summary>
    public static ref readonly Guid IID
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref WellKnownInterfaceIds.IID_IBindableVector;
    }

    /// <summary>
    /// Gets a pointer to the managed <see cref="global::System.Collections.IList"/> implementation.
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
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::System.Collections.IList>((ComInterfaceDispatch*)thisPtr);

            object? value = BindableIListAdapter.GetAt(unboxedValue, index);

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
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::System.Collections.IList>((ComInterfaceDispatch*)thisPtr);

            *size = BindableIListAdapter.Size(unboxedValue);

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
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::System.Collections.IList>((ComInterfaceDispatch*)thisPtr);

            BindableIReadOnlyListAdapter adapter = BindableIListAdapter.GetView(unboxedValue);

            *view = BindableIReadOnlyListAdapterMarshaller.ConvertToUnmanaged(adapter).DetachThisPtrUnsafe();

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
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::System.Collections.IList>((ComInterfaceDispatch*)thisPtr);

            object? item = WindowsRuntimeObjectMarshaller.ConvertToManaged(value);

            *result = BindableIListAdapter.IndexOf(unboxedValue, item, out *index);

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
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::System.Collections.IList>((ComInterfaceDispatch*)thisPtr);

            object? item = WindowsRuntimeObjectMarshaller.ConvertToManaged(value);

            BindableIListAdapter.SetAt(unboxedValue, index, item);

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
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::System.Collections.IList>((ComInterfaceDispatch*)thisPtr);

            object? item = WindowsRuntimeObjectMarshaller.ConvertToManaged(value);

            BindableIListAdapter.InsertAt(unboxedValue, index, item);

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
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::System.Collections.IList>((ComInterfaceDispatch*)thisPtr);

            BindableIListAdapter.RemoveAt(unboxedValue, index);

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
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::System.Collections.IList>((ComInterfaceDispatch*)thisPtr);

            object? item = WindowsRuntimeObjectMarshaller.ConvertToManaged(value);

            BindableIListAdapter.Append(unboxedValue, item);

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
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::System.Collections.IList>((ComInterfaceDispatch*)thisPtr);

            BindableIListAdapter.RemoveAtEnd(unboxedValue);

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
            var unboxedValue = ComInterfaceDispatch.GetInstance<global::System.Collections.IList>((ComInterfaceDispatch*)thisPtr);

            BindableIListAdapter.Clear(unboxedValue);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception e)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(e);
        }
    }
}

/// <summary>
/// The <see cref="IDynamicInterfaceCastable"/> implementation for <see cref="global::System.Collections.IList"/>.
/// </summary>
[DynamicInterfaceCastableImplementation]
file interface IListInterfaceImpl : global::System.Collections.IList
{
    /// <inheritdoc/>
    object? global::System.Collections.IList.this[int index]
    {
        get
        {
            var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(global::System.Collections.IList).TypeHandle);

            return BindableIListMethods.Item(thisReference, index);
        }
        set
        {
            var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(global::System.Collections.IList).TypeHandle);

            BindableIListMethods.Item(thisReference, index, value);
        }
    }

    /// <inheritdoc/>
    bool global::System.Collections.IList.IsFixedSize => false;

    /// <inheritdoc/>
    bool global::System.Collections.IList.IsReadOnly => false;

    /// <inheritdoc/>
    int global::System.Collections.IList.Add(object? value)
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(global::System.Collections.IList).TypeHandle);

        return BindableIListMethods.Add(thisReference, value);
    }

    /// <inheritdoc/>
    void global::System.Collections.IList.Clear()
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(global::System.Collections.IList).TypeHandle);

        BindableIListMethods.Clear(thisReference);
    }

    /// <inheritdoc/>
    bool global::System.Collections.IList.Contains(object? value)
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(global::System.Collections.IList).TypeHandle);

        return BindableIListMethods.Contains(thisReference, value);
    }

    /// <inheritdoc/>
    int global::System.Collections.IList.IndexOf(object? value)
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(global::System.Collections.IList).TypeHandle);

        return BindableIListMethods.IndexOf(thisReference, value);
    }

    /// <inheritdoc/>
    void global::System.Collections.IList.Insert(int index, object? value)
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(global::System.Collections.IList).TypeHandle);

        BindableIListMethods.Insert(thisReference, index, value);
    }

    /// <inheritdoc/>
    void global::System.Collections.IList.Remove(object? value)
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(global::System.Collections.IList).TypeHandle);

        BindableIListMethods.Remove(thisReference, value);
    }

    /// <inheritdoc/>
    void global::System.Collections.IList.RemoveAt(int index)
    {
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(global::System.Collections.IList).TypeHandle);

        BindableIListMethods.RemoveAt(thisReference, index);
    }

    /// <inheritdoc/>
    int ICollection.Count
    {
        get
        {
            var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(ICollection).TypeHandle);

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
        var thisReference = ((WindowsRuntimeObject)this).GetObjectReferenceForInterface(typeof(ICollection).TypeHandle);

        BindableIListMethods.CopyTo(thisReference, array, index);
    }
}
