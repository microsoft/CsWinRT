// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WinRT.Interop
{
    internal unsafe struct IReferenceTrackerTargetVftbl
    {
        public IUnknownVftbl IUnknownVftbl;
        public delegate* unmanaged[Stdcall]<IntPtr, uint> AddRefFromReferenceTracker;
        public delegate* unmanaged[Stdcall]<IntPtr, uint> ReleaseFromReferenceTracker;
        public delegate* unmanaged[Stdcall]<int> Peg;
        public delegate* unmanaged[Stdcall]<int> Unpeg;
    }
}
