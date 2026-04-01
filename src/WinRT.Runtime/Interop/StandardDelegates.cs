// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace WinRT.Interop
{
    // standard accessors/mutators
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _get_PropertyAsBoolean(IntPtr thisPtr, out byte value);

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal unsafe delegate int _get_PropertyAsBoolean_Abi(IntPtr thisPtr, byte* value);

    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _put_PropertyAsBoolean(IntPtr thisPtr, byte value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _get_PropertyAsChar(IntPtr thisPtr, out ushort value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _put_PropertyAsChar(IntPtr thisPtr, ushort value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _get_PropertyAsSByte(IntPtr thisPtr, out sbyte value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _put_PropertyAsSByte(IntPtr thisPtr, sbyte value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _get_PropertyAsByte(IntPtr thisPtr, out byte value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _put_PropertyAsByte(IntPtr thisPtr, byte value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _get_PropertyAsInt16(IntPtr thisPtr, out short value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _put_PropertyAsInt16(IntPtr thisPtr, short value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _get_PropertyAsUInt16(IntPtr thisPtr, out ushort value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _put_PropertyAsUInt16(IntPtr thisPtr, ushort value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _get_PropertyAsInt32(IntPtr thisPtr, out int value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _put_PropertyAsInt32(IntPtr thisPtr, int value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _get_PropertyAsUInt32(IntPtr thisPtr, out uint value);

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal unsafe delegate int _get_PropertyAsUInt32_Abi(IntPtr thisPtr, uint* value);

    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _put_PropertyAsUInt32(IntPtr thisPtr, uint value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _get_PropertyAsInt64(IntPtr thisPtr, out long value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _put_PropertyAsInt64(IntPtr thisPtr, long value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _get_PropertyAsUInt64(IntPtr thisPtr, out ulong value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _put_PropertyAsUInt64(IntPtr thisPtr, ulong value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _get_PropertyAsFloat(IntPtr thisPtr, out float value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _put_PropertyAsFloat(IntPtr thisPtr, float value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _get_PropertyAsDouble(IntPtr thisPtr, out double value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _put_PropertyAsDouble(IntPtr thisPtr, double value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _get_PropertyAsObject(IntPtr thisPtr, out IntPtr value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _put_PropertyAsObject(IntPtr thisPtr, IntPtr value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _get_PropertyAsGuid(IntPtr thisPtr, out Guid value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _put_PropertyAsGuid(IntPtr thisPtr, Guid value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _get_PropertyAsString(IntPtr thisPtr, out IntPtr value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public
#endif
    delegate int _put_PropertyAsString(IntPtr thisPtr, IntPtr value);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public 
#endif
    delegate int _add_EventHandler(IntPtr thisPtr, IntPtr handler, out EventRegistrationToken token);
    [EditorBrowsable(EditorBrowsableState.Never)]
#if EMBED
    internal
#else
    public 
#endif 
    delegate int _remove_EventHandler(IntPtr thisPtr, EventRegistrationToken token);

#if !NET
    internal unsafe delegate int _get_Current_IntPtr(void* thisPtr, out IntPtr __return_value__);
    internal unsafe delegate int _get_Current_Type(void* thisPtr, out ABI.System.Type __return_value__);

    internal unsafe delegate int _get_At_IntPtr(void* thisPtr, uint index, out IntPtr __return_value__);
    internal unsafe delegate int _get_At_Type(void* thisPtr, uint index, out ABI.System.Type __return_value__);
    internal unsafe delegate int _index_Of_IntPtr(void* thisPtr, IntPtr value, out uint index, out byte found);
    internal unsafe delegate int _index_Of_Type(void* thisPtr, ABI.System.Type value, out uint index, out byte found);
    internal unsafe delegate int _set_At_IntPtr(void* thisPtr, uint index, IntPtr value);
    internal unsafe delegate int _set_At_Type(void* thisPtr, uint index, ABI.System.Type value);
    internal unsafe delegate int _append_IntPtr(void* thisPtr, IntPtr value);
    internal unsafe delegate int _append_Type(void* thisPtr, ABI.System.Type value);

    internal unsafe delegate int _lookup_IntPtr_IntPtr(void* thisPtr, IntPtr key, out IntPtr value);
    internal unsafe delegate int _lookup_Type_Type(void* thisPtr, ABI.System.Type key, out ABI.System.Type value);
    internal unsafe delegate int _lookup_IntPtr_Type(void* thisPtr, IntPtr key, out ABI.System.Type value);
    internal unsafe delegate int _lookup_Type_IntPtr(void* thisPtr, ABI.System.Type key, out IntPtr value);
    internal unsafe delegate int _has_key_IntPtr(void* thisPtr, IntPtr key, out byte found);
    internal unsafe delegate int _has_key_Type(void* thisPtr, ABI.System.Type key, out byte found);
    internal unsafe delegate int _insert_IntPtr_IntPtr(void* thisPtr, IntPtr key, IntPtr value, out byte replaced);
    internal unsafe delegate int _insert_Type_Type(void* thisPtr, ABI.System.Type key, ABI.System.Type value, out byte replaced);
    internal unsafe delegate int _insert_IntPtr_Type(void* thisPtr, IntPtr key, ABI.System.Type value, out byte replaced);
    internal unsafe delegate int _insert_Type_IntPtr(void* thisPtr, ABI.System.Type key, IntPtr value, out byte replaced);

    internal unsafe delegate int _invoke_IntPtr_IntPtr(void* thisPtr, IntPtr sender, IntPtr args);
    internal unsafe delegate int _invoke_IntPtr_Type(void* thisPtr, IntPtr sender, ABI.System.Type args);
    internal unsafe delegate int _invoke_Type_IntPtr(void* thisPtr, ABI.System.Type sender, IntPtr args);
    internal unsafe delegate int _invoke_Type_Type(void* thisPtr, ABI.System.Type sender, ABI.System.Type args);

    internal unsafe delegate int _get_Key_IntPtr(IntPtr thisPtr, IntPtr* __return_value__);
#endif
}