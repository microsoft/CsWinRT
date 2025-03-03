// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Helpers to work with COM objects and query information from them.
/// </summary>
internal unsafe partial class ComObjectHelpers
{
    /// <summary>
    /// Checks whether a given COM object is free-threaded.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <returns>Whether <paramref name="thisPtr"/> represents a free-threaded object.</returns>
    /// <remarks>
    /// Objects are considered free-threaded in one of these cases:
    /// <list type="bullet">
    ///   <item>The object implements <c>IAgileObject</c>.</item>
    ///   <item>The object implements <c>IMarshal</c>, and the unmarshal class is the free-threaded in-proc marshaler.</item>
    /// </list>
    /// </remarks>
    public static bool IsFreeThreadedUnsafe(void* thisPtr)
    {
        // Check whether the object is free-threaded by querying for 'IAgileObject'
        if (IUnknownVftbl.QueryInterfaceUnsafe(thisPtr, in WellKnownInterfaceIds.IID_IAgileObject, out void* pAgileObject) >= 0)
        {
            _ = IUnknownVftbl.ReleaseUnsafe(pAgileObject);

            return true;
        }

        // Also check for 'IMarshal'
        if (IUnknownVftbl.QueryInterfaceUnsafe(thisPtr, in WellKnownInterfaceIds.IID_IMarshal, out void* pMarshal) >= 0)
        {
            Guid unmarshalClass;
            HRESULT hresult;

            // Get the class IID of the unmarshalling code for the current object
            fixed (Guid* riid = &WellKnownInterfaceIds.IID_IUnknown)
            {
                hresult = IMarshalVftbl.GetUnmarshalClassUnsafe(
                    thisPtr: pMarshal,
                    riid: riid,
                    pv: null,
                    dwDestContext: (uint)MSHCTX.MSHCTX_INPROC,
                    pvDestContext: null,
                    mshlflags: (uint)MSHLFLAGS.MSHLFLAGS_NORMAL,
                    pCid: &unmarshalClass);
            }

            _ = IUnknownVftbl.ReleaseUnsafe(pMarshal);

            // If we failed to retrieve the unmarshal class, just throw.
            // In practice, this shouldn't really happen at all.
            Marshal.ThrowExceptionForHR(hresult);

            // If the unmarshal class is the free-threaded in-proc marshaler, we consider the object as free-threaded
            if (unmarshalClass == FreeThreadedMarshaler.IID_InProcFreeThreadedMarshaler)
            {
                return true;
            }
        }

        return false;
    }
}
