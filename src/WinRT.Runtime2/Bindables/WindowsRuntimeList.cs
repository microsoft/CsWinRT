// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using WindowsRuntime.InteropServices;

namespace WindowsRuntime;

/// <summary>
/// The implementation of all projected Windows Runtime <see cref="IList"/> types.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevector"/>
internal sealed class WindowsRuntimeList : WindowsRuntimeObject,
    IList,
    IWindowsRuntimeInterface<IList>,
    IWindowsRuntimeInterface<IEnumerable>
{
    /// <summary>
    /// Creates a <see cref="WindowsRuntimeList"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    public WindowsRuntimeList(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference)
    {
    }

    /// <summary>
    /// Gets the lazy-loaded, cached object reference for <c>Windows.UI.Xaml.Interop.IBindableIterable</c> for the current object.
    /// </summary>
    private WindowsRuntimeObjectReference IBindableIterableObjectReference
    {
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            WindowsRuntimeObjectReference InitializeIIterableObjectReference()
            {
                _ = Interlocked.CompareExchange(
                    location1: ref field,
                    value: NativeObjectReference.As(in WellKnownWindowsInterfaceIIDs.IID_IBindableIterable),
                    comparand: null);

                return field;
            }

            return field ?? InitializeIIterableObjectReference();
        }
    }

    /// <inheritdoc/>
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
        DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
        UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected internal sealed override bool HasUnwrappableNativeObjectReference => true;

    /// <inheritdoc/>
    public bool IsFixedSize => false;

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <inheritdoc/>
    public int Count => BindableIListMethods.Count(NativeObjectReference);

    /// <inheritdoc/>
    public bool IsSynchronized => false;

    /// <inheritdoc/>
    public object SyncRoot => this;

    /// <inheritdoc/>
    public object? this[int index]
    {
        get => BindableIListMethods.Item(NativeObjectReference, index);
        set => BindableIListMethods.Item(NativeObjectReference, index, value);
    }

    /// <inheritdoc/>
    public int Add(object? value)
    {
        return BindableIListMethods.Add(NativeObjectReference, value);
    }

    /// <inheritdoc/>
    public void Clear()
    {
        BindableIListMethods.Clear(NativeObjectReference);
    }

    /// <inheritdoc/>
    public bool Contains(object? value)
    {
        return BindableIListMethods.Contains(NativeObjectReference, value);
    }

    /// <inheritdoc/>
    public int IndexOf(object? value)
    {
        return BindableIListMethods.IndexOf(NativeObjectReference, value);
    }

    /// <inheritdoc/>
    public void Insert(int index, object? value)
    {
        BindableIListMethods.Insert(NativeObjectReference, index, value);
    }

    /// <inheritdoc/>
    public void Remove(object? value)
    {
        BindableIListMethods.Remove(NativeObjectReference, value);
    }

    /// <inheritdoc/>
    public void RemoveAt(int index)
    {
        BindableIListMethods.RemoveAt(NativeObjectReference, index);
    }

    /// <inheritdoc/>
    public void CopyTo(Array array, int index)
    {
        BindableIListMethods.CopyTo(NativeObjectReference, array, index);
    }

    /// <inheritdoc/>
    public IEnumerator GetEnumerator()
    {
        return ABI.System.Collections.IEnumerableMethods.GetEnumerator(IBindableIterableObjectReference);
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IList>.GetInterface()
    {
        return NativeObjectReference.AsValue();
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IEnumerable>.GetInterface()
    {
        return IBindableIterableObjectReference.AsValue();
    }

    /// <inheritdoc/>
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
        DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
        UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected sealed override bool IsOverridableInterface(in Guid iid)
    {
        return false;
    }
}