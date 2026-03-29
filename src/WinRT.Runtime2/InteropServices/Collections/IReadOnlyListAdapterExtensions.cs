// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Collections.Generic;
using System.ComponentModel;
using WindowsRuntime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Extensions for the <see cref="IReadOnlyListAdapter{T}"/> type.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IReadOnlyListAdapterExtensions
{
    extension(IReadOnlyListAdapter<string>)
    {
        /// <inheritdoc cref="IReadOnlyListAdapter{T}.IndexOf"/>
        /// <remarks>
        /// This overload can be used to avoid a <see cref="string"/> allocation on the caller side.
        /// </remarks>
        public static bool IndexOf(IReadOnlyList<string> list, ReadOnlySpan<char> value, out uint index)
        {
            int count = list.Count;

            for (int i = 0; i < count; i++)
            {
                if (list[i].SequenceEqual(value))
                {
                    index = (uint)i;

                    return true;
                }
            }

            // Return '0' in case of failure (see notes in original overload)
            index = 0;

            return false;
        }

        /// <summary>
        /// Retrieves multiple items from the vector view beginning at the given index.
        /// </summary>
        /// <param name="list">The wrapped <see cref="IReadOnlyList{T}"/> instance.</param>
        /// <param name="startIndex">The zero-based index to start at.</param>
        /// <param name="itemsSize">The length of <paramref name="items"/>.</param>
        /// <param name="items">The target items to write.</param>
        /// <returns>The number of items that were retrieved. This value can be less than the size of <paramref name="items"/> if the end of the list is reached.</returns>
        /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivectorview-1.getmany"/>
        public static unsafe uint GetMany(IReadOnlyList<string> list, uint startIndex, uint itemsSize, HSTRING* items)
        {
            int count = list.Count;

            // The spec says:
            //   "Calling 'GetMany' with 'startIndex' equal to the length of the vector (last valid index + 1)
            //   and any specified capacity will succeed and return zero actual elements."
            if (startIndex == count)
            {
                return 0;
            }

            ArgumentOutOfRangeException.ThrowIfIndexLargerThanMaxValue(startIndex, count);

            // Empty arrays are supported, we just stop immediately
            if (itemsSize == 0)
            {
                return 0;
            }

            ArgumentNullException.ThrowIfNull(items);

            int itemCount = int.Min((int)itemsSize, count - (int)startIndex);
            int i = 0;

            try
            {
                // Marshal all input 'string'-s with 'HStringMarshaller' (note that 'HSTRING' is not a COM object)
                for (; i < itemCount; i++)
                {
                    items[i] = HStringMarshaller.ConvertToUnmanaged(list[i + (int)startIndex]);
                }
            }
            catch
            {
                // Make sure to release all marshalled objects so far (this shouldn't ever throw)
                for (int j = 0; j < i; j++)
                {
                    HStringMarshaller.Free(items[j]);
                }

                throw;
            }

            return (uint)itemCount;
        }
    }

    extension(IReadOnlyListAdapter<object>)
    {
        /// <inheritdoc cref="GetMany(IReadOnlyList{string}, uint, uint, void**)"/>
        public static unsafe uint GetMany(IReadOnlyList<object> list, uint startIndex, uint itemsSize, void** items)
        {
            int count = list.Count;

            if (startIndex == count)
            {
                return 0;
            }

            ArgumentOutOfRangeException.ThrowIfIndexLargerThanMaxValue(startIndex, count);

            if (itemsSize == 0)
            {
                return 0;
            }

            ArgumentNullException.ThrowIfNull(items);

            int itemCount = int.Min((int)itemsSize, count - (int)startIndex);
            int i = 0;

            try
            {
                for (; i < itemCount; i++)
                {
                    items[i] = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(list[i + (int)startIndex]).DetachThisPtrUnsafe();
                }
            }
            catch
            {
                for (int j = 0; j < i; j++)
                {
                    WindowsRuntimeUnknownMarshaller.Free(items[j]);
                }

                throw;
            }

            return (uint)itemCount;
        }
    }

    extension(IReadOnlyListAdapter<Exception>)
    {
        /// <inheritdoc cref="GetMany(IReadOnlyList{string}, uint, uint, void**)"/>
        public static unsafe uint GetMany(IReadOnlyList<Exception> list, uint startIndex, uint itemsSize, ABI.System.Exception* items)
        {
            int count = list.Count;

            if (startIndex == count)
            {
                return 0;
            }

            ArgumentOutOfRangeException.ThrowIfIndexLargerThanMaxValue(startIndex, count);

            if (itemsSize == 0)
            {
                return 0;
            }

            ArgumentNullException.ThrowIfNull(items);

            int itemCount = int.Min((int)itemsSize, count - (int)startIndex);

            for (int i = 0; i < itemCount; i++)
            {
                items[i] = ABI.System.ExceptionMarshaller.ConvertToUnmanaged(list[i + (int)startIndex]);
            }

            return (uint)itemCount;
        }
    }

    extension(IReadOnlyListAdapter<Type>)
    {
        /// <inheritdoc cref="GetMany(IReadOnlyList{string}, uint, uint, void**)"/>
        public static unsafe uint GetMany(IReadOnlyList<Type> list, uint startIndex, uint itemsSize, ABI.System.Type* items)
        {
            int count = list.Count;

            if (startIndex == count)
            {
                return 0;
            }

            ArgumentOutOfRangeException.ThrowIfIndexLargerThanMaxValue(startIndex, count);

            if (itemsSize == 0)
            {
                return 0;
            }

            ArgumentNullException.ThrowIfNull(items);

            int itemCount = int.Min((int)itemsSize, count - (int)startIndex);
            int i = 0;

            try
            {
                // Same as with 'string' above, but with the 'Type' marshaller
                for (; i < itemCount; i++)
                {
                    items[i] = ABI.System.TypeMarshaller.ConvertToUnmanaged(list[i + (int)startIndex]);
                }
            }
            catch
            {
                // Make sure to dispose all marshalled values so far (this shouldn't ever throw)
                for (int j = 0; j < i; j++)
                {
                    ABI.System.TypeMarshaller.Dispose(items[j]);
                }

                throw;
            }

            return (uint)itemCount;
        }
    }
}

/// <summary>
/// Extensions for the <see cref="IReadOnlyListAdapter{T}"/> type for blittable value types.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IReadOnlyListAdapterBlittableValueTypeExtensions
{
    extension<T>(IReadOnlyListAdapter<T>)
        where T : unmanaged
    {
        /// <inheritdoc cref="IReadOnlyListAdapterExtensions.GetMany(IReadOnlyList{string}, uint, uint, void**)"/>
        public static unsafe uint GetMany(IReadOnlyList<T> list, uint startIndex, uint itemsSize, T* items)
        {
            int count = list.Count;

            if (startIndex == count)
            {
                return 0;
            }

            ArgumentOutOfRangeException.ThrowIfIndexLargerThanMaxValue(startIndex, count);

            if (itemsSize == 0)
            {
                return 0;
            }

            ArgumentNullException.ThrowIfNull(items);

            int itemCount = int.Min((int)itemsSize, count - (int)startIndex);

            for (int i = 0; i < itemCount; i++)
            {
                items[i] = list[i + (int)startIndex];
            }

            return (uint)itemCount;
        }
    }
}

/// <summary>
/// Extensions for the <see cref="IReadOnlyListAdapter{T}"/> type for unmanaged value types.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IReadOnlyListAdapterUnmanagedValueTypeExtensions
{
    extension<T, TAbi>(IReadOnlyListAdapter<T>)
        where T : unmanaged
        where TAbi : unmanaged
    {
        /// <inheritdoc cref="IReadOnlyListAdapterExtensions.GetMany(IReadOnlyList{string}, uint, uint, void**)"/>
        public static unsafe uint GetMany<TElementMarshaller>(IReadOnlyList<T> list, uint startIndex, uint itemsSize, TAbi* items)
            where TElementMarshaller : IWindowsRuntimeUnmanagedValueTypeElementMarshaller<T, TAbi>
        {
            int count = list.Count;

            if (startIndex == count)
            {
                return 0;
            }

            ArgumentOutOfRangeException.ThrowIfIndexLargerThanMaxValue(startIndex, count);

            if (itemsSize == 0)
            {
                return 0;
            }

            ArgumentNullException.ThrowIfNull(items);

            int itemCount = int.Min((int)itemsSize, count - (int)startIndex);

            for (int i = 0; i < itemCount; i++)
            {
                items[i] = TElementMarshaller.ConvertToUnmanaged(list[i + (int)startIndex]);
            }

            return (uint)itemCount;
        }
    }
}

/// <summary>
/// Extensions for the <see cref="IReadOnlyListAdapter{T}"/> type for managed value types.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IReadOnlyListAdapterManagedValueTypeExtensions
{
    extension<T, TAbi>(IReadOnlyListAdapter<T>)
        where T : struct
        where TAbi : unmanaged
    {
        /// <inheritdoc cref="IReadOnlyListAdapterExtensions.GetMany(IReadOnlyList{string}, uint, uint, void**)"/>
        public static unsafe uint GetMany<TElementMarshaller>(IReadOnlyList<T> list, uint startIndex, uint itemsSize, TAbi* items)
            where TElementMarshaller : IWindowsRuntimeManagedValueTypeElementMarshaller<T, TAbi>
        {
            int count = list.Count;

            if (startIndex == count)
            {
                return 0;
            }

            ArgumentOutOfRangeException.ThrowIfIndexLargerThanMaxValue(startIndex, count);

            if (itemsSize == 0)
            {
                return 0;
            }

            ArgumentNullException.ThrowIfNull(items);

            int itemCount = int.Min((int)itemsSize, count - (int)startIndex);
            int i = 0;

            try
            {
                // Same as with 'string' above, but with the provided marshaller
                for (; i < itemCount; i++)
                {
                    items[i] = TElementMarshaller.ConvertToUnmanaged(list[i + (int)startIndex]);
                }
            }
            catch
            {
                // Make sure to dispose all marshalled values so far (this shouldn't ever throw)
                for (int j = 0; j < i; j++)
                {
                    TElementMarshaller.Dispose(items[j]);
                }

                throw;
            }

            return (uint)itemCount;
        }
    }
}

/// <summary>
/// Extensions for the <see cref="IReadOnlyListAdapter{T}"/> type for <see cref="KeyValuePair{TKey, TValue}"/> types.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IReadOnlyListAdapterKeyValuePairTypeExtensions
{
    extension<TKey, TValue>(IReadOnlyListAdapter<KeyValuePair<TKey, TValue>>)
    {
        /// <inheritdoc cref="IReadOnlyListAdapterExtensions.GetMany(IReadOnlyList{string}, uint, uint, void**)"/>
        public static unsafe uint GetMany<TElementMarshaller>(IReadOnlyList<KeyValuePair<TKey, TValue>> list, uint startIndex, uint itemsSize, void** items)
            where TElementMarshaller : IWindowsRuntimeKeyValuePairTypeElementMarshaller<TKey, TValue>
        {
            int count = list.Count;

            if (startIndex == count)
            {
                return 0;
            }

            ArgumentOutOfRangeException.ThrowIfIndexLargerThanMaxValue(startIndex, count);

            if (itemsSize == 0)
            {
                return 0;
            }

            ArgumentNullException.ThrowIfNull(items);

            int itemCount = int.Min((int)itemsSize, count - (int)startIndex);
            int i = 0;

            try
            {
                // Same as with 'string' above, but with the provided marshaller
                for (; i < itemCount; i++)
                {
                    items[i] = TElementMarshaller.ConvertToUnmanaged(list[i + (int)startIndex]).DetachThisPtrUnsafe();
                }
            }
            catch
            {
                // Make sure to release all marshalled values so far (this shouldn't ever throw)
                for (int j = 0; j < i; j++)
                {
                    WindowsRuntimeUnknownMarshaller.Free(items[j]);
                }

                throw;
            }

            return (uint)itemCount;
        }
    }
}

/// <summary>
/// Extensions for the <see cref="IReadOnlyListAdapter{T}"/> type for <see cref="Nullable{T}"/> types.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IReadOnlyListAdapterNullableTypeExtensions
{
    extension<T>(IReadOnlyListAdapter<T?>)
        where T : struct
    {
        /// <inheritdoc cref="IReadOnlyListAdapterExtensions.GetMany(IReadOnlyList{string}, uint, uint, void**)"/>
        public static unsafe uint GetMany<TElementMarshaller>(IReadOnlyList<T?> list, uint startIndex, uint itemsSize, void** items)
            where TElementMarshaller : IWindowsRuntimeNullableTypeElementMarshaller<T>
        {
            int count = list.Count;

            if (startIndex == count)
            {
                return 0;
            }

            ArgumentOutOfRangeException.ThrowIfIndexLargerThanMaxValue(startIndex, count);

            if (itemsSize == 0)
            {
                return 0;
            }

            ArgumentNullException.ThrowIfNull(items);

            int itemCount = int.Min((int)itemsSize, count - (int)startIndex);
            int i = 0;

            try
            {
                // Same as with 'string' above, but with the provided marshaller
                for (; i < itemCount; i++)
                {
                    items[i] = TElementMarshaller.ConvertToUnmanaged(list[i + (int)startIndex]).DetachThisPtrUnsafe();
                }
            }
            catch
            {
                // Make sure to release all marshalled values so far (this shouldn't ever throw)
                for (int j = 0; j < i; j++)
                {
                    WindowsRuntimeUnknownMarshaller.Free(items[j]);
                }

                throw;
            }

            return (uint)itemCount;
        }
    }
}

/// <summary>
/// Extensions for the <see cref="IReadOnlyListAdapter{T}"/> type for reference types.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IReadOnlyListAdapterReferenceTypeExtensions
{
    extension<T>(IReadOnlyListAdapter<T>)
        where T : class
    {
        /// <inheritdoc cref="IReadOnlyListAdapterExtensions.GetMany(IReadOnlyList{string}, uint, uint, void**)"/>
        public static unsafe uint GetMany<TElementMarshaller>(IReadOnlyList<T> list, uint startIndex, uint itemsSize, void** items)
            where TElementMarshaller : IWindowsRuntimeReferenceTypeElementMarshaller<T>
        {
            int count = list.Count;

            if (startIndex == count)
            {
                return 0;
            }

            ArgumentOutOfRangeException.ThrowIfIndexLargerThanMaxValue(startIndex, count);

            if (itemsSize == 0)
            {
                return 0;
            }

            ArgumentNullException.ThrowIfNull(items);

            int itemCount = int.Min((int)itemsSize, count - (int)startIndex);
            int i = 0;

            try
            {
                for (; i < itemCount; i++)
                {
                    items[i] = TElementMarshaller.ConvertToUnmanaged(list[i + (int)startIndex]).DetachThisPtrUnsafe();
                }
            }
            catch
            {
                for (int j = 0; j < i; j++)
                {
                    WindowsRuntimeUnknownMarshaller.Free(items[j]);
                }

                throw;
            }

            return (uint)itemCount;
        }
    }
}
#endif
