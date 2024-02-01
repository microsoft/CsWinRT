// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace WinRT.Interop
{
    [Guid("00000000-0000-0000-C000-000000000046")]
#if EMBED
    internal
#else
    public
#endif
    unsafe struct IUnknownVftbl
    {
        private void* _QueryInterface;
        public delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr*, int> QueryInterface { get => (delegate* unmanaged[Stdcall]<IntPtr, Guid*, IntPtr*, int>)_QueryInterface; set => _QueryInterface = (void*)value; }
        private void* _AddRef;
        public delegate* unmanaged[Stdcall]<IntPtr, uint> AddRef { get => (delegate* unmanaged[Stdcall]<IntPtr, uint>)_AddRef; set => _AddRef = (void*)value; }
        private void* _Release;
        public delegate* unmanaged[Stdcall]<IntPtr, uint> Release { get => (delegate* unmanaged[Stdcall]<IntPtr, uint>)_Release; set => _Release = (void*)value; }

        public static IUnknownVftbl AbiToProjectionVftbl => ComWrappersSupport.IUnknownVftbl;
        public static IntPtr AbiToProjectionVftblPtr => ComWrappersSupport.IUnknownVftblPtr;

        internal static readonly Guid IID = global::WinRT.Interop.IID.IID_IUnknown;

        // Avoids boxing when using default Equals.
        internal bool Equals(IUnknownVftbl other)
        {
            return _QueryInterface == other._QueryInterface && _AddRef == other._AddRef && _Release == other._Release;
        }

        internal unsafe static bool IsReferenceToManagedObject(IntPtr ptr)
        {
            return (**(IUnknownVftbl**)ptr).Equals(AbiToProjectionVftbl);
        }
    }
}
