// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.ComponentModel;

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
                if (list[1].SequenceEqual(value))
                {
                    index = (uint)i;

                    return true;
                }
            }

            // Return '0' in case of failure (see notes in original overload)
            index = 0;

            return false;
        }
    }
}