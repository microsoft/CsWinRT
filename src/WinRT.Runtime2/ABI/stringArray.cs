// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Buffers;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;
using WindowsRuntime;
using WindowsRuntime.InteropServices;
using WindowsRuntime.InteropServices.Marshalling;
using static System.Runtime.InteropServices.ComWrappers;

#pragma warning disable IDE0008, IDE1006

[assembly: TypeMap<WindowsRuntimeComWrappersTypeMapGroup>(
    value: "Windows.Foundation.IReferenceArray<HString>",
    target: typeof(ABI.System.string_Array),
    trimTarget: typeof(string[]))]

[assembly: TypeMapAssociation<WindowsRuntimeComWrappersTypeMapGroup>(
    typeof(string[]),
    typeof(ABI.System.string_Array))]

namespace ABI.System;

public interface IMyWinRTInterface
{
    void PassArray(ReadOnlySpan<string> array);
    void FillArray(Span<string> array);
    void ReceiveArray(out string[] array);
    string[] ReceiveArray();
}

public unsafe class MyProjectionClass : IMyWinRTInterface
{
    public void PassArray(ReadOnlySpan<string> array)
    {
        Unsafe.SkipInit(out InlineArray16<nint> inlineArray);
        nint[]? arrayFromPool = null;
        Span<nint> span = array.Length <= 16
            ? inlineArray[..array.Length]
            : (arrayFromPool = ArrayPool<nint>.Shared.Rent(array.Length));

        fixed (nint* spanPtr = span)
        {
            string_ArrayMarshaller.CopyToUnmanaged(array, (uint)span.Length, (void**)spanPtr);

            HRESULT hresult = PassArrayImpl((uint)span.Length, (void**)spanPtr);
        }

        if (arrayFromPool is not null)
        {
            ArrayPool<nint>.Shared.Return(arrayFromPool);
        }
    }

    public void FillArray(Span<string> array)
    {
        Unsafe.SkipInit(out InlineArray16<nint> inlineArray);
        nint[]? arrayFromPool = null;
        Span<nint> span = array.Length <= 16
            ? inlineArray[..array.Length]
            : (arrayFromPool = ArrayPool<nint>.Shared.Rent(array.Length));

        fixed (nint* spanPtr = span)
        {
            HRESULT hresult = PassArrayImpl((uint)span.Length, (void**)spanPtr);

            string_ArrayMarshaller.CopyToManaged(
                (uint)span.Length,
                (void**)spanPtr,
                array);
        }

        if (arrayFromPool is not null)
        {
            ArrayPool<nint>.Shared.Return(arrayFromPool);
        }
    }

    public void ReceiveArray(out string[] array)
    {
        uint size;
        HSTRING* arrayPtr;
        HRESULT hresult = ReceiveArrayImpl(&size, &arrayPtr);

        try
        {
            array = string_ArrayMarshaller.ConvertToManaged(size, arrayPtr);
        }
        finally
        {
            Marshal.FreeCoTaskMem((nint)arrayPtr);
        }
    }

    // Same as above
    public string[] ReceiveArray()
    {
        return null; // TODO
    }

    private static HRESULT PassArrayImpl(uint size, HSTRING* array)
    {
        return WellKnownErrorCodes.S_OK;
    }

    private static HRESULT ReceiveArrayImpl(uint* size, HSTRING** array)
    {
        *size = 0;
        *array = null;

        return WellKnownErrorCodes.S_OK;
    }
}

public unsafe class MyManagedClass
{
    private static HRESULT PassArray(void* thisPtr, uint size, HSTRING* array)
    {
        try
        {
            IMyWinRTInterface myInterface = ComWrappers.ComInterfaceDispatch.GetInstance<IMyWinRTInterface>((ComWrappers.ComInterfaceDispatch*)thisPtr);

            InlineArray16<string> inlineArray = default;
            string[]? arrayFromPool = null;
            Span<string> span = size <= 16
                ? inlineArray
                : (arrayFromPool = ArrayPool<string>.Shared.Rent((int)size)).AsSpan();

            string_ArrayMarshaller.CopyToManaged(size, array, span);

            myInterface.PassArray(span);

            if (arrayFromPool is not null)
            {
                ArrayPool<string>.Shared.Return(arrayFromPool ?? []);
            }

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception ex)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
        }
    }

    private static HRESULT FillArray(void* thisPtr, uint size, HSTRING* array)
    {
        try
        {
            IMyWinRTInterface myInterface = ComWrappers.ComInterfaceDispatch.GetInstance<IMyWinRTInterface>((ComWrappers.ComInterfaceDispatch*)thisPtr);

            InlineArray16<string> inlineArray = default;
            string[]? arrayFromPool = null;
            Span<string> span = size <= 16
                ? inlineArray
                : (arrayFromPool = ArrayPool<string>.Shared.Rent((int)size)).AsSpan();

            myInterface.FillArray(span);

            string_ArrayMarshaller.CopyToUnmanaged(span, size, array);

            if (arrayFromPool is not null)
            {
                ArrayPool<string>.Shared.Return(arrayFromPool);
            }

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception ex)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
        }
    }

    private static HRESULT ReceiveArray(void* thisPtr, uint* size, HSTRING** destination)
    {
        try
        {
            IMyWinRTInterface myInterface = ComWrappers.ComInterfaceDispatch.GetInstance<IMyWinRTInterface>((ComWrappers.ComInterfaceDispatch*)thisPtr);

            myInterface.ReceiveArray(out string[] array);

            string_ArrayMarshaller.ConvertToUnmanaged(array, out *size, out *destination);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception ex)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
        }
    }
}

[WindowsRuntimeClassName("Windows.Foundation.IReferenceArray<HString>")]
[NotifyCollectionChangedEventHandlerComWrappersMarshaller]
file static class string_Array;

public static unsafe class string_ArrayMarshaller
{
    public static void ConvertToUnmanaged(ReadOnlySpan<string> value, out uint size, out HSTRING* array)
    {
        if (value.IsEmpty)
        {
            size = 0;
            array = null;

            return;
        }

        size = (uint)value.Length;
        array = (HSTRING*)Marshal.AllocCoTaskMem(sizeof(void*) * value.Length);
    }

    public static string[] ConvertToManaged(uint size, HSTRING* value)
    {
        if (size == 0)
        {
            return [];
        }

        ArgumentNullException.ThrowIfNull(value);

        string[] result = new string[size];

        for (uint i = 0; i < size; i++)
        {
            result[i] = HStringMarshaller.ConvertToManaged(value[i]);
        }

        return result;
    }

    public static void CopyToManaged(
        uint size,
        HSTRING* value,
        Span<string> destination)
    {
        if (destination.Length != size)
        {
            throw new ArgumentException("Destination array is too small.", nameof(destination));
        }

        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(value);

        for (uint i = 0; i < size; i++)
        {
            destination[(int)i] = HStringMarshaller.ConvertToManaged(value[i]);
        }
    }

    public static void CopyToUnmanaged(
        ReadOnlySpan<string> value,
        uint size,
        HSTRING* destination)
    {
        if (value.Length != size)
        {
            throw new ArgumentException("Destination array is too small.", nameof(value));
        }

        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(destination);

        int i = 0;

        try
        {
            for (; i < size; i++)
            {
                destination[i] = HStringMarshaller.ConvertToUnmanaged(value[i]);
            }
        }
        catch
        {
            for (int j = 0; j < i; j++)
            {
                HStringMarshaller.Free(destination[j]);
            }

            throw;
        }
    }

    public static void Free(uint size, HSTRING* value)
    {
        if (size == 0)
        {
            return;
        }

        ArgumentNullException.ThrowIfNull(value);

        for (int i = 0; i < size; i++)
        {
            HStringMarshaller.Free(value[i]);
        }

        NativeMemory.Free(value);
    }
}

/// <summary>
/// A custom <see cref="IWindowsRuntimeObjectComWrappersCallback"/> implementation for <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventHandler"/>.
/// </summary>
file abstract unsafe class NotifyCollectionChangedEventHandlerComWrappersCallback : IWindowsRuntimeObjectComWrappersCallback
{
    /// <inheritdoc/>
    public static object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        wrapperFlags = CreatedWrapperFlags.NonWrapping;

        return null!; // TODO
    }
}

/// <summary>
/// The set of <see cref="ComInterfaceEntry"/> values for <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventHandler"/>.
/// </summary>
file struct NotifyCollectionChangedEventHandlerInterfaceEntries
{
    public ComInterfaceEntry NotifyCollectionChangedEventHandler;
    public ComInterfaceEntry IReferenceOfNotifyCollectionChangedEventHandler;
    public ComInterfaceEntry IPropertyValue;
    public ComInterfaceEntry IStringable;
    public ComInterfaceEntry IWeakReferenceSource;
    public ComInterfaceEntry IMarshal;
    public ComInterfaceEntry IAgileObject;
    public ComInterfaceEntry IInspectable;
    public ComInterfaceEntry IUnknown;
}

/// <summary>
/// The implementation of <see cref="NotifyCollectionChangedEventHandlerInterfaceEntries"/>.
/// </summary>
file static class NotifyCollectionChangedEventHandlerInterfaceEntriesImpl
{
    /// <summary>
    /// The <see cref="NotifyCollectionChangedEventHandlerInterfaceEntries"/> value for <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventHandler"/>.
    /// </summary>
    [FixedAddressValueType]
    public static readonly NotifyCollectionChangedEventHandlerInterfaceEntries Entries;

    /// <summary>
    /// Initializes <see cref="Entries"/>.
    /// </summary>
    static NotifyCollectionChangedEventHandlerInterfaceEntriesImpl()
    {
        Entries.IReferenceOfNotifyCollectionChangedEventHandler.IID = NotifyCollectionChangedEventHandlerReferenceImpl.IID;
        Entries.IReferenceOfNotifyCollectionChangedEventHandler.Vtable = NotifyCollectionChangedEventHandlerReferenceImpl.Vtable;
        Entries.IPropertyValue.IID = WellKnownInterfaceIds.IID_IPropertyValue;
        Entries.IPropertyValue.Vtable = IPropertyValueImpl.OtherTypeVtable;
        Entries.IStringable.IID = WellKnownInterfaceIds.IID_IStringable;
        Entries.IStringable.Vtable = IStringableImpl.Vtable;
        Entries.IWeakReferenceSource.IID = WellKnownInterfaceIds.IID_IWeakReferenceSource;
        Entries.IWeakReferenceSource.Vtable = IWeakReferenceSourceImpl.Vtable;
        Entries.IMarshal.IID = WellKnownInterfaceIds.IID_IMarshal;
        Entries.IMarshal.Vtable = IMarshalImpl.Vtable;
        Entries.IAgileObject.IID = WellKnownInterfaceIds.IID_IAgileObject;
        Entries.IAgileObject.Vtable = IUnknownImpl.Vtable;
        Entries.IInspectable.IID = WellKnownInterfaceIds.IID_IInspectable;
        Entries.IInspectable.Vtable = IInspectableImpl.Vtable;
        Entries.IUnknown.IID = WellKnownInterfaceIds.IID_IUnknown;
        Entries.IUnknown.Vtable = IUnknownImpl.Vtable;
    }
}

/// <summary>
/// A custom <see cref="WindowsRuntimeComWrappersMarshallerAttribute"/> implementation for <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventHandler"/>.
/// </summary>
file sealed unsafe class NotifyCollectionChangedEventHandlerComWrappersMarshallerAttribute : WindowsRuntimeComWrappersMarshallerAttribute
{
    /// <inheritdoc/>
    public override void* GetOrCreateComInterfaceForObject(object value)
    {
        return (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(value, CreateComInterfaceFlags.TrackerSupport);
    }

    /// <inheritdoc/>
    public override ComInterfaceEntry* ComputeVtables(out int count)
    {
        count = sizeof(NotifyCollectionChangedEventHandlerInterfaceEntries) / sizeof(ComInterfaceEntry);

        return (ComInterfaceEntry*)Unsafe.AsPointer(in NotifyCollectionChangedEventHandlerInterfaceEntriesImpl.Entries);
    }

    /// <inheritdoc/>
    public override object CreateObject(void* value, out CreatedWrapperFlags wrapperFlags)
    {
        wrapperFlags = CreatedWrapperFlags.NonWrapping;

        // TODO
        // return WindowsRuntimeArrayMarshaller.UnboxToManaged<string_ArrayComWrappersCallback>(
        //     value,
        //     int string_Array.IID);

        wrapperFlags = CreatedWrapperFlags.NonWrapping;

        return WindowsRuntimeDelegateMarshaller.UnboxToManaged<NotifyCollectionChangedEventHandlerComWrappersCallback>(value, in NotifyCollectionChangedEventHandlerReferenceImpl.IID)!;
    }
}

/// <summary>
/// Binding type for the <c>IReference`1</c> implementation for <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventHandler"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
file unsafe struct NotifyCollectionChangedEventHandlerReferenceVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, void**, HRESULT> get_Value;
}

/// <summary>
/// The <c>IReference`1</c> implementation for <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventHandler"/>.
/// </summary>
file static unsafe class NotifyCollectionChangedEventHandlerReferenceImpl
{
    /// <summary>
    /// The <see cref="NotifyCollectionChangedEventHandlerReferenceVftbl"/> value for the managed <c>IReference`1</c> implementation.
    /// </summary>
    [FixedAddressValueType]
    private static readonly NotifyCollectionChangedEventHandlerReferenceVftbl Vftbl;

    /// <summary>
    /// Initializes <see cref="Vftbl"/>.
    /// </summary>
    static NotifyCollectionChangedEventHandlerReferenceImpl()
    {
        *(IInspectableVftbl*)Unsafe.AsPointer(ref Vftbl) = *(IInspectableVftbl*)IInspectableImpl.Vtable;

        //Vftbl.get_Value = &get_Value;
    }

    /// <summary>
    /// Gets the IID for The IID for <c>IReference`1</c> of <see cref="global::System.Collections.Specialized.NotifyCollectionChangedEventHandler"/>.
    /// </summary>
    public static ref readonly Guid IID => ref WindowsRuntimeFeatureSwitches.UseWindowsUIXamlProjections
        ? ref WellKnownInterfaceIds.IID_WUX_IReferenceOfNotifyCollectionChangedEventHandler
        : ref WellKnownInterfaceIds.IID_MUX_IReferenceOfNotifyCollectionChangedEventHandler;

    /// <summary>
    /// Gets a pointer to the managed <c>IReference`1</c> implementation.
    /// </summary>
    public static nint Vtable
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => (nint)Unsafe.AsPointer(in Vftbl);
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ireference-1.value"/>
    private static HRESULT Value(void* thisPtr, uint* size, HSTRING** array)
    {
        try
        {
            var unboxedValue = ComInterfaceDispatch.GetInstance<string[]>((ComInterfaceDispatch*)thisPtr);

            string_ArrayMarshaller.ConvertToUnmanaged(unboxedValue, out *size, out *array);

            return WellKnownErrorCodes.S_OK;
        }
        catch (global::System.Exception ex)
        {
            return RestrictedErrorInfoExceptionMarshaller.ConvertToUnmanaged(ex);
        }
    }
}
