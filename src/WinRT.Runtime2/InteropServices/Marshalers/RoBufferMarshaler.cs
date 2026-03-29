// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A managed <c>IMarshal</c> implementation wrapping the buffer marshaler.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/robuffer/nf-robuffer-rogetbuffermarshaler"/>
internal sealed unsafe class RoBufferMarshaler
{
    /// <summary>
    /// The <see cref="FreeThreadedObjectReference"/> for the marshaler to use.
    /// </summary>
    private readonly FreeThreadedObjectReference _marshalObjectReference;

    /// <summary>
    /// Creates a new <see cref="RoBufferMarshaler"/> instance with the specified parameters.
    /// </summary>
    /// <param name="marshalObjectReference"><inheritdoc cref="_marshalObjectReference" path="/summary/node()"/></param>
    private RoBufferMarshaler(FreeThreadedObjectReference marshalObjectReference)
    {
        _marshalObjectReference = marshalObjectReference;
    }

    /// <summary>
    /// Gets the <see cref="RoBufferMarshaler"/> instance for the current thread.
    /// </summary>
    /// <remarks>
    /// The returned value is only meant to be used from the current thread.
    /// </remarks>
    [field: ThreadStatic]
    public static RoBufferMarshaler InstanceForCurrentThread
    {
        get
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            static RoBufferMarshaler InitializeInstanceForCurrentThread()
            {
                void* bufferMarshaler;

                // Create the buffer marshaler (the return will already be an 'IMarshal' interface pointer)
                HRESULT hresult = WindowsRuntimeImports.RoGetBufferMarshaler(&bufferMarshaler);

                RestrictedErrorInfo.ThrowExceptionForHR(hresult);

                // The returned marshaler is only going to be used on the current thread, so for convenience we can just
                // reuse the 'FreeThreadedObjectReference' type to provide a fast managed wrapper for it. We need one to
                // make sure that if the thread is collected, we'll still eventually release this object and not leak it.
                // We don't need a 'try/finally' here, because the only possible exception that can come out of this call
                // to the constructor is an 'OutOfMemoryException', which we wouldn't be able to handle anyway.
                FreeThreadedObjectReference objectReference = new(bufferMarshaler, referenceTrackerPtr: null);

                return field = new RoBufferMarshaler(objectReference);
            }

            return field ?? InitializeInstanceForCurrentThread();
        }
    }

    /// <exception cref="Exception">Thrown if the <see cref="IMarshalVftbl.GetUnmarshalClassUnsafe"/> call fails.</exception>
    /// <inheritdoc cref="IMarshalVftbl.GetUnmarshalClassUnsafe"/>
    public void GetUnmarshalClass(
        Guid* riid,
        void* pv,
        uint dwDestContext,
        void* pvDestContext,
        uint mshlflags,
        Guid* pCid)
    {
        HRESULT hresult;

        _marshalObjectReference.AddRefUnsafe();

        try
        {
            hresult = IMarshalVftbl.GetUnmarshalClassUnsafe(
                _marshalObjectReference.GetThisPtrUnsafe(),
                riid,
                pv,
                dwDestContext,
                pvDestContext,
                mshlflags,
                pCid);
        }
        finally
        {
            _marshalObjectReference.ReleaseUnsafe();
        }

        Marshal.ThrowExceptionForHR(hresult);
    }

    /// <exception cref="Exception">Thrown if the <see cref="IMarshalVftbl.GetMarshalSizeMaxUnsafe"/> call fails.</exception>
    /// <inheritdoc cref="IMarshalVftbl.GetMarshalSizeMaxUnsafe"/>
    public void GetMarshalSizeMax(
        Guid* riid,
        void* pv,
        uint dwDestContext,
        void* pvDestContext,
        uint mshlflags,
        uint* pSize)
    {
        HRESULT hresult;

        _marshalObjectReference.AddRefUnsafe();

        try
        {
            hresult = IMarshalVftbl.GetMarshalSizeMaxUnsafe(
                _marshalObjectReference.GetThisPtrUnsafe(),
                riid,
                pv,
                dwDestContext,
                pvDestContext,
                mshlflags,
                pSize);
        }
        finally
        {
            _marshalObjectReference.ReleaseUnsafe();
        }

        Marshal.ThrowExceptionForHR(hresult);
    }

    /// <exception cref="Exception">Thrown if the <see cref="IMarshalVftbl.MarshalInterfaceUnsafe"/> call fails.</exception>
    /// <inheritdoc cref="IMarshalVftbl.MarshalInterfaceUnsafe"/>
    public void MarshalInterface(
        void* pStm,
        Guid* riid,
        void* pv,
        uint dwDestContext,
        void* pvDestContext,
        uint mshlflags)
    {
        HRESULT hresult;

        _marshalObjectReference.AddRefUnsafe();

        try
        {
            hresult = IMarshalVftbl.MarshalInterfaceUnsafe(
                _marshalObjectReference.GetThisPtrUnsafe(),
                pStm,
                riid,
                pv,
                dwDestContext,
                pvDestContext,
                mshlflags);
        }
        finally
        {
            _marshalObjectReference.ReleaseUnsafe();
        }

        Marshal.ThrowExceptionForHR(hresult);
    }

    /// <exception cref="Exception">Thrown if the <see cref="IMarshalVftbl.UnmarshalInterfaceUnsafe"/> call fails.</exception>
    /// <inheritdoc cref="IMarshalVftbl.UnmarshalInterfaceUnsafe"/>
    public void UnmarshalInterface(
        void* pStm,
        Guid* riid,
        void** ppv)
    {
        HRESULT hresult;

        _marshalObjectReference.AddRefUnsafe();

        try
        {
            hresult = IMarshalVftbl.UnmarshalInterfaceUnsafe(
                _marshalObjectReference.GetThisPtrUnsafe(),
                pStm,
                riid,
                ppv);
        }
        finally
        {
            _marshalObjectReference.ReleaseUnsafe();
        }

        Marshal.ThrowExceptionForHR(hresult);
    }

    /// <exception cref="Exception">Thrown if the <see cref="IMarshalVftbl.ReleaseMarshalDataUnsafe"/> call fails.</exception>
    /// <inheritdoc cref="IMarshalVftbl.ReleaseMarshalDataUnsafe"/>
    public void ReleaseMarshalData(void* pStm)
    {
        HRESULT hresult;

        _marshalObjectReference.AddRefUnsafe();

        try
        {
            hresult = IMarshalVftbl.ReleaseMarshalDataUnsafe(_marshalObjectReference.GetThisPtrUnsafe(), pStm);
        }
        finally
        {
            _marshalObjectReference.ReleaseUnsafe();
        }

        Marshal.ThrowExceptionForHR(hresult);
    }

    /// <exception cref="Exception">Thrown if the <see cref="IMarshalVftbl.DisconnectObjectUnsafe"/> call fails.</exception>
    /// <inheritdoc cref="IMarshalVftbl.DisconnectObjectUnsafe"/>
    public void DisconnectObject(uint dwReserved)
    {
        HRESULT hresult;

        _marshalObjectReference.AddRefUnsafe();

        try
        {
            hresult = IMarshalVftbl.DisconnectObjectUnsafe(_marshalObjectReference.GetThisPtrUnsafe(), dwReserved);
        }
        finally
        {
            _marshalObjectReference.ReleaseUnsafe();
        }

        Marshal.ThrowExceptionForHR(hresult);
    }
}
#endif
