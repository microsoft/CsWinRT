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
}
