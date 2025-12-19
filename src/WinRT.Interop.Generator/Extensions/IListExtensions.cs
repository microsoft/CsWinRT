// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace WindowsRuntime.InteropGenerator;

/// <summary>
/// Extensions for the <see cref="IList{T}"/> type.
/// </summary>
internal static class IListExtensions
{
    extension<T>(IList<T> list)
        where T : class
    {
        /// <inheritdoc cref="IList{T}.IndexOf(T)"/>
        /// <remarks>
        /// This method only ever compares values by reference equality.
        /// </remarks>
        public int ReferenceIndexOf(T value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], value))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}