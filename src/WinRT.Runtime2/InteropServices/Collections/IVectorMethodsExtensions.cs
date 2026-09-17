// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using WindowsRuntime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Extensions for the <see cref="IVectorMethods"/> type.
/// </summary>
[WindowsRuntimeImplementationOnlyMember]
public static unsafe class IVectorMethodsExtensions
{
    // Note: all the 'GetMany' extensions in this file share the same structure. Except for the blittable
    // specialization, which can retrieve items straight into the target array, they retrieve the requested
    // items from the native vector in batches, into a stack buffer, and then marshal each batch to managed.
    // They can't be shared because each one needs a different ABI buffer type and marshalling logic, and
    // sharing code for all of them would require some additional abstraction on top which would in turn
    // increase overhead. To avoid that, we just keep a separate version of the code for each of them. Any
    // changes to these methods should be kept in sync.

    /// <summary>
    /// The maximum number of items to retrieve from a vector on each <c>GetMany</c> ABI call.
    /// </summary>
    internal const int GetManyBufferLength = 64;

    extension(IVectorMethods)
    {
        /// <summary>
        /// Retrieves multiple items from the vector, starting from the first one, and copies them to a target array.
        /// </summary>
        /// <param name="thisReference">The <see cref="WindowsRuntimeObjectReference"/> instance to use to invoke the native method.</param>
        /// <param name="array">The target array to copy the retrieved items to.</param>
        /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> to start copying to.</param>
        /// <param name="count">The number of items to retrieve from the vector.</param>
        /// <returns>The number of items that were retrieved. This value can be less than <paramref name="count"/> if the end of the vector is reached.</returns>
        /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1.getmany"/>
        public static int GetMany(WindowsRuntimeObjectReference thisReference, string[] array, int arrayIndex, int count)
        {
            using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

            void* thisPtr = thisValue.GetThisPtrUnsafe();
            HSTRING* items = stackalloc HSTRING[GetManyBufferLength];
            int copied = 0;

            while (copied < count)
            {
                uint capacity = (uint)int.Min(GetManyBufferLength, count - copied);
                uint actual;

                RestrictedErrorInfo.ThrowExceptionForHR(IVectorVftbl.GetManyUnsafe(thisPtr, (uint)copied, capacity, items, &actual));

                try
                {
                    // Marshal all retrieved items into the target array
                    for (int i = 0; i < actual; i++)
                    {
                        array[arrayIndex + copied + i] = HStringMarshaller.ConvertToManaged(items[i]);
                    }
                }
                finally
                {
                    // Make sure to release all retrieved items, even if marshalling failed (this shouldn't ever throw)
                    for (int i = 0; i < actual; i++)
                    {
                        HStringMarshaller.Free(items[i]);
                    }
                }

                copied += (int)actual;

                // If the vector returned fewer items than requested, we reached the end of the collection
                if (actual < capacity)
                {
                    break;
                }
            }

            return copied;
        }

        /// <inheritdoc cref="GetMany(WindowsRuntimeObjectReference, string[], int, int)"/>
        public static int GetMany(WindowsRuntimeObjectReference thisReference, object[] array, int arrayIndex, int count)
        {
            using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

            void* thisPtr = thisValue.GetThisPtrUnsafe();
            void** items = stackalloc void*[GetManyBufferLength];
            int copied = 0;

            while (copied < count)
            {
                uint capacity = (uint)int.Min(GetManyBufferLength, count - copied);
                uint actual;

                RestrictedErrorInfo.ThrowExceptionForHR(IVectorVftbl.GetManyUnsafe(thisPtr, (uint)copied, capacity, items, &actual));

                try
                {
                    for (int i = 0; i < actual; i++)
                    {
                        array[arrayIndex + copied + i] = WindowsRuntimeObjectMarshaller.ConvertToManaged(items[i])!;
                    }
                }
                finally
                {
                    for (int i = 0; i < actual; i++)
                    {
                        WindowsRuntimeUnknownMarshaller.Free(items[i]);
                    }
                }

                copied += (int)actual;

                if (actual < capacity)
                {
                    break;
                }
            }

            return copied;
        }

        /// <inheritdoc cref="GetMany(WindowsRuntimeObjectReference, string[], int, int)"/>
        public static int GetMany(WindowsRuntimeObjectReference thisReference, Exception[] array, int arrayIndex, int count)
        {
            using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

            void* thisPtr = thisValue.GetThisPtrUnsafe();
            ABI.System.Exception* items = stackalloc ABI.System.Exception[GetManyBufferLength];
            int copied = 0;

            while (copied < count)
            {
                uint capacity = (uint)int.Min(GetManyBufferLength, count - copied);
                uint actual;

                RestrictedErrorInfo.ThrowExceptionForHR(IVectorVftbl.GetManyUnsafe(thisPtr, (uint)copied, capacity, items, &actual));

                // Exception values are just 'HRESULT'-s, so there's nothing to release after marshalling
                for (int i = 0; i < actual; i++)
                {
                    array[arrayIndex + copied + i] = ABI.System.ExceptionMarshaller.ConvertToManaged(items[i])!;
                }

                copied += (int)actual;

                if (actual < capacity)
                {
                    break;
                }
            }

            return copied;
        }

        /// <inheritdoc cref="GetMany(WindowsRuntimeObjectReference, string[], int, int)"/>
        public static int GetMany(WindowsRuntimeObjectReference thisReference, Type[] array, int arrayIndex, int count)
        {
            using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

            void* thisPtr = thisValue.GetThisPtrUnsafe();
            ABI.System.Type* items = stackalloc ABI.System.Type[GetManyBufferLength];
            int copied = 0;

            while (copied < count)
            {
                uint capacity = (uint)int.Min(GetManyBufferLength, count - copied);
                uint actual;

                RestrictedErrorInfo.ThrowExceptionForHR(IVectorVftbl.GetManyUnsafe(thisPtr, (uint)copied, capacity, items, &actual));

                try
                {
                    // Same as with 'string' above, but with the 'Type' marshaller
                    for (int i = 0; i < actual; i++)
                    {
                        array[arrayIndex + copied + i] = ABI.System.TypeMarshaller.ConvertToManaged(items[i])!;
                    }
                }
                finally
                {
                    // Make sure to dispose all retrieved values (this shouldn't ever throw)
                    for (int i = 0; i < actual; i++)
                    {
                        ABI.System.TypeMarshaller.Dispose(items[i]);
                    }
                }

                copied += (int)actual;

                if (actual < capacity)
                {
                    break;
                }
            }

            return copied;
        }
    }
}

/// <summary>
/// Extensions for the <see cref="IVectorMethods"/> type for blittable value types.
/// </summary>
[WindowsRuntimeImplementationOnlyMember]
public static unsafe class IVectorMethodsBlittableValueTypeExtensions
{
    extension<T>(IVectorMethods)
        where T : unmanaged
    {
        /// <inheritdoc cref="IVectorMethodsExtensions.GetMany(WindowsRuntimeObjectReference, string[], int, int)"/>
        public static int GetMany(WindowsRuntimeObjectReference thisReference, T[] array, int arrayIndex, int count)
        {
            using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

            void* thisPtr = thisValue.GetThisPtrUnsafe();
            int copied = 0;

            // Blittable items don't need any marshalling, so we can retrieve them
            // directly into the target array, with no intermediate stack buffer.
            fixed (T* items = array)
            {
                while (copied < count)
                {
                    uint capacity = (uint)(count - copied);
                    uint actual;

                    RestrictedErrorInfo.ThrowExceptionForHR(IVectorVftbl.GetManyUnsafe(thisPtr, (uint)copied, capacity, items + arrayIndex + copied, &actual));

                    copied += (int)actual;

                    if (actual < capacity)
                    {
                        break;
                    }
                }
            }

            return copied;
        }
    }
}

/// <summary>
/// Extensions for the <see cref="IVectorMethods"/> type for unmanaged value types.
/// </summary>
[WindowsRuntimeImplementationOnlyMember]
public static unsafe class IVectorMethodsUnmanagedValueTypeExtensions
{
    extension<T, TAbi>(IVectorMethods)
        where T : unmanaged
        where TAbi : unmanaged
    {
        /// <inheritdoc cref="IVectorMethodsExtensions.GetMany(WindowsRuntimeObjectReference, string[], int, int)"/>
        public static int GetMany<TElementMarshaller>(WindowsRuntimeObjectReference thisReference, T[] array, int arrayIndex, int count)
            where TElementMarshaller : IWindowsRuntimeUnmanagedValueTypeElementMarshaller<T, TAbi>
        {
            using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

            void* thisPtr = thisValue.GetThisPtrUnsafe();
            TAbi* items = stackalloc TAbi[IVectorMethodsExtensions.GetManyBufferLength];
            int copied = 0;

            while (copied < count)
            {
                uint capacity = (uint)int.Min(IVectorMethodsExtensions.GetManyBufferLength, count - copied);
                uint actual;

                RestrictedErrorInfo.ThrowExceptionForHR(IVectorVftbl.GetManyUnsafe(thisPtr, (uint)copied, capacity, items, &actual));

                // Unmanaged value types have no resources to release after marshalling
                for (int i = 0; i < actual; i++)
                {
                    array[arrayIndex + copied + i] = TElementMarshaller.ConvertToManaged(items[i]);
                }

                copied += (int)actual;

                if (actual < capacity)
                {
                    break;
                }
            }

            return copied;
        }
    }
}

/// <summary>
/// Extensions for the <see cref="IVectorMethods"/> type for managed value types.
/// </summary>
[WindowsRuntimeImplementationOnlyMember]
public static unsafe class IVectorMethodsManagedValueTypeExtensions
{
    extension<T, TAbi>(IVectorMethods)
        where T : struct
        where TAbi : unmanaged
    {
        /// <inheritdoc cref="IVectorMethodsExtensions.GetMany(WindowsRuntimeObjectReference, string[], int, int)"/>
        public static int GetMany<TElementMarshaller>(WindowsRuntimeObjectReference thisReference, T[] array, int arrayIndex, int count)
            where TElementMarshaller : IWindowsRuntimeManagedValueTypeElementMarshaller<T, TAbi>
        {
            using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

            void* thisPtr = thisValue.GetThisPtrUnsafe();
            TAbi* items = stackalloc TAbi[IVectorMethodsExtensions.GetManyBufferLength];
            int copied = 0;

            while (copied < count)
            {
                uint capacity = (uint)int.Min(IVectorMethodsExtensions.GetManyBufferLength, count - copied);
                uint actual;

                RestrictedErrorInfo.ThrowExceptionForHR(IVectorVftbl.GetManyUnsafe(thisPtr, (uint)copied, capacity, items, &actual));

                try
                {
                    // Same as with 'string' above, but with the provided marshaller
                    for (int i = 0; i < actual; i++)
                    {
                        array[arrayIndex + copied + i] = TElementMarshaller.ConvertToManaged(items[i]);
                    }
                }
                finally
                {
                    // Make sure to dispose all retrieved values (this shouldn't ever throw)
                    for (int i = 0; i < actual; i++)
                    {
                        TElementMarshaller.Dispose(items[i]);
                    }
                }

                copied += (int)actual;

                if (actual < capacity)
                {
                    break;
                }
            }

            return copied;
        }
    }
}

/// <summary>
/// Extensions for the <see cref="IVectorMethods"/> type for <see cref="KeyValuePair{TKey, TValue}"/> types.
/// </summary>
[WindowsRuntimeImplementationOnlyMember]
public static unsafe class IVectorMethodsKeyValuePairTypeExtensions
{
    extension<TKey, TValue>(IVectorMethods)
    {
        /// <inheritdoc cref="IVectorMethodsExtensions.GetMany(WindowsRuntimeObjectReference, string[], int, int)"/>
        public static int GetMany<TElementMarshaller>(WindowsRuntimeObjectReference thisReference, KeyValuePair<TKey, TValue>[] array, int arrayIndex, int count)
            where TElementMarshaller : IWindowsRuntimeKeyValuePairTypeElementMarshaller<TKey, TValue>
        {
            using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

            void* thisPtr = thisValue.GetThisPtrUnsafe();
            void** items = stackalloc void*[IVectorMethodsExtensions.GetManyBufferLength];
            int copied = 0;

            while (copied < count)
            {
                uint capacity = (uint)int.Min(IVectorMethodsExtensions.GetManyBufferLength, count - copied);
                uint actual;

                RestrictedErrorInfo.ThrowExceptionForHR(IVectorVftbl.GetManyUnsafe(thisPtr, (uint)copied, capacity, items, &actual));

                try
                {
                    // Same as with 'string' above, but with the provided marshaller
                    for (int i = 0; i < actual; i++)
                    {
                        array[arrayIndex + copied + i] = TElementMarshaller.ConvertToManaged(items[i]);
                    }
                }
                finally
                {
                    // Make sure to release all retrieved values (this shouldn't ever throw)
                    for (int i = 0; i < actual; i++)
                    {
                        TElementMarshaller.Dispose(items[i]);
                    }
                }

                copied += (int)actual;

                if (actual < capacity)
                {
                    break;
                }
            }

            return copied;
        }
    }
}

/// <summary>
/// Extensions for the <see cref="IVectorMethods"/> type for <see cref="Nullable{T}"/> types.
/// </summary>
[WindowsRuntimeImplementationOnlyMember]
public static unsafe class IVectorMethodsNullableTypeExtensions
{
    extension<T>(IVectorMethods)
        where T : struct
    {
        /// <inheritdoc cref="IVectorMethodsExtensions.GetMany(WindowsRuntimeObjectReference, string[], int, int)"/>
        public static int GetMany<TElementMarshaller>(WindowsRuntimeObjectReference thisReference, T?[] array, int arrayIndex, int count)
            where TElementMarshaller : IWindowsRuntimeNullableTypeElementMarshaller<T>
        {
            using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

            void* thisPtr = thisValue.GetThisPtrUnsafe();
            void** items = stackalloc void*[IVectorMethodsExtensions.GetManyBufferLength];
            int copied = 0;

            while (copied < count)
            {
                uint capacity = (uint)int.Min(IVectorMethodsExtensions.GetManyBufferLength, count - copied);
                uint actual;

                RestrictedErrorInfo.ThrowExceptionForHR(IVectorVftbl.GetManyUnsafe(thisPtr, (uint)copied, capacity, items, &actual));

                try
                {
                    // Same as with 'string' above, but with the provided marshaller
                    for (int i = 0; i < actual; i++)
                    {
                        array[arrayIndex + copied + i] = TElementMarshaller.ConvertToManaged(items[i]);
                    }
                }
                finally
                {
                    // Make sure to release all retrieved values (this shouldn't ever throw)
                    for (int i = 0; i < actual; i++)
                    {
                        TElementMarshaller.Dispose(items[i]);
                    }
                }

                copied += (int)actual;

                if (actual < capacity)
                {
                    break;
                }
            }

            return copied;
        }
    }
}

/// <summary>
/// Extensions for the <see cref="IVectorMethods"/> type for reference types.
/// </summary>
[WindowsRuntimeImplementationOnlyMember]
public static unsafe class IVectorMethodsReferenceTypeExtensions
{
    extension<T>(IVectorMethods)
        where T : class
    {
        /// <inheritdoc cref="IVectorMethodsExtensions.GetMany(WindowsRuntimeObjectReference, string[], int, int)"/>
        public static int GetMany<TElementMarshaller>(WindowsRuntimeObjectReference thisReference, T[] array, int arrayIndex, int count)
            where TElementMarshaller : IWindowsRuntimeReferenceTypeElementMarshaller<T>
        {
            using WindowsRuntimeObjectReferenceValue thisValue = thisReference.AsValue();

            void* thisPtr = thisValue.GetThisPtrUnsafe();
            void** items = stackalloc void*[IVectorMethodsExtensions.GetManyBufferLength];
            int copied = 0;

            while (copied < count)
            {
                uint capacity = (uint)int.Min(IVectorMethodsExtensions.GetManyBufferLength, count - copied);
                uint actual;

                RestrictedErrorInfo.ThrowExceptionForHR(IVectorVftbl.GetManyUnsafe(thisPtr, (uint)copied, capacity, items, &actual));

                try
                {
                    for (int i = 0; i < actual; i++)
                    {
                        array[arrayIndex + copied + i] = TElementMarshaller.ConvertToManaged(items[i])!;
                    }
                }
                finally
                {
                    for (int i = 0; i < actual; i++)
                    {
                        TElementMarshaller.Dispose(items[i]);
                    }
                }

                copied += (int)actual;

                if (actual < capacity)
                {
                    break;
                }
            }

            return copied;
        }
    }
}
