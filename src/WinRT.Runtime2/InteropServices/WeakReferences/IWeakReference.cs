// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace WindowsRuntime;

/// <summary>
/// Represents a weak reference to an object.
/// </summary>
/// <see href="https://learn.microsoft.com/windows/win32/api/weakreference/nn-weakreference-iweakreference"/>
[Guid("00000037-0000-0000-C000-000000000046")]
[GeneratedComInterface]
internal unsafe partial interface IWeakReference
{
    /// <summary>
    /// Resolves a weak reference by returning a strong reference to the object.
    /// </summary>
    /// <param name="interfaceId">A reference to the interface identifier (IID) of the object.</param>
    /// <param name="weakReference">A strong reference to the object.</param>
    /// <returns>A strong reference to the object.</returns>
    /// <see href="https://learn.microsoft.com/windows/win32/api/weakreference/nf-weakreference-iweakreference-resolve(refiid_iinspectable)"/>
    [PreserveSig]
    HRESULT Resolve(in Guid interfaceId, out void* weakReference);
}