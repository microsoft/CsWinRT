// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;

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
    }
}