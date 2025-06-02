// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using static Windows.Win32.PInvoke;

namespace ComServerHelpers
{
    /// <summary>
    /// Utils class that contains commonly used methods for COM operations.
    /// </summary>
    internal static class Utils
    {
        /// <summary>
        /// Both StrategyBasedExtensions and default COM/WinRT servers are using
        /// StrategyBasedComWrappers for COM interop. To avoid multiple instances of
        /// these ComWrappers being created, they can always be retrieved from here.
        /// </summary>
        public static StrategyBasedComWrappers StrategyBasedComWrappers { get; } = new StrategyBasedComWrappers();

        /// <summary>
        /// Sets default GlobalOptions properties.
        /// </summary>
        static public unsafe void SetDefaultGlobalOptions()
        {
            Guid clsid = CLSID_GlobalOptions;
            Guid iid = IGlobalOptions.IID_Guid;

            IntPtr abiPtr = IntPtr.Zero;
            if (CoCreateInstance(&clsid, null, CLSCTX.CLSCTX_INPROC_SERVER, &iid, (void**)&abiPtr) == HRESULT.S_OK)
            {
                var options = (IGlobalOptions*)abiPtr;
                options->Set(GLOBALOPT_PROPERTIES.COMGLB_RO_SETTINGS, (nuint)GLOBALOPT_RO_FLAGS.COMGLB_FAST_RUNDOWN);
                Marshal.Release(abiPtr);
            }
        }
    }
}
