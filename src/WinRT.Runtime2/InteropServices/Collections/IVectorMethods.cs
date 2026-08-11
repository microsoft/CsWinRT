// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using WindowsRuntime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Methods for <c>Windows.Foundation.Collections.IVector&lt;T&gt;</c> types.
/// </summary>
[WindowsRuntimeImplementationOnlyMember]
public static unsafe class IVectorMethods
{
    private const int GetManyBufferLength = 64;

    /// <summary>
    /// Gets the number of items in the vector.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <returns>The number of items in the vector.</returns>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1.size"/>
    public static uint Size(WindowsRuntimeObjectReference thisReference)
    {
        // The vtable slot for 'get_Size' is identical between 'IVector<T>' and 'IVectorView<T>'
        return IVectorViewMethods.Size(thisReference);
    }

    /// <summary>
    /// Copies blittable elements from a vector through its <c>GetMany</c> ABI method.
    /// </summary>
    public static int GetMany<T>(WindowsRuntimeObjectReference thisReference, T[] array, int arrayIndex, int count)
        where T : unmanaged
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        int copied = 0;

        fixed (T* destination = &array[arrayIndex])
        {
            while (copied < count)
            {
                uint requested = (uint)(count - copied);
                uint actual;

                RestrictedErrorInfo.ThrowExceptionForHR(
                    InvokeGetMany(thisPtr, (uint)copied, requested, destination + copied, &actual));

                copied += (int)actual;

                if (actual < requested)
                {
                    break;
                }
            }
        }

        return copied;
    }

    /// <summary>
    /// Copies string elements from a vector through its <c>GetMany</c> ABI method.
    /// </summary>
    public static int GetManyStrings(WindowsRuntimeObjectReference thisReference, string[] array, int arrayIndex, int count)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        Span<nint> handles = stackalloc nint[GetManyBufferLength];
        int copied = 0;

        fixed (nint* handlesPtr = handles)
        {
            while (copied < count)
            {
                uint requested = (uint)int.Min(GetManyBufferLength, count - copied);
                uint actual;

                handles.Clear();

                RestrictedErrorInfo.ThrowExceptionForHR(
                    InvokeGetMany(thisPtr, (uint)copied, requested, handlesPtr, &actual));

                try
                {
                    for (int i = 0; i < actual; i++)
                    {
                        array[arrayIndex + copied + i] = HStringMarshaller.ConvertToManaged((void*)handles[i]);
                    }
                }
                finally
                {
                    for (int i = 0; i < actual; i++)
                    {
                        HStringMarshaller.Free((void*)handles[i]);
                    }
                }

                copied += (int)actual;

                if (actual < requested)
                {
                    break;
                }
            }
        }

        return copied;
    }

    /// <summary>
    /// Copies projected reference elements from a vector through its <c>GetMany</c> ABI method.
    /// </summary>
    public static int GetManyReferences<T, TElementMarshaller>(WindowsRuntimeObjectReference thisReference, T[] array, int arrayIndex, int count)
        where T : class
        where TElementMarshaller : IWindowsRuntimeReferenceTypeElementMarshaller<T>
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        Span<nint> nativeValues = stackalloc nint[GetManyBufferLength];
        int copied = 0;

        fixed (nint* nativeValuesPtr = nativeValues)
        {
            while (copied < count)
            {
                uint requested = (uint)int.Min(GetManyBufferLength, count - copied);
                uint actual;

                nativeValues.Clear();

                RestrictedErrorInfo.ThrowExceptionForHR(
                    InvokeGetMany(thisPtr, (uint)copied, requested, nativeValuesPtr, &actual));

                try
                {
                    for (int i = 0; i < actual; i++)
                    {
                        array[arrayIndex + copied + i] = TElementMarshaller.ConvertToManaged((void*)nativeValues[i])!;
                    }
                }
                finally
                {
                    for (int i = 0; i < actual; i++)
                    {
                        TElementMarshaller.Dispose((void*)nativeValues[i]);
                    }
                }

                copied += (int)actual;

                if (actual < requested)
                {
                    break;
                }
            }
        }

        return copied;
    }

    /// <summary>
    /// Copies ABI-transformed unmanaged values from a vector through its <c>GetMany</c> ABI method.
    /// </summary>
    public static int GetManyUnmanagedValues<T, TAbi, TElementMarshaller>(WindowsRuntimeObjectReference thisReference, T[] array, int arrayIndex, int count)
        where T : unmanaged
        where TAbi : unmanaged
        where TElementMarshaller : IWindowsRuntimeUnmanagedValueTypeElementMarshaller<T, TAbi>
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        Span<TAbi> nativeValues = stackalloc TAbi[GetManyBufferLength];
        int copied = 0;

        fixed (TAbi* nativeValuesPtr = nativeValues)
        {
            while (copied < count)
            {
                uint requested = (uint)int.Min(GetManyBufferLength, count - copied);
                uint actual;

                RestrictedErrorInfo.ThrowExceptionForHR(
                    InvokeGetMany(thisPtr, (uint)copied, requested, nativeValuesPtr, &actual));

                for (int i = 0; i < actual; i++)
                {
                    array[arrayIndex + copied + i] = TElementMarshaller.ConvertToManaged(nativeValues[i]);
                }

                copied += (int)actual;

                if (actual < requested)
                {
                    break;
                }
            }
        }

        return copied;
    }

    /// <summary>
    /// Copies managed value types from a vector through its <c>GetMany</c> ABI method.
    /// </summary>
    public static int GetManyManagedValues<T, TAbi, TElementMarshaller>(WindowsRuntimeObjectReference thisReference, T[] array, int arrayIndex, int count)
        where T : struct
        where TAbi : unmanaged
        where TElementMarshaller : IWindowsRuntimeManagedValueTypeElementMarshaller<T, TAbi>
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        Span<TAbi> nativeValues = stackalloc TAbi[GetManyBufferLength];
        int copied = 0;

        fixed (TAbi* nativeValuesPtr = nativeValues)
        {
            while (copied < count)
            {
                uint requested = (uint)int.Min(GetManyBufferLength, count - copied);
                uint actual;

                nativeValues.Clear();

                RestrictedErrorInfo.ThrowExceptionForHR(
                    InvokeGetMany(thisPtr, (uint)copied, requested, nativeValuesPtr, &actual));

                try
                {
                    for (int i = 0; i < actual; i++)
                    {
                        array[arrayIndex + copied + i] = TElementMarshaller.ConvertToManaged(nativeValues[i]);
                    }
                }
                finally
                {
                    for (int i = 0; i < actual; i++)
                    {
                        TElementMarshaller.Dispose(nativeValues[i]);
                    }
                }

                copied += (int)actual;

                if (actual < requested)
                {
                    break;
                }
            }
        }

        return copied;
    }

    /// <summary>
    /// Copies nullable values from a vector through its <c>GetMany</c> ABI method.
    /// </summary>
    public static int GetManyNullable<T, TElementMarshaller>(WindowsRuntimeObjectReference thisReference, T?[] array, int arrayIndex, int count)
        where T : struct
        where TElementMarshaller : IWindowsRuntimeNullableTypeElementMarshaller<T>
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        Span<nint> nativeValues = stackalloc nint[GetManyBufferLength];
        int copied = 0;

        fixed (nint* nativeValuesPtr = nativeValues)
        {
            while (copied < count)
            {
                uint requested = (uint)int.Min(GetManyBufferLength, count - copied);
                uint actual;

                nativeValues.Clear();

                RestrictedErrorInfo.ThrowExceptionForHR(
                    InvokeGetMany(thisPtr, (uint)copied, requested, nativeValuesPtr, &actual));

                try
                {
                    for (int i = 0; i < actual; i++)
                    {
                        array[arrayIndex + copied + i] = TElementMarshaller.ConvertToManaged((void*)nativeValues[i]);
                    }
                }
                finally
                {
                    for (int i = 0; i < actual; i++)
                    {
                        TElementMarshaller.Dispose((void*)nativeValues[i]);
                    }
                }

                copied += (int)actual;

                if (actual < requested)
                {
                    break;
                }
            }
        }

        return copied;
    }

    /// <summary>
    /// Copies key/value pairs from a vector through its <c>GetMany</c> ABI method.
    /// </summary>
    public static int GetManyKeyValuePairs<TKey, TValue, TElementMarshaller>(
        WindowsRuntimeObjectReference thisReference,
        System.Collections.Generic.KeyValuePair<TKey, TValue>[] array,
        int arrayIndex,
        int count)
        where TElementMarshaller : IWindowsRuntimeKeyValuePairTypeElementMarshaller<TKey, TValue>
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        Span<nint> nativeValues = stackalloc nint[GetManyBufferLength];
        int copied = 0;

        fixed (nint* nativeValuesPtr = nativeValues)
        {
            while (copied < count)
            {
                uint requested = (uint)int.Min(GetManyBufferLength, count - copied);
                uint actual;

                nativeValues.Clear();

                RestrictedErrorInfo.ThrowExceptionForHR(
                    InvokeGetMany(thisPtr, (uint)copied, requested, nativeValuesPtr, &actual));

                try
                {
                    for (int i = 0; i < actual; i++)
                    {
                        array[arrayIndex + copied + i] = TElementMarshaller.ConvertToManaged((void*)nativeValues[i]);
                    }
                }
                finally
                {
                    for (int i = 0; i < actual; i++)
                    {
                        TElementMarshaller.Dispose((void*)nativeValues[i]);
                    }
                }

                copied += (int)actual;

                if (actual < requested)
                {
                    break;
                }
            }
        }

        return copied;
    }

    /// <summary>
    /// Copies object elements from a vector through its <c>GetMany</c> ABI method.
    /// </summary>
    public static int GetManyObjects(WindowsRuntimeObjectReference thisReference, object[] array, int arrayIndex, int count)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        Span<nint> nativeValues = stackalloc nint[GetManyBufferLength];
        int copied = 0;

        fixed (nint* nativeValuesPtr = nativeValues)
        {
            while (copied < count)
            {
                uint requested = (uint)int.Min(GetManyBufferLength, count - copied);
                uint actual;

                nativeValues.Clear();

                RestrictedErrorInfo.ThrowExceptionForHR(
                    InvokeGetMany(thisPtr, (uint)copied, requested, nativeValuesPtr, &actual));

                try
                {
                    for (int i = 0; i < actual; i++)
                    {
                        array[arrayIndex + copied + i] = WindowsRuntimeObjectMarshaller.ConvertToManaged((void*)nativeValues[i])!;
                    }
                }
                finally
                {
                    for (int i = 0; i < actual; i++)
                    {
                        WindowsRuntimeUnknownMarshaller.Free((void*)nativeValues[i]);
                    }
                }

                copied += (int)actual;

                if (actual < requested)
                {
                    break;
                }
            }
        }

        return copied;
    }

    /// <summary>
    /// Copies <see cref="Type"/> elements from a vector through its <c>GetMany</c> ABI method.
    /// </summary>
    public static int GetManyTypes(WindowsRuntimeObjectReference thisReference, Type[] array, int arrayIndex, int count)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        ABI.System.Type* nativeValues = stackalloc ABI.System.Type[GetManyBufferLength];
        int copied = 0;

        while (copied < count)
        {
            uint requested = (uint)int.Min(GetManyBufferLength, count - copied);
            uint actual;

            RestrictedErrorInfo.ThrowExceptionForHR(
                InvokeGetMany(thisPtr, (uint)copied, requested, nativeValues, &actual));

            try
            {
                for (int i = 0; i < actual; i++)
                {
                    array[arrayIndex + copied + i] = ABI.System.TypeMarshaller.ConvertToManaged(nativeValues[i])!;
                }
            }
            finally
            {
                for (int i = 0; i < actual; i++)
                {
                    ABI.System.TypeMarshaller.Dispose(nativeValues[i]);
                }
            }

            copied += (int)actual;

            if (actual < requested)
            {
                break;
            }
        }

        return copied;
    }

    /// <summary>
    /// Copies <see cref="Exception"/> elements from a vector through its <c>GetMany</c> ABI method.
    /// </summary>
    public static int GetManyExceptions(WindowsRuntimeObjectReference thisReference, Exception[] array, int arrayIndex, int count)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();
        ABI.System.Exception* nativeValues = stackalloc ABI.System.Exception[GetManyBufferLength];
        int copied = 0;

        while (copied < count)
        {
            uint requested = (uint)int.Min(GetManyBufferLength, count - copied);
            uint actual;

            RestrictedErrorInfo.ThrowExceptionForHR(
                InvokeGetMany(thisPtr, (uint)copied, requested, nativeValues, &actual));

            for (int i = 0; i < actual; i++)
            {
                array[arrayIndex + copied + i] = ABI.System.ExceptionMarshaller.ConvertToManaged(nativeValues[i])!;
            }

            copied += (int)actual;

            if (actual < requested)
            {
                break;
            }
        }

        return copied;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static HRESULT InvokeGetMany(void* thisPtr, uint startIndex, uint capacity, void* items, uint* actual)
    {
        return ((delegate* unmanaged[MemberFunction]<void*, uint, uint, void*, uint*, HRESULT>)(*(void***)thisPtr)[16])(
            thisPtr,
            startIndex,
            capacity,
            items,
            actual);
    }

    /// <summary>
    /// Removes the item at the specified index in the vector.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <param name="index">The zero-based index of the vector item to remove.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1.removeat"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void RemoveAt(WindowsRuntimeObjectReference thisReference, uint index)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();

        RestrictedErrorInfo.ThrowExceptionForHR(((IVectorVftbl*)*(void***)thisPtr)->RemoveAt(thisPtr, index));
    }

    /// <summary>
    /// Removes all items from the vector.
    /// </summary>
    /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
    /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1.clear"/>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Clear(WindowsRuntimeObjectReference thisReference)
    {
        using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

        void* thisPtr = thisValue.GetThisPtrUnsafe();

        RestrictedErrorInfo.ThrowExceptionForHR(((IVectorVftbl*)*(void***)thisPtr)->Clear(thisPtr));
    }
}
