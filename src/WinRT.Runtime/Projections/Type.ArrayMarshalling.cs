// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ABI.System
{
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

        public static unsafe void CopyManagedArray(global::System.Type[] array, IntPtr data)
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
