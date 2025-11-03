// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A marshalling-optimized wrapper for a Windows Runtime object.
/// </summary>
[Obsolete(WindowsRuntimeConstants.PrivateImplementationDetailObsoleteMessage,
    DiagnosticId = WindowsRuntimeConstants.PrivateImplementationDetailObsoleteDiagnosticId,
    UrlFormat = WindowsRuntimeConstants.CsWinRTDiagnosticsUrlFormat)]
[EditorBrowsable(EditorBrowsableState.Never)]
public readonly unsafe ref struct WindowsRuntimeObjectReferenceValue
{
    /// <summary>
    /// The <see cref="WindowsRuntimeObjectReference"/> instance being wrapped, if available.
    /// </summary>
    private readonly WindowsRuntimeObjectReference? _objectReference;

    /// <summary>
    /// The COM object pointer being wrapped, if available.
    /// </summary>
    private readonly void* _thisPtr;

    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeObjectReferenceValue"/> instance with the specified parameters.
    /// </summary>
    /// <param name="objectReference">The <see cref="WindowsRuntimeObjectReference"/> instance to wrap.</param>
    internal WindowsRuntimeObjectReferenceValue(WindowsRuntimeObjectReference objectReference)
    {
        _objectReference = objectReference;
        _thisPtr = null;

        objectReference.AddRefUnsafe();
    }

    /// <summary>
    /// Creates a new <see cref="WindowsRuntimeObjectReferenceValue"/> instance with the specified parameters.
    /// </summary>
    /// <param name="thisPtr">The COM object pointer to wrap.</param>
    internal WindowsRuntimeObjectReferenceValue(void* thisPtr)
    {
        _objectReference = null;
        _thisPtr = thisPtr;
    }

    /// <summary>
    /// Gets a value indicating whether the current instance represents <see langword="null"/>.
    /// </summary>
    public bool IsNull
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _objectReference is null && _thisPtr is null;
    }

    /// <summary>
    /// Gets the underlying pointer owned by the current instance, after incrementing its reference count.
    /// </summary>
    /// <returns>The underlying pointer owned by the current instance.</returns>
    /// <remarks>
    /// This method will increment the reference count of the returned pointer.
    /// </remarks>
    public void* GetThisPtr()
    {
        if (_objectReference is not null)
        {
            // We're manually getting the pointer and incrementing its reference
            // count, to avoid 'GetThisPtr()' adding and removing a reference on
            // the managed reference tracker, since we're already holding it.
            // This is functionally the same, but it's a bit more efficient.
            void* thisPtr = _objectReference.GetThisPtrUnsafe();

            _ = IUnknownVftbl.AddRefUnsafe(thisPtr);

            return thisPtr;
        }

        if (_thisPtr != null)
        {
            _ = IUnknownVftbl.AddRefUnsafe(_thisPtr);

            return _thisPtr;
        }

        return null;
    }

    /// <summary>
    /// Gets the underlying pointer owned by the current instance.
    /// </summary>
    /// <returns>The underlying pointer owned by the current instance.</returns>
    /// <remarks>
    /// This method does not increment the managed reference count of the current object.
    /// </remarks>
    public void* GetThisPtrUnsafe()
    {
        return _objectReference is not null ? _objectReference.GetThisPtrUnsafe() : _thisPtr;
    }

    /// <summary>
    /// Detaches the underlying pointer owned by the current instance, and transfers ownership.
    /// </summary>
    /// <returns>The underlying pointer that will be owned by the caller.</returns>
    /// <remarks>
    /// <para>
    /// Using any other methods on the current instance after calling <see cref="DetachThisPtrUnsafe"/>
    /// <b>is undefined behavior</b>, as the current instance will no longer own the underlying pointer.
    /// </para>
    /// <para>
    /// For the same reason, this method should not be used when the current instance is declared in
    /// a <see langword="using"/> block or statement (as that would call <see cref="Dispose"/>).
    /// </para>
    /// </remarks>
    public void* DetachThisPtrUnsafe()
    {
        if (_objectReference is not null)
        {
            void* thisPtr = _objectReference.GetThisPtrUnsafe();

            _ = IUnknownVftbl.AddRefUnsafe(thisPtr);

            _objectReference.ReleaseUnsafe();

            return thisPtr;
        }

        return _thisPtr;
    }

    /// <summary>
    /// Disposes the current instance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is analogous to <see cref="System.IDisposable.Dispose"/>, but with one crucial
    /// difference. That is: <b>calling this method more than once is undefined behavior</b>.
    /// </para>
    /// <para>
    /// Similarly, using any other methods after <see cref="Dispose"/> is called <b>is also undefined behavior</b>.
    /// </para>
    /// <para>
    /// This method is meant to primarily be used by generated marshalling code, or in very advanced scenarios.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        if (_objectReference is not null)
        {
            _objectReference.ReleaseUnsafe();
        }
        else if (_thisPtr != null)
        {
            _ = IUnknownVftbl.ReleaseUnsafe(_thisPtr);
        }
    }
}
