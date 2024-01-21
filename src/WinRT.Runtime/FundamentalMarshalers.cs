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

    // This partial declaration contains additional marshalling methods for the ABI type for System.Type.
    // These are hardcoded for the ABI.System.Type type, which makes them AOT-safe. Previously, these were
    // relying on AOT-unfriendly code in MarshalNonBlittable<T>. Because this is a well known type, we can
    // just hardcode explicit marshalling methods for it instead to make using this type fully AOT-safe.
    partial struct Type
    {
        internal struct MarshalerArray
        {
            public void Dispose()
            {
                if (_marshalers != null)
                {
                    foreach (var marshaler in _marshalers)
                    {
                        DisposeMarshaler((Marshaler)marshaler);
                    }
                }
                if (_array != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(_array);
                }
            }

            public IntPtr _array;
            public object[] _marshalers;
        }

        internal static unsafe MarshalerArray CreateMarshalerArray(global::System.Type[] array)
        {
            MarshalerArray m = new MarshalerArray();
            if (array is null)
            {
                return m;
            }
            bool success = false;
            try
            {
                int length = array.Length;
                var abi_element_size = sizeof(Type);
                var byte_length = length * abi_element_size;
                m._array = Marshal.AllocCoTaskMem(byte_length);
                m._marshalers = new object[length];
                var element = (byte*)m._array.ToPointer();
                for (int i = 0; i < length; i++)
                {
                    m._marshalers[i] = CreateMarshaler(array[i]);
                    CopyAbi((Marshaler)m._marshalers[i], (IntPtr)element);
                    element += abi_element_size;
                }
                success = true;
                return m;
            }
            finally
            {
                if (!success)
                {
                    m.Dispose();
                }
            }
        }

        internal static (int length, IntPtr data) GetAbiArray(object box)
        {
            var m = (MarshalerArray)box;
            return (m._marshalers?.Length ?? 0, m._array);
        }

        internal static unsafe global::System.Type[] FromAbiArray(object box)
        {
            if (box is null)
            {
                return null;
            }
            var abi = ((int length, IntPtr data))box;
            if (abi.data == IntPtr.Zero)
            {
                return null;
            }
            var array = new global::System.Type[abi.length];
            var data = (byte*)abi.data.ToPointer();
            var abi_element_size = sizeof(Type);
            for (int i = 0; i < abi.length; i++)
            {
                var abi_element = Unsafe.ReadUnaligned<Type>(data);
                array[i] = FromAbi(abi_element);
                data += abi_element_size;
            }
            return array;
        }

        internal static unsafe (int length, IntPtr data) FromManagedArray(global::System.Type[] array)
        {
            if (array is null)
            {
                return (0, IntPtr.Zero);
            }
            IntPtr data = IntPtr.Zero;
            int i = 0;
            bool success = false;
            try
            {
                int length = array.Length;
                var abi_element_size = sizeof(Type);
                var byte_length = length * abi_element_size;
                data = Marshal.AllocCoTaskMem(byte_length);
                var bytes = (byte*)data.ToPointer();
                for (i = 0; i < length; i++)
                {
                    CopyManaged(array[i], (IntPtr)bytes);
                    bytes += abi_element_size;
                }
                success = true;
                return (i, data);
            }
            finally
            {
                if (!success)
                {
                    DisposeAbiArray((i, data));
                }
            }
        }

        internal static unsafe void CopyManagedArray(global::System.Type[] array, IntPtr data)
        {
            if (array is null)
            {
                return;
            }
            DisposeAbiArrayElements((array.Length, data));
            int i = 0;
            bool success = false;
            try
            {
                int length = array.Length;
                var abi_element_size = sizeof(Type);
                var byte_length = length * abi_element_size;
                var bytes = (byte*)data.ToPointer();
                for (i = 0; i < length; i++)
                {
                    CopyManaged(array[i], (IntPtr)bytes);
                    bytes += abi_element_size;
                }
                success = true;
            }
            finally
            {
                if (!success)
                {
                    DisposeAbiArrayElements((i, data));
                }
            }
        }

        internal static void DisposeMarshalerArray(object box) => ((MarshalerArray)box).Dispose();

        internal static unsafe void DisposeAbiArray(object box)
        {
            if (box == null) return;
            var abi = ((int length, IntPtr data))box;
            if (abi.data == IntPtr.Zero) return;
            DisposeAbiArrayElements(abi);
            Marshal.FreeCoTaskMem(abi.data);
        }

        private static unsafe void DisposeAbiArrayElements((int length, IntPtr data) abi)
        {
            var data = (byte*)abi.data.ToPointer();
            var abi_element_size = sizeof(Type);
            for (int i = 0; i < abi.length; i++)
            {
                var abi_element = Unsafe.ReadUnaligned<Type>(data);
                DisposeAbi(abi_element);
                data += abi_element_size;
            }
        }
    }
}

