// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace ComServerHelpers.Windows.Com;

[GeneratedComInterface]
[Guid("00000035-0000-0000-C000-000000000046")]
internal unsafe partial interface IActivationFactory : IInspectable
{
    [PreserveSig]
    global::Windows.Win32.Foundation.HRESULT ActivateInstance(void** instance);
}