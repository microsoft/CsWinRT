// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using WindowsRuntime.InteropServices;

#pragma warning disable CA1816

namespace WindowsRuntime;

/// <summary>
/// The implementation of all projected Windows Runtime <see cref="IEnumerable{T}"/> types.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
/// <typeparam name="TIIterableMethods">The <c>Windows.Foundation.Collections.IIterable&lt;T&gt;</c> implementation type.</typeparam>
/// <remarks>
/// This type should only be used as a base type by generated generic instantiations.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterable-1"/>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class WindowsRuntimeEnumerable<T, TIIterableMethods> : WindowsRuntimeObject, IEnumerable<T>, IWindowsRuntimeInterface<IEnumerable<T>>
    where TIIterableMethods : IIterableMethodsImpl<T>
{
    /// <summary>
    /// Creates a <see cref="WindowsRuntimeEnumerable{T, TIEnumerableMethods}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    protected WindowsRuntimeEnumerable(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference)
    {
    }

    /// <inheritdoc/>
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
        DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
        UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected internal sealed override bool HasUnwrappableNativeObjectReference => true;

    /// <inheritdoc/>
    public IEnumerator<T> GetEnumerator()
    {
        return TIIterableMethods.First(NativeObjectReference);
    }

    /// <inheritdoc/>
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IEnumerable<T>>.GetInterface()
    {
        return NativeObjectReference.AsValue();
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
