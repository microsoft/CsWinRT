// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using WinRT.Interop;

namespace WinRT
{
#if EMBED
    internal
#else
    public
#endif
    static class ActivationSupport
    {
        public static Func<string, Guid, IntPtr> ActivationHandler { get; set; }
    }
}