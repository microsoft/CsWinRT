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
/// The implementation of all projected Windows Runtime readonly <see cref="IList"/> types.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindablevectorview"/>
/// <remarks>
/// There is no <c>IReadOnlyList</c> interface in .NET, so this type only implements <see cref="IEnumerable"/>.
/// This still provides better performance when casting to that interface for enumeration than having to go through
/// an opaque marshalled object and using <see cref="System.Runtime.InteropServices.IDynamicInterfaceCastable"/>.
/// </remarks>
[WindowsRuntimeManagedOnlyType]
internal sealed class WindowsRuntimeReadOnlyList : WindowsRuntimeObject,
    IEnumerable,
    IWindowsRuntimeInterface<IEnumerable>
{
    /// <summary>
    /// Creates a <see cref="WindowsRuntimeReadOnlyList"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    public WindowsRuntimeReadOnlyList(WindowsRuntimeObjectReference nativeObjectReference)
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
    protected internal override bool HasUnwrappableNativeObjectReference => true;

    /// <inheritdoc/>
    public IEnumerator GetEnumerator()
    {
        return ABI.System.Collections.IEnumerableMethods.GetEnumerator(IBindableIterableObjectReference);
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
    protected override bool IsOverridableInterface(in Guid iid)
    {
        return false;
    }
}