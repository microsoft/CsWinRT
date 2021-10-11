using System;
using System.Runtime.InteropServices;

namespace WinRT.Interop
{
    [Guid("11D3B13A-180E-4789-A8BE-7712882893E6")]
    internal unsafe struct IReferenceTrackerVftbl
    {
        public global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
        private void* _ConnectFromTrackerSource_0;
        private void* _DisconnectFromTrackerSource_1;
        private void* _FindTrackerTargets_2;
        private void* _GetReferenceTrackerManager_3;
        private void* _AddRefFromTrackerSource_4;
        public delegate* unmanaged[Stdcall]<IntPtr, int> AddRefFromTrackerSource { get => (delegate* unmanaged[Stdcall]<IntPtr, int>)_AddRefFromTrackerSource_4; set => _AddRefFromTrackerSource_4 = (void*)value; }
        private void* _ReleaseFromTrackerSource_5;
        public delegate* unmanaged[Stdcall]<IntPtr, int> ReleaseFromTrackerSource { get => (delegate* unmanaged[Stdcall]<IntPtr, int>)_ReleaseFromTrackerSource_5; set => _ReleaseFromTrackerSource_5 = (void*)value; }
        private void* _PegFromTrackerSource_6;

        internal static readonly Guid IID = new(0x11D3B13A, 0x180E, 0x4789, 0xA8, 0xBE, 0x77, 0x12, 0x88, 0x28, 0x93, 0xE6);
    }
}