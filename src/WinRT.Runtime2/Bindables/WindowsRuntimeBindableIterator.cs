// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using WindowsRuntime.InteropServices;

#pragma warning disable IDE0046

namespace WindowsRuntime;

/// <summary>
/// The implementation of all projected Windows Runtime <see cref="IEnumerator"/> types.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.ui.xaml.interop.ibindableiterator"/>
internal sealed unsafe class WindowsRuntimeBindableIterator : WindowsRuntimeObject, IEnumerator, IWindowsRuntimeInterface<IEnumerator>
{
    /// <summary>
    /// Indicates whether the underlying enumerator has been initialized.
    /// </summary>
    private bool _isInitialized = false;

    /// <summary>
    /// Indicates whether the previous call to <c>HasCurrent</c> was successful.
    /// </summary>
    private bool _hadCurrent = true;

    /// <summary>
    /// Creates a <see cref="WindowsRuntimeBindableIterator"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    public WindowsRuntimeBindableIterator(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference)
    {
    }

    /// <inheritdoc/>
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage, DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected internal override bool HasUnwrappableNativeObjectReference => true;

    /// <inheritdoc/>
    public object? Current
    {
        get
        {
            // Equivalent logic to 'WindowsRuntimeEnumerator<T>', see comments there for the whole type
            if (!_isInitialized)
            {
                throw new InvalidOperationException("InvalidOperation_EnumNotStarted");
            }

            if (!_hadCurrent)
            {
                throw new InvalidOperationException("InvalidOperation_EnumEnded");
            }

            return field!;
        }
        private set;
    }

    /// <inheritdoc/>
    [MemberNotNullWhen(true, nameof(Current))]
    public bool MoveNext()
    {
        if (!_hadCurrent)
        {
            return false;
        }

        try
        {
            if (!_isInitialized)
            {
                _hadCurrent = IBindableIteratorMethods.HasCurrent(NativeObjectReference);
                _isInitialized = true;
            }
            else
            {
                _hadCurrent = IBindableIteratorMethods.MoveNext(NativeObjectReference);
            }

            if (_hadCurrent)
            {
                Current = IBindableIteratorMethods.Current(NativeObjectReference);
            }

            return _hadCurrent;
        }
        catch (Exception e) when (e.HResult == WellKnownErrorCodes.E_BOUNDS)
        {
            throw new InvalidOperationException("InvalidOperation_EnumFailedVersion", e);
        }
    }

    /// <inheritdoc/>
    public void Reset()
    {
        throw new NotSupportedException("Calling 'IEnumerator.Reset()' is not supported on native Windows Runtime enumerators.");
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IEnumerator>.GetInterface()
    {
        return NativeObjectReference.AsValue();
    }

    /// <inheritdoc/>
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage, DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected override bool IsOverridableInterface(in Guid iid)
    {
        return false;
    }
}
