// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;

namespace ComServerHelpers.Internal.Windows;

/// <summary>
/// A type that allows working with pointers to COM objects more securely.
/// </summary>
/// <typeparam name="T">The type to wrap in the current <see cref="ComPtr{T}"/> instance.</typeparam>
/// <remarks>
/// While this type is not marked as <see langword="ref"/> so that it can also be used in fields, make sure to
/// keep the reference counts properly tracked if you do store <see cref="ComPtr{T}"/> instances on the heap.
/// </remarks>
internal unsafe struct ComPtr<T> : IDisposable
    where T : unmanaged
{
    /// <summary>
    /// The raw pointer to a COM object, if existing.
    /// </summary>
    private T* ptr_;

    /// <summary>
    /// Creates a new <see cref="ComPtr{T}"/> instance from a raw pointer and increments the ref count.
    /// </summary>
    /// <param name="other">The raw pointer to wrap.</param>
    public ComPtr(T* other)
    {
        ptr_ = other;

        InternalAddRef();
    }

    /// <summary>
    /// Creates a new <see cref="ComPtr{T}"/> instance from a second one and increments the ref count.
    /// </summary>
    /// <param name="other">The other <see cref="ComPtr{T}"/> instance to copy.</param>
    public ComPtr(ComPtr<T> other)
    {
        ptr_ = other.ptr_;

        InternalAddRef();
    }

    /// <summary>
    /// Converts a raw pointer to a new <see cref="ComPtr{T}"/> instance and increments the ref count.
    /// </summary>
    /// <param name="other">The raw pointer to wrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator ComPtr<T>(T* other) => new(other);

    /// <summary>
    /// Unwraps a <see cref="ComPtr{T}"/> instance and returns the internal raw pointer.
    /// </summary>
    /// <param name="other">The <see cref="ComPtr{T}"/> instance to unwrap.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator T*(ComPtr<T> other) => other.Get();

    /// <summary>
    /// Releases the current COM object, if any, and replaces the internal pointer with an input raw pointer.
    /// </summary>
    /// <param name="other">The input raw pointer to wrap.</param>
    /// <remarks>This method will release the current raw pointer, if any, but it will not increment the references for <paramref name="other"/>.</remarks>
    public void Attach(T* other)
    {
        if (ptr_ is not null)
        {
            _ = ((IUnknown*)ptr_)->Release();
        }

        ptr_ = other;
    }

    /// <summary>
    /// Returns the raw pointer wrapped by the current instance, and resets the current <see cref="ComPtr{T}"/> value.
    /// </summary>
    /// <returns>The raw pointer wrapped by the current <see cref="ComPtr{T}"/> value.</returns>
    /// <remarks>This method will not change the reference count for the COM object in use.</remarks>
    public T* Detach()
    {
        T* ptr = ptr_;

        ptr_ = null;

        return ptr;
    }

    /// <summary>
    /// Increments the reference count for the current COM object, if any, and copies its address to a target raw pointer.
    /// </summary>
    /// <param name="ptr">The target raw pointer to copy the address of the current COM object to.</param>
    /// <returns>This method always returns <see cref="HRESULT.S_OK"/>.</returns>
    public readonly HRESULT CopyTo(T** ptr)
    {
        InternalAddRef();

        *ptr = ptr_;

        return HRESULT.S_OK;
    }

    /// <summary>
    /// Increments the reference count for the current COM object, if any, and copies its address to a target raw pointer.
    /// </summary>
    /// <param name="ptr">The target raw pointer to copy the address of the current COM object to.</param>
    /// <returns>This method always returns <see cref="HRESULT.S_OK"/>.</returns>
    public readonly HRESULT CopyTo(ref T* ptr)
    {
        InternalAddRef();

        ptr = ptr_;

        return HRESULT.S_OK;
    }

    /// <summary>
    /// Converts the current COM object reference to a given interface type and assigns that to a target raw pointer.
    /// </summary>
    /// <param name="ptr">The target raw pointer to copy the address of the current COM object to.</param>
    /// <returns>The result of <see cref="IUnknown.QueryInterface(Guid*, void**)"/> for the target type <typeparamref name="U"/>.</returns>
    public readonly HRESULT CopyTo<U>(U** ptr)
        where U : unmanaged, IComIID
    {
        Guid iid = U.Guid;
        return ((IUnknown*)ptr_)->QueryInterface(&iid, (void**)ptr);
    }

    /// <summary>
    /// Converts the current COM object reference to a given interface type and assigns that to a target raw pointer.
    /// </summary>
    /// <param name="ptr">The target raw pointer to copy the address of the current COM object to.</param>
    /// <returns>The result of <see cref="IUnknown.QueryInterface(Guid*, void**)"/> for the target type <typeparamref name="U"/>.</returns>
    public readonly HRESULT CopyTo<U>(ref U* ptr)
        where U : unmanaged, IComIID
    {
        void** tmp = null;

        Guid iid = U.Guid;
        HRESULT @ref = ((IUnknown*)ptr_)->QueryInterface(&iid, tmp);

        ptr = (U*)tmp;

        return @ref;
    }

    /// <summary>
    /// Converts the current object reference to a type indicated by the given IID and assigns that to a target address.
    /// </summary>
    /// <param name="riid">The IID indicating the interface type to convert the COM object reference to.</param>
    /// <param name="ptr">The target raw pointer to copy the address of the current COM object to.</param>
    /// <returns>The result of <see cref="IUnknown.QueryInterface(Guid*, void**)"/> for the target IID.</returns>
    public readonly int CopyTo(Guid* riid, void** ptr)
    {
        return ((IUnknown*)ptr_)->QueryInterface(riid, ptr);
    }

    /// <inheritdoc/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        T* pointer = ptr_;

        if (pointer is not null)
        {
            ptr_ = null;

            _ = ((IUnknown*)pointer)->Release();
        }
    }

    /// <summary>
    /// Gets the currently wrapped raw pointer to a COM object.
    /// </summary>
    /// <returns>The raw pointer wrapped by the current <see cref="ComPtr{T}"/> instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly T* Get()
    {
        return ptr_;
    }

    /// <summary>
    /// Gets the address of the current <see cref="ComPtr{T}"/> instance as a raw <typeparamref name="T"/> double pointer.
    /// This method is only valid when the current <see cref="ComPtr{T}"/> instance is on the stack or pinned.
    /// </summary>
    /// <returns>The raw pointer to the current <see cref="ComPtr{T}"/> instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly T** GetAddressOf()
    {
        return (T**)Unsafe.AsPointer(ref Unsafe.AsRef(in this));
    }

    /// <summary>
    /// Gets the address of the current <see cref="ComPtr{T}"/> instance as a raw <typeparamref name="T"/> double pointer.
    /// </summary>
    /// <returns>The raw pointer to the current <see cref="ComPtr{T}"/> instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ref T* GetPinnableReference()
    {
        fixed (T** ptr = &ptr_)
        {
            return ref *ptr;
        }
    }

    /// <summary>
    /// Releases the current COM object in use and gets the address of the <see cref="ComPtr{T}"/> instance as a raw <typeparamref name="T"/> double pointer.
    /// This method is only valid when the current <see cref="ComPtr{T}"/> instance is on the stack or pinned.
    /// </summary>
    /// <returns>The raw pointer to the current <see cref="ComPtr{T}"/> instance.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T** ReleaseAndGetAddressOf()
    {
        _ = InternalRelease();

        return GetAddressOf();
    }

    /// <summary>
    /// Resets the current instance by decrementing the reference count for the target COM object and setting the internal raw pointer to <see langword="null"/>.
    /// </summary>
    /// <returns>The updated reference count for the COM object that was in use, if any.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint Reset()
    {
        return InternalRelease();
    }

    /// <summary>
    /// Swaps the current COM object reference with that of a given <see cref="ComPtr{T}"/> instance.
    /// </summary>
    /// <param name="r">The target <see cref="ComPtr{T}"/> instance to swap with the current one.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Swap(ComPtr<T>* r)
    {
        T* tmp = ptr_;

        ptr_ = r->ptr_;

        r->ptr_ = tmp;
    }

    /// <summary>
    /// Swaps the current COM object reference with that of a given <see cref="ComPtr{T}"/> instance.
    /// </summary>
    /// <param name="other">The target <see cref="ComPtr{T}"/> instance to swap with the current one.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Swap(ref ComPtr<T> other)
    {
        T* tmp = ptr_;

        ptr_ = other.ptr_;

        other.ptr_ = tmp;
    }

    // Increments the reference count for the current COM object, if any
    private readonly void InternalAddRef()
    {
        T* temp = ptr_;

        if (temp is not null)
        {
            _ = ((IUnknown*)temp)->AddRef();
        }
    }

    // Decrements the reference count for the current COM object, if any
    private uint InternalRelease()
    {
        uint @ref = 0;
        T* temp = ptr_;

        if (temp is not null)
        {
            ptr_ = null;

            @ref = ((IUnknown*)temp)->Release();
        }

        return @ref;
    }
}