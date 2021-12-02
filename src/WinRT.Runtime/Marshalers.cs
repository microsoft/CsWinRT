// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
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
                    (char*)Unsafe.AsPointer(ref Unsafe.AsRef(in GetPinnableReference())),
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
                    chars, value.Length, (IntPtr*)_header, &hstring));
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
            Marshal.ThrowExceptionForHR(
                Platform.WindowsCreateString(value, value.Length, &handle));
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
        protected static readonly Type HelperType = typeof(T).GetHelperType();
        protected static readonly Type AbiType = typeof(T).GetAbiType();
        protected static readonly Type MarshalerType = typeof(T).GetMarshalerType();
        internal static readonly Type MarshalerArrayType = typeof(T).GetMarshalerArrayType();

        public static readonly Func<T, object> CreateMarshaler = (T value) => CreateMarshalerLazy.Value(value);
        private static readonly Lazy<Func<T, object>> CreateMarshalerLazy = new(BindCreateMarshaler);
        private static Func<T, object> BindCreateMarshaler()
        {
            var parms = new[] { Expression.Parameter(typeof(T), "arg") };
            return Expression.Lambda<Func<T, object>>(
                Expression.Convert(Expression.Call(HelperType.GetMethod("CreateMarshaler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static), parms),
                    typeof(object)), parms).Compile();
        }

        public static readonly Func<object, object> GetAbi = (object objRef) => GetAbiLazy.Value(objRef);
        private static readonly Lazy<Func<object, object>> GetAbiLazy = new(BindGetAbi);
        private static Func<object, object> BindGetAbi()
        {
            var parms = new[] { Expression.Parameter(typeof(object), "arg") };
            return Expression.Lambda<Func<object, object>>(
                Expression.Convert(Expression.Call(HelperType.GetMethod("GetAbi", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
                    new[] { Expression.Convert(parms[0], MarshalerType) }),
                        typeof(object)), parms).Compile();
        }

        public static readonly Action<object, IntPtr> CopyAbi = (object box, IntPtr dest) => CopyAbiLazy.Value(box, dest);
        private static readonly Lazy<Action<object, IntPtr>> CopyAbiLazy = new(BindCopyAbi);
        private static Action<object, IntPtr> BindCopyAbi()
        {
            var copyAbi = HelperType.GetMethod("CopyAbi", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (copyAbi == null) return null;
            var parms = new[] { Expression.Parameter(typeof(object), "arg"), Expression.Parameter(typeof(IntPtr), "dest") };
            return Expression.Lambda<Action<object, IntPtr>>(
                Expression.Call(copyAbi,
                    new Expression[] { Expression.Convert(parms[0], MarshalerType), parms[1] }), parms).Compile();
        }

        public static readonly Func<object, T> FromAbi = (object box) => FromAbiLazy.Value(box);
        private static readonly Lazy<Func<object, T>> FromAbiLazy = new(BindFromAbi);
        private static Func<object, T> BindFromAbi()
        {
            var parms = new[] { Expression.Parameter(typeof(object), "arg") };
            return Expression.Lambda<Func<object, T>>(
                Expression.Call(HelperType.GetMethod("FromAbi", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
                    new[] { Expression.Convert(parms[0], AbiType) }), parms).Compile();
        }

        public static readonly Func<T, object> FromManaged = (T value) => FromManagedLazy.Value(value);
        private static readonly Lazy<Func<T, object>> FromManagedLazy = new(BindFromManaged);
        private static Func<T, object> BindFromManaged()
        {
            var parms = new[] { Expression.Parameter(typeof(T), "arg") };
            return Expression.Lambda<Func<T, object>>(
                Expression.Convert(Expression.Call(HelperType.GetMethod("FromManaged", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static), parms),
                    typeof(object)), parms).Compile();
        }

        public static readonly Action<T, IntPtr> CopyManaged = (T value, IntPtr dest) => CopyManagedLazy.Value(value, dest);
        private static readonly Lazy<Action<T, IntPtr>> CopyManagedLazy = new(BindCopyManaged);
        private static Action<T, IntPtr> BindCopyManaged()
        {
            var copyManaged = HelperType.GetMethod("CopyManaged", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (copyManaged == null) return null;
            var parms = new[] { Expression.Parameter(typeof(T), "arg"), Expression.Parameter(typeof(IntPtr), "dest") };
            return Expression.Lambda<Action<T, IntPtr>>(
                Expression.Call(copyManaged, parms), parms).Compile();
        }

        public static readonly Action<object> DisposeMarshaler = (object objRef) => DisposeMarshalerLazy.Value(objRef);
        private static readonly Lazy<Action<object>> DisposeMarshalerLazy = new(BindDisposeMarshaler);
        private static Action<object> BindDisposeMarshaler()
        {
            var parms = new[] { Expression.Parameter(typeof(object), "arg") };
            return Expression.Lambda<Action<object>>(
                Expression.Call(HelperType.GetMethod("DisposeMarshaler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
                    new[] { Expression.Convert(parms[0], MarshalerType) }), parms).Compile();
        }

        internal static readonly Action<object> DisposeAbi = (object box) => DisposeAbiLazy.Value(box);
        private static readonly Lazy<Action<object>> DisposeAbiLazy = new(BindDisposeAbi);
        private static Action<object> BindDisposeAbi()
        {
            var disposeAbi = HelperType.GetMethod("DisposeAbi", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (disposeAbi == null) return null;
            var parms = new[] { Expression.Parameter(typeof(object), "arg") };
            return Expression.Lambda<Action<object>>(
                Expression.Call(disposeAbi, new[] { Expression.Convert(parms[0], AbiType) }), parms).Compile();
        }

        internal static readonly Func<T[], object> CreateMarshalerArray = (T[] array) => CreateMarshalerArrayLazy.Value(array);
        private static readonly Lazy<Func<T[], object>> CreateMarshalerArrayLazy = new(BindCreateMarshalerArray);
        private static Func<T[], object> BindCreateMarshalerArray()
        {
            var createMarshalerArray = HelperType.GetMethod("CreateMarshalerArray", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (createMarshalerArray == null) return null;
            var parms = new[] { Expression.Parameter(typeof(T[]), "arg") };
            return Expression.Lambda<Func<T[], object>>(
                Expression.Convert(Expression.Call(createMarshalerArray, parms), typeof(object)), parms).Compile();
        }

        internal static readonly Func<object, (int, IntPtr)> GetAbiArray = (object box) => GetAbiArrayLazy.Value(box);
        private static readonly Lazy<Func<object, (int, IntPtr)>> GetAbiArrayLazy = new(BindGetAbiArray);
        private static Func<object, (int, IntPtr)> BindGetAbiArray()
        {
            var getAbiArray = HelperType.GetMethod("GetAbiArray", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (getAbiArray == null) return null;
            var parms = new[] { Expression.Parameter(typeof(object), "arg") };
            return Expression.Lambda<Func<object, (int, IntPtr)>>(
                Expression.Convert(Expression.Call(getAbiArray, parms), typeof((int, IntPtr))), parms).Compile();
        }

        internal static readonly Func<object, T[]> FromAbiArray = (object box) => FromAbiArrayLazy.Value(box);
        private static readonly Lazy<Func<object, T[]>> FromAbiArrayLazy = new(BindFromAbiArray);
        private static Func<object, T[]> BindFromAbiArray()
        {
            var fromAbiArray = HelperType.GetMethod("FromAbiArray", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (fromAbiArray == null) return null;
            var parms = new[] { Expression.Parameter(typeof(object), "arg") };
            return Expression.Lambda<Func<object, T[]>>(
                Expression.Call(fromAbiArray, parms), parms).Compile();
        }

        internal static readonly Func<T[], (int, IntPtr)> FromManagedArray = (T[] array) => FromManagedArrayLazy.Value(array);
        private static readonly Lazy<Func<T[], (int, IntPtr)>> FromManagedArrayLazy = new(BindFromManagedArray);
        private static Func<T[], (int, IntPtr)> BindFromManagedArray()
        {
            var fromManagedArray = HelperType.GetMethod("FromManagedArray", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (fromManagedArray == null) return null;
            var parms = new[] { Expression.Parameter(typeof(T[]), "arg") };
            return Expression.Lambda<Func<T[], (int, IntPtr)>>(
                Expression.Convert(Expression.Call(fromManagedArray, parms), typeof((int, IntPtr))), parms).Compile();
        }

        internal static readonly Action<object> DisposeMarshalerArray = (object box) => DisposeMarshalerArrayLazy.Value(box);
        private static readonly Lazy<Action<object>> DisposeMarshalerArrayLazy = new(BindDisposeMarshalerArray);
        private static Action<object> BindDisposeMarshalerArray()
        {
            var disposeMarshalerArray = HelperType.GetMethod("DisposeMarshalerArray", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (disposeMarshalerArray == null) return null;
            var parms = new[] { Expression.Parameter(typeof(object), "arg") };
            return Expression.Lambda<Action<object>>(
                Expression.Call(disposeMarshalerArray, Expression.Convert(parms[0], MarshalerArrayType)), parms).Compile();
        }

        internal static readonly Action<object> DisposeAbiArray = (object box) => DisposeAbiArrayLazy.Value(box);
        private static readonly Lazy<Action<object>> DisposeAbiArrayLazy = new(BindDisposeAbiArray);
        private static Action<object> BindDisposeAbiArray()
        {
            var disposeAbiArray = HelperType.GetMethod("DisposeAbiArray", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            if (disposeAbiArray == null) return null;
            var parms = new[] { Expression.Parameter(typeof(object), "arg") };
            return Expression.Lambda<Action<object>>(
                Expression.Call(disposeAbiArray, parms), parms).Compile();
        }

        private static unsafe void CopyManagedFallback(T value, IntPtr dest)
        {
            *(IntPtr*)dest.ToPointer() =
                (value is null) ? IntPtr.Zero : ((IObjectReference) CreateMarshaler(value)).GetRef();
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
                if (_array != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(_array);
                }
            }

            public IntPtr _array;
            public IObjectReference[] _marshalers;
        }

        public static unsafe MarshalerArray CreateMarshalerArray(T[] array, Func<T, IObjectReference> createMarshaler)
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
                m._marshalers = new IObjectReference[length];
                var element = (IntPtr*)m._array.ToPointer();
                for (int i = 0; i < length; i++)
                {
                    m._marshalers[i] = createMarshaler(array[i]);
                    element[i] = GetAbi(m._marshalers[i]);
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

        public static (int length, IntPtr data) GetAbiArray(object box)
        {
            var m = (MarshalerArray)box;
            return (m._marshalers?.Length ?? 0, m._array);
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

        public static void DisposeMarshaler(IObjectReference objRef)
        {
            objRef?.Dispose();
        }

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
        private static readonly Type HelperType = typeof(T).GetHelperType();
        private static Func<T, IObjectReference> _ToAbi;
        private static Func<IntPtr, T> _FromAbi;
        private static Func<IObjectReference, IObjectReference> _As;

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

            if (_As is null)
            {
                _As = BindAs();
            }

            var inspectable = MarshalInspectable<T>.CreateMarshaler(value, true);

            return _As(inspectable);
        }

        public static IntPtr GetAbi(IObjectReference value) => 
            value is null ? IntPtr.Zero : MarshalInterfaceHelper<T>.GetAbi(value);

        public static void DisposeAbi(IntPtr thisPtr) => MarshalInterfaceHelper<T>.DisposeAbi(thisPtr);

        public static void DisposeMarshaler(IObjectReference value) => MarshalInterfaceHelper<T>.DisposeMarshaler(value);

        public static IntPtr FromManaged(T value)
        {
            if (value is null)
            {
                return IntPtr.Zero;
            }
            return CreateMarshaler(value).GetRef();
        }

        public static unsafe void CopyManaged(T value, IntPtr dest)
        {
            *(IntPtr*)dest.ToPointer() =
                (value is null) ? IntPtr.Zero : CreateMarshaler(value).GetRef();
        }

        public static unsafe MarshalInterfaceHelper<T>.MarshalerArray CreateMarshalerArray(T[] array) => MarshalInterfaceHelper<T>.CreateMarshalerArray(array, (o) => CreateMarshaler(o));

        public static (int length, IntPtr data) GetAbiArray(object box) => MarshalInterfaceHelper<T>.GetAbiArray(box);

        public static unsafe T[] FromAbiArray(object box) => MarshalInterfaceHelper<T>.FromAbiArray(box, FromAbi);

        public static unsafe void CopyAbiArray(T[] array, object box) => MarshalInterfaceHelper<T>.CopyAbiArray(array, box, FromAbi);

        public static unsafe (int length, IntPtr data) FromManagedArray(T[] array) => MarshalInterfaceHelper<T>.FromManagedArray(array, (o) => FromManaged(o));

        public static unsafe void CopyManagedArray(T[] array, IntPtr data) => MarshalInterfaceHelper<T>.CopyManagedArray(array, data, (o, dest) => CopyManaged(o, dest));

        public static void DisposeMarshalerArray(object box) => MarshalInterfaceHelper<T>.DisposeMarshalerArray(box);

        public static unsafe void DisposeAbiArray(object box) => MarshalInterfaceHelper<T>.DisposeAbiArray(box);

        private static Func<IntPtr, T> BindFromAbi()
        {
            var fromAbiMethod = HelperType.GetMethod("FromAbi", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            var objReferenceConstructor = HelperType.GetConstructor(BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance, null, new[] { fromAbiMethod.ReturnType }, null);
            var parms = new[] { Expression.Parameter(typeof(IntPtr), "arg") };
            return Expression.Lambda<Func<IntPtr, T>>(
                    Expression.New(objReferenceConstructor,
                        Expression.Call(fromAbiMethod, parms[0])), parms).Compile();
        }

        private static Func<T, IObjectReference> BindToAbi()
        {
            var parms = new[] { Expression.Parameter(typeof(T), "arg") };
            return Expression.Lambda<Func<T, IObjectReference>>(
                Expression.MakeMemberAccess(
                    Expression.Convert(parms[0], HelperType),
                    HelperType.GetField("_obj", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)), parms).Compile();
        }

        private static Func<IObjectReference, IObjectReference> BindAs()
        {
            var vftblType = HelperType.FindVftblType();
            if (vftblType is not null)
            {
                var parms = new[] { Expression.Parameter(typeof(IObjectReference), "arg") };
                return Expression.Lambda<Func<IObjectReference, IObjectReference>>(
                    Expression.Call(
                        parms[0],
                        typeof(IObjectReference).GetMethod("As", Type.EmptyTypes).MakeGenericMethod(vftblType)
                        ), parms).Compile();
            }
            else
            {
                Guid iid = GuidGenerator.GetIID(HelperType);
                return obj => obj.As<IUnknownVftbl>(iid);
            }
        }
    }

#if EMBED
    internal
#else 
    public
#endif
    static class MarshalInspectable<T>
    {
        public static IObjectReference CreateMarshaler(T o, bool unwrapObject = true)
        {
            if (o is null)
            {
                return null;
            }

            if (unwrapObject && ComWrappersSupport.TryUnwrapObject(o, out var objRef))
            {
                return objRef.As<IInspectable.Vftbl>(IInspectable.IID);
            }
            var publicType = o.GetType();
            Type helperType = Projections.FindCustomHelperTypeMapping(publicType, true);
            if (helperType != null)
            {
                var parms = new[] { Expression.Parameter(typeof(object), "arg") };
                var createMarshaler = Expression.Lambda<Func<object, IObjectReference>>(
                    Expression.Call(helperType.GetMethod("CreateMarshaler", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static), 
                        new[] { Expression.Convert(parms[0], publicType) }), parms).Compile();
                return createMarshaler(o);
            }

            return ComWrappersSupport.CreateCCWForObject<IInspectable.Vftbl>(o, IInspectable.IID);
        }

        public static IntPtr GetAbi(IObjectReference objRef) =>
            objRef is null ? IntPtr.Zero : MarshalInterfaceHelper<T>.GetAbi(objRef);

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
                    return (T)ComWrappersSupport.FindObject<object>(iunknownPtr);
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

        public static void DisposeAbi(IntPtr ptr) => MarshalInterfaceHelper<T>.DisposeAbi(ptr);
        public static IntPtr FromManaged(T o, bool unwrapObject = true)
        {
            using var objRef = CreateMarshaler(o, unwrapObject);
            return objRef?.GetRef() ?? IntPtr.Zero;
        }

        public static unsafe void CopyManaged(T o, IntPtr dest, bool unwrapObject = true)
        {
            using var objRef = CreateMarshaler(o, unwrapObject);
            *(IntPtr*)dest.ToPointer() = objRef?.GetRef() ?? IntPtr.Zero;
        }

        public static unsafe MarshalInterfaceHelper<T>.MarshalerArray CreateMarshalerArray(T[] array) => MarshalInterfaceHelper<T>.CreateMarshalerArray(array, (o) => CreateMarshaler(o));

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

        public static T FromAbi<T>(IntPtr nativeDelegate, Func<ObjectReference<IDelegateVftbl>, T> createCallback)
            where T : System.Delegate
        {
            if (nativeDelegate == IntPtr.Zero)
            {
                return null;
            }
            else if (IUnknownVftbl.IsReferenceToManagedObject(nativeDelegate))
            {
                return ComWrappersSupport.FindObject<T>(nativeDelegate);
            }
            else
            {
                var abiDelegate = ComWrappersSupport.GetObjectReferenceForInterface<IDelegateVftbl>(nativeDelegate);
                return (T) ComWrappersSupport.TryRegisterObjectForInterface(createCallback(abiDelegate), nativeDelegate);
            }
        }
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
                CreateMarshaler = MarshalGeneric<T>.CreateMarshaler;
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
            else if (type.IsValueType || type == typeof(Type))
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

                if (AbiType == null)
                {
                    AbiType = type;
                    CreateMarshaler = (T value) => value;
                    GetAbi = (object box) => box;
                    FromAbi = (object value) => (T)value;
                    FromManaged = (T value) => value;
                    DisposeMarshaler = (object box) => { };
                    DisposeAbi = (object box) => { };
                    if (type.IsEnum)
                    {
                        // For marshaling non-blittable enum arrays via MarshalNonBlittable
                        unsafe void CopyEnum(object value, IntPtr dest) 
                        {
                            if (type.GetEnumUnderlyingType() == typeof(int))
                            { 
                                *(int*)dest.ToPointer() = (int)Convert.ChangeType(value, typeof(int));
                            }
                            else
                            {
                                *(uint*)dest.ToPointer() = (uint)Convert.ChangeType(value, typeof(uint));
                            }
                        }
                        CopyAbi = (object value, IntPtr dest) => CopyEnum(value, dest);
                        CopyManaged = (T value, IntPtr dest) => CopyEnum(value, dest);
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
                    GetAbi = MarshalNonBlittable<T>.GetAbi;
                    CopyAbi = MarshalNonBlittable<T>.CopyAbi;
                    FromAbi = MarshalNonBlittable<T>.FromAbi;
                    FromManaged = MarshalNonBlittable<T>.FromManaged;
                    CopyManaged = MarshalNonBlittable<T>.CopyManaged;
                    DisposeMarshaler = MarshalNonBlittable<T>.DisposeMarshaler;
                    DisposeAbi = (object box) => { };
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
                CreateMarshaler = (T value) => MarshalInterface<T>.CreateMarshaler(value);
                GetAbi = (object objRef) => MarshalInterface<T>.GetAbi((IObjectReference)objRef);
                FromAbi = (object value) => MarshalInterface<T>.FromAbi((IntPtr)value);
                FromManaged = (T value) => ((IObjectReference)CreateMarshaler(value)).GetRef();
                DisposeMarshaler = (object objRef) => MarshalInterface<T>.DisposeMarshaler((IObjectReference)objRef);
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
                CreateMarshaler = (T value) => MarshalInspectable<T>.CreateMarshaler(value);
                GetAbi = (object objRef) => MarshalInspectable<T>.GetAbi((IObjectReference)objRef);
                FromAbi = (object box) => MarshalInspectable<T>.FromAbi((IntPtr)box);
                FromManaged = (T value) => MarshalInspectable<T>.FromManaged(value);
                CopyManaged = (T value, IntPtr dest) => MarshalInspectable<T>.CopyManaged(value, dest);
                DisposeMarshaler = (object objRef) => MarshalInspectable<T>.DisposeMarshaler((IObjectReference)objRef);
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
                CreateMarshaler = MarshalGeneric<T>.CreateMarshaler;
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

        public static readonly Type AbiType;
        public static readonly Type RefAbiType;
        public static readonly Func<T, object> CreateMarshaler;
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
