// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A marshalling-optimized wrapper for a Windows Runtime object.
/// </summary>
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
            return _objectReference.GetThisPtr();
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
    /// Disposes the current instance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is analogous to <see cref="System.IDisposable.Dispose"/>, but with one crucial
    /// difference. That is: <b>calling this method more than once is undefined behavior</b>.
    /// </para>
    /// <para>
    /// This type is meant to primarily be used by generated marshalling code, or in very advanced scenarios.
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
