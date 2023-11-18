// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace WinRT
{
    internal static class GenericDelegateHelper
    {
        internal static ConditionalWeakTable<IObjectReference, object> DelegateTable = new();

        internal unsafe static Delegate CreateDelegate(IntPtr ptr, ref Delegate delegateRef, Type delegateType, int offset)
        {
            var newDelegate = Marshal.GetDelegateForFunctionPointer((*(IntPtr**)ptr)[offset], delegateType);
            global::System.Threading.Interlocked.CompareExchange(ref delegateRef, newDelegate, null);
            return delegateRef;
        }
    }
}
