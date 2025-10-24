// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// A managed <c>IMarshal</c> implementation wrapping the free-threaded marshaler.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/combaseapi/nf-combaseapi-cocreatefreethreadedmarshaler"/>
internal sealed unsafe class FreeThreadedMarshaler
{
    /// <summary>
    /// A lock to synchronize the initialization of <see cref="IID_InProcFreeThreadedMarshaler"/>.
    /// </summary>
    private static readonly Lock IID_InProcFreeThreadedMarshalerLock = new();

    /// <summary>
    /// The <see cref="FreeThreadedMarshaler"/> instance for the current thread, if initialized.
    /// </summary>
    [ThreadStatic]
    private static FreeThreadedMarshaler? instanceForCurrentThread;

    /// <summary>
    /// The boxed value of <see cref="IID_InProcFreeThreadedMarshaler"/>, if initialized (it'll be a <see cref="Guid"/>).
    /// </summary>
    private static volatile object? iid_InProcFreeThreadedMarshalerBox;

    /// <summary>
    /// The <see cref="FreeThreadedObjectReference"/> for the marshaler to use.
    /// </summary>
    private readonly FreeThreadedObjectReference _marshalObjectReference;

    /// <summary>
    /// Creates a new <see cref="FreeThreadedMarshaler"/> instance with the specified parameters.
    /// </summary>
    /// <param name="marshalObjectReference"><inheritdoc cref="_marshalObjectReference" path="/summary/node()"/></param>
    private FreeThreadedMarshaler(FreeThreadedObjectReference marshalObjectReference)
    {
        _marshalObjectReference = marshalObjectReference;
    }

    /// <summary>
    /// Gets the IID of the free-threaded in-proc marshaler implementation.
    /// </summary>
    public static ref readonly Guid IID_InProcFreeThreadedMarshaler
    {
        get
        {
            [MemberNotNull(nameof(iid_InProcFreeThreadedMarshalerBox))]
            [MethodImpl(MethodImplOptions.NoInlining)]
            static object InitializeIID_InProcFreeThreadedMarshaler()
            {
                lock (IID_InProcFreeThreadedMarshalerLock)
                {
                    Guid iid_unmarshalClass;

                    // Query the IID from the free-threaded marshaler for the current thread.
                    // This will always be the same from any thread, so it doesn't matter.
                    fixed (Guid* riid = &WellKnownInterfaceIds.IID_IUnknown)
                    {
                        InstanceForCurrentThread.GetUnmarshalClass(
                            riid: riid,
                            pv: null,
                            dwDestContext: (uint)MSHCTX.MSHCTX_INPROC,
                            pvDestContext: null,
                            mshlflags: (uint)MSHLFLAGS.MSHLFLAGS_NORMAL,
                            pCid: &iid_unmarshalClass);
                    }

                    // Box the returned IID for later (it's effectively a 'Guid?' equivalent)
                    return iid_InProcFreeThreadedMarshalerBox = iid_unmarshalClass;
                }
            }

            // The 'unbox !!T' IL instruction returns a "controlled mutability managed reference", meaning it is safe to
            // dereference it, as long as you don't reassign to it. As we're returning a 'ref readonly' here, this is fine.
            return ref Unsafe.Unbox<Guid>(iid_InProcFreeThreadedMarshalerBox ?? InitializeIID_InProcFreeThreadedMarshaler());
        }
    }

    /// <summary>
    /// Gets the <see cref="FreeThreadedMarshaler"/> instance for the current thread.
    /// </summary>
    /// <remarks>
    /// The returned value is only meant to be used from the current thread.
    /// </remarks>
    public static FreeThreadedMarshaler InstanceForCurrentThread
    {
        get
        {
            [MemberNotNull(nameof(instanceForCurrentThread))]
            [MethodImpl(MethodImplOptions.NoInlining)]
            static FreeThreadedMarshaler InitializeInstanceForCurrentThread()
            {
                // Create the free-threaded marshaler
                void* marshalUnknownPtr;
                WindowsRuntimeImports.CoCreateFreeThreadedMarshaler(punkOuter: null, ppunkMarshal: &marshalUnknownPtr).Assert();
                try
                {
                    IUnknownVftbl.QueryInterfaceUnsafe(marshalUnknownPtr, in WellKnownInterfaceIds.IID_IMarshal, out void* marshalPtr).Assert();
                    // The returned marshaler is documented to be free-threaded, so we can instantiate 'FreeThreadedObjectReference'
                    // directly. This also should allow inlining all virtual calls to the object in this class, in the stubs below.
                    FreeThreadedObjectReference objectReference = new(marshalPtr, referenceTrackerPtr: null);
                    return instanceForCurrentThread = new FreeThreadedMarshaler(objectReference);
                }
                finally
                {
                    _ = IUnknownVftbl.ReleaseUnsafe(marshalUnknownPtr);
                }
            }

            return instanceForCurrentThread ?? InitializeInstanceForCurrentThread();
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
        _marshalObjectReference.AddRefUnsafe();

        try
        {
            IMarshalVftbl.GetUnmarshalClassUnsafe(
                _marshalObjectReference.GetThisPtrUnsafe(),
                riid,
                pv,
                dwDestContext,
                pvDestContext,
                mshlflags,
                pCid).Assert();
        }
        finally
        {
            _marshalObjectReference.ReleaseUnsafe();
        }

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
        _marshalObjectReference.AddRefUnsafe();

        try
        {
            IMarshalVftbl.GetMarshalSizeMaxUnsafe(
                _marshalObjectReference.GetThisPtrUnsafe(),
                riid,
                pv,
                dwDestContext,
                pvDestContext,
                mshlflags,
                pSize).Assert();
        }
        finally
        {
            _marshalObjectReference.ReleaseUnsafe();
        }
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
        _marshalObjectReference.AddRefUnsafe();
        try
        {
            IMarshalVftbl.MarshalInterfaceUnsafe(
                _marshalObjectReference.GetThisPtrUnsafe(),
                pStm,
                riid,
                pv,
                dwDestContext,
                pvDestContext,
                mshlflags).Assert();
        }
        finally
        {
            _marshalObjectReference.ReleaseUnsafe();
        }
    }

    /// <exception cref="Exception">Thrown if the <see cref="IMarshalVftbl.UnmarshalInterfaceUnsafe"/> call fails.</exception>
    /// <inheritdoc cref="IMarshalVftbl.UnmarshalInterfaceUnsafe"/>
    public void UnmarshalInterface(
        void* pStm,
        Guid* riid,
        void** ppv)
    {
        _marshalObjectReference.AddRefUnsafe();
        try
        {
            IMarshalVftbl.UnmarshalInterfaceUnsafe(
                _marshalObjectReference.GetThisPtrUnsafe(),
                pStm,
                riid,
                ppv).Assert();
        }
        finally
        {
            _marshalObjectReference.ReleaseUnsafe();
        }
    }

    /// <exception cref="Exception">Thrown if the <see cref="IMarshalVftbl.ReleaseMarshalDataUnsafe"/> call fails.</exception>
    /// <inheritdoc cref="IMarshalVftbl.ReleaseMarshalDataUnsafe"/>
    public void ReleaseMarshalData(void* pStm)
    {
        _marshalObjectReference.AddRefUnsafe();
        try
        {
            IMarshalVftbl.ReleaseMarshalDataUnsafe(_marshalObjectReference.GetThisPtrUnsafe(), pStm).Assert();
        }
        finally
        {
            _marshalObjectReference.ReleaseUnsafe();
        }
    }

    /// <exception cref="Exception">Thrown if the <see cref="IMarshalVftbl.DisconnectObjectUnsafe"/> call fails.</exception>
    /// <inheritdoc cref="IMarshalVftbl.DisconnectObjectUnsafe"/>
    public void DisconnectObject(uint dwReserved)
    {
        _marshalObjectReference.AddRefUnsafe();
        try
        {
            IMarshalVftbl.DisconnectObjectUnsafe(_marshalObjectReference.GetThisPtrUnsafe(), dwReserved).Assert();
        }
        finally
        {
            _marshalObjectReference.ReleaseUnsafe();
        }
    }
}
