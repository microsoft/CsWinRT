// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace WinRT.Interop
{
    // This is internal both in the embedded case, but also if we're on modern .NET,
    // because projections only use this vftbl type downlevel, and running older
    // projections against a newer version of CsWinRT is not supported.
#if EMBED || NET
    internal
#else 
    public
#endif
    struct IDelegateVftbl
    {
        public IUnknownVftbl IUnknownVftbl;
        public IntPtr Invoke;
    }
}
