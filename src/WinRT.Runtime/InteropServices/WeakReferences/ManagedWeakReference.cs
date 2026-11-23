// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// The built-in default implementation of <c>IWeakReference</c>, wrapping managed objects.
/// This is a thin native object, which stores a weak GC handle to the target object.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/weakreference/nn-weakreference-iweakreference"/>
/// <remarks>
/// This type intentionally doesn't use <see cref="ComWrappers"/> (nor the COM source generators). The object only ever
/// holds a weak handle to the target, so it can never participate in a reference cycle, and doesn't need tracker support.
/// </remarks>
internal unsafe struct ManagedWeakReference
{
    /// <summary>
    /// The vtable pointer for the current instance.
    /// </summary>
    private IWeakReferenceVftbl* _vftbl;

    /// <summary>
    /// The raw <see cref="WeakGCHandle{T}"/> to the target object.
    /// </summary>
    private nint _weakHandle;

    /// <summary>
    /// The current reference count for the object (from <c>IUnknown</c>).
    /// </summary>
    private volatile uint _referenceCount;

    /// <summary>
    /// Creates a new <see cref="ManagedWeakReference"/> instance for the input target object.
    /// </summary>
    /// <param name="targetObject">The target object to create a weak reference for.</param>
    /// <returns>A pointer to the newly initialized <see cref="ManagedWeakReference"/> instance, with a reference count of 1.</returns>
    public static ManagedWeakReference* Create(object targetObject)
    {
        ManagedWeakReference* @this = (ManagedWeakReference*)NativeMemory.Alloc((nuint)sizeof(ManagedWeakReference));

        @this->_vftbl = (IWeakReferenceVftbl*)ManagedWeakReferenceImpl.Vtable;
        @this->_weakHandle = WeakGCHandle<object>.ToIntPtr(new WeakGCHandle<object>(targetObject));
        @this->_referenceCount = 1;

        return @this;
    }

    /// <inheritdoc cref="IUnknownVftbl.QueryInterfaceUnsafe(void*, System.Guid*, void**)"/>
    /// <remarks>This method never throws.</remarks>
    public HRESULT QueryInterface(Guid* riid, void** ppvObject)
    {
        if (ppvObject is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        if (riid->Equals(WellKnownWindowsInterfaceIIDs.IID_IUnknown) ||
            riid->Equals(WellKnownWindowsInterfaceIIDs.IID_IWeakReference))
        {
            _ = Interlocked.Increment(ref _referenceCount);

            *ppvObject = Unsafe.AsPointer(ref this);

            return WellKnownErrorCodes.S_OK;
        }

        *ppvObject = null;

        return WellKnownErrorCodes.E_NOINTERFACE;
    }

    /// <inheritdoc cref="IUnknownVftbl.AddRefUnsafe"/>
    /// <remarks>This method never throws.</remarks>
    public uint AddRef()
    {
        return Interlocked.Increment(ref _referenceCount);
    }

    /// <inheritdoc cref="IUnknownVftbl.ReleaseUnsafe"/>
    /// <remarks>This method never throws.</remarks>
    public uint Release()
    {
        uint referenceCount = Interlocked.Decrement(ref _referenceCount);

        if (referenceCount == 0)
        {
            WeakGCHandle<object>.FromIntPtr(_weakHandle).Dispose();

            NativeMemory.Free(Unsafe.AsPointer(ref this));
        }

        return referenceCount;
    }

    /// <summary>
    /// Implements <c>IWeakReference.Resolve</c>.
    /// </summary>
    /// <param name="riid">A reference to the interface identifier (IID) of the object.</param>
    /// <param name="objectReference">A strong reference to the object, or <see langword="null"/> if it is no longer alive.</param>
    /// <returns>If this method succeeds, then it returns <c>S_OK</c>. Otherwise, it returns an <c>HRESULT</c> error code.</returns>
    /// <see href="https://learn.microsoft.com/windows/win32/api/weakreference/nf-weakreference-iweakreference-resolve(refiid_iinspectable)"/>
    public readonly HRESULT Resolve(Guid* riid, void** objectReference)
    {
        if (objectReference is null)
        {
            return WellKnownErrorCodes.E_POINTER;
        }

        *objectReference = null;

        // If the target is gone, this still succeeds, but returns a null pointer (as per the contract of 'IWeakReference')
        if (!WeakGCHandle<object>.FromIntPtr(_weakHandle).TryGetTarget(out object? targetObject))
        {
            return WellKnownErrorCodes.S_OK;
        }

        // A CCW existed when this weak reference was created, but it might not exist anymore: native callers can release all their references
        // to it and only keep this weak reference, while the target stays alive from the managed side. In that case a new CCW has to be created.
        // There is no public API to only look up an existing CCW, so we always go through 'ComWrappers', which returns the existing one if any.
        void* thisPtr = (void*)WindowsRuntimeComWrappers.Default.GetOrCreateComInterfaceForObject(targetObject, CreateComInterfaceFlags.TrackerSupport);

        // If the caller is requesting 'IUnknown', we can avoid a 'QueryInterface' call
        if (riid->Equals(WellKnownWindowsInterfaceIIDs.IID_IUnknown))
        {
            *objectReference = thisPtr;

            return WellKnownErrorCodes.S_OK;
        }

        // Try to resolve the specific interface requested from the caller. Most callers (including CsWinRT) ask for 'IUnknown', but
        // native consumers (eg. 'winrt::weak_ref<T>::get()') might also ask for a specific interface, so we need to handle this too.
        HRESULT hresult = IUnknownVftbl.QueryInterfaceUnsafe(thisPtr, riid, objectReference);

        _ = IUnknownVftbl.ReleaseUnsafe(thisPtr);

        return hresult;
    }
}
