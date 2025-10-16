// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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
        ValidateIndex(index);

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

    /// <summary>
    /// Validates an input index value.
    /// </summary>
    /// <param name="index">The index value to validate.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="index"/> is out of range for <see cref="_list"/>.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidateIndex(uint index)
    {
        if (index >= (uint)_list.Count)
        {
            [DoesNotReturn]
            [StackTraceHidden]
            static void ThrowArgumentOutOfRangeException()
                => throw new ArgumentOutOfRangeException(nameof(index), "ArgumentOutOfRange_IndexLargerThanMaxValue") { HResult = WellKnownErrorCodes.E_BOUNDS };

            ThrowArgumentOutOfRangeException();
        }
    }
}
