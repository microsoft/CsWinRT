using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Linq.Expressions;

#pragma warning disable CS0649

namespace WinRT.Interop
{
    // IActivationFactory
    [Guid("00000035-0000-0000-C000-000000000046")]
    internal unsafe struct IActivationFactoryVftbl
    {
        public IInspectable.Vftbl IInspectableVftbl;
        private void* _ActivateInstance;
        public delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int> ActivateInstance => (delegate* unmanaged[Stdcall]<IntPtr, IntPtr*, int>)_ActivateInstance;
    }

    // IDelegate
    internal struct IDelegateVftbl
    {
        public IUnknownVftbl IUnknownVftbl;
        public IntPtr Invoke;
    }

    [Guid("64BD43F8-bFEE-4EC4-B7EB-2935158DAE21")]
    internal unsafe struct IReferenceTrackerTargetVftbl
    {
        public global::WinRT.Interop.IUnknownVftbl IUnknownVftbl;
        private void* _AddRefFromReferenceTracker;
        public delegate* unmanaged[Stdcall]<IntPtr, uint> AddRefFromReferenceTracker => (delegate* unmanaged[Stdcall]<IntPtr, uint>)_AddRefFromReferenceTracker;
        private void* _ReleaseFromReferenceTracker;
        public delegate* unmanaged[Stdcall]<IntPtr, uint> ReleaseFromReferenceTracker => (delegate* unmanaged[Stdcall]<IntPtr, uint>)_ReleaseFromReferenceTracker;
        private void* _Peg;
        private void* _Unpeg;
    }

}
