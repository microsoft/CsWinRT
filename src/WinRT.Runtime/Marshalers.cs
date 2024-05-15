// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using WinRT.Interop;

#pragma warning disable 0169 // The field 'xxx' is never used
#pragma warning disable 0649 // Field 'xxx' is never assigned to, and will always have its default value

namespace WinRT
{
    internal static class MarshalExtensions
    {
        public static void Dispose(this GCHandle handle)
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }

#if !NET
        public static unsafe ref readonly char GetPinnableReference(this string str)
        {
            fixed (char* p = str)
            {
                return ref *p;
            }
        }
#endif
    }

#if EMBED
    internal
#else 
    public
#endif
    class MarshalString
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct HSTRING_HEADER
        {
            IntPtr reserved1;
            int reserved2;
            int reserved3;
            int reserved4;
            int reserved5;
        }

        private IntPtr _header;
        private GCHandle _gchandle;

        public ref struct Pinnable
        {
            private HSTRING_HEADER _header;
            private string _value;
#if DEBUG
            private bool _pinned;
#endif

            public Pinnable(string value)
            {
                _value = value ?? "";
                _header = default;
#if DEBUG
                _pinned = false;
#endif            
            }

            public ref readonly char GetPinnableReference()
            {
#if DEBUG
                _pinned = true;
#endif
                return ref _value.GetPinnableReference();
            }

            public unsafe IntPtr GetAbi()
            {
#if DEBUG
                // We assume that the string is pinned by the calling code
                Debug.Assert(_pinned);
#endif
                if (_value == "") 
                {
                    return IntPtr.Zero;
                }
                IntPtr hstring;
                Marshal.ThrowExceptionForHR(Platform.WindowsCreateStringReference(
                    (ushort*)Unsafe.AsPointer(ref Unsafe.AsRef(in GetPinnableReference())),
                    _value.Length,
                    (IntPtr*)Unsafe.AsPointer(ref _header),
                    &hstring));
                return hstring;
            }
        }

        public static Pinnable CreatePinnable(string value) => new(value);

        public static IntPtr GetAbi(ref Pinnable p) => p.GetAbi();

        public MarshalString(string value)
        {
            _gchandle = GCHandle.Alloc(value, GCHandleType.Pinned);
            _header = IntPtr.Zero;
        }

        public void Dispose()
        {
            _gchandle.Dispose();
            _gchandle = default;
            Marshal.FreeHGlobal(_header);
            _header = IntPtr.Zero;
        }

        public static MarshalString CreateMarshaler(string value)
        {
            return string.IsNullOrEmpty(value) ? null : new MarshalString(value);
        }

        public unsafe IntPtr GetAbi()
        {
            var value = (string)_gchandle.Target;
            fixed (char* chars = value)
            { 
                IntPtr hstring;
                Debug.Assert(_header == IntPtr.Zero);
                _header = Marshal.AllocHGlobal(Unsafe.SizeOf<HSTRING_HEADER>());
                Marshal.ThrowExceptionForHR(Platform.WindowsCreateStringReference(
                    (ushort*)chars, value.Length, (IntPtr*)_header, &hstring));
                return hstring;
            }
        }

        public static IntPtr GetAbi(MarshalString m) => m is null ? IntPtr.Zero : m.GetAbi();

        public static IntPtr GetAbi(object box) => box is null ? IntPtr.Zero : GetAbi((MarshalString)box);

        public static void DisposeMarshaler(MarshalString m) => m?.Dispose();

        public static void DisposeMarshaler(object box)
        {
            if (box != null)
                DisposeMarshaler(((MarshalString)box));
        }

        public static void DisposeAbi(IntPtr hstring)
        {
            if (hstring != IntPtr.Zero)
                Platform.WindowsDeleteString(hstring);
        }

        public static void DisposeAbi(object abi)
        {
            if (abi != null)
                DisposeAbi(((IntPtr)abi));
        }

        public static unsafe string FromAbi(IntPtr value)
        {
            if (value == IntPtr.Zero)
                return "";
            uint length;
            var buffer = Platform.WindowsGetStringRawBuffer(value, &length);
            return new string(buffer, 0, (int)length);
        }

        public static unsafe IntPtr FromManaged(string value)
        {
            if (value is null)
            {
                return IntPtr.Zero;
            }
            IntPtr handle;
            fixed (char* lpValue = value)
            {
                Marshal.ThrowExceptionForHR(
                    Platform.WindowsCreateString((ushort*)lpValue, value.Length, &handle));
            }
            return handle;
        }

        public struct MarshalerArray
        {
            public void Dispose()
            {
                if (_marshalers != null)
                {
                    foreach (var marshaler in _marshalers)
                    {
                        marshaler?.Dispose();
                    }
                }
                if (_array != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(_array);
                }
            }

            public IntPtr _array;
            public MarshalString[] _marshalers;
        }

        public static unsafe MarshalerArray CreateMarshalerArray(string[] array)
        {
            var m = new MarshalerArray();
            if (array is null)
            {
                return m;
            }
            bool success = false;
            try
            {
                var length = array.Length;
                m._array = Marshal.AllocCoTaskMem(length * Marshal.SizeOf<IntPtr>());
                m._marshalers = new MarshalString[length];
                var elements = (IntPtr*)m._array.ToPointer();
                for (int i = 0; i < length; i++)
                {
                    m._marshalers[i] = MarshalString.CreateMarshaler(array[i]);
                    elements[i] = MarshalString.GetAbi(m._marshalers[i]);
                };
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

        public static (int length, IntPtr data) GetAbiArray(object box)
        {
            var m = (MarshalerArray)box;
            return (m._marshalers?.Length ?? 0, m._array);
        }

        public static unsafe string[] FromAbiArray(object box)
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
            string[] array = new string[abi.length];
            var data = (IntPtr*)abi.data.ToPointer();
            for (int i = 0; i < abi.length; i++)
            {
                array[i] = MarshalString.FromAbi(data[i]);
            }
            return array;
        }

        public static unsafe void CopyAbiArray(string[] array, object box)
        {
            var abi = ((int length, IntPtr data))box;
            var data = (IntPtr*)abi.data.ToPointer();
            for (int i = 0; i < abi.length; i++)
            {
                array[i] = MarshalString.FromAbi(data[i]);
            }
        }

        public static unsafe (int length, IntPtr data) FromManagedArray(string[] array)
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
                var length = array.Length;
                data = Marshal.AllocCoTaskMem(length * Marshal.SizeOf<IntPtr>());
                var elements = (IntPtr*)data;
                for (i = 0; i < length; i++)
                {
                    elements[i] = MarshalString.FromManaged(array[i]);
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

        public static unsafe void CopyManagedArray(string[] array, IntPtr data)
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
                var length = array.Length;
                var elements = (IntPtr*)data;
                for (i = 0; i < length; i++)
                {
                    elements[i] = MarshalString.FromManaged(array[i]);
                };
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

        public static void DisposeMarshalerArray(object box)
        {
            if (box != null)
                ((MarshalerArray)box).Dispose();
        }

        public static unsafe void DisposeAbiArrayElements((int length, IntPtr data) abi)
        {
            var elements = (IntPtr*)abi.data;
            for (int i = 0; i < abi.length; i++)
            {
                DisposeAbi(elements[i]);
            }
        }

        public static unsafe void DisposeAbiArray(object box)
        {
            if (box == null) return;
            var abi = ((int length, IntPtr data))box;
            DisposeAbiArrayElements(abi);
            Marshal.FreeCoTaskMem(abi.data);
        }
    }

#if EMBED
    internal
#else
    public
#endif
    struct MarshalBlittable<T>
    {
        public struct MarshalerArray
        {
            public MarshalerArray(Array array) => _gchandle = array is null ? default : GCHandle.Alloc(array, GCHandleType.Pinned);
            public void Dispose() => _gchandle.Dispose();

            public GCHandle _gchandle;
        };

        public static MarshalerArray CreateMarshalerArray(Array array) => new MarshalerArray(array);

        public static (int length, IntPtr data) GetAbiArray(object box)
        {
            var m = (MarshalerArray)box;
            return m._gchandle.IsAllocated ? (((Array)m._gchandle.Target).Length, m._gchandle.AddrOfPinnedObject()) : (0, IntPtr.Zero);
        }

        public static unsafe T[] FromAbiArray(object box)
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

            // For empty arrays, we can end up returning the same managed object
            // when using ReadOnlySpan.ToArray. But a unique object is expected
            // by the caller for RCW creation.
            if (abi.length == 0)
            {
                return new T[0];
            }

            var abiSpan = new ReadOnlySpan<T>(abi.data.ToPointer(), abi.length);
            return abiSpan.ToArray();
        }

        public static unsafe (int length, IntPtr data) FromManagedArray(Array array)
        {
            if (array is null)
            {
                return (0, IntPtr.Zero);
            }
            var length = array.Length;
            var byte_length = length * Marshal.SizeOf<T>();
            var data = Marshal.AllocCoTaskMem(byte_length);
            CopyManagedArray(array, data);
            return (length, data);
        }

        public static unsafe void CopyManagedArray(Array array, IntPtr data)
        {
            if (array is null)
            {
                return;
            }
            var length = array.Length;
            var byte_length = length * Marshal.SizeOf<T>();
            var array_handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            var array_data = array_handle.AddrOfPinnedObject();
            Buffer.MemoryCopy(array_data.ToPointer(), data.ToPointer(), byte_length, byte_length);
            array_handle.Free();
        }

        public static void DisposeMarshalerArray(object box)
        {
            if (box != null)
                ((MarshalerArray)box).Dispose();
        }

        public static void DisposeAbiArray(object box)
        {
            if (box == null) return;
            var abi = ((int length, IntPtr data))box;
            Marshal.FreeCoTaskMem(abi.data);
        }
    }

#if EMBED
    internal
#else
    public
#endif
    class MarshalGeneric<T>
    {
#if NET
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
#endif
        protected static readonly Type HelperType = typeof(T).GetHelperType();

        protected static readonly Type AbiType = typeof(T).GetAbiType();
        protected static readonly Type MarshalerType = typeof(T).GetMarshalerType();
        private static readonly bool MarshalByObjectReferenceValueSupported = typeof(T).GetMarshaler2Type() == typeof(ObjectReferenceValue);

        public static readonly Func<T, object> CreateMarshaler = (T value) => CreateMarshalerLazy.Value(value);
        private static readonly Lazy<Func<T, object>> CreateMarshalerLazy = new(BindCreateMarshaler);
        private static Func<T, object> BindCreateMarshaler()
        {
            var createMarshaler = HelperType.GetMethod("CreateMarshaler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return (T arg) => createMarshaler.Invoke(null, new object[] { arg });
        }

        internal static Func<T, object> CreateMarshaler2 => MarshalByObjectReferenceValueSupported ? CreateMarshaler2Lazy.Value : CreateMarshaler;
        private static readonly Lazy<Func<T, object>> CreateMarshaler2Lazy = new(BindCreateMarshaler2);
        private static Func<T, object> BindCreateMarshaler2()
        {
            var createMarshaler = (Func<T, ObjectReferenceValue>)HelperType.GetMethod("CreateMarshaler2", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).
                CreateDelegate(typeof(Func<T, ObjectReferenceValue>));
            return (T arg) => createMarshaler(arg);
        }

        public static readonly Func<object, object> GetAbi = MarshalByObjectReferenceValueSupported ? (object objRef) => Marshaler.GetAbi(objRef, GetAbiLazy) :
            (object objRef) => GetAbiLazy.Value(objRef);
        private static readonly Lazy<Func<object, object>> GetAbiLazy = new(BindGetAbi);
        private static Func<object, object> BindGetAbi()
        {
            var getAbi = HelperType.GetMethod("GetAbi", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return (object arg) => getAbi.Invoke(null, new[] { arg });
        }

        public static readonly Action<object, IntPtr> CopyAbi = (object box, IntPtr dest) => CopyAbiLazy.Value(box, dest);
        private static readonly Lazy<Action<object, IntPtr>> CopyAbiLazy = new(BindCopyAbi);
        private static Action<object, IntPtr> BindCopyAbi()
        {
            var copyAbi = HelperType.GetMethod("CopyAbi", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (copyAbi == null) return null;
            return (object arg, IntPtr dest) => copyAbi.Invoke(null, new[] { arg, dest });
        }

        public static readonly Func<object, T> FromAbi = (object box) => FromAbiLazy.Value(box);
        private static readonly Lazy<Func<object, T>> FromAbiLazy = new(BindFromAbi);
        private static Func<object, T> BindFromAbi()
        {
            var fromAbi = HelperType.GetMethod("FromAbi", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return (object arg) => (T)fromAbi.Invoke(null, new[] { arg });
        }

        public static readonly Func<T, object> FromManaged = (T value) => FromManagedLazy.Value(value);
        private static readonly Lazy<Func<T, object>> FromManagedLazy = new(BindFromManaged);
        private static Func<T, object> BindFromManaged()
        {
            var fromManaged = HelperType.GetMethod("FromManaged", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return (T arg) => fromManaged.Invoke(null, new object[] { arg });
        }

        public static readonly Action<T, IntPtr> CopyManaged = (T value, IntPtr dest) => CopyManagedLazy.Value(value, dest);
        private static readonly Lazy<Action<T, IntPtr>> CopyManagedLazy = new(BindCopyManaged);
        private static Action<T, IntPtr> BindCopyManaged()
        {
            var copyManaged = HelperType.GetMethod("CopyManaged", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (copyManaged == null) return null;
            return (T arg, IntPtr dest) => copyManaged.Invoke(null, new object[] { arg, dest });
        }

        public static readonly Action<object> DisposeMarshaler = MarshalByObjectReferenceValueSupported ? (object objRef) => Marshaler.DisposeMarshaler(objRef, DisposeMarshalerLazy) : (object objRef) => DisposeMarshalerLazy.Value(objRef);
        private static readonly Lazy<Action<object>> DisposeMarshalerLazy = new(BindDisposeMarshaler);
        private static Action<object> BindDisposeMarshaler()
        {
            var disposeMarshaler = HelperType.GetMethod("DisposeMarshaler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return (object arg) => disposeMarshaler.Invoke(null, new[] { arg });
        }

        internal static readonly Action<object> DisposeAbi = (object box) => DisposeAbiLazy.Value(box);
        private static readonly Lazy<Action<object>> DisposeAbiLazy = new(BindDisposeAbi);
        private static Action<object> BindDisposeAbi()
        {
            var disposeAbi = HelperType.GetMethod("DisposeAbi", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (disposeAbi == null) return null;
            return (object arg) => disposeAbi.Invoke(null, new[] { arg });
        }

        internal static readonly Func<T[], object> CreateMarshalerArray = (T[] array) => CreateMarshalerArrayLazy.Value(array);
        private static readonly Lazy<Func<T[], object>> CreateMarshalerArrayLazy = new(BindCreateMarshalerArray);
        private static Func<T[], object> BindCreateMarshalerArray()
        {
            var createMarshalerArray = HelperType.GetMethod("CreateMarshalerArray", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (createMarshalerArray == null) return null;
            return (T[] arg) => createMarshalerArray.Invoke(null, new object[] { arg });
        }

        internal static readonly Func<object, (int, IntPtr)> GetAbiArray = (object box) => GetAbiArrayLazy.Value(box);
        private static readonly Lazy<Func<object, (int, IntPtr)>> GetAbiArrayLazy = new(BindGetAbiArray);
        private static Func<object, (int, IntPtr)> BindGetAbiArray()
        {
            var getAbiArray = HelperType.GetMethod("GetAbiArray", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (getAbiArray == null) return null;
            return (object arg) => ((int, IntPtr))getAbiArray.Invoke(null, new object[] { arg });
        }

        internal static readonly Func<object, T[]> FromAbiArray = (object box) => FromAbiArrayLazy.Value(box);
        private static readonly Lazy<Func<object, T[]>> FromAbiArrayLazy = new(BindFromAbiArray);
        private static Func<object, T[]> BindFromAbiArray()
        {
            var fromAbiArray = HelperType.GetMethod("FromAbiArray", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (fromAbiArray == null) return null;
            return (object arg) => (T[])fromAbiArray.Invoke(null, new[] { arg });
        }

        internal static readonly Func<T[], (int, IntPtr)> FromManagedArray = (T[] array) => FromManagedArrayLazy.Value(array);
        private static readonly Lazy<Func<T[], (int, IntPtr)>> FromManagedArrayLazy = new(BindFromManagedArray);
        private static Func<T[], (int, IntPtr)> BindFromManagedArray()
        {
            var fromManagedArray = HelperType.GetMethod("FromManagedArray", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (fromManagedArray == null) return null;
            return (T[] arg) => ((int, IntPtr))fromManagedArray.Invoke(null, new object[] { arg });
        }

        internal static readonly Action<object> DisposeMarshalerArray = (object box) => DisposeMarshalerArrayLazy.Value(box);
        private static readonly Lazy<Action<object>> DisposeMarshalerArrayLazy = new(BindDisposeMarshalerArray);
        private static Action<object> BindDisposeMarshalerArray()
        {
            var disposeMarshalerArray = HelperType.GetMethod("DisposeMarshalerArray", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (disposeMarshalerArray == null) return null;
            return (object arg) => disposeMarshalerArray.Invoke(null, new object[] { arg });
        }

        internal static readonly Action<object> DisposeAbiArray = (object box) => DisposeAbiArrayLazy.Value(box);
        private static readonly Lazy<Action<object>> DisposeAbiArrayLazy = new(BindDisposeAbiArray);
        private static Action<object> BindDisposeAbiArray()
        {
            var disposeAbiArray = HelperType.GetMethod("DisposeAbiArray", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (disposeAbiArray == null) return null;
            return (object arg) => disposeAbiArray.Invoke(null, new object[] { arg });
        }

        private static unsafe void CopyManagedFallback(T value, IntPtr dest)
        {
            if (MarshalByObjectReferenceValueSupported)
            {
                *(IntPtr*)dest.ToPointer() =
                    (value is null) ? IntPtr.Zero : ((ObjectReferenceValue)CreateMarshaler2(value)).Detach();
            }
            else
            {
                *(IntPtr*)dest.ToPointer() =
                    (value is null) ? IntPtr.Zero : ((IObjectReference)CreateMarshaler(value)).GetRef();
            }
        }

        internal static unsafe void CopyManagedArray(T[] array, IntPtr data) => MarshalInterfaceHelper<T>.CopyManagedArray(array, data, CopyManagedLazy.Value ?? CopyManagedFallback);
    }

#if EMBED
    internal
#else
    public
#endif
    class MarshalNonBlittable<T> : MarshalGeneric<T>
    {
        private static readonly new Type AbiType = typeof(T).IsEnum ? Enum.GetUnderlyingType(typeof(T)) : MarshalGeneric<T>.AbiType;

        public struct MarshalerArray
        {
            public void Dispose()
            {
                if (_marshalers != null)
                {
                    foreach (var marshaler in _marshalers)
                    {
                        Marshaler<T>.DisposeMarshaler(marshaler);
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

        public static new unsafe MarshalerArray CreateMarshalerArray(T[] array)
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
                var abi_element_size = Marshal.SizeOf(AbiType);
                var byte_length = length * abi_element_size;
                m._array = Marshal.AllocCoTaskMem(byte_length);
                m._marshalers = new object[length];
                var element = (byte*)m._array.ToPointer();
                for (int i = 0; i < length; i++)
                {
                    m._marshalers[i] = Marshaler<T>.CreateMarshaler(array[i]);
                    Marshaler<T>.CopyAbi(m._marshalers[i], (IntPtr)element);
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

        public static new(int length, IntPtr data) GetAbiArray(object box)
        {
            var m = (MarshalerArray)box;
            return (m._marshalers?.Length ?? 0, m._array);
        }

        public static new unsafe T[] FromAbiArray(object box)
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
            var array = new T[abi.length];
            var data = (byte*)abi.data.ToPointer();
            var abi_element_size = Marshal.SizeOf(AbiType);
            for (int i = 0; i < abi.length; i++)
            {
                var abi_element = Marshal.PtrToStructure((IntPtr)data, AbiType);
                array[i] = Marshaler<T>.FromAbi(abi_element);
                data += abi_element_size;
            }
            return array;
        }

        public static unsafe void CopyAbiArray(T[] array, object box)
        {
            var abi = ((int length, IntPtr data))box;
            if (abi.data == IntPtr.Zero)
            {
                return;
            }
            var data = (byte*)abi.data.ToPointer();
            var abi_element_size = Marshal.SizeOf(AbiType);
            for (int i = 0; i < abi.length; i++)
            {
                var abi_element = Marshal.PtrToStructure((IntPtr)data, AbiType);
                array[i] = Marshaler<T>.FromAbi(abi_element);
                data += abi_element_size;
            }
        }

        public static new unsafe (int length, IntPtr data) FromManagedArray(T[] array)
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
                var abi_element_size = Marshal.SizeOf(AbiType);
                var byte_length = length * abi_element_size;
                data = Marshal.AllocCoTaskMem(byte_length);
                var bytes = (byte*)data.ToPointer();
                for (i = 0; i < length; i++)
                {
                    Marshaler<T>.CopyManaged(array[i], (IntPtr)bytes);
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

        public static new unsafe void CopyManagedArray(T[] array, IntPtr data)
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
                var abi_element_size = Marshal.SizeOf(AbiType);
                var byte_length = length * abi_element_size;
                var bytes = (byte*)data.ToPointer();
                for (i = 0; i < length; i++)
                {
                    Marshaler<T>.CopyManaged(array[i], (IntPtr)bytes);
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

        public static new void DisposeMarshalerArray(object box) => ((MarshalerArray)box).Dispose();

        public static unsafe void DisposeAbiArrayElements((int length, IntPtr data) abi)
        {
            var data = (byte*)abi.data.ToPointer();
            var abi_element_size = Marshal.SizeOf(AbiType);
            for (int i = 0; i < abi.length; i++)
            {
                var abi_element = Marshal.PtrToStructure((IntPtr)data, AbiType);
                Marshaler<T>.DisposeAbi(abi_element);
                data += abi_element_size;
            }
        }

        public static new unsafe void DisposeAbiArray(object box)
        {
            if (box == null) return;
            var abi = ((int length, IntPtr data))box;
            if (abi.data == IntPtr.Zero) return;
            DisposeAbiArrayElements(abi);
            Marshal.FreeCoTaskMem(abi.data);
        }
    }

#if EMBED
    internal
#else
    public
#endif
    class MarshalInterfaceHelper<T>
    {
        public struct MarshalerArray
        {
            public void Dispose()
            {
                if (_marshalers != null)
                {
                    foreach (var marshaler in _marshalers)
                    {
                        DisposeMarshaler(marshaler);
                    }
                }

                if (_objectReferenceValues != null)
                {
                    foreach (var objectReferenceValue in _objectReferenceValues)
                    {
                        objectReferenceValue.Dispose();
                    }
                }

                if (_array != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(_array);
                }
            }

            public IntPtr _array;
            public IObjectReference[] _marshalers;
            internal ObjectReferenceValue[] _objectReferenceValues;
        }

        private static unsafe MarshalerArray CreateMarshalerArray(
            T[] array, 
            Func<T, IObjectReference> createMarshaler,
            Func<T, ObjectReferenceValue> createMarshaler2)
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
                var byte_length = length * IntPtr.Size;
                m._array = Marshal.AllocCoTaskMem(byte_length);
                var element = (IntPtr*)m._array.ToPointer();
                if (createMarshaler2 != null)
                {
                    m._objectReferenceValues = new ObjectReferenceValue[length];
                    for (int i = 0; i < length; i++)
                    {
                        m._objectReferenceValues[i] = createMarshaler2(array[i]);
                        element[i] = GetAbi(m._objectReferenceValues[i]);
                    }
                }
                else
                {
                    m._marshalers = new IObjectReference[length];
                    for (int i = 0; i < length; i++)
                    {
                        m._marshalers[i] = createMarshaler(array[i]);
                        element[i] = GetAbi(m._marshalers[i]);
                    }
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

        public static unsafe MarshalerArray CreateMarshalerArray(T[] array, Func<T, IObjectReference> createMarshaler) => 
            CreateMarshalerArray(array, createMarshaler, null);

        public static unsafe MarshalerArray CreateMarshalerArray2(T[] array, Func<T, ObjectReferenceValue> createMarshaler) => 
            CreateMarshalerArray(array, null, createMarshaler);

        public static (int length, IntPtr data) GetAbiArray(object box)
        {
            var m = (MarshalerArray)box;
            return (m._objectReferenceValues?.Length ?? m._marshalers?.Length ?? 0, m._array);
        }

        public static unsafe T[] FromAbiArray(object box, Func<IntPtr, T> fromAbi)
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
            var array = new T[abi.length];
            var data = (IntPtr*)abi.data.ToPointer();
            for (int i = 0; i < abi.length; i++)
            {
                array[i] = fromAbi(data[i]);
            }
            return array;
        }

        public static unsafe void CopyAbiArray(T[] array, object box, Func<IntPtr, T> fromAbi)
        {
            if (box is null)
            {
                return;
            }
            var abi = ((int length, IntPtr data))box;
            var data = (IntPtr*)abi.data.ToPointer();
            for (int i = 0; i < abi.length; i++)
            {
                array[i] = fromAbi(data[i]);
            }
        }

        public static unsafe (int length, IntPtr data) FromManagedArray(T[] array, Func<T, IntPtr> fromManaged)
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
                var byte_length = length * IntPtr.Size;
                data = Marshal.AllocCoTaskMem(byte_length);
                var native = (IntPtr*)data.ToPointer();
                for (i = 0; i < length; i++)
                {
                    native[i] = fromManaged(array[i]);
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

        public static unsafe void CopyManagedArray(T[] array, IntPtr data, Action<T, IntPtr> copyManaged)
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
                var byte_length = length * IntPtr.Size;
                var bytes = (byte*)data.ToPointer();
                for (i = 0; i < length; i++)
                {
                    copyManaged(array[i], (IntPtr)bytes);
                    bytes += IntPtr.Size;
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

        public static void DisposeMarshalerArray(object box) => ((MarshalerArray)box).Dispose();

        public static unsafe void DisposeAbiArrayElements((int length, IntPtr data) abi)
        {
            var data = (IntPtr*)abi.data.ToPointer();
            for (int i = 0; i < abi.length; i++)
            {
                DisposeAbi(data[i]);
            }
        }

        public static unsafe void DisposeAbiArray(object box)
        {
            if (box == null) return;
            var abi = ((int length, IntPtr data))box;
            if (abi.data == IntPtr.Zero) return;
            DisposeAbiArrayElements(abi);
            Marshal.FreeCoTaskMem(abi.data);
        }

        public static IntPtr GetAbi(IObjectReference objRef)
        {
            return objRef?.ThisPtr ?? IntPtr.Zero;
        }

        public static IntPtr GetAbi(ObjectReferenceValue value) => value.GetAbi();

        public static void DisposeMarshaler(IObjectReference objRef) => objRef?.Dispose();

        public static void DisposeMarshaler(ObjectReferenceValue value) => value.Dispose();

        public static void DisposeAbi(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return;
            unsafe
            {
                (**(IUnknownVftbl**)ptr).Release(ptr);
            }
        }
    }

#if EMBED
    internal
#else
    public
#endif
    struct MarshalInterface<T>
    {
#if NET
        [DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicFields | 
            DynamicallyAccessedMemberTypes.NonPublicFields | 
            DynamicallyAccessedMemberTypes.PublicNestedTypes |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
#endif
        private static readonly Type HelperType = typeof(T).GetHelperType();
        private static Func<T, IObjectReference> _ToAbi;
        private static Func<T, IObjectReference> _CreateMarshaler;

        public static T FromAbi(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return (T)(object)null;
            }

            return MarshalInspectable<T>.FromAbi(ptr);
        }

        public static IObjectReference CreateMarshaler(T value)
        {
            if (value is null)
            {
                return null;
            }

            // If the value passed in is the native implementation of the interface
            // use the ToAbi delegate since it will be faster than reflection.
            if (value.GetType() == HelperType)
            {
                if (_ToAbi == null)
                {
                    _ToAbi = BindToAbi();
                }
                var ptr = _ToAbi(value).GetRef();
                // We can use ObjectReference.Attach here since this API is
                // only used during marshalling where we deterministically dispose
                // on the same thread (and as a result don't need to capture context).
                return ObjectReference<IUnknownVftbl>.Attach(ref ptr);
            }

            if (_CreateMarshaler is null)
            {
                _CreateMarshaler = BindCreateMarshaler();
            }

            return _CreateMarshaler(value);
        }

        public static ObjectReferenceValue CreateMarshaler2(T value, Guid iid = default)
        {
            if (value is null)
            {
                return new ObjectReferenceValue();
            }

            // If the value passed in is the native implementation of the interface
            // use the ToAbi delegate since it will be faster than reflection.
            if (value.GetType() == HelperType)
            {
                if (_ToAbi == null)
                {
                    _ToAbi = BindToAbi();
                }
                return _ToAbi(value).AsValue();
            }

            return MarshalInspectable<T>.CreateMarshaler2(value, iid == default ? GuidGenerator.GetIID(HelperType) : iid, true);
        }

        public static IntPtr GetAbi(IObjectReference value) =>
            value is null ? IntPtr.Zero : MarshalInterfaceHelper<T>.GetAbi(value);

        public static IntPtr GetAbi(ObjectReferenceValue value) => MarshalInterfaceHelper<T>.GetAbi(value);

        public static void DisposeAbi(IntPtr thisPtr) => MarshalInterfaceHelper<T>.DisposeAbi(thisPtr);

        public static void DisposeMarshaler(IObjectReference value) => MarshalInterfaceHelper<T>.DisposeMarshaler(value);

        public static void DisposeMarshaler(ObjectReferenceValue value) => MarshalInterfaceHelper<T>.DisposeMarshaler(value);

        internal static void DisposeMarshaler(object value)
        {
            if (value is ObjectReferenceValue objRefValue)
            {
                DisposeMarshaler(objRefValue);
            }
            else
            {
                DisposeMarshaler((IObjectReference)value);
            }
        }

        public static IntPtr FromManaged(T value)
        {
            return CreateMarshaler2(value).Detach();
        }

        public static unsafe void CopyManaged(T value, IntPtr dest)
        {
            *(IntPtr*)dest.ToPointer() = CreateMarshaler2(value).Detach();
        }

        public static MarshalInterfaceHelper<T>.MarshalerArray CreateMarshalerArray(T[] array) => MarshalInterfaceHelper<T>.CreateMarshalerArray2(array, (o) => CreateMarshaler2(o));

        public static (int length, IntPtr data) GetAbiArray(object box) => MarshalInterfaceHelper<T>.GetAbiArray(box);

        public static unsafe T[] FromAbiArray(object box) => MarshalInterfaceHelper<T>.FromAbiArray(box, FromAbi);

        public static unsafe void CopyAbiArray(T[] array, object box) => MarshalInterfaceHelper<T>.CopyAbiArray(array, box, FromAbi);

        public static unsafe (int length, IntPtr data) FromManagedArray(T[] array) => MarshalInterfaceHelper<T>.FromManagedArray(array, (o) => FromManaged(o));

        public static unsafe void CopyManagedArray(T[] array, IntPtr data) => MarshalInterfaceHelper<T>.CopyManagedArray(array, data, (o, dest) => CopyManaged(o, dest));

        public static void DisposeMarshalerArray(object box) => MarshalInterfaceHelper<T>.DisposeMarshalerArray(box);

        public static unsafe void DisposeAbiArray(object box) => MarshalInterfaceHelper<T>.DisposeAbiArray(box);

        private static Func<T, IObjectReference> BindToAbi()
        {
            var objField = HelperType.GetField("_obj", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            return (T arg) => (IObjectReference) objField.GetValue(arg);
        }

        private static Func<T, IObjectReference> BindCreateMarshaler()
        {
            var vftblType = HelperType.FindVftblType();
            Guid iid = GuidGenerator.GetIID(HelperType);
            if (vftblType is not null)
            {
                var methodInfo = typeof(MarshalInspectable<T>).GetMethod("CreateMarshaler", new Type[] { typeof(T), typeof(Guid), typeof(bool) }).
                    MakeGenericMethod(vftblType);
                var createMarshaler = (Func<T, Guid, bool, IObjectReference>) methodInfo.CreateDelegate(typeof(Func<T, Guid, bool, IObjectReference>));
                return obj => createMarshaler(obj, iid, true);
            }
            else
            {
                return obj => MarshalInspectable<T>.CreateMarshaler<IUnknownVftbl>(obj, iid, true);
            }
        }
    }

#if EMBED
    internal
#else 
    public
#endif
    static class MarshalInspectable<
#if NET6_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces | DynamicallyAccessedMemberTypes.NonPublicConstructors)]
#elif NET
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
        T>
    {
        public static IObjectReference CreateMarshaler<V>(
#if NET6_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
#elif NET
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
            T o,
            Guid iid,
            bool unwrapObject = true)
        {
            if (o is null)
            {
                return null;
            }

            if (unwrapObject && ComWrappersSupport.TryUnwrapObject(o, out var objRef))
            {
                return objRef.As<V>(iid);
            }
            var publicType = o.GetType();
            Type helperType = Projections.FindCustomHelperTypeMapping(publicType, true);
            if (helperType != null)
            {
                var createMarshaler = helperType.GetMethod("CreateMarshaler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                return (IObjectReference) createMarshaler.Invoke(null, new[] { (object) o });
            }

            return ComWrappersSupport.CreateCCWForObject<V>(o, iid);
        }

        public static IObjectReference CreateMarshaler(
#if NET6_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
#elif NET
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
            T o,
            bool unwrapObject = true)
        {
            return CreateMarshaler<IInspectable.Vftbl>(o, InterfaceIIDs.IInspectable_IID, unwrapObject);
        }

        public static ObjectReferenceValue CreateMarshaler2(
#if NET6_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
#elif NET
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
            T o,
            Guid iid,
            bool unwrapObject = true)
        {
            if (o is null)
            {
                return new ObjectReferenceValue();
            }

            if (unwrapObject && ComWrappersSupport.TryUnwrapObject(o, out var objRef))
            {
                return objRef.AsValue(iid);
            }
            var publicType = o.GetType();
            Type helperType = Projections.FindCustomHelperTypeMapping(publicType, true);
            if (helperType != null)
            {
                var createMarshaler = helperType.GetMethod("CreateMarshaler2", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                return ((ObjectReferenceValue)createMarshaler.Invoke(null, new[] { (object)o }));
            }

            return ComWrappersSupport.CreateCCWForObjectForMarshaling(o, iid);
        }

        public static ObjectReferenceValue CreateMarshaler2(
#if NET6_0_OR_GREATER
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
#elif NET
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
            T o, bool unwrapObject = true) => CreateMarshaler2(o, InterfaceIIDs.IInspectable_IID, unwrapObject);

        public static IntPtr GetAbi(IObjectReference objRef) =>
            objRef is null ? IntPtr.Zero : MarshalInterfaceHelper<T>.GetAbi(objRef);

        public static IntPtr GetAbi(ObjectReferenceValue value) => value.GetAbi();

        public static T FromAbi(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return default;
            }

            IntPtr iunknownPtr = IntPtr.Zero;
            try
            {
                Guid iid_iunknown = IUnknownVftbl.IID;
                Marshal.QueryInterface(ptr, ref iid_iunknown, out iunknownPtr);
                if (IUnknownVftbl.IsReferenceToManagedObject(iunknownPtr))
                {
                    // We use a global instance of ComWrappers, but it's possible to use different projections of the same type
                    // when both server and client are managed and in the same process.
                    // In this case, we need to check if the object is of the same type as the one we're trying to cast to.
                    // If it's not, we need to create an RCW for the object.
                    // We cannot use T directly here as it may lead to invalid cast due to the above reason.
                    if (ComWrappersSupport.FindObject<object>(iunknownPtr) is T obj)
                    {
                        return obj;
                    }
                    else
                    {
                        return ComWrappersSupport.CreateRcwForComObject<T>(ptr);
                    }
                }
                else
                {
                    return ComWrappersSupport.CreateRcwForComObject<T>(ptr);
                }
            }
            finally
            {
                DisposeAbi(iunknownPtr);
            }
        }

        public static void DisposeMarshaler(IObjectReference objRef) => MarshalInterfaceHelper<T>.DisposeMarshaler(objRef);

        public static void DisposeMarshaler(ObjectReferenceValue value) => value.Dispose();

        internal static void DisposeMarshaler(object value)
        {
            if (value is ObjectReferenceValue objRefValue)
            {
                DisposeMarshaler(objRefValue);
            }
            else
            {
                DisposeMarshaler((IObjectReference)value);
            }
        }

        public static void DisposeAbi(IntPtr ptr) => MarshalInterfaceHelper<T>.DisposeAbi(ptr);
        public static IntPtr FromManaged(T o, bool unwrapObject = true)
        {
            return CreateMarshaler2(o, unwrapObject).Detach();
        }

        public static unsafe void CopyManaged(T o, IntPtr dest, bool unwrapObject = true)
        {
            *(IntPtr*)dest.ToPointer() = CreateMarshaler2(o, unwrapObject).Detach();
        }

        public static MarshalInterfaceHelper<T>.MarshalerArray CreateMarshalerArray(T[] array) => MarshalInterfaceHelper<T>.CreateMarshalerArray2(array, (o) => CreateMarshaler2(o));

        public static (int length, IntPtr data) GetAbiArray(T box) => MarshalInterfaceHelper<T>.GetAbiArray(box);

        internal static (int length, IntPtr data) GetAbiArray(object box) => MarshalInterfaceHelper<T>.GetAbiArray(box);

        public static unsafe T[] FromAbiArray(T box) => MarshalInterfaceHelper<T>.FromAbiArray(box, FromAbi);

        internal static unsafe T[] FromAbiArray(object box) => MarshalInterfaceHelper<T>.FromAbiArray(box, FromAbi);

        public static unsafe (int length, IntPtr data) FromManagedArray(T[] array) => MarshalInterfaceHelper<T>.FromManagedArray(array, (o) => FromManaged(o));

        public static unsafe void CopyManagedArray(T[] array, IntPtr data) => MarshalInterfaceHelper<T>.CopyManagedArray(array, data, (o, dest) => CopyManaged(o, dest));

        public static void DisposeMarshalerArray(T box) => MarshalInterfaceHelper<T>.DisposeMarshalerArray(box);

        public static unsafe void DisposeAbiArray(T box) => MarshalInterfaceHelper<T>.DisposeAbiArray(box);

        internal static void DisposeMarshalerArray(object box) => MarshalInterfaceHelper<T>.DisposeMarshalerArray(box);

        internal static unsafe void DisposeAbiArray(object box) => MarshalInterfaceHelper<T>.DisposeAbiArray(box);

    }

#if EMBED
    internal
#else
    public
#endif
    static class MarshalDelegate
    {
        public static IObjectReference CreateMarshaler(object o, Guid delegateIID, bool unwrapObject = true)
        {
            if (o is null)
            {
                return null;
            }

            if (unwrapObject && ComWrappersSupport.TryUnwrapObject(o, out var objRef))
            {
                return objRef.As<global::WinRT.Interop.IDelegateVftbl>(delegateIID);
            }

            return ComWrappersSupport.CreateCCWForObject<global::WinRT.Interop.IDelegateVftbl>(o, delegateIID);
        }

        public static ObjectReferenceValue CreateMarshaler2(object o, Guid delegateIID, bool unwrapObject = true)
        {
            if (o is null)
            {
                return new ObjectReferenceValue();
            }

            if (unwrapObject && ComWrappersSupport.TryUnwrapObject(o, out var objRef))
            {
                return objRef.AsValue(delegateIID);
            }

            return ComWrappersSupport.CreateCCWForObjectForMarshaling(o, delegateIID);
        }

        public static T FromAbi<T>(IntPtr nativeDelegate)
            where T : System.Delegate
        {
            if (nativeDelegate == IntPtr.Zero)
            {
                return null;
            }
            else if (IUnknownVftbl.IsReferenceToManagedObject(nativeDelegate))
            {
                // We use a global instance of ComWrappers, but it's possible to use different projections of the same type
                // when both server and client are managed and in the same process.
                // In this case, we need to check if the object is of the same type as the one we're trying to cast to.
                // If it's not, we need to create an RCW for the object.
                // We cannot use T directly here as it may lead to invalid cast due to the above reason.
                if (ComWrappersSupport.FindObject<object>(nativeDelegate) is T obj)
                {
                    return obj;
                }
                else
                {
                    return ComWrappersSupport.CreateRcwForComObject<T>(nativeDelegate);
                }
            }
            else
            {
                return ComWrappersSupport.CreateRcwForComObject<T>(nativeDelegate);
            }
        }
    }

    internal static class Marshaler
    {
        internal static Action<object> EmptyFunc = (object box) => { };
        internal static Func<object, object> ReturnParameterFunc = (object box) => box;
        internal static unsafe Action<object, IntPtr> CopyIntEnumFunc = 
            (object value, IntPtr dest) => *(int*)dest.ToPointer() = (int)Convert.ChangeType(value, typeof(int));
        internal static unsafe Action<object, IntPtr> CopyUIntEnumFunc =
            (object value, IntPtr dest) => *(uint*)dest.ToPointer() = (uint)Convert.ChangeType(value, typeof(uint));
        internal static Action<object, Lazy<Action<object>>> DisposeMarshaler = (object arg, Lazy<Action<object>> genericDisposeMarshaler) =>
        {
            if (arg is ObjectReferenceValue objectReferenceValue)
            {
                objectReferenceValue.Dispose();
            }
            else
            {
                genericDisposeMarshaler.Value(arg);
            }
        };
        internal static Func<object, Lazy<Func<object, object>>, object> GetAbi = (object arg, Lazy<Func<object, object>> genericGetAbi) => arg is ObjectReferenceValue objectReferenceValue ? objectReferenceValue.GetAbi() : genericGetAbi.Value(arg);
    }

#if EMBED
    internal
#else
    public
#endif
    class Marshaler<T>
    {
        static Marshaler()
        {
            Type type = typeof(T);

            // structs cannot contain arrays, and arrays may only ever appear as parameters
            if (type.IsArray)
            {
                throw new InvalidOperationException("Arrays may not be marshaled generically.");
            }

            if (type == typeof(String))
            {
                AbiType = typeof(IntPtr);
                CreateMarshaler = (T value) => MarshalString.CreateMarshaler((string)(object)value);
                CreateMarshaler2 = CreateMarshaler;
                GetAbi = (object box) => MarshalString.GetAbi(box);
                FromAbi = (object value) => (T)(object)MarshalString.FromAbi((IntPtr)value);
                FromManaged = (T value) => MarshalString.FromManaged((string)(object)value);
                DisposeMarshaler = (object box) => MarshalString.DisposeMarshaler(box);
                DisposeAbi = (object box) => MarshalString.DisposeAbi(box);
                CreateMarshalerArray = (T[] array) => MarshalString.CreateMarshalerArray((string[])(object)array);
                GetAbiArray = (object box) => MarshalString.GetAbiArray(box);
                FromAbiArray = (object box) => (T[])(object)MarshalString.FromAbiArray(box);
                FromManagedArray = (T[] array) => MarshalString.FromManagedArray((string[])(object)array);
                CopyManagedArray = (T[] array, IntPtr data) => MarshalString.CopyManagedArray((string[])(object)array, data);
                DisposeMarshalerArray = (object box) => MarshalString.DisposeMarshalerArray(box);
                DisposeAbiArray = (object box) => MarshalString.DisposeAbiArray(box);
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.KeyValuePair<,>))
            {
                AbiType = typeof(IntPtr);
                CreateMarshaler = MarshalGeneric<T>.CreateMarshaler2;
                CreateMarshaler2 = MarshalGeneric<T>.CreateMarshaler2;
                GetAbi = MarshalGeneric<T>.GetAbi;
                CopyAbi = MarshalGeneric<T>.CopyAbi;
                FromAbi = MarshalGeneric<T>.FromAbi;
                FromManaged = MarshalGeneric<T>.FromManaged;
                CopyManaged = MarshalGeneric<T>.CopyManaged;
                DisposeMarshaler = MarshalGeneric<T>.DisposeMarshaler;
                DisposeAbi = MarshalGeneric<T>.DisposeAbi;
                CreateMarshalerArray = MarshalGeneric<T>.CreateMarshalerArray;
                GetAbiArray = MarshalGeneric<T>.GetAbiArray;
                FromAbiArray = MarshalGeneric<T>.FromAbiArray;
                FromManagedArray = MarshalGeneric<T>.FromManagedArray;
                CopyManagedArray = (T[] array, IntPtr data) => MarshalGeneric<T>.CopyManagedArray(array, data);
                DisposeMarshalerArray = (object box) => MarshalInterface<T>.DisposeMarshalerArray(box);
                DisposeAbiArray = (object box) => MarshalInterface<T>.DisposeAbiArray(box);
            }
            else if (type == typeof(Type))
            {
                AbiType = typeof(ABI.System.Type);
                CreateMarshaler = (T value) => ABI.System.Type.CreateMarshaler((Type)(object)value);
                CreateMarshaler2 = CreateMarshaler;
                GetAbi = (object box) => ABI.System.Type.GetAbi((ABI.System.Type.Marshaler)box);
                FromAbi = (object value) => (T)(object)ABI.System.Type.FromAbi((ABI.System.Type)value);
                CopyAbi = (object box, IntPtr dest) => ABI.System.Type.CopyAbi((ABI.System.Type.Marshaler)box, dest);
                CopyManaged = (T value, IntPtr dest) => ABI.System.Type.CopyManaged((global::System.Type)(object)value, dest);
                FromManaged = (T value) => ABI.System.Type.FromManaged((Type)(object)value);
                DisposeMarshaler = (object box) => ABI.System.Type.DisposeMarshaler((ABI.System.Type.Marshaler)box);
                DisposeAbi = (object box) => ABI.System.Type.DisposeAbi((ABI.System.Type)box);
                CreateMarshalerArray = (T[] array) => MarshalNonBlittable<T>.CreateMarshalerArray(array);
                GetAbiArray = (object box) => MarshalNonBlittable<T>.GetAbiArray(box);
                FromAbiArray = (object box) => MarshalNonBlittable<T>.FromAbiArray(box);
                FromManagedArray = (T[] array) => MarshalNonBlittable<T>.FromManagedArray(array);
                CopyManagedArray = (T[] array, IntPtr data) => MarshalNonBlittable<T>.CopyManagedArray(array, data);
                DisposeMarshalerArray = (object box) => MarshalNonBlittable<T>.DisposeMarshalerArray(box);
                DisposeAbiArray = (object box) => MarshalNonBlittable<T>.DisposeAbiArray(box);
            }
            else if (type.IsValueType)
            {
                if (type == typeof(bool))
                {
                    AbiType = typeof(byte);
                }
                else if (type == typeof(char))
                {
                    AbiType = typeof(ushort);
                }
                else
                {
                    AbiType = type.FindHelperType();
                    if (AbiType != null)
                    {
                        // Could still be blittable and the 'ABI.*' type exists for other reasons (e.g. it's a mapped type)
                        if (AbiType.GetMethod("FromAbi", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static) == null)
                        {
                            AbiType = null;
                        }
                    }
                }

                if (AbiType == null)
                {
                    Func<T, object> ReturnTypedParameterFunc = (T value) => value;
                    AbiType = type;
                    CreateMarshaler = ReturnTypedParameterFunc;
                    CreateMarshaler2 = CreateMarshaler;
                    GetAbi = Marshaler.ReturnParameterFunc;
                    FromAbi = (object value) => (T)value;
                    FromManaged = ReturnTypedParameterFunc;
                    DisposeMarshaler = Marshaler.EmptyFunc;
                    DisposeAbi = Marshaler.EmptyFunc;
                    if (type.IsEnum)
                    {
                        // For marshaling non-blittable enum arrays via MarshalNonBlittable
                        if (type.GetEnumUnderlyingType() == typeof(int))
                        {
                            CopyAbi = Marshaler.CopyIntEnumFunc;
                            CopyManaged = (T value, IntPtr dest) => Marshaler.CopyIntEnumFunc(value, dest);
                        }
                        else
                        {
                            CopyAbi = Marshaler.CopyUIntEnumFunc;
                            CopyManaged = (T value, IntPtr dest) => Marshaler.CopyUIntEnumFunc(value, dest);
                        }
                    }
                    CreateMarshalerArray = (T[] array) => MarshalBlittable<T>.CreateMarshalerArray(array);
                    GetAbiArray = (object box) => MarshalBlittable<T>.GetAbiArray(box);
                    FromAbiArray = (object box) => MarshalBlittable<T>.FromAbiArray(box);
                    FromManagedArray = (T[] array) => MarshalBlittable<T>.FromManagedArray(array);
                    CopyManagedArray = (T[] array, IntPtr data) => MarshalBlittable<T>.CopyManagedArray(array, data);
                    DisposeMarshalerArray = (object box) => MarshalBlittable<T>.DisposeMarshalerArray(box);
                    DisposeAbiArray = (object box) => MarshalBlittable<T>.DisposeAbiArray(box);
                }
                else
                {
                    CreateMarshaler = MarshalNonBlittable<T>.CreateMarshaler;
                    CreateMarshaler2 = CreateMarshaler;
                    GetAbi = MarshalNonBlittable<T>.GetAbi;
                    CopyAbi = MarshalNonBlittable<T>.CopyAbi;
                    FromAbi = MarshalNonBlittable<T>.FromAbi;
                    FromManaged = MarshalNonBlittable<T>.FromManaged;
                    CopyManaged = MarshalNonBlittable<T>.CopyManaged;
                    DisposeMarshaler = MarshalNonBlittable<T>.DisposeMarshaler;
                    DisposeAbi = MarshalNonBlittable<T>.DisposeAbi;
                    CreateMarshalerArray = (T[] array) => MarshalNonBlittable<T>.CreateMarshalerArray(array);
                    GetAbiArray = (object box) => MarshalNonBlittable<T>.GetAbiArray(box);
                    FromAbiArray = (object box) => MarshalNonBlittable<T>.FromAbiArray(box);
                    FromManagedArray = (T[] array) => MarshalNonBlittable<T>.FromManagedArray(array);
                    CopyManagedArray = (T[] array, IntPtr data) => MarshalNonBlittable<T>.CopyManagedArray(array, data);
                    DisposeMarshalerArray = (object box) => MarshalNonBlittable<T>.DisposeMarshalerArray(box);
                    DisposeAbiArray = (object box) => MarshalNonBlittable<T>.DisposeAbiArray(box);
                }
            }
            else if (type.IsInterface)
            {
                AbiType = typeof(IntPtr);
                CreateMarshaler = (T value) => MarshalInterface<T>.CreateMarshaler2(value);
                CreateMarshaler2 = CreateMarshaler;
                GetAbi = (object objRef) => objRef is ObjectReferenceValue objRefValue ? 
                    MarshalInspectable<object>.GetAbi(objRefValue) : MarshalInterface<T>.GetAbi((IObjectReference)objRef);
                FromAbi = (object value) => MarshalInterface<T>.FromAbi((IntPtr)value);
                FromManaged = (T value) => MarshalInterface<T>.CreateMarshaler2(value).Detach();
                DisposeMarshaler = MarshalInterface<T>.DisposeMarshaler;
                DisposeAbi = (object box) => MarshalInterface<T>.DisposeAbi((IntPtr)box);
                CreateMarshalerArray = (T[] array) => MarshalInterface<T>.CreateMarshalerArray(array);
                GetAbiArray = (object box) => MarshalInterface<T>.GetAbiArray(box);
                FromAbiArray = (object box) => MarshalInterface<T>.FromAbiArray(box);
                FromManagedArray = (T[] array) => MarshalInterface<T>.FromManagedArray(array);
                CopyManagedArray = (T[] array, IntPtr data) => MarshalInterface<T>.CopyManagedArray(array, data);
                DisposeMarshalerArray = (object box) => MarshalInterface<T>.DisposeMarshalerArray(box);
                DisposeAbiArray = (object box) => MarshalInterface<T>.DisposeAbiArray(box);
            }
            else if (typeof(T) == typeof(object))
            {
                AbiType = typeof(IntPtr);
                CreateMarshaler = (T value) => MarshalInspectable<T>.CreateMarshaler2(value);
                CreateMarshaler2 = CreateMarshaler;
                GetAbi = (object objRef) => objRef is ObjectReferenceValue objRefValue ? 
                    MarshalInspectable<T>.GetAbi(objRefValue) : MarshalInspectable<T>.GetAbi((IObjectReference)objRef);
                FromAbi = (object box) => MarshalInspectable<T>.FromAbi((IntPtr)box);
                FromManaged = (T value) => MarshalInspectable<T>.FromManaged(value);
                CopyManaged = (T value, IntPtr dest) => MarshalInspectable<T>.CopyManaged(value, dest);
                DisposeMarshaler = MarshalInspectable<T>.DisposeMarshaler;
                DisposeAbi = (object box) => MarshalInspectable<T>.DisposeAbi((IntPtr)box);
                CreateMarshalerArray = (T[] array) => MarshalInspectable<T>.CreateMarshalerArray(array);
                GetAbiArray = (object box) => MarshalInspectable<T>.GetAbiArray(box);
                FromAbiArray = (object box) => MarshalInspectable<T>.FromAbiArray(box);
                FromManagedArray = (T[] array) => MarshalInspectable<T>.FromManagedArray(array);
                CopyManagedArray = (T[] array, IntPtr data) => MarshalInspectable<T>.CopyManagedArray(array, data);
                DisposeMarshalerArray = (object box) => MarshalInspectable<T>.DisposeMarshalerArray(box);
                DisposeAbiArray = (object box) => MarshalInspectable<T>.DisposeAbiArray(box);
            }
            else // delegate, class 
            {
                AbiType = typeof(IntPtr);
                // Prior to CsWinRT 1.3.1, generic marshalers were used for delegates and they assumed the marshaler type was IObjectReference.
                // Due to that, not updating the CreateMarshaler to the new version in that instance, and only updating it for new code using
                // CreateMarshaler2.
                CreateMarshaler = typeof(T).IsDelegate() ? MarshalGeneric<T>.CreateMarshaler : MarshalGeneric<T>.CreateMarshaler2;
                CreateMarshaler2 = MarshalGeneric<T>.CreateMarshaler2;
                GetAbi = MarshalGeneric<T>.GetAbi;
                FromAbi = MarshalGeneric<T>.FromAbi;
                FromManaged = MarshalGeneric<T>.FromManaged;
                CopyManaged = MarshalGeneric<T>.CopyManaged;
                DisposeMarshaler = MarshalGeneric<T>.DisposeMarshaler;
                DisposeAbi = MarshalGeneric<T>.DisposeAbi;
                CreateMarshalerArray = MarshalGeneric<T>.CreateMarshalerArray;
                GetAbiArray = MarshalGeneric<T>.GetAbiArray;
                FromAbiArray = MarshalGeneric<T>.FromAbiArray;
                FromManagedArray = MarshalGeneric<T>.FromManagedArray;
                CopyManagedArray = (T[] array, IntPtr data) => MarshalGeneric<T>.CopyManagedArray(array, data);
                DisposeMarshalerArray = MarshalGeneric<T>.DisposeMarshalerArray;
                DisposeAbiArray = MarshalGeneric<T>.DisposeAbiArray;
            }
            RefAbiType = AbiType.MakeByRefType();
        }

#if NET
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
#endif
        public static readonly Type AbiType;
        public static readonly Type RefAbiType;
        public static readonly Func<T, object> CreateMarshaler;
        internal static readonly Func<T, object> CreateMarshaler2;
        public static readonly Func<object, object> GetAbi;
        public static readonly Action<object, IntPtr> CopyAbi;
        public static readonly Func<object, T> FromAbi;
        public static readonly Func<T, object> FromManaged;
        public static readonly Action<T, IntPtr> CopyManaged;
        public static readonly Action<object> DisposeMarshaler;
        public static readonly Action<object> DisposeAbi;
        public static readonly Func<T[], object> CreateMarshalerArray;
        public static readonly Func<object, (int, IntPtr)> GetAbiArray;
        public static readonly Func<object, T[]> FromAbiArray;
        public static readonly Func<T[], (int, IntPtr)> FromManagedArray;
        public static readonly Action<T[], IntPtr> CopyManagedArray;
        public static readonly Action<object> DisposeMarshalerArray;
        public static readonly Action<object> DisposeAbiArray;
    }
}
