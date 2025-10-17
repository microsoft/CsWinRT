// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A proxy type for <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector.getview"/>.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevectorview"/>
[WindowsRuntimeManagedOnlyType]
internal sealed class IBindableVectorViewAdapter : IEnumerable
{
    /// <summary>
    /// The wrapped <see cref="IList"/> instance that contains the items in the list.
    /// </summary>
    private readonly IList _list;

    /// <summary>
    /// Creates a <see cref="IBindableVectorViewAdapter"/> instance with the specified parameters.
    /// </summary>
    /// <param name="list">The <see cref="IList"/> instance to wrap.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="list"/> is <see langword="null"/>.</exception>
    public IBindableVectorViewAdapter(IList list)
    {
        ArgumentNullException.ThrowIfNull(list);

        _list = list;
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevectorview.size"/>
    public uint Size => (uint)_list.Count;

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevectorview.getat"/>
    public object? GetAt(uint index)
    {
        // The validation logic is the same as for 'IReadOnlyList<T>'
        IReadOnlyListAdapterHelpers.EnsureIndexInValidRange(index, _list.Count);

        return _list[(int)index];
    }

    /// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevectorview.indexof"/>
    public bool IndexOf(object? value, out uint index)
    {
        int result = _list.IndexOf(value);

        if (result == -1)
        {
            index = 0;

            return false;
        }

        index = (uint)result;

        return true;
    }

    /// <inheritdoc/>
    public IEnumerator GetEnumerator()
    {
        return _list.GetEnumerator();
    }
}
