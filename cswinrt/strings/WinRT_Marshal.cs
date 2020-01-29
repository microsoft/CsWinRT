using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Linq.Expressions;

#pragma warning disable 0169 // The field 'xxx' is never used
#pragma warning disable 0649 // Field 'xxx' is never assigned to, and will always have its default value

namespace WinRT
{
    public static class MarshalExtensions
    {
        public static void Dispose(this GCHandle handle)
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }
    }

    // TODO: minimize heap allocations for marshalers by eliminating explicit try/finally
    // and adopting ref structs with non-IDisposable Dispose and 'using var ...' pattern,
    // as well as passing marshalers to FromAbi by ref so they can be disposed.
    class MarshalString
    {
        public unsafe struct HStringHeader // sizeof(HSTRING_HEADER)
        {
            public fixed byte Reserved[24];
        };
        public HStringHeader _header;
        public GCHandle _gchandle;
        public IntPtr _handle;

        public bool Dispose()
        {
            _gchandle.Dispose();
            return false;
        }

        public static unsafe MarshalString CreateMarshaler(string value)
        {
            var m = new MarshalString();
            try
            {
                m._gchandle = GCHandle.Alloc(value, GCHandleType.Pinned);
                fixed (void* chars = value, header = &m._header, handle = &m._handle)
                {
                    Marshal.ThrowExceptionForHR(Platform.WindowsCreateStringReference(
                        (char*)chars, value.Length, (IntPtr*)header, (IntPtr*)handle));
                };
                return m;
            }
            catch (Exception) when (m.Dispose())
            {
                // Will never execute
                return default;
            }
        }

        public static IntPtr GetAbi(MarshalString m) => m._handle;

        public static IntPtr GetAbi(object box) => ((MarshalString)box)._handle;

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
            uint length;
            var buffer = Platform.WindowsGetStringRawBuffer(value, &length);
            return new string(buffer, 0, (int)length);
        }

        public static unsafe IntPtr FromManaged(string value)
        {
            IntPtr handle;
            Marshal.ThrowExceptionForHR(
                Platform.WindowsCreateString(value, value.Length, &handle));
            return handle;
        }

        public struct MarshalerArray
        {
            public bool Dispose()
            {
                foreach (var abi_string in _abi_strings)
                {
                    abi_string.Dispose();
                }
                Marshal.FreeCoTaskMem(_array);
                return false;
            }

            public IntPtr _array;
            public MarshalString[] _abi_strings;
        }

        public static unsafe MarshalerArray CreateMarshalerArray(string[] array)
        {
            var m = new MarshalerArray();
            try
            {
                var length = array.Length;
                m._array = Marshal.AllocCoTaskMem(length * Marshal.SizeOf<IntPtr>());
                var elements = (IntPtr*)m._array;
                m._abi_strings = new MarshalString[length];
                for (int i = 0; i < length; i++)
                {
                    m._abi_strings[i] = MarshalString.CreateMarshaler(array[i]);
                    elements[i] = MarshalString.GetAbi(m._abi_strings[i]);
                };
                return m;
            }
            catch (Exception) when (m.Dispose())
            {
                // Will never execute
                return default;
            }
        }

        public static (int length, IntPtr data) GetAbiArray(object box)
        {
            var m = (MarshalerArray)box;
            return (m._abi_strings.Length, m._array);
        }

        public static unsafe string[] FromAbiArray(object box)
        {
            var abi = ((int length, IntPtr data))box;
            string[] array = new string[abi.length];
            var data = (IntPtr*)abi.data.ToPointer();
            for (int i = 0; i < abi.length; i++)
            {
                array[i] = MarshalString.FromAbi(data[i]);
            }
            return array;
        }

        public static void DisposeMarshalerArray(object box)
        {
            if (box != null)
                ((MarshalerArray)box).Dispose();
        }

        public static unsafe void DisposeAbiArray(object box)
        {
            if (box != null)
            {
                var abi = ((int length, IntPtr data))box;
                var elements = (IntPtr*)abi.data;
                for (int i = 0; i < abi.length; i++)
                {
                    DisposeAbi(elements[i]);
                }
                Marshal.FreeCoTaskMem(abi.data);
            }
        }
    }

    struct MarshalBlittable<T>
    {
        public struct MarshalerArray
        {
            public MarshalerArray(Array array) => _gchandle = GCHandle.Alloc(array, GCHandleType.Pinned);
            public void Dispose() => _gchandle.Dispose();

            public GCHandle _gchandle;
        };

        public static MarshalerArray CreateMarshalerArray(Array array) => new MarshalerArray(array);

        public static (int length, IntPtr data) GetAbiArray(object box)
        {
            var m = (MarshalerArray)box;
            return (((Array)m._gchandle.Target).Length, m._gchandle.AddrOfPinnedObject());
        }

        public static unsafe T[] FromAbiArray(object box)
        {
            var abi = ((int length, IntPtr data))box;
            // TODO: consider System.Memory nuget package dependency here:
            //var abiSpan = new ReadOnlySpan<T>(abi.data.ToPointer(), abi.length);
            //return abiSpan.ToArray();
            var array = (T[])Array.CreateInstance(typeof(T), abi.length);
            var array_handle = GCHandle.Alloc(array, GCHandleType.Pinned);
            var array_data = array_handle.AddrOfPinnedObject();
            var byte_length = abi.length * Marshal.SizeOf<T>();
            Buffer.MemoryCopy(abi.data.ToPointer(), array_data.ToPointer(), byte_length, byte_length);
            array_handle.Free();
            return array;
        }

        public static void DisposeMarshalerArray(object box)
        {
            if (box != null)
                ((MarshalerArray)box).Dispose();
        }

        public static void DisposeAbiArray(object box)
        {
            if (box != null)
            {
                var abi = ((int length, IntPtr data))box;
                Marshal.FreeCoTaskMem(abi.data);
            }
        }
    }

    class MarshalGeneric<T>
    {
        protected static readonly Type HelperType = typeof(T).GetHelperType();
        protected static readonly Type AbiType = typeof(T).GetHelperType().GetMethod("GetAbi").ReturnType;
        protected static readonly Type MarshalerType = typeof(T).GetHelperType().GetMethod("CreateMarshaler").ReturnType;

        static MarshalGeneric()
        {
            CreateMarshaler = BindCreateMarshaler();
            GetAbi = BindGetAbi();
            FromAbi = BindFromAbi();
            DisposeMarshaler = BindDisposeMarshaler();
        }

        public static readonly Func<T, object> CreateMarshaler;
        private static Func<T, object> BindCreateMarshaler()
        {
            var parms = new[] { Expression.Parameter(typeof(T), "arg") };
            return Expression.Lambda<Func<T, object>>(
                Expression.Convert(Expression.Call(HelperType.GetMethod("CreateMarshaler"), parms),
                    typeof(object)), parms).Compile();
        }

        public static readonly Func<object, object> GetAbi;
        private static Func<object, object> BindGetAbi()
        {
            var parms = new[] { Expression.Parameter(typeof(object), "arg") };
            return Expression.Lambda<Func<object, object>>(
                Expression.Convert(Expression.Call(HelperType.GetMethod("GetAbi"),
                    new[] { Expression.Convert(parms[0], MarshalerType) }),
                        typeof(object)), parms).Compile();
        }

        public static readonly Func<object, T> FromAbi;
        private static Func<object, T> BindFromAbi()
        {
            var parms = new[] { Expression.Parameter(typeof(object), "arg") };
            return Expression.Lambda<Func<object, T>>(
                Expression.Call(HelperType.GetMethod("FromAbi"),
                    new[] { Expression.Convert(parms[0], AbiType) }), parms).Compile();
        }

        public static readonly Action<object> DisposeMarshaler;
        private static Action<object> BindDisposeMarshaler()
        {
            var parms = new[] { Expression.Parameter(typeof(object), "arg") };
            return Expression.Lambda<Action<object>>(
                Expression.Call(HelperType.GetMethod("DisposeMarshaler"),
                    new[] { Expression.Convert(parms[0], MarshalerType) }), parms).Compile();
        }
    }

    class MarshalNonBlittable<T> : MarshalGeneric<T>
    {
        public struct MarshalerArray
        {
            public bool Dispose()
            {
                _gchandle.Dispose();
                foreach (var abi_element in _abi_elements)
                {
                    Marshaler<T>.DisposeMarshaler(abi_element);
                }
                return false;
            }

            public GCHandle _gchandle;
            public object[] _abi_elements;
        }

        public static MarshalerArray CreateMarshalerArray(T[] array)
        {
            MarshalerArray m = new MarshalerArray();
            try
            {
                var length = array.Length;
                // TODO: consider Marshal.AllocCoTaskMem based on sizeof(HelperType)
                // and a Marshaler<T>.CopyTo to allow blitting into array directly
                // without boxing
                var abi_array = Array.CreateInstance(HelperType, length);
                m._abi_elements = new object[length];
                for (int i = 0; i < length; i++)
                {
                    m._abi_elements[i] = Marshaler<T>.CreateMarshaler(array[i]);
                    abi_array.SetValue(Marshaler<T>.GetAbi(m._abi_elements[i]), i);
                }
                m._gchandle = GCHandle.Alloc(abi_array, GCHandleType.Pinned);
                return m;
            }
            catch (Exception) when (m.Dispose())
            {
                // Will never execute
                return default;
            }
        }

        public static (int length, IntPtr data) GetAbiArray(object box)
        {
            var m = (MarshalerArray)box;
            return (((Array)m._gchandle.Target).Length, m._gchandle.AddrOfPinnedObject());
        }

        public static unsafe T[] FromAbiArray(object box)
        {
            var abi = ((int length, IntPtr data))box;
            var array = new T[abi.length];
            var data = (byte*)abi.data.ToPointer();
            var abi_element_size = Marshal.SizeOf(HelperType);
            for (int i = 0; i < abi.length; i++)
            {
                var abi_element = Marshal.PtrToStructure((IntPtr)data, HelperType);
                array[i] = Marshaler<T>.FromAbi(abi_element);
                data += abi_element_size;
            }
            return array;
        }

        public static void DisposeMarshalerArray(object box) => ((MarshalerArray)box).Dispose();

        public static unsafe void DisposeAbiArray(object box)
        {
            var abi = ((int length, IntPtr data))box;
            var data = (byte*)abi.data.ToPointer();
            var abi_element_size = Marshal.SizeOf(HelperType);
            for (int i = 0; i < abi.length; i++)
            {
                var abi_element = Marshal.PtrToStructure((IntPtr)data, HelperType);
                Marshaler<T>.DisposeAbi(abi_element);
                data += abi_element_size;
            }
            Marshal.FreeCoTaskMem(abi.data);
        }
    }

    struct MarshalInterface<T>
    {
        private static readonly Type HelperType = typeof(T).GetHelperType();
        private static Func<T, IntPtr> _ToAbi;
        private static Func<IntPtr, T> _FromAbi;

        public static T FromAbi(IntPtr ptr)
        {
            // TODO: Check if the value is a CCW and return the underlying object.
            if (_FromAbi == null)
            {
                _FromAbi = BindFromAbi();
            }
            return _FromAbi(ptr);
        }

        public static IntPtr CreateMarshaler(T value) => ToAbi(value);

        public static IntPtr GetAbi(object value) => (IntPtr)value;

        public static void DisposeAbi(IntPtr thisPtr) => IInspectable.DisposeAbi(thisPtr);

        public static void DisposeMarshaler(object value) { }

        public static IntPtr FromManaged(T value)
        {
            Type type = value.GetType();
            var objRef = GetObjectReference(value, type);
            if (objRef != null)
            {
                return objRef.GetRef();
            }
            throw new InvalidCastException($"Unable to get native interface of type '{typeof(T)}' for object of type '{type}'.");
        }

        public static IntPtr ToAbi(T value)
        {
            // If the value passed in is the native implementation of the interface
            // use the ToAbi delegate since it will be faster than reflection.
            if (value.GetType() == HelperType)
            {
                if (_ToAbi == null)
                {
                    _ToAbi = BindToAbi();
                }
                return _ToAbi(value);
            }

            Type type = value.GetType();
            var objRef = GetObjectReference(value, type);
            if (objRef != null)
            {
                return objRef.ThisPtr;
            }

            // The type is a class. We need to determine if it's an RCW or a managed class.
            Type interfaceTagType = type.GetNestedType("InterfaceTag`1", BindingFlags.NonPublic | BindingFlags.DeclaredOnly)?.MakeGenericType(typeof(T));
            // If the type declares a nested InterfaceTag<I> type, then it is a generated RCW.
            if (interfaceTagType != null)
            {
                Type interfaceType = typeof(T);
                T iface = default;

                for (Type currentRcwType = type; currentRcwType != typeof(object); currentRcwType = interfaceType.BaseType)
                {
                    interfaceTagType = type.GetNestedType("InterfaceTag`1", BindingFlags.NonPublic | BindingFlags.DeclaredOnly)?.MakeGenericType(typeof(T));
                    if (interfaceTagType == null)
                    {
                        throw new InvalidOperationException($"Found a non-RCW type '{currentRcwType}' that is a base class of a generated RCW type '{type}'.");
                    }

                    Type interfaceTag = interfaceTagType.MakeGenericType(interfaceType);
                    MethodInfo asInternalMethod = type.GetMethod("AsInternal", BindingFlags.NonPublic | BindingFlags.DeclaredOnly | BindingFlags.Instance, null, new[] { interfaceTag }, null);
                    if (asInternalMethod != null)
                    {
                        iface = (T)asInternalMethod.Invoke(value, new[] { Activator.CreateInstance(interfaceTag) });
                        break;
                    }
                }

                if (iface == null)
                {
                    throw new InvalidCastException($"Unable to get native interface of type '{interfaceType}' for object of type '{type}'.");
                }

                if (_ToAbi == null)
                {
                    _ToAbi = BindToAbi();
                }
                return _ToAbi(iface);
            }

            // TODO: Create a CCW for user-defined implementations of interfaces.
            throw new NotImplementedException("Generating a CCW for a user-defined class is not currently implemented");
        }

        private static IObjectReference GetObjectReference(T value, Type type)
        {
            MethodInfo asInterfaceMethod = type.GetMethod("AsInterface`1");
            // If the type has an AsInterface<A> method, then it is an interface.
            if (asInterfaceMethod != null)
            {
                var objRef = (IObjectReference)asInterfaceMethod.MakeGenericMethod(typeof(T)).Invoke(value, null);
                return objRef;
            }
            return null;
        }

        private static Func<IntPtr, T> BindFromAbi()
        {
            var fromAbiMethod = HelperType.GetMethod("FromAbi");
            var objReferenceConstructor = HelperType.GetConstructor(new[] { fromAbiMethod.ReturnType });
            var parms = new[] { Expression.Parameter(typeof(IntPtr), "arg") };
            return Expression.Lambda<Func<IntPtr, T>>(
                    Expression.New(objReferenceConstructor,
                        Expression.Call(fromAbiMethod, parms[0])), parms).Compile();
        }

        private static Func<T, IntPtr> BindToAbi()
        {
            var parms = new[] { Expression.Parameter(typeof(T), "arg") };
            return Expression.Lambda<Func<T, IntPtr>>(
                Expression.MakeMemberAccess(
                    Expression.Convert(parms[0], HelperType),
                    HelperType.GetProperty("ThisPtr")), parms).Compile();
        }
    }

    public class Marshaler<T>
    {
        static Marshaler()
        {
            Type type = typeof(T);

            // structs cannot contain arrays, and arrays may only ever appear as parameters
            if (type.IsArray)
            {
                throw new InvalidOperationException("Arrays may not be marshaled generically.");
            }

            if (type == typeof(bool))
            {
                AbiType = typeof(byte);
                CreateMarshaler = (T value) => value;
                GetAbi = (object box) => (bool)box ? 1 : 0;
                FromAbi = (object box) => (T)(object)((byte)box != 0);
                DisposeMarshaler = (object box) => { };
                DisposeAbi = (object box) => { };
                CreateMarshalerArray = (T[] array) => MarshalNonBlittable<T>.CreateMarshalerArray(array);
                GetAbiArray = (object box) => MarshalNonBlittable<T>.GetAbiArray(box);
                FromAbiArray = (object box) => MarshalNonBlittable<T>.FromAbiArray(box);
                DisposeMarshalerArray = (object box) => MarshalNonBlittable<T>.DisposeMarshalerArray(box);
                DisposeAbiArray = (object box) => MarshalNonBlittable<T>.DisposeAbiArray(box);
            }
            else if (type == typeof(String))
            {
                AbiType = typeof(IntPtr);
                CreateMarshaler = (T value) => MarshalString.CreateMarshaler((string)(object)value);
                GetAbi = (object box) => MarshalString.GetAbi(box);
                FromAbi = (object value) => (T)(object)MarshalString.FromAbi((IntPtr)value);
                DisposeMarshaler = (object box) => MarshalString.DisposeMarshaler(box);
                DisposeAbi = (object box) => MarshalString.DisposeAbi(box);
                CreateMarshalerArray = (T[] array) => MarshalString.CreateMarshalerArray((string[])(object)array);
                GetAbiArray = (object box) => MarshalString.GetAbiArray(box);
                FromAbiArray = (object box) => (T[])(object)MarshalString.FromAbiArray(box);
                DisposeMarshalerArray = (object box) => MarshalString.DisposeMarshalerArray(box);
                DisposeAbiArray = (object box) => MarshalString.DisposeAbiArray(box);
            }
            else if (type.IsValueType)
            {
                // If type is blittable just pass through
                AbiType = type.FindHelperType();
                if (AbiType != null)
                {
                    // Could still be blittable and the 'ABI.*' type exists for other reasons (e.g. it's a mapped type)
                    if (AbiType.GetMethod("FromAbi") == null)
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
                    DisposeMarshaler = (object box) => { };
                    DisposeAbi = (object box) => { };
                    CreateMarshalerArray = (T[] array) => MarshalBlittable<T>.CreateMarshalerArray(array);
                    GetAbiArray = (object box) => MarshalBlittable<T>.GetAbiArray(box);
                    FromAbiArray = (object box) => MarshalBlittable<T>.FromAbiArray(box);
                    DisposeMarshalerArray = (object box) => MarshalBlittable<T>.DisposeMarshalerArray(box);
                    DisposeAbiArray = (object box) => MarshalBlittable<T>.DisposeAbiArray(box);
                }
                else // bind to marshaler
                {
                    CreateMarshaler = MarshalNonBlittable<T>.CreateMarshaler;
                    GetAbi = MarshalNonBlittable<T>.GetAbi;
                    FromAbi = MarshalNonBlittable<T>.FromAbi;
                    DisposeMarshaler = MarshalNonBlittable<T>.DisposeMarshaler;
                    DisposeAbi = (object box) => { };
                    CreateMarshalerArray = (T[] array) => MarshalNonBlittable<T>.CreateMarshalerArray(array);
                    GetAbiArray = (object box) => MarshalNonBlittable<T>.GetAbiArray(box);
                    FromAbiArray = (object box) => MarshalNonBlittable<T>.FromAbiArray(box);
                    DisposeMarshalerArray = (object box) => MarshalNonBlittable<T>.DisposeMarshalerArray(box);
                    DisposeAbiArray = (object box) => MarshalNonBlittable<T>.DisposeAbiArray(box);
                }
            }
            else if (type.IsDelegate())
            {
                AbiType = typeof(IntPtr);
                CreateMarshaler = MarshalGeneric<T>.CreateMarshaler;
                GetAbi = MarshalGeneric<T>.GetAbi;
                FromAbi = MarshalGeneric<T>.FromAbi;
                DisposeMarshaler = MarshalGeneric<T>.DisposeMarshaler;
                DisposeAbi = (object box) => { };
            }
            else if (type.IsInterface)
            {
                AbiType = typeof(IntPtr);
                CreateMarshaler = (T value) => MarshalInterface<T>.CreateMarshaler(value);
                GetAbi = (object box) => MarshalInterface<T>.GetAbi((IntPtr)box);
                FromAbi = (object value) => (T)(object)MarshalInterface<T>.FromAbi((IntPtr)value);
                DisposeMarshaler = (object box) => { };
                DisposeAbi = (object box) => { };
            }
            else if (typeof(T) == typeof(WinRT.IInspectable))
            {
                AbiType = typeof(IntPtr);
                CreateMarshaler = (T abi) => abi;
                GetAbi = (object abi) => ((WinRT.IInspectable)abi).ThisPtr;
                FromAbi = (object abi) => (T)(object)IInspectable.FromAbi((IntPtr)abi);
                DisposeMarshaler = (object box) => { };
                DisposeAbi = (object box) => { };
            }
            else // class type
            {
                AbiType = typeof(IntPtr);
                var interfaceAbiType = type.GetField("_default", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)?.FieldType;
                if (interfaceAbiType is null)
                {
                    throw new ArgumentException($"Unable to marshal non-WinRT class: '{type.FullName}'");
                }
                CreateMarshaler = (T value) => value;
                GetAbi = BindClassGetAbi();
                FromAbi = BindClassFromAbi(interfaceAbiType);
                DisposeMarshaler = (object box) => { };
                DisposeAbi = (object box) => { };
            }
            RefAbiType = AbiType.MakeByRefType();
        }

        private static Func<object, T> BindClassFromAbi(Type AbiType)
        {
            var fromAbiMethod = AbiType.GetMethod("FromAbi");
            var objReferenceConstructor = AbiType.GetConstructor(new[] { fromAbiMethod.ReturnType });
            var parms = new[] { Expression.Parameter(typeof(object), "arg") };
            return Expression.Lambda<Func<object, T>>(
                Expression.New(
                    typeof(T).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.CreateInstance, null, new[] { AbiType }, null),
                    Expression.New(objReferenceConstructor,
                        Expression.Call(fromAbiMethod, Expression.Convert(parms[0], typeof(IntPtr))))), parms).Compile();
        }

        private static Func<object, object> BindClassGetAbi()
        {
            var thisPtrField = (Func<T, IntPtr>)typeof(T).GetProperty("ThisPtr").GetMethod.CreateDelegate(typeof(Func<T, IntPtr>));
            return (object value) => (object)thisPtrField((T)value);
        }

        public static readonly Type AbiType;
        public static readonly Type RefAbiType;
        public static readonly Func<T, object> CreateMarshaler;
        public static readonly Func<object, object> GetAbi;
        public static readonly Func<object, T> FromAbi;
        public static readonly Action<object> DisposeMarshaler;
        public static readonly Action<object> DisposeAbi;
        public static readonly Func<T[], object> CreateMarshalerArray;
        public static readonly Func<object, (int, IntPtr)> GetAbiArray;
        public static readonly Func<object, T[]> FromAbiArray;
        public static readonly Action<object> DisposeMarshalerArray;
        public static readonly Action<object> DisposeAbiArray;
    }
}
