// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if !REFERENCE_ASSEMBLY
using System;
using System.Runtime.InteropServices;
using Windows.Foundation;

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IPropertyValue</c> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/uwp/api/windows.foundation.ipropertyvalue"/>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct IPropertyValueVftbl
{
    public delegate* unmanaged[MemberFunction]<void*, Guid*, void**, HRESULT> QueryInterface;
    public delegate* unmanaged[MemberFunction]<void*, uint> AddRef;
    public delegate* unmanaged[MemberFunction]<void*, uint> Release;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetIids;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetRuntimeClassName;
    public delegate* unmanaged[MemberFunction]<void*, TrustLevel*, HRESULT> GetTrustLevel;
    public delegate* unmanaged[MemberFunction]<void*, PropertyType*, HRESULT> get_Type;
    public delegate* unmanaged[MemberFunction]<void*, bool*, HRESULT> get_IsNumericScalar;
    public delegate* unmanaged[MemberFunction]<void*, byte*, HRESULT> GetUInt8;
    public delegate* unmanaged[MemberFunction]<void*, short*, HRESULT> GetInt16;
    public delegate* unmanaged[MemberFunction]<void*, ushort*, HRESULT> GetUInt16;
    public delegate* unmanaged[MemberFunction]<void*, int*, HRESULT> GetInt32;
    public delegate* unmanaged[MemberFunction]<void*, uint*, HRESULT> GetUInt32;
    public delegate* unmanaged[MemberFunction]<void*, long*, HRESULT> GetInt64;
    public delegate* unmanaged[MemberFunction]<void*, ulong*, HRESULT> GetUInt64;
    public delegate* unmanaged[MemberFunction]<void*, float*, HRESULT> GetSingle;
    public delegate* unmanaged[MemberFunction]<void*, double*, HRESULT> GetDouble;
    public delegate* unmanaged[MemberFunction]<void*, char*, HRESULT> GetChar16;
    public delegate* unmanaged[MemberFunction]<void*, bool*, HRESULT> GetBoolean;
    public delegate* unmanaged[MemberFunction]<void*, HSTRING*, HRESULT> GetString;
    public delegate* unmanaged[MemberFunction]<void*, Guid*, HRESULT> GetGuid;
    public delegate* unmanaged[MemberFunction]<void*, ABI.System.DateTimeOffset*, HRESULT> GetDateTime;
    public delegate* unmanaged[MemberFunction]<void*, ABI.System.TimeSpan*, HRESULT> GetTimeSpan;
    public delegate* unmanaged[MemberFunction]<void*, Point*, HRESULT> GetPoint;
    public delegate* unmanaged[MemberFunction]<void*, Size*, HRESULT> GetSize;
    public delegate* unmanaged[MemberFunction]<void*, Rect*, HRESULT> GetRect;
    public delegate* unmanaged[MemberFunction]<void*, uint*, byte**, HRESULT> GetUInt8Array;
    public delegate* unmanaged[MemberFunction]<void*, uint*, short**, HRESULT> GetInt16Array;
    public delegate* unmanaged[MemberFunction]<void*, uint*, ushort**, HRESULT> GetUInt16Array;
    public delegate* unmanaged[MemberFunction]<void*, uint*, int**, HRESULT> GetInt32Array;
    public delegate* unmanaged[MemberFunction]<void*, uint*, uint**, HRESULT> GetUInt32Array;
    public delegate* unmanaged[MemberFunction]<void*, uint*, long**, HRESULT> GetInt64Array;
    public delegate* unmanaged[MemberFunction]<void*, uint*, ulong**, HRESULT> GetUInt64Array;
    public delegate* unmanaged[MemberFunction]<void*, uint*, float**, HRESULT> GetSingleArray;
    public delegate* unmanaged[MemberFunction]<void*, uint*, double**, HRESULT> GetDoubleArray;
    public delegate* unmanaged[MemberFunction]<void*, uint*, char**, HRESULT> GetChar16Array;
    public delegate* unmanaged[MemberFunction]<void*, uint*, bool**, HRESULT> GetBooleanArray;
    public delegate* unmanaged[MemberFunction]<void*, uint*, HSTRING**, HRESULT> GetStringArray;
    public delegate* unmanaged[MemberFunction]<void*, uint*, void***, HRESULT> GetInspectableArray;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Guid**, HRESULT> GetGuidArray;
    public delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.DateTimeOffset**, HRESULT> GetDateTimeArray;
    public delegate* unmanaged[MemberFunction]<void*, uint*, ABI.System.TimeSpan**, HRESULT> GetTimeSpanArray;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Point**, HRESULT> GetPointArray;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Size**, HRESULT> GetSizeArray;
    public delegate* unmanaged[MemberFunction]<void*, uint*, Rect**, HRESULT> GetRectArray;
}
#endif
