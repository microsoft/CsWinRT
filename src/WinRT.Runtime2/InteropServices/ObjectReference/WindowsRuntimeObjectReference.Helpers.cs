// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <inheritdoc cref="WindowsRuntimeObjectReference"/>
public unsafe partial class WindowsRuntimeObjectReference
{
    /// <summary>
    /// Performs a <c>QueryInterface</c> call on the underlying COM object to retrieve the requested interface pointer.
    /// </summary>
    /// <param name="iid">The IID of the interface to query for.</param>
    /// <returns>A <see cref="WindowsRuntimeObjectReference"/> instance for the requested interface.</returns>
    /// <exception cref="Exception">Thrown if the <c>QueryInterface</c> call fails for any reason.</exception>
    public WindowsRuntimeObjectReference As(in Guid iid)
    {
        HRESULT hresult = DerivedTryAsNative(in iid, out WindowsRuntimeObjectReference? objectReference);

        Marshal.ThrowExceptionForHR(hresult);

        return objectReference!;
    }

    /// <summary>
    /// Performs a <c>QueryInterface</c> call on the underlying COM object to retrieve the requested interface pointer.
    /// </summary>
    /// <param name="iid">The IID of the interface to query for.</param>
    /// <returns>The COM object pointer for the requested interface.</returns>
    /// <exception cref="Exception">Thrown if the <c>QueryInterface</c> call fails for any reason.</exception>
    public void* AsUnsafe(in Guid iid)
    {
        AsUnsafe(in iid, out void* pv);

        return pv;
    }

    /// <summary>
    /// Performs a <c>QueryInterface</c> call on the underlying COM object to retrieve the requested interface pointer.
    /// </summary>
    /// <param name="iid">The IID of the interface to query for.</param>
    /// <param name="ppv">The resulting COM object pointer to retrieve.</param>
    /// <exception cref="Exception">Thrown if the <c>QueryInterface</c> call fails for any reason.</exception>
    public void AsUnsafe(in Guid iid, out void* ppv)
    {
        HRESULT hresult = TryAsNative(in iid, out ppv);

        Marshal.ThrowExceptionForHR(hresult);
    }

    /// <inheritdoc cref="AsUnsafe(in Guid, out void*)"/>
    public void AsUnsafe(in Guid iid, out nint ppv)
    {
        HRESULT hresult = TryAsNative(in iid, out ppv);

        Marshal.ThrowExceptionForHR(hresult);
    }

    /// <summary>
    /// Performs a <c>QueryInterface</c> call on the underlying COM object to retrieve the requested interface pointer.
    /// </summary>
    /// <param name="iid">The IID of the interface to query for.</param>
    /// <param name="objectReference">The resulting <see cref="WindowsRuntimeObjectReference"/> instance for the requested interface.</param>
    /// <returns>Whether the requested interface was retrieved successfully.</returns>
    public bool TryAs(in Guid iid, [NotNullWhen(true)] out WindowsRuntimeObjectReference? objectReference)
    {
        HRESULT hresult = DerivedTryAsNative(in iid, out objectReference);

        return WellKnownErrorCodes.Succeeded(hresult);
    }

    /// <summary>
    /// Performs a <c>QueryInterface</c> call on the underlying COM object to retrieve the requested interface pointer.
    /// </summary>
    /// <param name="iid">The IID of the interface to query for.</param>
    /// <param name="ppv">The resulting COM pointer for the requested interface.</param>
    /// <returns>Whether the requested interface was retrieved successfully.</returns>
    public bool TryAsUnsafe(in Guid iid, out void* ppv)
    {
        HRESULT hresult = TryAsNative(in iid, out ppv);

        return WellKnownErrorCodes.Succeeded(hresult);
    }

    /// <inheritdoc cref="TryAsUnsafe(in Guid, out void*)"/>
    public bool TryAsUnsafe(in Guid iid, out nint ppv)
    {
        HRESULT hresult = TryAsNative(in iid, out ppv);

        return WellKnownErrorCodes.Succeeded(hresult);
    }

    /// <summary>
    /// Performs a <c>QueryInterface</c> call on the underlying COM object to retrieve the requested interface pointer.
    /// </summary>
    /// <param name="iid">The IID of the interface to query for.</param>
    /// <param name="objectReference">The resulting <see cref="WindowsRuntimeObjectReference"/> instance for the requested interface.</param>
    /// <returns>The <c>HRESULT</c> value for the <c>QueryInterface</c> call</returns>
    private protected abstract HRESULT DerivedTryAsNative(in Guid iid, out WindowsRuntimeObjectReference? objectReference);

    /// <summary>
    /// Performs a <c>QueryInterface</c> call on the underlying COM object to retrieve the requested interface pointer.
    /// </summary>
    /// <param name="iid">The IID of the interface to query for.</param>
    /// <param name="ppv">The resulting COM pointer for the requested interface.</param>
    /// <returns>The <c>HRESULT</c> value for the <c>QueryInterface</c> call</returns>
    private HRESULT TryAsNative(in Guid iid, out void* ppv)
    {
        ppv = null;

        AddRefUnsafe();

        try
        {
            return IUnknownVftbl.QueryInterfaceUnsafe(GetThisPtrUnsafe(), in iid, out ppv);
        }
        finally
        {
            ReleaseUnsafe();
        }
    }

    /// <inheritdoc cref="TryAsNative(in Guid, out void*)"/>
    private HRESULT TryAsNative(in Guid iid, out nint ppv)
    {
        ppv = (nint)null;

        AddRefUnsafe();

        try
        {
            // We need to use a local, as 'Unsafe.As' can't be used with pointer types
            HRESULT hresult = IUnknownVftbl.QueryInterfaceUnsafe(GetThisPtrUnsafe(), in iid, out void* ppvNative);

            ppv = (nint)ppvNative;

            return hresult;
        }
        finally
        {
            ReleaseUnsafe();
        }
    }
}
