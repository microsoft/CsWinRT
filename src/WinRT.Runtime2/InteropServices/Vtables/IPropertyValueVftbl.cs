// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Windows.Foundation;

#pragma warning disable CS0649

namespace WindowsRuntime.InteropServices;

/// <summary>
/// Binding type for the <c>IPropertyValue</c> interface vtable.
/// </summary>
/// <see href="https://learn.microsoft.com/en-us/uwp/api/windows.foundation.ipropertyvalue"/>
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
    public delegate* unmanaged[MemberFunction]<void*, int*, byte**, HRESULT> GetUInt8Array;
    public delegate* unmanaged[MemberFunction]<void*, int*, short**, HRESULT> GetInt16Array;
    public delegate* unmanaged[MemberFunction]<void*, int*, ushort**, HRESULT> GetUInt16Array;
    public delegate* unmanaged[MemberFunction]<void*, int*, int**, HRESULT> GetInt32Array;
    public delegate* unmanaged[MemberFunction]<void*, int*, uint**, HRESULT> GetUInt32Array;
    public delegate* unmanaged[MemberFunction]<void*, int*, long**, HRESULT> GetInt64Array;
    public delegate* unmanaged[MemberFunction]<void*, int*, ulong**, HRESULT> GetUInt64Array;
    public delegate* unmanaged[MemberFunction]<void*, int*, float**, HRESULT> GetSingleArray;
    public delegate* unmanaged[MemberFunction]<void*, int*, double**, HRESULT> GetDoubleArray;
    public delegate* unmanaged[MemberFunction]<void*, int*, char**, HRESULT> GetChar16Array;
    public delegate* unmanaged[MemberFunction]<void*, int*, bool**, HRESULT> GetBooleanArray;
    public delegate* unmanaged[MemberFunction]<void*, int*, HSTRING**, HRESULT> GetStringArray;
    public delegate* unmanaged[MemberFunction]<void*, int*, void***, HRESULT> GetInspectableArray;
    public delegate* unmanaged[MemberFunction]<void*, int*, Guid**, HRESULT> GetGuidArray;
    public delegate* unmanaged[MemberFunction]<void*, int*, ABI.System.DateTimeOffset**, HRESULT> GetDateTimeArray;
    public delegate* unmanaged[MemberFunction]<void*, int*, ABI.System.TimeSpan**, HRESULT> GetTimeSpanArray;
    public delegate* unmanaged[MemberFunction]<void*, int*, Point**, HRESULT> GetPointArray;
    public delegate* unmanaged[MemberFunction]<void*, int*, Size**, HRESULT> GetSizeArray;
    public delegate* unmanaged[MemberFunction]<void*, int*, Rect**, HRESULT> GetRectArray;
}
