// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ABI.System
{
    /// <summary>
    /// Non-generic marshalling stubs used from <see cref="global::WinRT.MarshalGeneric{T}"/>.
    /// This avoids the generic instantiations for all additional stubs for these ABI types.
    /// </summary>
    internal static class NonBlittableMarshallingStubs
    {
        // This can be shared for all DisposeMarshaler and DisposeAbi methods for these ABI types.
        // None of these has any special logic that needs to run when those two APIs are invoked.
        public static readonly Action<object> NoOpFunc = NoOp;

        public static object Boolean_CreateMarshaler(bool value) => Boolean.CreateMarshaler(value);
        public static object Boolean_GetAbi(object value) => Boolean.GetAbi((bool)value);
        public static bool Boolean_FromAbi(object abi) => Boolean.FromAbi((byte)abi);
        public static void Boolean_CopyAbi(object value, IntPtr dest) => Boolean.CopyAbi((bool)value, dest);
        public static object Boolean_FromManaged(bool value) => Boolean.FromManaged(value);

        public static object Char_CreateMarshaler(char value) => Char.CreateMarshaler(value);
        public static object Char_GetAbi(object value) => Char.GetAbi((char)value);
        public static char Char_FromAbi(object abi) => Char.FromAbi((ushort)abi);
        public static void Char_CopyAbi(object value, IntPtr dest) => Char.CopyAbi((char)value, dest);
        public static object Char_FromManaged(char value) => Char.FromManaged(value);

        public static object TimeSpan_CreateMarshaler(global::System.TimeSpan value) => TimeSpan.CreateMarshaler(value);
        public static object TimeSpan_GetAbi(object value) => TimeSpan.GetAbi((TimeSpan.Marshaler)value);
        public static global::System.TimeSpan TimeSpan_FromAbi(object abi) => TimeSpan.FromAbi((TimeSpan)abi);
        public static void TimeSpan_CopyAbi(object value, IntPtr dest) => TimeSpan.CopyAbi((TimeSpan.Marshaler)value, dest);
        public static object TimeSpan_FromManaged(global::System.TimeSpan value) => TimeSpan.FromManaged(value);

        public static object DateTimeOffset_CreateMarshaler(global::System.DateTimeOffset value) => DateTimeOffset.CreateMarshaler(value);
        public static object DateTimeOffset_GetAbi(object value) => DateTimeOffset.GetAbi((DateTimeOffset.Marshaler)value);
        public static global::System.DateTimeOffset DateTimeOffset_FromAbi(object abi) => DateTimeOffset.FromAbi((DateTimeOffset)abi);
        public static unsafe void DateTimeOffset_CopyAbi(object value, IntPtr dest) => DateTimeOffset.CopyAbi((DateTimeOffset.Marshaler)value, dest);
        public static object DateTimeOffset_FromManaged(global::System.DateTimeOffset value) => DateTimeOffset.FromManaged(value);

        public static object Type_CreateMarshalerArray(global::System.Type[] value) => Type.CreateMarshalerArray(value);
        public static object Exception_CreateMarshalerArray(global::System.Exception[] value) => global::WinRT.MarshalNonBlittable<global::System.Exception>.CreateMarshalerArray(value);

        private static void NoOp(object obj)
        {
        }
    }

    internal struct Boolean
    {
        public static bool CreateMarshaler(bool value) => value;
        public static byte GetAbi(bool value) => (byte)(value ? 1 : 0);
        public static bool FromAbi(byte abi) => abi != 0;
        public static unsafe void CopyAbi(bool value, IntPtr dest) => *(byte*)dest.ToPointer() = GetAbi(value);
        public static byte FromManaged(bool value) => GetAbi(value);
        public static unsafe void CopyManaged(bool arg, IntPtr dest) => *(byte*)dest.ToPointer() = FromManaged(arg);
        public static void DisposeMarshaler(bool m) { }
        public static void DisposeAbi(byte abi) { }
    }

    internal struct Char
    {
        public static char CreateMarshaler(char value) => value;
        public static ushort GetAbi(char value) => (ushort)value;
        public static char FromAbi(ushort abi) => (char)abi;
        public static unsafe void CopyAbi(char value, IntPtr dest) => *(ushort*)dest.ToPointer() = GetAbi(value);
        public static ushort FromManaged(char value) => GetAbi(value);
        public static unsafe void CopyManaged(char arg, IntPtr dest) => *(ushort*)dest.ToPointer() = FromManaged(arg);
        public static void DisposeMarshaler(char m) { }
        public static void DisposeAbi(ushort abi) { }
    }
}

