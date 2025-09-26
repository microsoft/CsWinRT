// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Foundation;
using static System.Runtime.InteropServices.ComWrappers;

namespace WindowsRuntime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
internal struct ReferenceInterfaceEntries
{
    public ComInterfaceEntry IReferenceValue;
    public ComInterfaceEntry IPropertyValue;
    public ComInterfaceEntry IStringable;
    public ComInterfaceEntry IWeakReferenceSource;
    public ComInterfaceEntry IMarshal;
    public ComInterfaceEntry IAgileObject;
    public ComInterfaceEntry IInspectable;
    public ComInterfaceEntry IUnknown;
}