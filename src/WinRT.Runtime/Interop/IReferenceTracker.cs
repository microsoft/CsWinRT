using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace WinRT.Interop
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("11d3b13a-180e-4789-a8be-7712882893e6")]
    internal interface IReferenceTracker
    {
        void ConnectFromTrackerSource();
        void DisconnectFromTrackerSource();
        void FindTrackerTargets(IntPtr callback);
        void GetReferenceTrackerManager(out IntPtr value);
        void AddRefFromTrackerSource();
        void ReleaseFromTrackerSource();
        void PegFromTrackerSource();
    };
}
