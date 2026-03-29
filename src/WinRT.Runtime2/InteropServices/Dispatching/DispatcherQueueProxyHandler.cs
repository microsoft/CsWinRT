// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A custom <see href="https://learn.microsoft.com/uwp/api/windows.system.dispatcherqueuehandler"><c>IDispatcherQueueHandler</c></see> object, that
/// internally stores a captured <see cref="SendOrPostCallback"/> instance and the input captured state. This allows consumers to enqueue a state and
/// a cached stateless delegate without any managed allocations.
/// </summary>
internal unsafe struct DispatcherQueueProxyHandler
{
    /// <summary>
    /// The vtable pointer for the current instance.
    /// </summary>
    private IDispatcherQueueHandlerVftbl* _vftbl;

    /// <summary>
    /// The <see cref="GCHandle"/> to the captured <see cref="SendOrPostCallback"/>.
    /// </summary>
    private GCHandle<SendOrPostCallback> _callbackHandle;

    /// <summary>
    /// The <see cref="GCHandle"/> to the captured state (if present, or a <see langword="null"/> handle otherwise).
    /// </summary>
    private GCHandle<object> _stateHandle;

    /// <summary>
    /// The current reference count for the object (from <c>IUnknown</c>).
    /// </summary>
    private volatile uint _referenceCount;

    /// <summary>
    /// Creates a new <see cref="DispatcherQueueProxyHandler"/> instance for the input callback and state.
    /// </summary>
    /// <param name="handler">The input <see cref="SendOrPostCallback"/> callback to enqueue.</param>
    /// <param name="state">The input state to capture and pass to the callback.</param>
    /// <returns>A pointer to the newly initialized <see cref="DispatcherQueueProxyHandler"/> instance.</returns>
    public static DispatcherQueueProxyHandler* Create(SendOrPostCallback handler, object? state)
    {
        DispatcherQueueProxyHandler* @this = (DispatcherQueueProxyHandler*)NativeMemory.Alloc((nuint)sizeof(DispatcherQueueProxyHandler));

        @this->_vftbl = (IDispatcherQueueHandlerVftbl*)ABI.WindowsRuntime.InteropServices.DispatcherQueueProxyHandlerImpl.Vtable;
        @this->_callbackHandle = new GCHandle<SendOrPostCallback>(handler);
        @this->_stateHandle = state is not null ? new GCHandle<object>(state) : default;
        @this->_referenceCount = 1;

        return @this;
    }

    /// <inheritdoc cref="IUnknownVftbl.QueryInterfaceUnsafe(void*, System.Guid*, void**)"/>
    /// <remarks>This method never throws.</remarks>
    public HRESULT QueryInterface(Guid* riid, void** ppvObject)
    {
        if (riid->Equals(WellKnownWindowsInterfaceIIDs.IID_IUnknown) ||
            riid->Equals(WellKnownWindowsInterfaceIIDs.IID_IAgileObject) ||
            riid->Equals(WellKnownXamlInterfaceIIDs.IID_DispatcherQueueHandler))
        {
            _ = Interlocked.Increment(ref _referenceCount);

            *ppvObject = Unsafe.AsPointer(ref this);

            return WellKnownErrorCodes.S_OK;
        }

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
            _callbackHandle.Dispose();
            _stateHandle.Dispose();

            NativeMemory.Free(Unsafe.AsPointer(ref this));
        }

        return referenceCount;
    }

    /// <summary>
    /// Implements <see href="https://learn.microsoft.com/uwp/api/windows.system.dispatcherqueuehandler"><c>IDispatcherQueueHandler.Invoke()</c></see>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void Invoke()
    {
        object? state = _stateHandle.IsAllocated ? _stateHandle.Target : null;

        _callbackHandle.Target(state);
    }
}
#endif
