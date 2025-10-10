// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using WindowsRuntime.InteropServices;

#pragma warning disable CA1816, IDE0046

namespace WindowsRuntime;

/// <summary>
/// The implementation of all projected Windows Runtime <see cref="IEnumerator{T}"/> types.
/// </summary>
/// <typeparam name="T">The type of objects to enumerate.</typeparam>
/// <typeparam name="TIIteratorMethods">The <see cref="IIteratorMethodsImpl{T}"/> implementation type.</typeparam>
/// <remarks>
/// This type should only be used as a base type by generated generic instantiations.
/// </remarks>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.collections.iiterator-1"/>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage)]
[EditorBrowsable(EditorBrowsableState.Never)]
public abstract unsafe class WindowsRuntimeEnumerator<T, TIIteratorMethods> : WindowsRuntimeObject, IEnumerator<T>, IWindowsRuntimeInterface<IEnumerator<T>>
    where TIIteratorMethods : IIteratorMethodsImpl<T>
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
    /// Creates a <see cref="WindowsRuntimeEnumerator{T, TIIteratorMethods}"/> instance with the specified parameters.
    /// </summary>
    /// <param name="nativeObjectReference">The inner Windows Runtime object reference to wrap in the current instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="nativeObjectReference"/> is <see langword="null"/>.</exception>
    protected WindowsRuntimeEnumerator(WindowsRuntimeObjectReference nativeObjectReference)
        : base(nativeObjectReference)
    {
    }

    /// <inheritdoc/>
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected internal sealed override bool HasUnwrappableNativeObjectReference => true;

    /// <inheritdoc/>
    public T Current
    {
        get
        {
            // The enumerator has not been advanced to the first element yet
            if (!_isInitialized)
            {
                throw new InvalidOperationException("InvalidOperation_EnumNotStarted");
            }

            // The enumerator has reached the end of the collection
            if (!_hadCurrent)
            {
                throw new InvalidOperationException("InvalidOperation_EnumEnded");
            }

            return field!;
        }
        private set;
    }

    /// <inheritdoc/>
    object IEnumerator.Current => Current!;

    /// <inheritdoc/>
    [MemberNotNullWhen(true, nameof(Current))]
    public bool MoveNext()
    {
        // If we've passed the end of the iteration, 'IEnumerable<T>' should return 'false',
        // while 'IIterable<T>' will fail the interface call. So we need to check for that.
        if (!_hadCurrent)
        {
            return false;
        }

        // 'IIterator<T>' starts at index '0', rather than '-1'. If this is the first call, we
        // need to just check 'HasCurrent' rather than actually moving to the next element.
        try
        {
            if (!_isInitialized)
            {
                _hadCurrent = IIteratorMethods.HasCurrent(NativeObjectReference);
                _isInitialized = true;
            }
            else
            {
                _hadCurrent = IIteratorMethods.MoveNext(NativeObjectReference);
            }

            // We want to save away the current value for two reasons:
            //  1. Accessing 'Current' is cheap on other iterators, so having it be a property which is a
            //     simple field access preserves the expected performance characteristics (as opposed to
            //     triggering a COM call every time the property is accessed).
            //  2. This allows us to preserve the same semantics as generic collection iteration when iterating
            //     beyond the end of the collection, namely that 'Current' continues to return the last value
            //     of the collection.
            if (_hadCurrent)
            {
                Current = TIIteratorMethods.Current(NativeObjectReference);
            }

            return _hadCurrent;
        }
        catch (Exception e) when (e.HResult == WellKnownErrorCodes.E_BOUNDS)
        {
            // Translate 'E_BOUNDS' into an 'InvalidOperationException' for an updated enumeration
            throw new InvalidOperationException("InvalidOperation_EnumFailedVersion", e);
        }
    }

    /// <inheritdoc/>
    public void Reset()
    {
        throw new NotSupportedException("Calling 'IEnumerator.Reset()' is not supported on native Windows Runtime enumerators.");
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    /// <inheritdoc/>
    WindowsRuntimeObjectReferenceValue IWindowsRuntimeInterface<IEnumerator<T>>.GetInterface()
    {
        return NativeObjectReference.AsValue();
    }

    /// <inheritdoc/>
    [Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    protected sealed override bool IsOverridableInterface(in Guid iid)
    {
        return false;
    }
}
