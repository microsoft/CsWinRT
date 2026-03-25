// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Helpers to work with COM objects and query information from them.
/// </summary>
internal static unsafe class ComObjectHelpers
{
    /// <summary>
    /// Checks whether a given COM object is free-threaded.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <returns>
    /// The <c>HRESULT</c> for the operation, which is defined as follows:
    /// <list type="bullet">
    ///   <item><c>S_OK</c>: if <paramref name="thisPtr"/> represents a free-threaded object.</item>
    ///   <item><c>S_FALSE</c>: if <paramref name="thisPtr"/> doesn't represent a free-threaded object.</item>
    ///   <item>A failure <c>HRESULT</c> otherwise.</item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// Objects are considered free-threaded in one of these cases:
    /// <list type="bullet">
    ///   <item>The object implements <c>IAgileObject</c>.</item>
    ///   <item>The object implements <c>IMarshal</c>, and the unmarshal class is the free-threaded in-proc marshaler.</item>
    /// </list>
    /// </remarks>
    /// <exception cref="Exception">Thrown if the fallback attempt to query for <c>IMarshal</c> succeeds, but then fails to call <c>GetUnmarshalClass</c>.</exception>
    public static HRESULT IsFreeThreadedUnsafe(void* thisPtr)
    {
        // Check whether the object is free-threaded by querying for 'IAgileObject'
        if (IUnknownVftbl.QueryInterfaceUnsafe(thisPtr, in WellKnownWindowsInterfaceIIDs.IID_IAgileObject, out void* pAgileObject) >= WellKnownErrorCodes.S_OK)
        {
            _ = IUnknownVftbl.ReleaseUnsafe(pAgileObject);

            return WellKnownErrorCodes.S_OK;
        }

        // Also check for 'IMarshal'
        if (IUnknownVftbl.QueryInterfaceUnsafe(thisPtr, in WellKnownWindowsInterfaceIIDs.IID_IMarshal, out void* pMarshal) >= WellKnownErrorCodes.S_OK)
        {
            Guid unmarshalClass;
            HRESULT hresult;

            // Get the class IID of the unmarshalling code for the current object
            fixed (Guid* riid = &WellKnownWindowsInterfaceIIDs.IID_IUnknown)
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

            // If we failed to retrieve the unmarshal class, report a failure. In practice, this shouldn't
            // really happen at all. The reason why we return an 'HRESULT' from this method instead of
            // throwing an exception is to allow callers constructing a 'WindowsRuntimeObjectReference' to
            // efficiently transfer ownership without needing a 'try/finally' block to ensure they can
            // release the input object in case this method failed at this point. By doing this instead,
            // because we are only doing direct native calls, we can ensure the whole operation is never
            // throwing an exception, and we can just handle failure scenarios with normal flow control.
            if (hresult.Failed)
            {
                return hresult;
            }

            // If the unmarshal class is the free-threaded in-proc marshaler, we consider the object as free-threaded
            if (unmarshalClass == FreeThreadedMarshaler.IID_InProcFreeThreadedMarshaler)
            {
                return WellKnownErrorCodes.S_OK;
            }
        }

        return WellKnownErrorCodes.S_FALSE;
    }

    /// <summary>
    /// Validates the provided marshaling type for a given object.
    /// </summary>
    /// <param name="thisPtr">The target COM object.</param>
    /// <param name="marshalingType">The <see cref="CreateObjectReferenceMarshalingType"/> value available in metadata for the type being marshalled.</param>
    /// <remarks>
    /// This method will fail-fast if the validation fails.
    /// </remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ValidateMarshalingType(void* thisPtr, CreateObjectReferenceMarshalingType marshalingType)
    {
        if (!WindowsRuntimeFeatureSwitches.EnableMarshalingTypeValidation)
        {
            return;
        }

        // Move the logic into a non-inlineable method to help the containing one get inlined correctly
        [MethodImpl(MethodImplOptions.NoInlining)]
        static void PerformValidation(void* thisPtr, CreateObjectReferenceMarshalingType marshalingType)
        {
            // If no static type information is available, we can avoid the validation overhead
            if (marshalingType == CreateObjectReferenceMarshalingType.Unknown)
            {
                return;
            }

            HRESULT hresult = IsFreeThreadedUnsafe(thisPtr);

            // If the object declared a marshalling type that doesn't match what we can compute at runtime, fail immediately
            if ((marshalingType == CreateObjectReferenceMarshalingType.Agile && hresult == WellKnownErrorCodes.S_FALSE) ||
                (marshalingType == CreateObjectReferenceMarshalingType.Standard && hresult == WellKnownErrorCodes.S_OK))
            {
                Environment.FailFast($"The input object ('{(nint)thisPtr:X16}') declared the incorrect marshalling type '{marshalingType}'.");
            }
        }

        PerformValidation(thisPtr, marshalingType);
    }
}