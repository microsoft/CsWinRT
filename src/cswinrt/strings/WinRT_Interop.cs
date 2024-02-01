// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.


using System;

#pragma warning disable CS0649

namespace WinRT.Interop
{
    // IDelegate
    internal struct IDelegateVftbl
    {
        public IUnknownVftbl IUnknownVftbl;
        public IntPtr Invoke;
    }
}
