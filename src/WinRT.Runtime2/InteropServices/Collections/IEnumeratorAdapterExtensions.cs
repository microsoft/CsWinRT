// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using WindowsRuntime.InteropServices.Marshalling;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Extensions for the <see cref="IEnumeratorAdapter{T}"/> type.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IEnumeratorAdapterExtensions
{
    // Note: all the extensions in this file match the ones in 'IReadOnlyListAdapterExtensions',
    // just adapted for 'IEnumerator<T>'. Any changes to these methods should be kept in sync.

    extension(IEnumeratorAdapter<string> adapter)
    {
        /// <summary>
        /// Retrieves multiple items from the iterator.
        /// </summary>
        /// <param name="itemsSize">The length of <paramref name="items"/>.</param>
        /// <param name="items">The target items to write.</param>
        /// <returns>The number of items that were retrieved. This value can be less than the size of <paramref name="items"/> if the end of the iterator is reached.</returns>
        /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterator-1.getmany"/>
        public unsafe uint GetMany(uint itemsSize, HSTRING* items)
        {
            // If the target array is empty, we just stop immediately
            if (itemsSize == 0)
            {
                return 0;
            }

            ArgumentNullException.ThrowIfNull(items);

            uint index = 0;

            try
            {
                // Copy all items into the target buffer
                for (; index < itemsSize & adapter.HasCurrent; index++)
                {
                    items[index] = HStringMarshaller.ConvertToUnmanaged(adapter.Current);

                    _ = adapter.MoveNext();
                }
            }
            catch
            {
                // Make sure to release all marshalled objects so far (this shouldn't ever throw)
                for (uint j = 0; j < index; j++)
                {
                    HStringMarshaller.Free(items[j]);
                }

                throw;
            }

            return index;
        }
    }

    extension(IEnumeratorAdapter<object> adapter)
    {
        /// <inheritdoc cref="GetMany(IEnumeratorAdapter{string}, uint, void**)"/>
        public unsafe uint GetMany(uint itemsSize, void** items)
        {
            if (itemsSize == 0)
            {
                return 0;
            }

            ArgumentNullException.ThrowIfNull(items);

            uint index = 0;

            try
            {
                for (; index < itemsSize & adapter.HasCurrent; index++)
                {
                    items[index] = WindowsRuntimeObjectMarshaller.ConvertToUnmanaged(adapter.Current).DetachThisPtrUnsafe();

                    _ = adapter.MoveNext();
                }
            }
            catch
            {
                for (uint j = 0; j < index; j++)
                {
                    WindowsRuntimeUnknownMarshaller.Free(items[j]);
                }

                throw;
            }

            return index;
        }
    }

    extension(IEnumeratorAdapter<Exception> adapter)
    {
        /// <inheritdoc cref="GetMany(IEnumeratorAdapter{string}, uint, void**)"/>
        public unsafe uint GetMany(uint itemsSize, ABI.System.Exception* items)
        {
            if (itemsSize == 0)
            {
                return 0;
            }

            ArgumentNullException.ThrowIfNull(items);

            uint index = 0;

            for (; index < itemsSize & adapter.HasCurrent; index++)
            {
                items[index] = ABI.System.ExceptionMarshaller.ConvertToUnmanaged(adapter.Current);

                _ = adapter.MoveNext();
            }

            return index;
        }
    }

    extension(IEnumeratorAdapter<Type> adapter)
    {
        /// <inheritdoc cref="GetMany(IEnumeratorAdapter{string}, uint, void**)"/>
        public unsafe uint GetMany(uint itemsSize, ABI.System.Type* items)
        {
            if (itemsSize == 0)
            {
                return 0;
            }

            ArgumentNullException.ThrowIfNull(items);

            uint index = 0;

            try
            {
                for (; index < itemsSize & adapter.HasCurrent; index++)
                {
                    items[index] = ABI.System.TypeMarshaller.ConvertToUnmanaged(adapter.Current);

                    _ = adapter.MoveNext();
                }
            }
            catch
            {
                // Make sure to dispose all marshalled values so far (this shouldn't ever throw)
                for (uint j = 0; j < index; j++)
                {
                    ABI.System.TypeMarshaller.Dispose(items[j]);
                }

                throw;
            }

            return index;
        }
    }
}

/// <summary>
/// Extensions for the <see cref="IEnumeratorAdapter{T}"/> type for blittable value types.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IEnumeratorAdapterBlittableValueTypeExtensions
{
    extension<T>(IEnumeratorAdapter<T> adapter)
        where T : unmanaged
    {
        /// <inheritdoc cref="IEnumeratorAdapterExtensions.GetMany(IEnumeratorAdapter{string}, uint, void**)"/>
        public unsafe uint GetMany(uint itemsSize, T* items)
        {
            if (itemsSize == 0)
            {
                return 0;
            }

            ArgumentNullException.ThrowIfNull(items);

            uint index = 0;

            for (; index < itemsSize & adapter.HasCurrent; index++)
            {
                items[index] = adapter.Current;

                _ = adapter.MoveNext();
            }

            return index;
        }
    }
}

/// <summary>
/// Extensions for the <see cref="IEnumeratorAdapter{T}"/> type for unmanaged value types.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IEnumeratorAdapterUnmanagedValueTypeExtensions
{
    extension<T, TAbi>(IEnumeratorAdapter<T> adapter)
        where T : unmanaged
        where TAbi : unmanaged
    {
        /// <inheritdoc cref="IEnumeratorAdapterExtensions.GetMany(IEnumeratorAdapter{string}, uint, void**)"/>
        public unsafe uint GetMany<TElementMarshaller>(uint itemsSize, TAbi* items)
            where TElementMarshaller : IWindowsRuntimeUnmanagedValueTypeElementMarshaller<T, TAbi>
        {
            if (itemsSize == 0)
            {
                return 0;
            }

            ArgumentNullException.ThrowIfNull(items);

            uint index = 0;

            for (; index < itemsSize & adapter.HasCurrent; index++)
            {
                items[index] = TElementMarshaller.ConvertToUnmanaged(adapter.Current);

                _ = adapter.MoveNext();
            }

            return index;
        }
    }
}

/// <summary>
/// Extensions for the <see cref="IEnumeratorAdapter{T}"/> type for managed value types.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IEnumeratorAdapterManagedValueTypeExtensions
{
    extension<T, TAbi>(IEnumeratorAdapter<T> adapter)
        where T : struct
        where TAbi : unmanaged
    {
        /// <inheritdoc cref="IEnumeratorAdapterExtensions.GetMany(IEnumeratorAdapter{string}, uint, void**)"/>
        public unsafe uint GetMany<TElementMarshaller>(uint itemsSize, TAbi* items)
            where TElementMarshaller : IWindowsRuntimeManagedValueTypeElementMarshaller<T, TAbi>
        {
            if (itemsSize == 0)
            {
                return 0;
            }

            ArgumentNullException.ThrowIfNull(items);

            uint index = 0;

            try
            {
                for (; index < itemsSize & adapter.HasCurrent; index++)
                {
                    items[index] = TElementMarshaller.ConvertToUnmanaged(adapter.Current);

                    _ = adapter.MoveNext();
                }
            }
            catch
            {
                for (uint j = 0; j < index; j++)
                {
                    TElementMarshaller.Dispose(items[j]);
                }

                throw;
            }

            return index;
        }
    }
}

/// <summary>
/// Extensions for the <see cref="IEnumeratorAdapter{T}"/> type for <see cref="KeyValuePair{TKey, TValue}"/> types.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IEnumeratorAdapterKeyValuePairTypeExtensions
{
    extension<TKey, TValue>(IEnumeratorAdapter<KeyValuePair<TKey, TValue>> adapter)
    {
        /// <inheritdoc cref="IEnumeratorAdapterExtensions.GetMany(IEnumeratorAdapter{string}, uint, void**)"/>
        public unsafe uint GetMany<TElementMarshaller>(uint itemsSize, void** items)
            where TElementMarshaller : IWindowsRuntimeKeyValuePairTypeElementMarshaller<TKey, TValue>
        {
            if (itemsSize == 0)
            {
                return 0;
            }

            ArgumentNullException.ThrowIfNull(items);

            uint index = 0;

            try
            {
                for (; index < itemsSize & adapter.HasCurrent; index++)
                {
                    items[index] = TElementMarshaller.ConvertToUnmanaged(adapter.Current).DetachThisPtrUnsafe();

                    _ = adapter.MoveNext();
                }
            }
            catch
            {
                for (uint j = 0; j < index; j++)
                {
                    WindowsRuntimeUnknownMarshaller.Free(items[j]);
                }

                throw;
            }

            return index;
        }
    }
}

/// <summary>
/// Extensions for the <see cref="IEnumeratorAdapter{T}"/> type for <see cref="Nullable{T}"/> types.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IEnumeratorAdapterNullableTypeExtensions
{
    extension<T>(IEnumeratorAdapter<T?> adapter)
        where T : struct
    {
        /// <inheritdoc cref="IEnumeratorAdapterExtensions.GetMany(IEnumeratorAdapter{string}, uint, void**)"/>
        public unsafe uint GetMany<TElementMarshaller>(uint itemsSize, void** items)
            where TElementMarshaller : IWindowsRuntimeNullableTypeElementMarshaller<T>
        {
            if (itemsSize == 0)
            {
                return 0;
            }

            ArgumentNullException.ThrowIfNull(items);

            uint index = 0;

            try
            {
                for (; index < itemsSize & adapter.HasCurrent; index++)
                {
                    items[index] = TElementMarshaller.ConvertToUnmanaged(adapter.Current).DetachThisPtrUnsafe();

                    _ = adapter.MoveNext();
                }
            }
            catch
            {
                for (uint j = 0; j < index; j++)
                {
                    WindowsRuntimeUnknownMarshaller.Free(items[j]);
                }

                throw;
            }

            return index;
        }
    }
}

/// <summary>
/// Extensions for the <see cref="IEnumeratorAdapter{T}"/> type for reference types.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IEnumeratorAdapterReferenceTypeExtensions
{
    extension<T>(IEnumeratorAdapter<T> adapter)
        where T : class
    {
        /// <inheritdoc cref="IEnumeratorAdapterExtensions.GetMany(IEnumeratorAdapter{string}, uint, void**)"/>
        public unsafe uint GetMany<TElementMarshaller>(uint itemsSize, void** items)
            where TElementMarshaller : IWindowsRuntimeReferenceTypeElementMarshaller<T>
        {
            if (itemsSize == 0)
            {
                return 0;
            }

            ArgumentNullException.ThrowIfNull(items);

            uint index = 0;

            try
            {
                for (; index < itemsSize & adapter.HasCurrent; index++)
                {
                    items[index] = TElementMarshaller.ConvertToUnmanaged(adapter.Current).DetachThisPtrUnsafe();

                    _ = adapter.MoveNext();
                }
            }
            catch
            {
                for (uint j = 0; j < index; j++)
                {
                    WindowsRuntimeUnknownMarshaller.Free(items[j]);
                }

                throw;
            }

            return index;
        }
    }
}