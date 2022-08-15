// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;

namespace ABI.System
{
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

