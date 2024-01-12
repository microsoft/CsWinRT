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
                _header = Marshal.AllocHGlobal(sizeof(HSTRING_HEADER));
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

        /// <summary>
        /// Marshals an input <c>HSTRING</c> value to a <see cref="ReadOnlySpan{T}"/> value.
        /// </summary>
        /// <param name="value">The input <c>HSTRING</c> value to marshal.</param>
        /// <returns>The resulting <see cref="ReadOnlySpan{T}"/> value.</returns>
        /// <remarks>
        /// <para>
        /// This method is equivalent to <see cref="FromAbi"/>, but it does not create a new <see cref="string"/> instance.
        /// Doing so makes it zero-allocation, but extra care should be taken by callers to ensure that the returned value
        /// does not escape the scope where the source <c>HSTRING</c> is valid.
        /// </para>
        /// <para>
        /// For instance, if this method is invoked in the scope of a method that receives the <c>HSTRING</c> value as one of
        /// its parameters, the resulting <see cref="ReadOnlySpan{T}"/> is always valid for the scope of such method. But, if
        /// the <c>HSTRING</c> was created by reference in a given scope, the resulting <see cref="ReadOnlySpan{T}"/> value
        /// will also only be valid within such scope, and should not be used outside of it.
        /// </para>
        /// </remarks>
        public static unsafe ReadOnlySpan<char> FromAbiUnsafe(IntPtr value)
        {
            if (value == IntPtr.Zero)
            {
                return "".AsSpan();
            }

            uint length;
            char* buffer = Platform.WindowsGetStringRawBuffer(value, &length);

            return new(buffer, (int)length);
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
                m._array = Marshal.AllocCoTaskMem(length * sizeof(IntPtr));
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
                data = Marshal.AllocCoTaskMem(length * sizeof(IntPtr));
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
        protected static readonly Type HelperType;
        protected static readonly Type AbiType;
        protected static readonly Type MarshalerType;
        internal static readonly bool MarshalByObjectReferenceValueSupported;

        public static readonly Func<T, object> CreateMarshaler;
        public static readonly Func<object, object> GetAbi;
        public static readonly Action<object, IntPtr> CopyAbi;
        public static readonly Func<object, T> FromAbi;
        public static readonly Func<T, object> FromManaged;
        public static readonly Action<T, IntPtr> CopyManaged;
        public static readonly Action<object> DisposeMarshaler;
        internal static readonly Func<T, object> CreateMarshaler2;
        internal static readonly Action<object> DisposeAbi;
        internal static readonly Func<T[], object> CreateMarshalerArray;
        internal static readonly Func<object, (int, IntPtr)> GetAbiArray;
        internal static readonly Func<object, T[]> FromAbiArray;
        internal static readonly Func<T[], (int, IntPtr)> FromManagedArray;
        internal static readonly Action<object> DisposeMarshalerArray;
        internal static readonly Action<object> DisposeAbiArray;

        static MarshalGeneric()
        {
            // Special case some well known projected types that are blittable.
            // For these, we directly load the ABI type and leave everything else
            // set to default (null). That is, we have no special marshallers.
            if (typeof(T) == typeof(global::System.Numerics.Vector2))
            {
                HelperType = typeof(global::ABI.System.Numerics.Vector2);
                AbiType = null;
                MarshalerType = null;
                MarshalByObjectReferenceValueSupported = false;
            }
            else if (typeof(T) == typeof(global::System.Numerics.Vector3))
            {
                HelperType = typeof(global::ABI.System.Numerics.Vector3);
                AbiType = null;
                MarshalerType = null;
                MarshalByObjectReferenceValueSupported = false;
            }
            else if (typeof(T) == typeof(global::System.Numerics.Vector4))
            {
                HelperType = typeof(global::ABI.System.Numerics.Vector4);
                AbiType = null;
                MarshalerType = null;
                MarshalByObjectReferenceValueSupported = false;
            }
            else if (typeof(T) == typeof(global::System.Numerics.Plane))
            {
                HelperType = typeof(global::ABI.System.Numerics.Plane);
                AbiType = null;
                MarshalerType = null;
                MarshalByObjectReferenceValueSupported = false;
            }
            else if (typeof(T) == typeof(global::System.Numerics.Matrix3x2))
            {
                HelperType = typeof(global::ABI.System.Numerics.Matrix3x2);
                AbiType = null;
                MarshalerType = null;
                MarshalByObjectReferenceValueSupported = false;
            }
            else if (typeof(T) == typeof(global::System.Numerics.Matrix4x4))
            {
                HelperType = typeof(global::ABI.System.Numerics.Matrix4x4);
                AbiType = null;
                MarshalerType = null;
                MarshalByObjectReferenceValueSupported = false;
            }
            else if (typeof(T) == typeof(global::System.Numerics.Quaternion))
            {
                HelperType = typeof(global::ABI.System.Numerics.Quaternion);
                AbiType = null;
                MarshalerType = null;
                MarshalByObjectReferenceValueSupported = false;
            }
            else if (typeof(T) == typeof(int) ||
                     typeof(T) == typeof(byte) ||
                     typeof(T) == typeof(sbyte) ||
                     typeof(T) == typeof(short) ||
                     typeof(T) == typeof(ushort) ||
                     typeof(T) == typeof(uint) ||
                     typeof(T) == typeof(long) ||
                     typeof(T) == typeof(ulong) ||
                     typeof(T) == typeof(float) ||
                     typeof(T) == typeof(double) ||
                     typeof(T) == typeof(Guid))
            {
                // Special case some well known primitive types that we know might be constructed
                // for this type, but not actually used. For these, we just keep all default values.
                // No consumer would ever actually be trying to use this marshaller for these types.
                return;
            }
            else if (typeof(T) == typeof(bool))
            {
                // Same as above, but we do have an ABI type
                HelperType = typeof(global::ABI.System.Boolean);
                AbiType = typeof(byte);
                MarshalerType = typeof(bool);
                MarshalByObjectReferenceValueSupported = false;
                CreateMarshaler = (Func<T, object>)(object)((Func<bool, bool>)global::ABI.System.Boolean.CreateMarshaler).WithObjectTResult();
                CreateMarshaler2 = CreateMarshaler;
                GetAbi = ((Func<bool, byte>)global::ABI.System.Boolean.GetAbi).WithObjectParams();
                FromAbi = (Func<object, T>)(object)((Func<byte, bool>)global::ABI.System.Boolean.FromAbi).WithObjectT();
                CopyAbi = ((Action<bool, IntPtr>)global::ABI.System.Boolean.CopyAbi).WithObjectT1();
                FromManaged = (Func<T, object>)(object)((Func<bool, byte>)global::ABI.System.Boolean.FromManaged).WithObjectTResult();
                CopyManaged = (Action<T, IntPtr>)(object)(Action<bool, IntPtr>)global::ABI.System.Boolean.CopyManaged;
                DisposeMarshaler = ((Action<bool>)global::ABI.System.Boolean.DisposeMarshaler).WithObjectParams();
                DisposeAbi = ((Action<byte>)global::ABI.System.Boolean.DisposeAbi).WithObjectParams();

            }
            else if (typeof(T) == typeof(char))
            {
                HelperType = typeof(global::ABI.System.Char);
                AbiType = typeof(ushort);
                MarshalerType = typeof(char);
                MarshalByObjectReferenceValueSupported = false;
                CreateMarshaler = (Func<T, object>)(object)((Func<char, char>)global::ABI.System.Char.CreateMarshaler).WithObjectTResult();
                CreateMarshaler2 = CreateMarshaler;
                GetAbi = ((Func<char, ushort>)global::ABI.System.Char.GetAbi).WithObjectParams();
                FromAbi = (Func<object, T>)(object)((Func<ushort, char>)global::ABI.System.Char.FromAbi).WithObjectT();
                CopyAbi = ((Action<char, IntPtr>)global::ABI.System.Char.CopyAbi).WithObjectT1();
                FromManaged = (Func<T, object>)(object)((Func<char, ushort>)global::ABI.System.Char.FromManaged).WithObjectTResult();
                CopyManaged = (Action<T, IntPtr>)(object)(Action<char, IntPtr>)global::ABI.System.Char.CopyManaged;
                DisposeMarshaler = ((Action<char>)global::ABI.System.Char.DisposeMarshaler).WithObjectParams();
                DisposeAbi = ((Action<ushort>)global::ABI.System.Char.DisposeAbi).WithObjectParams();
            }
            else if (typeof(T) == typeof(TimeSpan))
            {
                // Another well known projected type that we might will be constructed
                HelperType = typeof(global::ABI.System.TimeSpan);
                AbiType = typeof(global::ABI.System.TimeSpan);
                MarshalerType = typeof(global::ABI.System.TimeSpan.Marshaler);
                MarshalByObjectReferenceValueSupported = false;
                CreateMarshaler = (Func<T, object>)(object)((Func<global::System.TimeSpan, global::ABI.System.TimeSpan.Marshaler>)global::ABI.System.TimeSpan.CreateMarshaler).WithObjectTResult();
                CreateMarshaler2 = CreateMarshaler;
                GetAbi = ((Func<global::ABI.System.TimeSpan.Marshaler, global::ABI.System.TimeSpan>)global::ABI.System.TimeSpan.GetAbi).WithObjectParams();
                FromAbi = (Func<object, T>)(object)((Func<global::ABI.System.TimeSpan, global::System.TimeSpan>)global::ABI.System.TimeSpan.FromAbi).WithObjectT();
                CopyAbi = ((Action<global::ABI.System.TimeSpan.Marshaler, IntPtr>)global::ABI.System.TimeSpan.CopyAbi).WithObjectT1();
                FromManaged = (Func<T, object>)(object)((Func<global::System.TimeSpan, global::ABI.System.TimeSpan>)global::ABI.System.TimeSpan.FromManaged).WithObjectTResult();
                CopyManaged = (Action<T, IntPtr>)(object)(Action<global::System.TimeSpan, IntPtr>)global::ABI.System.TimeSpan.CopyManaged;
                DisposeMarshaler = ((Action<global::ABI.System.TimeSpan.Marshaler>)global::ABI.System.TimeSpan.DisposeMarshaler).WithObjectParams();
                DisposeAbi = ((Action<global::ABI.System.TimeSpan>)global::ABI.System.TimeSpan.DisposeAbi).WithObjectParams();
            }
            else if (typeof(T) == typeof(DateTimeOffset))
            {
                // DateTimeOffset also has a custom marshaller and is always constructed.
                // We can do the same as with TimeSpan: just special case all delegates.
                HelperType = typeof(global::ABI.System.DateTimeOffset);
                AbiType = typeof(global::ABI.System.DateTimeOffset);
                MarshalerType = typeof(global::ABI.System.DateTimeOffset.Marshaler);
                MarshalByObjectReferenceValueSupported = false;
                CreateMarshaler = (Func<T, object>)(object)((Func<global::System.DateTimeOffset, global::ABI.System.DateTimeOffset.Marshaler>)global::ABI.System.DateTimeOffset.CreateMarshaler).WithObjectTResult();
                CreateMarshaler2 = CreateMarshaler;
                GetAbi = ((Func<global::ABI.System.DateTimeOffset.Marshaler, global::ABI.System.DateTimeOffset>)global::ABI.System.DateTimeOffset.GetAbi).WithObjectParams();
                FromAbi = (Func<object, T>)(object)((Func<global::ABI.System.DateTimeOffset, global::System.DateTimeOffset>)global::ABI.System.DateTimeOffset.FromAbi).WithObjectT();
                CopyAbi = ((Action<global::ABI.System.DateTimeOffset.Marshaler, IntPtr>)global::ABI.System.DateTimeOffset.CopyAbi).WithObjectT1();
                FromManaged = (Func<T, object>)(object)((Func<global::System.DateTimeOffset, global::ABI.System.DateTimeOffset>)global::ABI.System.DateTimeOffset.FromManaged).WithObjectTResult();
                CopyManaged = (Action<T, IntPtr>)(object)(Action<global::System.DateTimeOffset, IntPtr>)global::ABI.System.DateTimeOffset.CopyManaged;
                DisposeMarshaler = ((Action<global::ABI.System.DateTimeOffset.Marshaler>)global::ABI.System.DateTimeOffset.DisposeMarshaler).WithObjectParams();
                DisposeAbi = ((Action<global::ABI.System.DateTimeOffset>)global::ABI.System.DateTimeOffset.DisposeAbi).WithObjectParams();
            }
            else if (typeof(T).IsValueType)
            {
                // Value types can have custom marshaller types and use value types in places where we can't construct
                // delegates in the same efficient way as with reference types. Use the fallback path in this case
                HelperType = typeof(T).GetHelperType();
                AbiType = typeof(T).GetAbiType();
                MarshalerType = typeof(T).GetMarshalerType();
                MarshalByObjectReferenceValueSupported = typeof(T).GetMarshaler2Type() == typeof(ObjectReferenceValue);

                MarshalGenericFallback<T> fallback = new(HelperType);

                CreateMarshaler = fallback.CreateMarshaler;
                CreateMarshaler2 = CreateMarshaler2 = MarshalByObjectReferenceValueSupported ? fallback.CreateMarshaler2 : CreateMarshaler;
                GetAbi = fallback.GetAbi;
                CopyAbi = fallback.CopyAbi;
                FromAbi = fallback.FromAbi;
                FromManaged = fallback.FromManaged;
                CopyManaged = fallback.CopyManaged;
                DisposeMarshaler = fallback.DisposeMarshaler;
                DisposeAbi = fallback.DisposeAbi;
                CreateMarshalerArray = fallback.CreateMarshalerArray;
                GetAbiArray = fallback.GetAbiArray;
                FromAbiArray = fallback.FromAbiArray;
                FromManagedArray = fallback.FromManagedArray;
                DisposeMarshalerArray = fallback.DisposeMarshalerArray;
                DisposeAbiArray = fallback.DisposeAbiArray;
            }
            else
            {
                // Fallback case for all other types (could be anything, really). These would be reference types,
                // which means we can make some assumptions on the shape of the helper type methods. Specifically,
                // we expect the returned marshallers to be IObjectReference-s, and the ABI to be an IntPtr value.
                HelperType = typeof(T).GetHelperType();
                AbiType = typeof(T).GetAbiType();
                MarshalerType = typeof(T).GetMarshalerType();
                MarshalByObjectReferenceValueSupported = typeof(T).GetMarshaler2Type() == typeof(ObjectReferenceValue);

#if NET
                CreateMarshaler = HelperType.GetMethod("CreateMarshaler", BindingFlags.Public | BindingFlags.Static)?.CreateDelegate<Func<T, IObjectReference>>();
                CreateMarshaler2 = MarshalByObjectReferenceValueSupported
                    ? HelperType.GetMethod("CreateMarshaler2", BindingFlags.Public | BindingFlags.Static)?.CreateDelegate<Func<T, ObjectReferenceValue>>().WithObjectTResult()
                    : CreateMarshaler;
                GetAbi = HelperType.GetMethod("GetAbi", BindingFlags.Public | BindingFlags.Static)?.CreateDelegate<Func<IObjectReference, IntPtr>>().WithObjectParams();
                CopyAbi = null; // Not used for class types
                FromAbi = HelperType.GetMethod("FromAbi", BindingFlags.Public | BindingFlags.Static)?.CreateDelegate<Func<IntPtr, T>>().WithObjectT();
                FromManaged = HelperType.GetMethod("FromManaged", BindingFlags.Public | BindingFlags.Static)?.CreateDelegate<Func<T, IntPtr>>().WithObjectTResult();
                CopyManaged = null; // Also not used for class types
                DisposeMarshaler = HelperType.GetMethod("DisposeMarshaler", BindingFlags.Public | BindingFlags.Static)?.CreateDelegate<Action<IObjectReference>>().WithObjectParams();
                DisposeAbi = HelperType.GetMethod("DisposeAbi", BindingFlags.Public | BindingFlags.Static)?.CreateDelegate<Action<IntPtr>>().WithObjectParams();
                CreateMarshalerArray = HelperType.GetMethod("CreateMarshalerArray", BindingFlags.Public | BindingFlags.Static)?.CreateDelegate<Func<T[], MarshalInterfaceHelper<T>.MarshalerArray>>().WithObjectTResult();
                GetAbiArray = HelperType.GetMethod("GetAbiArray", BindingFlags.Public | BindingFlags.Static)?.CreateDelegate<Func<object, (int, IntPtr)>>();
                FromAbiArray = HelperType.GetMethod("FromAbiArray", BindingFlags.Public | BindingFlags.Static)?.CreateDelegate<Func<object, T[]>>();
                FromManagedArray = HelperType.GetMethod("FromManagedArray", BindingFlags.Public | BindingFlags.Static)?.CreateDelegate<Func<T[], (int, IntPtr)>>();
                DisposeMarshalerArray = HelperType.GetMethod("DisposeMarshalerArray", BindingFlags.Public | BindingFlags.Static)?.CreateDelegate<Action<MarshalInterfaceHelper<T>.MarshalerArray>>().WithObjectParams();
                DisposeAbiArray = HelperType.GetMethod("DisposeAbiArray", BindingFlags.Public | BindingFlags.Static)?.CreateDelegate<Action<object>>();
#else
                MarshalGenericFallback<T> fallback = new(HelperType);

                CreateMarshaler = fallback.CreateMarshaler;
                CreateMarshaler2 = CreateMarshaler2 = MarshalByObjectReferenceValueSupported ? fallback.CreateMarshaler2 : CreateMarshaler;
                GetAbi = fallback.GetAbi;
                CopyAbi = fallback.CopyAbi;
                FromAbi = fallback.FromAbi;
                FromManaged = fallback.FromManaged;
                CopyManaged = fallback.CopyManaged;
                DisposeMarshaler = fallback.DisposeMarshaler;
                DisposeAbi = fallback.DisposeAbi;
                CreateMarshalerArray = fallback.CreateMarshalerArray;
                GetAbiArray = fallback.GetAbiArray;
                FromAbiArray = fallback.FromAbiArray;
                FromManagedArray = fallback.FromManagedArray;
                DisposeMarshalerArray = fallback.DisposeMarshalerArray;
                DisposeAbiArray = fallback.DisposeAbiArray;
#endif
            }
        }
    }

    internal sealed class MarshalGenericFallback<T>
    {
        private readonly MethodInfo _createMarshaler;
        private readonly MethodInfo _getAbi;
        private readonly MethodInfo _copyAbi;
        private readonly MethodInfo _fromAbi;
        private readonly MethodInfo _fromManaged;
        private readonly MethodInfo _copyManaged;
        private readonly MethodInfo _disposeMarshaler;
        private readonly MethodInfo _createMarshaler2;
        private readonly MethodInfo _disposeAbi;
        private readonly MethodInfo _createMarshalerArray;
        private readonly MethodInfo _getAbiArray;
        private readonly MethodInfo _fromAbiArray;
        private readonly MethodInfo _fromManagedArray;
        private readonly MethodInfo _disposeMarshalerArray;
        private readonly MethodInfo _disposeAbiArray;

        public MarshalGenericFallback(
#if NET
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
#endif
            Type helperType)
        {
            _createMarshaler = helperType.GetMethod("CreateMarshaler", BindingFlags.Public | BindingFlags.Static);
            _getAbi = helperType.GetMethod("GetAbi", BindingFlags.Public | BindingFlags.Static);
            _copyAbi = helperType.GetMethod("CopyAbi", BindingFlags.Public | BindingFlags.Static);
            _fromAbi = helperType.GetMethod("FromAbi", BindingFlags.Public | BindingFlags.Static);
            _fromManaged = helperType.GetMethod("FromManaged", BindingFlags.Public | BindingFlags.Static);
            _copyManaged = helperType.GetMethod("CopyManaged", BindingFlags.Public | BindingFlags.Static);
            _disposeMarshaler = helperType.GetMethod("DisposeMarshaler", BindingFlags.Public | BindingFlags.Static);
            _createMarshaler2 = helperType.GetMethod("CreateMarshaler2", BindingFlags.Public | BindingFlags.Static);
            _disposeAbi = helperType.GetMethod("DisposeAbi", BindingFlags.Public | BindingFlags.Static);
            _createMarshalerArray = helperType.GetMethod("CreateMarshalerArray", BindingFlags.Public | BindingFlags.Static);
            _getAbiArray = helperType.GetMethod("GetAbiArray", BindingFlags.Public | BindingFlags.Static);
            _fromAbiArray = helperType.GetMethod("FromAbiArray", BindingFlags.Public | BindingFlags.Static);
            _fromManagedArray = helperType.GetMethod("FromManagedArray", BindingFlags.Public | BindingFlags.Static);
            _disposeMarshalerArray = helperType.GetMethod("DisposeMarshalerArray", BindingFlags.Public | BindingFlags.Static);
            _disposeAbiArray = helperType.GetMethod("DisposeAbiArray", BindingFlags.Public | BindingFlags.Static);
        }

        public object CreateMarshaler(T arg) => _createMarshaler.Invoke(null, new object[] { arg });

        public object CreateMarshaler2(T arg) => _createMarshaler2.Invoke(null, new object[] { arg });

        public object GetAbi(object arg) => _getAbi.Invoke(null, new[] { arg });

        public void CopyAbi(object arg, IntPtr dest) => _copyAbi.Invoke(null, new[] { arg, dest });

        public T FromAbi(object arg) => (T)_fromAbi.Invoke(null, new[] { arg });

        public object FromManaged(T arg) => _fromManaged.Invoke(null, new object[] { arg });

        public void CopyManaged(T arg, IntPtr dest) => _copyManaged.Invoke(null, new object[] { arg, dest });

        public void DisposeMarshaler(object arg) => _disposeMarshaler.Invoke(null, new[] { arg });

        public void DisposeAbi(object arg) => _disposeAbi.Invoke(null, new[] { arg });

        public object CreateMarshalerArray(T[] arg) => _createMarshalerArray.Invoke(null, new object[] { arg });

        public (int, IntPtr) GetAbiArray(object arg) => ((int, IntPtr))_getAbiArray.Invoke(null, new object[] { arg });

        public T[] FromAbiArray(object arg) => (T[])_fromAbiArray.Invoke(null, new[] { arg });

        public (int, IntPtr) FromManagedArray(T[] arg) => ((int, IntPtr))_fromManagedArray.Invoke(null, new object[] { arg });

        public void DisposeMarshalerArray(object arg) => _disposeMarshalerArray.Invoke(null, new object[] { arg });

        public void DisposeAbiArray(object arg) => _disposeAbiArray.Invoke(null, new object[] { arg });
    }

    internal static class MarshalGenericHelper<T>
    {
        private static unsafe void CopyManagedFallback(T value, IntPtr dest)
        {
            if (MarshalGeneric<T>.MarshalByObjectReferenceValueSupported)
            {
                *(IntPtr*)dest.ToPointer() =
                    (value is null) ? IntPtr.Zero : ((ObjectReferenceValue)MarshalGeneric<T>.CreateMarshaler2(value)).Detach();
            }
            else
            {
                *(IntPtr*)dest.ToPointer() =
                    (value is null) ? IntPtr.Zero : ((IObjectReference)MarshalGeneric<T>.CreateMarshaler(value)).GetRef();
            }
        }

        internal static unsafe void CopyManagedArray(T[] array, IntPtr data) => MarshalInterfaceHelper<T>.CopyManagedArray(array, data, MarshalGeneric<T>.CopyManaged ?? CopyManagedFallback);
    }

#if EMBED
    internal
#else
    public
#endif
    class MarshalNonBlittable<T> : MarshalGeneric<T>
    {
        private static readonly new Type AbiType = GetAbiType();

        private static Type GetAbiType()
        {
            if (typeof(T).IsEnum)
            {
                return Enum.GetUnderlyingType(typeof(T));
            }

            // These 4 types are true non blittable types that are valid to use here
            if (typeof(T) == typeof(bool)) return typeof(byte);
            if (typeof(T) == typeof(char)) return typeof(ushort);
            if (typeof(T) == typeof(global::System.TimeSpan)) return typeof(global::ABI.System.TimeSpan);
            if (typeof(T) == typeof(DateTimeOffset)) return typeof(global::ABI.System.DateTimeOffset);

            // These types are actually blittable, but this marshaller is still constructed elsewhere.
            // Just return null instead of using MarshalGeneric<T>, to avoid constructing that too.
            if (typeof(T) == typeof(int) ||
                typeof(T) == typeof(byte) ||
                typeof(T) == typeof(sbyte) ||
                typeof(T) == typeof(short) ||
                typeof(T) == typeof(ushort) ||
                typeof(T) == typeof(uint) ||
                typeof(T) == typeof(long) ||
                typeof(T) == typeof(ulong) ||
                typeof(T) == typeof(float) ||
                typeof(T) == typeof(double) ||
                typeof(T) == typeof(Guid) ||
                typeof(T) == typeof(global::Windows.Foundation.Point) ||
                typeof(T) == typeof(global::Windows.Foundation.Rect) ||
                typeof(T) == typeof(global::Windows.Foundation.Size) ||
                typeof(T) == typeof(global::System.Numerics.Matrix3x2) ||
                typeof(T) == typeof(global::System.Numerics.Matrix4x4) ||
                typeof(T) == typeof(global::System.Numerics.Plane) ||
                typeof(T) == typeof(global::System.Numerics.Quaternion) ||
                typeof(T) == typeof(global::System.Numerics.Vector2) ||
                typeof(T) == typeof(global::System.Numerics.Vector3) ||
                typeof(T) == typeof(global::System.Numerics.Vector4))
            {
                return null;
            }

            // Fallback path with the original logic
            return typeof(T).GetAbiType();
        }

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

        public static unsafe (int length, IntPtr data) FromManagedArray(T[] array) => MarshalInterfaceHelper<T>.FromManagedArray(array, FromManaged);

        public static unsafe void CopyManagedArray(T[] array, IntPtr data) => MarshalInterfaceHelper<T>.CopyManagedArray(array, data, CopyManaged);

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
            T o,
            bool unwrapObject = true)
        {
            return CreateMarshaler<IInspectable.Vftbl>(o, InterfaceIIDs.IInspectable_IID, unwrapObject);
        }

        public static ObjectReferenceValue CreateMarshaler2(
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

        public static unsafe void CopyAbiArray(T[] array, object box) => MarshalInterfaceHelper<T>.CopyAbiArray(array, box, FromAbi);

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
                return objRef.As<IDelegateVftbl>(delegateIID);
            }

            return ComWrappersSupport.CreateCCWForObject<IDelegateVftbl>(o, delegateIID);
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
            where T : Delegate
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
            // Structs cannot contain arrays, and arrays may only ever appear as parameters
            if (typeof(T).IsArray)
            {
                throw new InvalidOperationException("Arrays may not be marshaled generically.");
            }

            if (typeof(T) == typeof(string))
            {
                AbiType = typeof(IntPtr);
                CreateMarshaler = (T value) => MarshalString.CreateMarshaler((string)(object)value);
                CreateMarshaler2 = CreateMarshaler;
                GetAbi = (object box) => MarshalString.GetAbi(box);
                FromAbi = (object value) => (T)(object)MarshalString.FromAbi((IntPtr)value);
                FromManaged = (T value) => MarshalString.FromManaged((string)(object)value);
                DisposeMarshaler = MarshalString.DisposeMarshaler;
                DisposeAbi = MarshalString.DisposeAbi;
                CreateMarshalerArray = (T[] array) => MarshalString.CreateMarshalerArray((string[])(object)array);
                GetAbiArray = MarshalString.GetAbiArray;
                FromAbiArray = (object box) => (T[])(object)MarshalString.FromAbiArray(box);
                FromManagedArray = (T[] array) => MarshalString.FromManagedArray((string[])(object)array);
                CopyManagedArray = (T[] array, IntPtr data) => MarshalString.CopyManagedArray((string[])(object)array, data);
                DisposeMarshalerArray = MarshalString.DisposeMarshalerArray;
                DisposeAbiArray = MarshalString.DisposeAbiArray;
            }
            else if (typeof(T) == typeof(Type))
            {
                AbiType = typeof(ABI.System.Type);
                CreateMarshaler = (T value) => ABI.System.Type.CreateMarshaler((Type)(object)value);
                CreateMarshaler2 = CreateMarshaler;
                GetAbi = (object box) => ABI.System.Type.GetAbi((ABI.System.Type.Marshaler)box);
                FromAbi = (object value) => (T)(object)ABI.System.Type.FromAbi((ABI.System.Type)value);
                CopyAbi = (object box, IntPtr dest) => ABI.System.Type.CopyAbi((ABI.System.Type.Marshaler)box, dest);
                CopyManaged = (T value, IntPtr dest) => ABI.System.Type.CopyManaged((Type)(object)value, dest);
                FromManaged = (T value) => ABI.System.Type.FromManaged((Type)(object)value);
                DisposeMarshaler = (object box) => ABI.System.Type.DisposeMarshaler((ABI.System.Type.Marshaler)box);
                DisposeAbi = (object box) => ABI.System.Type.DisposeAbi((ABI.System.Type)box);
                CreateMarshalerArray = (T[] array) => MarshalNonBlittable<T>.CreateMarshalerArray(array);
                GetAbiArray = MarshalNonBlittable<T>.GetAbiArray;
                FromAbiArray = MarshalNonBlittable<T>.FromAbiArray;
                FromManagedArray = MarshalNonBlittable<T>.FromManagedArray;
                CopyManagedArray = MarshalNonBlittable<T>.CopyManagedArray;
                DisposeMarshalerArray = MarshalNonBlittable<T>.DisposeMarshalerArray;
                DisposeAbiArray = MarshalNonBlittable<T>.DisposeAbiArray;
            }
            else if (typeof(T).IsValueType)
            {
                if (typeof(T) == typeof(bool))
                {
                    AbiType = typeof(byte);
                }
                else if (typeof(T) == typeof(char))
                {
                    AbiType = typeof(ushort);
                }
                else if (typeof(T) == typeof(int) ||
                         typeof(T) == typeof(byte) ||
                         typeof(T) == typeof(sbyte) ||
                         typeof(T) == typeof(short) ||
                         typeof(T) == typeof(ushort) ||
                         typeof(T) == typeof(uint) ||
                         typeof(T) == typeof(long) ||
                         typeof(T) == typeof(ulong) ||
                         typeof(T) == typeof(float) ||
                         typeof(T) == typeof(double) ||
                         typeof(T) == typeof(Guid) ||
                         typeof(T) == typeof(global::Windows.Foundation.Point) ||
                         typeof(T) == typeof(global::Windows.Foundation.Rect) ||
                         typeof(T) == typeof(global::Windows.Foundation.Size) ||
                         typeof(T) == typeof(global::System.Numerics.Matrix3x2) ||
                         typeof(T) == typeof(global::System.Numerics.Matrix4x4) ||
                         typeof(T) == typeof(global::System.Numerics.Plane) ||
                         typeof(T) == typeof(global::System.Numerics.Quaternion) ||
                         typeof(T) == typeof(global::System.Numerics.Vector2) ||
                         typeof(T) == typeof(global::System.Numerics.Vector3) ||
                         typeof(T) == typeof(global::System.Numerics.Vector4))
                {
                    // Manually handle well known primitive types and common types, as well
                    // as two common projected types (below). This allows the linker to trim
                    // all the non-taken branch below, which it wouldn't otherwise do, because
                    // the path below with the fallback logic to check for ABI types is dynamic.
                    AbiType = null;
                }
                else if (typeof(T) == typeof(global::System.TimeSpan))
                {
                    AbiType = typeof(global::ABI.System.TimeSpan);
                }
                else if (typeof(T) == typeof(global::System.DateTimeOffset))
                {
                    AbiType = typeof(global::ABI.System.DateTimeOffset);
                }
                else
                {
                    AbiType = typeof(T).FindHelperType();
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
                    AbiType = typeof(T);
                    CreateMarshaler = ReturnTypedParameterFunc;
                    CreateMarshaler2 = CreateMarshaler;
                    GetAbi = Marshaler.ReturnParameterFunc;
                    FromAbi = (object value) => (T)value;
                    FromManaged = ReturnTypedParameterFunc;
                    DisposeMarshaler = Marshaler.EmptyFunc;
                    DisposeAbi = Marshaler.EmptyFunc;
                    if (typeof(T).IsEnum)
                    {
                        // For marshaling non-blittable enum arrays via MarshalNonBlittable
                        if (typeof(T).GetEnumUnderlyingType() == typeof(int))
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
                    GetAbiArray = MarshalBlittable<T>.GetAbiArray;
                    FromAbiArray = MarshalBlittable<T>.FromAbiArray;
                    FromManagedArray = MarshalBlittable<T>.FromManagedArray;
                    CopyManagedArray = MarshalBlittable<T>.CopyManagedArray;
                    DisposeMarshalerArray = MarshalBlittable<T>.DisposeMarshalerArray;
                    DisposeAbiArray = MarshalBlittable<T>.DisposeAbiArray;
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
                    GetAbiArray = MarshalNonBlittable<T>.GetAbiArray;
                    FromAbiArray = MarshalNonBlittable<T>.FromAbiArray;
                    FromManagedArray = MarshalNonBlittable<T>.FromManagedArray;
                    CopyManagedArray = MarshalNonBlittable<T>.CopyManagedArray;
                    DisposeMarshalerArray = MarshalNonBlittable<T>.DisposeMarshalerArray;
                    DisposeAbiArray = MarshalNonBlittable<T>.DisposeAbiArray;
                }
            }
            else if (typeof(T).IsInterface)
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
                GetAbiArray = MarshalInterface<T>.GetAbiArray;
                FromAbiArray = MarshalInterface<T>.FromAbiArray;
                FromManagedArray = MarshalInterface<T>.FromManagedArray;
                CopyManagedArray = MarshalInterface<T>.CopyManagedArray;
                DisposeMarshalerArray = MarshalInterface<T>.DisposeMarshalerArray;
                DisposeAbiArray = MarshalInterface<T>.DisposeAbiArray;
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
                GetAbiArray = MarshalInspectable<T>.GetAbiArray;
                FromAbiArray = MarshalInspectable<T>.FromAbiArray;
                FromManagedArray = MarshalInspectable<T>.FromManagedArray;
                CopyManagedArray = MarshalInspectable<T>.CopyManagedArray;
                DisposeMarshalerArray = MarshalInspectable<T>.DisposeMarshalerArray;
                DisposeAbiArray = MarshalInspectable<T>.DisposeAbiArray;
            }
            else if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(System.Collections.Generic.KeyValuePair<,>))
            {
                // This check for KeyValuePair<,> types cannot be statically determined, so we move it
                // down to still allow the linker to see more possible branches before. This should
                // avoid constructing all of these MarshalGeneric<T> types when not actually needed.
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
                CopyManagedArray = MarshalGenericHelper<T>.CopyManagedArray;
                DisposeMarshalerArray = MarshalInterface<T>.DisposeMarshalerArray;
                DisposeAbiArray = MarshalInterface<T>.DisposeAbiArray;
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
                CopyManagedArray = MarshalGenericHelper<T>.CopyManagedArray;
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
