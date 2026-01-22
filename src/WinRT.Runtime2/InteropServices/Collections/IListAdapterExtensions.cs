// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using WindowsRuntime.InteropServices.Marshalling;

#pragma warning disable CS0419 // TODO: fix XML docs once supported for extensions

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Extensions for the <see cref="IListAdapter{T}"/> type.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public static class IListAdapterExtensions
{
    extension(IListAdapter<string>)
    {
        /// <inheritdoc cref="IListAdapter{T}.IndexOf"/>
        /// <remarks><inheritdoc cref="IReadOnlyListAdapterExtensions.IndexOf(IReadOnlyList{string}, ReadOnlySpan{char}, out uint)" path="/remarks/node()"/></remarks>
        public static bool IndexOf(IList<string> list, ReadOnlySpan<char> value, out uint index)
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

            index = 0;

            return false;
        }

        /// <summary>
        /// Retrieves multiple items from the vector view beginning at the given index.
        /// </summary>
        /// <param name="list">The wrapped <see cref="IList{T}"/> instance.</param>
        /// <param name="startIndex">The zero-based index to start at.</param>
        /// <param name="itemsSize">The length of <paramref name="items"/>.</param>
        /// <param name="items">The target items to write.</param>
        /// <returns>The number of items that were retrieved. This value can be less than the size of <paramref name="items"/> if the end of the list is reached.</returns>
        /// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.ivector-1.getmany"/>
        public static unsafe uint GetMany(IList<string> list, uint startIndex, uint itemsSize, HSTRING* items)
        {
            int count = list.Count;

            // See notes in 'GetMany' for 'IReadOnlyList<string>'
            if (startIndex == count)
            {
                return 0;
            }

            IReadOnlyListAdapterHelpers.EnsureIndexInValidRange(startIndex, count);

            // Empty spans are supported, we just stop immediately
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
                    items[i] = HStringMarshaller.ConvertToUnmanaged(list[i + (int)startIndex]);
                }
            }
            catch
            {
                for (int j = 0; j < i; j++)
                {
                    HStringMarshaller.Free(items[j]);
                }

                throw;
            }

            return (uint)itemCount;
        }
    }

    extension(IListAdapter<Exception>)
    {
        /// <inheritdoc cref="IListAdapterExtensions.GetMany"/>
        public static unsafe uint GetMany(IList<Exception> list, uint startIndex, uint itemsSize, ABI.System.Exception* items)
        {
            int count = list.Count;

            if (startIndex == count)
            {
                return 0;
            }

            IReadOnlyListAdapterHelpers.EnsureIndexInValidRange(startIndex, count);

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

    extension(IListAdapter<Type>)
    {
        /// <inheritdoc cref="IListAdapterExtensions.GetMany"/>
        public static unsafe uint GetMany(IList<Type> list, uint startIndex, uint itemsSize, ABI.System.Type* items)
        {
            int count = list.Count;

            if (startIndex == count)
            {
                return 0;
            }

            IReadOnlyListAdapterHelpers.EnsureIndexInValidRange(startIndex, count);

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